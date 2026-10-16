using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Models.ViewModels;
using PortalDocente.Security;

namespace PortalDocente.Services;

public sealed class UsuarioAdministracionService : IUsuarioAdministracionService
{
    private const string ErrorCorreoDuplicado = "Ya existe una cuenta con ese correo.";
    private const string ErrorRolInvalido = "Selecciona uno de los roles permitidos.";
    private const string ErrorRolesNoConfigurados = "Falta configurar el rol seleccionado en la base de datos.";
    private const string ErrorDependencias = "No se puede eliminar esta cuenta porque tiene información académica relacionada.";
    private const string ErrorUltimoAdmin = "No se puede quitar la cuenta ni el rol del último administrador activo.";
    private const string ErrorAutoBorrado = "No puedes eliminar tu propia cuenta mientras tienes la sesión iniciada.";

    private readonly PortalDbContext _db;

    public UsuarioAdministracionService(PortalDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UsuarioAdministracionItemViewModel>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _db.Usuarios
            .AsNoTracking()
            .Include(usuario => usuario.Roles)
                .ThenInclude(usuarioRol => usuarioRol.Rol)
            .OrderBy(usuario => usuario.NombreCompleto)
            .ToListAsync(cancellationToken);

        return usuarios.Select(usuario => new UsuarioAdministracionItemViewModel
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Correo,
            Rol = string.Join(", ", usuario.Roles.Select(usuarioRol => EtiquetaRol(usuarioRol.Rol.Nombre))),
            Activo = usuario.Activo
        }).ToList();
    }

    public async Task<UsuarioAdministracionEditViewModel?> ObtenerAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(item => item.Docente)
            .Include(item => item.Estudiante)
            .Include(item => item.Roles)
                .ThenInclude(usuarioRol => usuarioRol.Rol)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (usuario == null)
            return null;

        return new UsuarioAdministracionEditViewModel
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Correo,
            Rol = usuario.Roles.Count == 1 ? usuario.Roles.Single().Rol.Nombre : string.Empty,
            Activo = usuario.Activo,
            CodigoInstitucional = usuario.Estudiante?.CodigoInstitucional,
            TituloAcademico = usuario.Docente?.TituloAcademico
        };
    }

    public async Task<string?> CrearAsync(
        UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelo.Password) || modelo.Password.Length < 12)
            return "La contraseña inicial debe tener al menos 12 caracteres.";

        var rol = await ObtenerRolAsync(modelo.Rol, cancellationToken);
        if (rol.Error != null)
            return rol.Error;

        var correo = modelo.Correo.Trim().ToLowerInvariant();
        if (await CorreoExisteAsync(correo, null, cancellationToken))
            return ErrorCorreoDuplicado;

        var ahora = DateTimeOffset.UtcNow;
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(modelo.Password),
            NombreCompleto = modelo.NombreCompleto.Trim(),
            Activo = modelo.Activo,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        usuario.Roles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = rol.Rol!.Id,
            AsignadoEn = ahora,
            Rol = rol.Rol
        });
        CrearPerfil(usuario, modelo, modelo.Rol, ahora);
        _db.Usuarios.Add(usuario);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCorreoDuplicado(exception))
        {
            return ErrorCorreoDuplicado;
        }

        return null;
    }

    public async Task<string?> ActualizarAsync(
        UsuarioAdministracionEditViewModel modelo, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(modelo.Password)
            && (string.IsNullOrWhiteSpace(modelo.Password) || modelo.Password.Length < 12))
        {
            return "La nueva contraseña debe tener al menos 12 caracteres.";
        }

        await using var transaccion = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var usuario = await _db.Usuarios
            .Include(item => item.Docente)
            .Include(item => item.Estudiante)
            .Include(item => item.Roles)
            .SingleOrDefaultAsync(item => item.Id == modelo.Id, cancellationToken);

        if (usuario == null)
            return "No se encontró la cuenta que quieres modificar.";

        var rolResult = await ObtenerRolAsync(modelo.Rol, cancellationToken);
        if (rolResult.Error != null)
            return rolResult.Error;

        var correo = modelo.Correo.Trim().ToLowerInvariant();
        if (await CorreoExisteAsync(correo, usuario.Id, cancellationToken))
            return ErrorCorreoDuplicado;

        var esAdminActual = usuario.Roles.Any(usuarioRol => usuarioRol.RolId == rolResult.Rol!.Id)
                            && modelo.Rol == ClaimsPrincipalExtensions.RolAdmin;
        var tieneRolAdmin = await _db.UsuariosRoles
            .AnyAsync(usuarioRol =>
                usuarioRol.UsuarioId == usuario.Id
                && usuarioRol.Rol.Nombre == ClaimsPrincipalExtensions.RolAdmin,
                cancellationToken);
        if (tieneRolAdmin
            && (!esAdminActual || !modelo.Activo)
            && !await ExisteOtroAdministradorActivoAsync(usuario.Id, cancellationToken))
        {
            return ErrorUltimoAdmin;
        }

        var errorPerfil = SincronizarPerfil(usuario, modelo);
        if (errorPerfil != null)
            return errorPerfil;

        usuario.Correo = correo;
        usuario.NombreCompleto = modelo.NombreCompleto.Trim();
        usuario.Activo = modelo.Activo;
        usuario.ActualizadoEn = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(modelo.Password))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(modelo.Password);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsCorreoDuplicado(exception))
        {
            return ErrorCorreoDuplicado;
        }

        _db.UsuariosRoles.RemoveRange(usuario.Roles);
        usuario.Roles.Clear();
        await _db.SaveChangesAsync(cancellationToken);

        _db.UsuariosRoles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = rolResult.Rol!.Id,
            AsignadoEn = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        if (transaccion != null)
            await transaccion.CommitAsync(cancellationToken);

        return null;
    }

    public async Task<string?> EliminarAsync(
        Guid id, Guid administradorId, CancellationToken cancellationToken = default)
    {
        if (id == administradorId)
            return ErrorAutoBorrado;

        await using var transaccion = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var usuario = await _db.Usuarios
            .Include(item => item.Docente)
            .Include(item => item.Estudiante)
            .Include(item => item.Roles)
                .ThenInclude(usuarioRol => usuarioRol.Rol)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (usuario == null)
            return "No se encontró la cuenta que quieres eliminar.";

        if (usuario.Roles.Any(usuarioRol => usuarioRol.Rol.Nombre == ClaimsPrincipalExtensions.RolAdmin)
            && !await ExisteOtroAdministradorActivoAsync(usuario.Id, cancellationToken))
        {
            return ErrorUltimoAdmin;
        }

        var errorDependencias = await ComprobarDependenciasAsync(usuario, cancellationToken);
        if (errorDependencias != null)
            return errorDependencias;

        if (usuario.Docente != null)
            _db.Docentes.Remove(usuario.Docente);
        if (usuario.Estudiante != null)
            _db.Estudiantes.Remove(usuario.Estudiante);
        _db.UsuariosRoles.RemoveRange(usuario.Roles);
        _db.Usuarios.Remove(usuario);

        await _db.SaveChangesAsync(cancellationToken);
        if (transaccion != null)
            await transaccion.CommitAsync(cancellationToken);

        return null;
    }

    private string? SincronizarPerfil(
        Usuario usuario, UsuarioAdministracionEditViewModel modelo)
    {
        var ahora = DateTimeOffset.UtcNow;
        if (modelo.Rol == ClaimsPrincipalExtensions.RolDocente)
        {
            if (usuario.Docente == null)
            {
                usuario.Docente = new Docente
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuario.Id,
                    CreadoEn = ahora
                };
                _db.Docentes.Add(usuario.Docente);
            }
            usuario.Docente.TituloAcademico = modelo.TituloAcademico?.Trim();
        }
        else if (modelo.Rol == ClaimsPrincipalExtensions.RolEstudiante)
        {
            if (usuario.Estudiante == null)
            {
                usuario.Estudiante = new Estudiante
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = usuario.Id,
                    CreadoEn = ahora
                };
                _db.Estudiantes.Add(usuario.Estudiante);
            }
            usuario.Estudiante.CodigoInstitucional = modelo.CodigoInstitucional?.Trim();
        }

        return null;
    }

    private async Task<string?> ComprobarDependenciasAsync(
        Usuario usuario, CancellationToken cancellationToken)
    {
        if (usuario.Docente != null
            && await _db.Cohortes.AnyAsync(cohorte => cohorte.DocenteId == usuario.Docente.Id, cancellationToken))
        {
            return ErrorDependencias;
        }

        if (usuario.Estudiante == null)
            return null;

        var tieneMatriculas = await _db.CursosEstudiantes
            .AnyAsync(matricula => matricula.EstudianteId == usuario.Estudiante.Id, cancellationToken);
        var tienePartidas = await _db.Partidas
            .IgnoreQueryFilters()
            .AnyAsync(partida => partida.EstudianteId == usuario.Estudiante.Id, cancellationToken);
        return tieneMatriculas || tienePartidas ? ErrorDependencias : null;
    }

    private async Task<bool> CorreoExisteAsync(
        string correo, Guid? excluirId, CancellationToken cancellationToken) =>
        await _db.Usuarios.AnyAsync(
            usuario => usuario.Correo.ToLower() == correo
                       && (!excluirId.HasValue || usuario.Id != excluirId.Value),
            cancellationToken);

    private async Task<bool> ExisteOtroAdministradorActivoAsync(
        Guid usuarioId, CancellationToken cancellationToken) =>
        await _db.UsuariosRoles.AnyAsync(
            usuarioRol => usuarioRol.Rol.Nombre == ClaimsPrincipalExtensions.RolAdmin
                          && usuarioRol.Usuario.Activo
                          && usuarioRol.UsuarioId != usuarioId,
            cancellationToken);

    private async Task<(Rol? Rol, string? Error)> ObtenerRolAsync(
        string nombre, CancellationToken cancellationToken)
    {
        if (!ClaimsPrincipalExtensions.EsRolPortalValido(nombre))
            return (null, ErrorRolInvalido);

        var rol = await _db.Roles.SingleOrDefaultAsync(item => item.Nombre == nombre, cancellationToken);
        return rol == null ? (null, ErrorRolesNoConfigurados) : (rol, null);
    }

    private static void CrearPerfil(
        Usuario usuario, UsuarioAdministracionEditViewModel modelo, string rol, DateTimeOffset ahora)
    {
        if (rol == ClaimsPrincipalExtensions.RolDocente)
        {
            usuario.Docente = new Docente
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                TituloAcademico = modelo.TituloAcademico?.Trim(),
                CreadoEn = ahora
            };
        }
        else if (rol == ClaimsPrincipalExtensions.RolEstudiante)
        {
            usuario.Estudiante = new Estudiante
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                CodigoInstitucional = modelo.CodigoInstitucional?.Trim(),
                CreadoEn = ahora
            };
        }
    }

    private static string EtiquetaRol(string rol) => rol switch
    {
        ClaimsPrincipalExtensions.RolAdmin => "Administrador",
        ClaimsPrincipalExtensions.RolDocente => "Docente",
        ClaimsPrincipalExtensions.RolEstudiante => "Estudiante",
        _ => rol
    };

    private static bool EsCorreoDuplicado(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
