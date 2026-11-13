using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Models.Api;
using PortalDocente.Services;
using Xunit;

namespace PortalDocente.Tests.Services;

public class ResultadoRegistroServiceTests
{
    [Fact]
    public async Task RegistrarAsync_GuardaPartidaYKpiRelacionados()
    {
        await using var db = CrearBaseDeDatos();
        var datos = await PrepararDatosAsync(db);
        var request = CrearSolicitud(datos.Estudiante.Id);

        var resultado = await CrearServicio(db).RegistrarAsync(datos.Usuario.Id, request, CancellationToken.None);

        Assert.Equal(RegistroResultadoEstado.Registrada, resultado.Estado);
        Assert.Equal(request.IdempotencyKey, resultado.Resultado!.PartidaId);
        var partida = await db.Partidas.Include(item => item.Kpi).SingleAsync();
        Assert.Equal(datos.Estudiante.Id, partida.EstudianteId);
        Assert.Equal(datos.Nivel.Id, partida.NivelId);
        Assert.Equal("unity", partida.OrigenRegistro);
        Assert.True(partida.Finalizada);
        Assert.Equal(91.5m, partida.Kpi!.PuntuacionGlobal);
        Assert.Equal(94m, partida.Kpi.PctEntregasATiempo);
        Assert.Equal(40800m, partida.Kpi.CostoTotal);
    }

    [Fact]
    public async Task RegistrarAsync_MismoIdempotencyKeyYContenidoNoDuplicaLaPartida()
    {
        await using var db = CrearBaseDeDatos();
        var datos = await PrepararDatosAsync(db);
        var servicio = CrearServicio(db);
        var request = CrearSolicitud(datos.Estudiante.Id);

        await servicio.RegistrarAsync(datos.Usuario.Id, request, CancellationToken.None);
        var repeticion = await servicio.RegistrarAsync(datos.Usuario.Id, request, CancellationToken.None);

        Assert.Equal(RegistroResultadoEstado.Repetida, repeticion.Estado);
        Assert.True(repeticion.Resultado!.Duplicada);
        Assert.Single(await db.Partidas.ToListAsync());
        Assert.Single(await db.Kpis.ToListAsync());
    }

    [Fact]
    public async Task RegistrarAsync_RechazaReutilizarClaveConOtroPuntaje()
    {
        await using var db = CrearBaseDeDatos();
        var datos = await PrepararDatosAsync(db);
        var servicio = CrearServicio(db);
        var request = CrearSolicitud(datos.Estudiante.Id);

        await servicio.RegistrarAsync(datos.Usuario.Id, request, CancellationToken.None);
        var repeticion = await servicio.RegistrarAsync(
            datos.Usuario.Id,
            request with { PuntajeGlobal = 80m },
            CancellationToken.None);

        Assert.Equal(RegistroResultadoEstado.ClaveIdempotenciaEnUso, repeticion.Estado);
        Assert.Single(await db.Partidas.ToListAsync());
    }

    [Fact]
    public async Task RegistrarAsync_NoAceptaEscenarioDeOtraCohorteDelEstudiante()
    {
        await using var db = CrearBaseDeDatos();
        var datos = await PrepararDatosAsync(db);
        var otroEscenario = new Escenario
        {
            Id = Guid.NewGuid(),
            CohorteId = Guid.NewGuid(),
            Nombre = "Escenario externo",
            Activo = true,
            CreadoEn = DateTimeOffset.UtcNow,
            ActualizadoEn = DateTimeOffset.UtcNow
        };
        db.Escenarios.Add(otroEscenario);
        db.Niveles.Add(new Nivel
        {
            Id = Guid.NewGuid(),
            EscenarioId = otroEscenario.Id,
            Numero = 1,
            Escenario = otroEscenario
        });
        await db.SaveChangesAsync();

        var resultado = await CrearServicio(db).RegistrarAsync(
            datos.Usuario.Id,
            CrearSolicitud(datos.Estudiante.Id) with { Escenario = otroEscenario.Nombre },
            CancellationToken.None);

        Assert.Equal(RegistroResultadoEstado.EscenarioNoDisponible, resultado.Estado);
        Assert.Empty(await db.Partidas.ToListAsync());
        Assert.Empty(await db.Kpis.ToListAsync());
    }

    private static ResultadoRegistroService CrearServicio(PortalDbContext db) =>
        new(db, NullLogger<ResultadoRegistroService>.Instance);

    private static async Task<(Usuario Usuario, Estudiante Estudiante, Nivel Nivel)> PrepararDatosAsync(
        PortalDbContext db)
    {
        var ahora = DateTimeOffset.UtcNow;
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Correo = "estudiante@example.test",
            PasswordHash = "hash-no-utilizado",
            NombreCompleto = "Estudiante de prueba",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        var estudiante = new Estudiante
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Usuario = usuario,
            CreadoEn = ahora
        };
        var curso = new Curso
        {
            Id = Guid.NewGuid(),
            Nombre = "Curso de prueba",
            PeriodoAcademico = "2026",
            CreadoEn = ahora
        };
        var cohorte = new Cohorte
        {
            Id = Guid.NewGuid(),
            CursoId = curso.Id,
            Curso = curso,
            DocenteId = Guid.NewGuid(),
            Nombre = "Cohorte activa",
            CreadoEn = ahora
        };
        var escenario = new Escenario
        {
            Id = Guid.NewGuid(),
            CohorteId = cohorte.Id,
            Cohorte = cohorte,
            Nombre = "Importación Marítima - Asia a GYE",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };
        var nivel = new Nivel
        {
            Id = Guid.NewGuid(),
            EscenarioId = escenario.Id,
            Escenario = escenario,
            Numero = 1
        };

        db.Usuarios.Add(usuario);
        db.Estudiantes.Add(estudiante);
        db.Cursos.Add(curso);
        db.Cohortes.Add(cohorte);
        db.CursosEstudiantes.Add(new CursoEstudiante
        {
            Id = Guid.NewGuid(),
            EstudianteId = estudiante.Id,
            Estudiante = estudiante,
            CohorteId = cohorte.Id,
            Cohorte = cohorte,
            Estado = "activo",
            MatriculadoEn = ahora
        });
        db.Escenarios.Add(escenario);
        db.Niveles.Add(nivel);
        await db.SaveChangesAsync();

        return (usuario, estudiante, nivel);
    }

    private static RegistrarResultadoRequest CrearSolicitud(Guid estudianteId) =>
        new()
        {
            IdEstudiante = estudianteId,
            Escenario = "Importación Marítima - Asia a GYE",
            Nivel = "Nivel 1",
            Fecha = DateTimeOffset.Parse("2026-11-13T20:00:00Z"),
            PuntajeGlobal = 91.5m,
            EntregasATiempo = 94m,
            UtilizacionRecursos = 90.5m,
            EficienciaRutas = 89m,
            CostoTotal = 40800m,
            IdempotencyKey = Guid.NewGuid()
        };

    private static PortalDbContext CrearBaseDeDatos()
    {
        var options = new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PortalDbContext(options);
    }
}
