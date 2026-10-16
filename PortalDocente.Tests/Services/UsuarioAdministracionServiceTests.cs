using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Models.ViewModels;
using PortalDocente.Security;
using PortalDocente.Services;
using Xunit;

namespace PortalDocente.Tests.Services;

public class UsuarioAdministracionServiceTests
{
    [Fact]
    public async Task CrearAsync_CreaUsuarioConHashYPerfilDocente()
    {
        await using var db = CrearBaseDeDatos();
        await AgregarRolesAsync(db);
        var service = new UsuarioAdministracionService(db);
        var modelo = CrearModelo(ClaimsPrincipalExtensions.RolDocente);

        var error = await service.CrearAsync(modelo);
        var usuario = await db.Usuarios
            .Include(item => item.Roles)
            .Include(item => item.Docente)
            .SingleAsync();

        Assert.Null(error);
        Assert.Equal(modelo.Correo.ToLowerInvariant(), usuario.Correo);
        Assert.NotEqual(modelo.Password, usuario.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(modelo.Password, usuario.PasswordHash));
        Assert.Single(usuario.Roles);
        Assert.NotNull(usuario.Docente);
    }

    [Fact]
    public async Task ActualizarAsync_CambiaRolYReemplazaElPerfilSinAsignarRolesMultiples()
    {
        await using var db = CrearBaseDeDatos();
        await AgregarRolesAsync(db);
        var service = new UsuarioAdministracionService(db);
        var modelo = CrearModelo(ClaimsPrincipalExtensions.RolDocente);
        Assert.Null(await service.CrearAsync(modelo));
        db.ChangeTracker.Clear();
        Assert.Single(await db.UsuariosRoles.ToListAsync());
        Assert.Single(await db.Docentes.ToListAsync());
        var usuario = await db.Usuarios.SingleAsync();
        var edicion = await service.ObtenerAsync(usuario.Id);
        Assert.NotNull(edicion);
        edicion!.Rol = ClaimsPrincipalExtensions.RolEstudiante;
        edicion.CodigoInstitucional = "EST-001";

        var error = await service.ActualizarAsync(edicion);
        var actualizado = await db.Usuarios
            .Include(item => item.Roles)
                .ThenInclude(usuarioRol => usuarioRol.Rol)
            .Include(item => item.Docente)
            .Include(item => item.Estudiante)
            .SingleAsync();

        Assert.Null(error);
        Assert.Single(actualizado.Roles);
        Assert.Equal(ClaimsPrincipalExtensions.RolEstudiante, actualizado.Roles.Single().Rol.Nombre);
        Assert.NotNull(actualizado.Docente);
        Assert.Equal("EST-001", actualizado.Estudiante?.CodigoInstitucional);
        Assert.Null(edicion.Password);
    }

    [Fact]
    public async Task EliminarAsync_BloqueaEliminacionDeEstudianteConMatricula()
    {
        await using var db = CrearBaseDeDatos();
        await AgregarRolesAsync(db);
        await ProgresoConsultaTestData.SeedAsync(db, 1);
        var admin = CrearCuentaConRol("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        var estudiante = await db.Estudiantes.SingleAsync();

        var error = await new UsuarioAdministracionService(db)
            .EliminarAsync(estudiante.UsuarioId, admin.Id);

        Assert.Contains("información académica relacionada", error);
        Assert.True(await db.Usuarios.AnyAsync(usuario => usuario.Id == estudiante.UsuarioId));
    }

    [Fact]
    public async Task EliminarAsync_ImpideEliminarLaPropiaCuenta()
    {
        await using var db = CrearBaseDeDatos();
        var admin = CrearCuentaConRol("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var error = await new UsuarioAdministracionService(db).EliminarAsync(admin.Id, admin.Id);

        Assert.Contains("propia cuenta", error);
        Assert.True(await db.Usuarios.AnyAsync(usuario => usuario.Id == admin.Id));
    }

    [Fact]
    public async Task EliminarAsync_ImpideQuitarElUltimoAdministrador()
    {
        await using var db = CrearBaseDeDatos();
        var admin = CrearCuentaConRol("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var error = await new UsuarioAdministracionService(db)
            .EliminarAsync(admin.Id, Guid.NewGuid());

        Assert.Contains("último administrador activo", error);
        Assert.True(await db.Usuarios.AnyAsync(usuario => usuario.Id == admin.Id));
    }

    [Fact]
    public async Task ActualizarAsync_ImpideDesactivarElUltimoAdministrador()
    {
        await using var db = CrearBaseDeDatos();
        var admin = CrearCuentaConRol("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new UsuarioAdministracionService(db);
        var modelo = await service.ObtenerAsync(admin.Id);
        Assert.NotNull(modelo);
        modelo!.Activo = false;

        var error = await service.ActualizarAsync(modelo);

        Assert.Contains("último administrador activo", error);
        Assert.True((await db.Usuarios.SingleAsync(usuario => usuario.Id == admin.Id)).Activo);
    }

    [Fact]
    public async Task EliminarAsync_EliminaUsuarioSinDependencias()
    {
        await using var db = CrearBaseDeDatos();
        var admin = CrearCuentaConRol("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        var usuario = CrearCuentaConRol("temporal@example.test", ClaimsPrincipalExtensions.RolEstudiante);
        usuario.Estudiante = new Estudiante
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            CreadoEn = DateTimeOffset.UtcNow
        };
        db.Usuarios.AddRange(admin, usuario);
        await db.SaveChangesAsync();

        var error = await new UsuarioAdministracionService(db).EliminarAsync(usuario.Id, admin.Id);

        Assert.Null(error);
        Assert.False(await db.Usuarios.AnyAsync(item => item.Id == usuario.Id));
        Assert.Empty(await db.Estudiantes.ToListAsync());
    }

    private static PortalDbContext CrearBaseDeDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PortalDbContext(options);
    }

    private static UsuarioAdministracionEditViewModel CrearModelo(string rol) => new()
    {
        NombreCompleto = "Cuenta de prueba",
        Correo = $"cuenta-{Guid.NewGuid():N}@example.test",
        Rol = rol,
        Password = "Clave-inicial-segura-123!",
        Activo = true
    };

    private static Usuario CrearCuentaConRol(string correo, string nombreRol)
    {
        var ahora = DateTimeOffset.UtcNow;
        var rol = new Rol
        {
            Id = Guid.NewGuid(),
            Nombre = nombreRol,
            CreadoEn = ahora
        };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password-de-prueba-123!"),
            NombreCompleto = correo,
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        usuario.Roles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = rol.Id,
            Rol = rol,
            AsignadoEn = ahora
        });
        return usuario;
    }

    private static async Task AgregarRolesAsync(PortalDbContext db)
    {
        var ahora = DateTimeOffset.UtcNow;
        db.Roles.AddRange(
            new Rol { Id = Guid.NewGuid(), Nombre = ClaimsPrincipalExtensions.RolAdmin, CreadoEn = ahora },
            new Rol { Id = Guid.NewGuid(), Nombre = ClaimsPrincipalExtensions.RolDocente, CreadoEn = ahora },
            new Rol { Id = Guid.NewGuid(), Nombre = ClaimsPrincipalExtensions.RolEstudiante, CreadoEn = ahora });
        await db.SaveChangesAsync();
    }
}
