using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Security;
using PortalDocente.Services;
using Xunit;

namespace PortalDocente.Tests.Services;

public class UsuarioAutenticacionServiceTests
{
    [Fact]
    public async Task AutenticarAsync_AceptaAdministradorConRolUnico()
    {
        await using var db = CrearBaseDeDatos();
        var (usuario, password) = CrearUsuario("admin@example.test", ClaimsPrincipalExtensions.RolAdmin);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var resultado = await new UsuarioAutenticacionService(db).AutenticarAsync(usuario.Correo, password);

        Assert.NotNull(resultado.Usuario);
        Assert.Equal(usuario.Id, resultado.Usuario.Id);
        Assert.Equal(ClaimsPrincipalExtensions.RolAdmin, resultado.Usuario.Rol);
        Assert.Null(resultado.Usuario.DocenteId);
        Assert.Null(resultado.Error);
    }

    [Fact]
    public async Task AutenticarAsync_AceptaDocenteConPerfilYRolUnico()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 0);

        var resultado = await new UsuarioAutenticacionService(db).AutenticarAsync(
            ProgresoConsultaTestData.DocenteCorreo,
            ProgresoConsultaTestData.DocentePassword);

        Assert.NotNull(resultado.Usuario);
        Assert.Equal(ClaimsPrincipalExtensions.RolDocente, resultado.Usuario.Rol);
        Assert.Equal(ProgresoConsultaTestData.DocenteId, resultado.Usuario.DocenteId);
    }

    [Fact]
    public async Task AutenticarAsync_RechazaEstudianteConMensajeExplicito()
    {
        await using var db = CrearBaseDeDatos();
        var (usuario, password) = CrearUsuario("estudiante@example.test", ClaimsPrincipalExtensions.RolEstudiante);
        usuario.Estudiante = new Estudiante
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            CreadoEn = DateTimeOffset.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var resultado = await new UsuarioAutenticacionService(db).AutenticarAsync(usuario.Correo, password);

        Assert.Null(resultado.Usuario);
        Assert.Equal("Los estudiantes no tienen acceso al Portal Docente.", resultado.Error);
    }

    [Fact]
    public async Task AutenticarAsync_RechazaCuentaConVariosRoles()
    {
        await using var db = CrearBaseDeDatos();
        var (usuario, password) = CrearUsuario("multirol@example.test", ClaimsPrincipalExtensions.RolAdmin);
        var rolDocente = new Rol
        {
            Id = Guid.NewGuid(),
            Nombre = ClaimsPrincipalExtensions.RolDocente,
            CreadoEn = DateTimeOffset.UtcNow
        };
        usuario.Roles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = rolDocente.Id,
            Rol = rolDocente,
            AsignadoEn = DateTimeOffset.UtcNow
        });
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var resultado = await new UsuarioAutenticacionService(db).AutenticarAsync(usuario.Correo, password);

        Assert.Null(resultado.Usuario);
        Assert.Equal("Credenciales incorrectas.", resultado.Error);
    }

    private static PortalDbContext CrearBaseDeDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PortalDbContext(options);
    }

    private static (Usuario Usuario, string Password) CrearUsuario(string correo, string rol)
    {
        const string password = "Clave-segura-de-prueba-123!";
        var ahora = DateTimeOffset.UtcNow;
        var entidadRol = new Rol
        {
            Id = Guid.NewGuid(),
            Nombre = rol,
            CreadoEn = ahora
        };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            NombreCompleto = "Usuario de prueba",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        usuario.Roles.Add(new UsuarioRol
        {
            UsuarioId = usuario.Id,
            RolId = entidadRol.Id,
            Rol = entidadRol,
            AsignadoEn = ahora
        });

        return (usuario, password);
    }
}
