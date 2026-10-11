using Microsoft.EntityFrameworkCore;
using PortalDocente.Data.Entities;

namespace PortalDocente.Data;

/// <summary>
/// Datos de demostración claramente identificados como prueba.
/// Solo se usa cuando Demo:UseInMemoryData = true.
/// </summary>
public static class DemoDataSeeder
{
    public static readonly Guid DocenteUsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DocenteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid EstudianteConProgresoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid EstudianteSinProgresoId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public const string DemoDocenteCorreo = "docente.demo@udla.edu.ec";
    public const string DemoDocentePassword = "DemoPortal2026!";

    public static async Task SeedAsync(PortalDbContext db)
    {
        if (await db.Usuarios.AnyAsync())
            return;

        var ahora = DateTimeOffset.UtcNow;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoDocentePassword);

        var docenteUsuario = new Usuario
        {
            Id = DocenteUsuarioId,
            Correo = DemoDocenteCorreo,
            PasswordHash = passwordHash,
            NombreCompleto = "Docente Demo (prueba)",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        var docente = new Docente
        {
            Id = DocenteId,
            UsuarioId = DocenteUsuarioId,
            TituloAcademico = "Mgtr. Logística (demo)",
            CreadoEn = ahora
        };

        var est1Usuario = new Usuario
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Correo = "ana.estudiante.demo@udla.edu.ec",
            PasswordHash = passwordHash,
            NombreCompleto = "Ana Pérez Demo",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        var est2Usuario = new Usuario
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Correo = "luis.estudiante.demo@udla.edu.ec",
            PasswordHash = passwordHash,
            NombreCompleto = "Luis Gómez Demo",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        var estudiante1 = new Estudiante
        {
            Id = EstudianteConProgresoId,
            UsuarioId = est1Usuario.Id,
            CodigoInstitucional = "DEMO-EST-001",
            CreadoEn = ahora
        };

        var estudiante2 = new Estudiante
        {
            Id = EstudianteSinProgresoId,
            UsuarioId = est2Usuario.Id,
            CodigoInstitucional = "DEMO-EST-002",
            CreadoEn = ahora
        };

        var curso = new Curso
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            Nombre = "Logística, Transporte y Distribución (demo)",
            PeriodoAcademico = "2026-1",
            CreadoEn = ahora
        };

        var cohorte = new Cohorte
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            CursoId = curso.Id,
            DocenteId = docente.Id,
            Nombre = "Cohorte A (demo)",
            CreadoEn = ahora
        };

        var matricula1 = new CursoEstudiante
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999991"),
            EstudianteId = estudiante1.Id,
            CohorteId = cohorte.Id,
            Estado = "activo",
            MatriculadoEn = ahora.AddDays(-30)
        };

        var matricula2 = new CursoEstudiante
        {
            Id = Guid.Parse("99999999-9999-9999-9999-999999999992"),
            EstudianteId = estudiante2.Id,
            CohorteId = cohorte.Id,
            Estado = "activo",
            MatriculadoEn = ahora.AddDays(-28)
        };

        var escenario = new Escenario
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CohorteId = cohorte.Id,
            Nombre = "Rutas nacionales Ecuador (demo)",
            Dificultad = "intermedio",
            Activo = true,
            CreadoEn = ahora,
            ActualizadoEn = ahora
        };

        var nivel1 = new Nivel
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
            EscenarioId = escenario.Id,
            Numero = 1,
            PresupuestoInicial = 5000m
        };

        var nivel2 = new Nivel
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"),
            EscenarioId = escenario.Id,
            Numero = 2,
            PresupuestoInicial = 4500m
        };

        var partida1Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var partida2Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
        var partida3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3");

        var partidas = new[]
        {
            new Partida
            {
                Id = partida1Id,
                EstudianteId = estudiante1.Id,
                NivelId = nivel1.Id,
                IdempotencyKey = "demo-partida-1",
                OrigenRegistro = "local",
                EstadoSincronizacion = "sincronizado",
                FechaCreacion = ahora.AddDays(-10),
                FechaRecepcion = ahora.AddDays(-10),
                Finalizada = true
            },
            new Partida
            {
                Id = partida2Id,
                EstudianteId = estudiante1.Id,
                NivelId = nivel1.Id,
                IdempotencyKey = "demo-partida-2",
                OrigenRegistro = "local",
                EstadoSincronizacion = "sincronizado",
                FechaCreacion = ahora.AddDays(-5),
                FechaRecepcion = ahora.AddDays(-5),
                Finalizada = true
            },
            new Partida
            {
                Id = partida3Id,
                EstudianteId = estudiante1.Id,
                NivelId = nivel2.Id,
                IdempotencyKey = "demo-partida-3",
                OrigenRegistro = "local",
                EstadoSincronizacion = "sincronizado",
                FechaCreacion = ahora.AddDays(-1),
                FechaRecepcion = ahora.AddDays(-1),
                Finalizada = true
            }
        };

        var kpis = new[]
        {
            new Kpi
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
                PartidaId = partida1Id,
                CostoTotal = 3200.50m,
                PctEntregasATiempo = 70.00m,
                UtilizacionRecursos = 64.00m,
                EficienciaRutas = 68.00m,
                PuntuacionGlobal = 58.48m,
                FechaCreacion = ahora.AddDays(-10)
            },
            new Kpi
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd2"),
                PartidaId = partida2Id,
                CostoTotal = 2800.00m,
                PctEntregasATiempo = 85.00m,
                UtilizacionRecursos = 72.00m,
                EficienciaRutas = 80.00m,
                PuntuacionGlobal = 72.52m,
                FechaCreacion = ahora.AddDays(-5)
            },
            new Kpi
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd3"),
                PartidaId = partida3Id,
                CostoTotal = 2500.00m,
                PctEntregasATiempo = 90.00m,
                UtilizacionRecursos = 78.00m,
                EficienciaRutas = 88.00m,
                PuntuacionGlobal = 79.15m,
                FechaCreacion = ahora.AddDays(-1)
            }
        };

        db.Usuarios.AddRange(docenteUsuario, est1Usuario, est2Usuario);
        db.Docentes.Add(docente);
        db.Estudiantes.AddRange(estudiante1, estudiante2);
        db.Cursos.Add(curso);
        db.Cohortes.Add(cohorte);
        db.CursosEstudiantes.AddRange(matricula1, matricula2);
        db.Escenarios.Add(escenario);
        db.Niveles.AddRange(nivel1, nivel2);
        db.Partidas.AddRange(partidas);
        db.Kpis.AddRange(kpis);

        await db.SaveChangesAsync();
    }
}
