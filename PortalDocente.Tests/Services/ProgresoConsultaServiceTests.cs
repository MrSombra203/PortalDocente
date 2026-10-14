using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Services;
using Xunit;

namespace PortalDocente.Tests.Services;

public class ProgresoConsultaServiceTests
{
    [Fact]
    public async Task ListarEstudiantesAsync_PaginaLosResultadosYConservaElTotal()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 27);

        var service = new ProgresoConsultaService(db);
        var primeraPagina = await service.ListarEstudiantesAsync(ProgresoConsultaTestData.DocenteId, null);
        var segundaPagina = await service.ListarEstudiantesAsync(ProgresoConsultaTestData.DocenteId, null, 2);

        Assert.Equal(27, primeraPagina.TotalEstudiantes);
        Assert.Equal(2, primeraPagina.TotalPaginas);
        Assert.Equal(25, primeraPagina.Estudiantes.Count);
        Assert.Equal(2, segundaPagina.Estudiantes.Count);
        Assert.Equal(2, segundaPagina.PaginaActual);
        Assert.Empty(primeraPagina.Estudiantes.Select(x => x.Id).Intersect(segundaPagina.Estudiantes.Select(x => x.Id)));
    }

    [Fact]
    public async Task ListarEstudiantesAsync_AjustaUnaPaginaFueraDeRango()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 2);

        var resultado = await new ProgresoConsultaService(db)
            .ListarEstudiantesAsync(ProgresoConsultaTestData.DocenteId, null, 0);

        Assert.Equal(1, resultado.PaginaActual);
        Assert.Equal(2, resultado.TotalEstudiantes);
        Assert.Equal(2, resultado.Estudiantes.Count);
    }

    [Fact]
    public async Task ValidarDocenteAsync_AceptaUsuarioConCredencialesYRolEnLaBase()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 0);

        var resultado = await new ProgresoConsultaService(db).ValidarDocenteAsync(
            ProgresoConsultaTestData.DocenteCorreo,
            ProgresoConsultaTestData.DocentePassword);

        Assert.True(resultado.ok);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), resultado.usuarioId);
        Assert.Equal(ProgresoConsultaTestData.DocenteId, resultado.docenteId);
        Assert.Equal(ProgresoConsultaTestData.DocenteCorreo, resultado.correo);
        Assert.Equal("docente", resultado.rol);
        Assert.Null(resultado.error);
    }

    [Fact]
    public async Task ValidarDocenteAsync_RechazaDocenteSinRolAsignado()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 0);
        db.UsuariosRoles.Remove(await db.UsuariosRoles.SingleAsync());
        await db.SaveChangesAsync();

        var resultado = await new ProgresoConsultaService(db).ValidarDocenteAsync(
            ProgresoConsultaTestData.DocenteCorreo,
            ProgresoConsultaTestData.DocentePassword);

        Assert.False(resultado.ok);
        Assert.Null(resultado.usuarioId);
        Assert.Null(resultado.docenteId);
        Assert.Null(resultado.correo);
        Assert.Null(resultado.rol);
        Assert.Equal("Credenciales incorrectas o la cuenta no es de docente.", resultado.error);
    }

    [Fact]
    public async Task ValidarDocenteAsync_RechazaUnaContraseñaIncorrecta()
    {
        await using var db = CrearBaseDeDatos();
        await ProgresoConsultaTestData.SeedAsync(db, 0);

        var resultado = await new ProgresoConsultaService(db).ValidarDocenteAsync(
            ProgresoConsultaTestData.DocenteCorreo,
            "Contraseña incorrecta de prueba");

        Assert.False(resultado.ok);
        Assert.Null(resultado.usuarioId);
        Assert.Null(resultado.docenteId);
        Assert.Null(resultado.correo);
        Assert.Null(resultado.rol);
        Assert.Equal("Credenciales incorrectas o la cuenta no es de docente.", resultado.error);
    }

    private static PortalDbContext CrearBaseDeDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PortalDbContext(options);
    }
}

internal static class ProgresoConsultaTestData
{
    public static readonly Guid DocenteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public const string DocenteCorreo = "docente.pruebas@example.test";
    public const string DocentePassword = "Solo-pruebas-Portal-123!";

    public static async Task SeedAsync(PortalDbContext db, int cantidadEstudiantes)
    {
        var ahora = DateTimeOffset.UtcNow;
        var usuarioDocente = new Usuario
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Correo = DocenteCorreo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DocentePassword),
            NombreCompleto = "Docente de prueba",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        var docente = new Docente
        {
            Id = DocenteId,
            UsuarioId = usuarioDocente.Id,
            CreadoEn = ahora
        };
        var rolDocente = new Rol
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Nombre = "docente",
            Descripcion = "Rol para pruebas automatizadas",
            CreadoEn = ahora
        };
        var asignacionRol = new UsuarioRol
        {
            UsuarioId = usuarioDocente.Id,
            RolId = rolDocente.Id,
            AsignadoEn = ahora
        };
        var curso = new Curso
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Nombre = "Curso de pruebas",
            PeriodoAcademico = "periodo-test",
            CreadoEn = ahora
        };
        var cohorte = new Cohorte
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CursoId = curso.Id,
            DocenteId = docente.Id,
            Nombre = "Cohorte de pruebas",
            CreadoEn = ahora
        };

        db.Usuarios.Add(usuarioDocente);
        db.Docentes.Add(docente);
        db.Roles.Add(rolDocente);
        db.UsuariosRoles.Add(asignacionRol);
        db.Cursos.Add(curso);
        db.Cohortes.Add(cohorte);

        for (var i = 1; i <= cantidadEstudiantes; i++)
        {
            var usuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Correo = $"estudiante{i}@example.test",
                PasswordHash = "hash de prueba no utilizado para autenticación",
                NombreCompleto = $"Estudiante {i:D2}",
                Activo = true,
                CreadoEn = ahora,
                ActualizadoEn = ahora
            };
            var estudiante = new Estudiante
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuario.Id,
                CodigoInstitucional = $"TEST-{i:D3}",
                CreadoEn = ahora
            };

            db.Usuarios.Add(usuario);
            db.Estudiantes.Add(estudiante);
            db.CursosEstudiantes.Add(new CursoEstudiante
            {
                Id = Guid.NewGuid(),
                EstudianteId = estudiante.Id,
                CohorteId = cohorte.Id,
                Estado = "activo",
                MatriculadoEn = ahora
            });
        }

        await db.SaveChangesAsync();
    }
}
