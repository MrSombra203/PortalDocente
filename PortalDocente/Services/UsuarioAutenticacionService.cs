using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Security;

namespace PortalDocente.Services;

public sealed class UsuarioAutenticacionService : IUsuarioAutenticacionService
{
    private const string ErrorCredenciales = "Credenciales incorrectas.";
    private const string ErrorEstudiante = "Los estudiantes no tienen acceso al Portal Docente.";
    private readonly PortalDbContext _db;

    public UsuarioAutenticacionService(PortalDbContext db)
    {
        _db = db;
    }

    public async Task<(UsuarioAutenticado? Usuario, string? Error)> AutenticarAsync(
        string correo, string password, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarUsuarioAsync(correo, cancellationToken);
        if (usuario == null || !CredencialesValidas(usuario, password))
            return (null, ErrorCredenciales);

        var rol = usuario.Roles.Single().Rol.Nombre;
        if (!EsCuentaPortalValida(usuario, rol))
        {
            return (null, ErrorCredenciales);
        }

        if (rol == ClaimsPrincipalExtensions.RolEstudiante)
            return (null, ErrorEstudiante);

        return (new UsuarioAutenticado(
            usuario.Id,
            usuario.Correo,
            usuario.NombreCompleto,
            rol,
            usuario.Docente?.Id), null);
    }

    public async Task<(UsuarioEstudianteAutenticado? Usuario, string? Error)> AutenticarEstudianteAsync(
        string correo, string password, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarUsuarioAsync(correo, cancellationToken);
        if (usuario == null || !CredencialesValidas(usuario, password))
            return (null, ErrorCredenciales);

        if (usuario.Roles.Single().Rol.Nombre != ClaimsPrincipalExtensions.RolEstudiante
            || usuario.Estudiante == null)
        {
            return (null, ErrorCredenciales);
        }

        return (new UsuarioEstudianteAutenticado(
            usuario.Id,
            usuario.Estudiante.Id,
            usuario.Correo,
            usuario.NombreCompleto), null);
    }

    private async Task<PortalDocente.Data.Entities.Usuario?> BuscarUsuarioAsync(
        string correo, CancellationToken cancellationToken)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        return await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Docente)
            .Include(u => u.Estudiante)
            .Include(u => u.Roles)
                .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(
                u => u.Correo.ToLower() == correoNormalizado && u.Activo,
                cancellationToken);
    }

    private static bool CredencialesValidas(PortalDocente.Data.Entities.Usuario? usuario, string password)
    {
        if (usuario == null || usuario.Roles.Count != 1)
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static bool EsCuentaPortalValida(PortalDocente.Data.Entities.Usuario usuario, string rol) =>
        ClaimsPrincipalExtensions.EsRolPortalValido(rol)
        && (rol != ClaimsPrincipalExtensions.RolDocente || usuario.Docente != null)
        && (rol != ClaimsPrincipalExtensions.RolEstudiante || usuario.Estudiante != null);
}
