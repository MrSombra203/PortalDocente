using Microsoft.EntityFrameworkCore;
using PortalDocente.Data;
using PortalDocente.Models.ViewModels;

namespace PortalDocente.Services;

public class ProgresoConsultaService : IProgresoConsultaService
{
    private readonly PortalDbContext _db;

    public ProgresoConsultaService(PortalDbContext db)
    {
        _db = db;
    }

    private static IQueryable<Guid> EstudiantesDelDocente(PortalDbContext db, Guid docenteId) =>
        db.Estudiantes
            .Where(e => e.Matriculas.Any(m => m.Cohorte.DocenteId == docenteId && m.Estado == "activo"))
            .Select(e => e.Id);

    public async Task<DashboardViewModel> ObtenerDashboardAsync(Guid docenteId, CancellationToken cancellationToken = default)
    {
        var estudianteIds = EstudiantesDelDocente(_db, docenteId);

        var totalEstudiantes = await estudianteIds.CountAsync(cancellationToken);
        var conProgreso = await _db.Partidas
            .Where(p => estudianteIds.Contains(p.EstudianteId))
            .Select(p => p.EstudianteId)
            .Distinct()
            .CountAsync(cancellationToken);
        var totalPartidas = await _db.Partidas
            .Where(p => estudianteIds.Contains(p.EstudianteId))
            .CountAsync(cancellationToken);
        var partidasFinalizadas = await _db.Partidas
            .Where(p => estudianteIds.Contains(p.EstudianteId) && p.Finalizada)
            .CountAsync(cancellationToken);

        var partidasRecientes = await _db.Partidas
            .AsNoTracking()
            .Where(p => estudianteIds.Contains(p.EstudianteId))
            .Include(p => p.Kpi)
            .Include(p => p.Estudiante).ThenInclude(e => e.Usuario)
            .Include(p => p.Nivel).ThenInclude(n => n.Escenario)
            .OrderByDescending(p => p.FechaCreacion)
            .Take(30)
            .ToListAsync(cancellationToken);

        var idsDestacados = partidasRecientes
            .Select(p => p.EstudianteId)
            .Distinct()
            .ToList();
        var partidasPorEstudiante = await _db.Partidas
            .Where(p => idsDestacados.Contains(p.EstudianteId))
            .GroupBy(p => p.EstudianteId)
            .Select(g => new { EstudianteId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(g => g.EstudianteId, g => g.Total, cancellationToken);

        var destacados = partidasRecientes
            .GroupBy(p => p.EstudianteId)
            .Select(g =>
            {
                var ultima = g.OrderByDescending(p => p.FechaCreacion).First();
                var nombre = ultima.Estudiante.Usuario.NombreCompleto;
                return new DashboardEstudianteDestacadoViewModel
                {
                    Id = g.Key,
                    Codigo = ultima.Estudiante.CodigoInstitucional ?? g.Key.ToString()[..8],
                    NombreCompleto = nombre,
                    Puntuacion = ultima.Kpi?.PuntuacionGlobal,
                    Partidas = partidasPorEstudiante.GetValueOrDefault(g.Key),
                    Iniciales = ObtenerIniciales(nombre)
                };
            })
            .OrderByDescending(e => e.Puntuacion ?? 0)
            .Take(5)
            .ToList();

        var actividad = partidasRecientes.Take(5).Select(p => new DashboardActividadViewModel
        {
            Titulo = p.Estudiante.Usuario.NombreCompleto,
            Detalle = $"Nivel {p.Nivel.Numero} · {p.Nivel.Escenario.Nombre}" +
                      (p.Kpi != null ? $" · {p.Kpi.PuntuacionGlobal:0.#} pts" : string.Empty),
            Fecha = p.FechaCreacion,
            Icono = p.Finalizada ? "trophy" : "play"
        }).ToList();

        var evolucion = partidasRecientes
            .Where(p => p.Kpi != null)
            .GroupBy(p => p.FechaCreacion.Date)
            .OrderBy(g => g.Key)
            .Select(g => new EvolucionPuntoViewModel
            {
                Fecha = new DateTimeOffset(g.Key, TimeSpan.Zero),
                Puntuacion = Math.Round(g.Average(p => p.Kpi!.PuntuacionGlobal), 2),
                Etiqueta = g.Key.ToString("dd/MM")
            })
            .TakeLast(7)
            .ToList();
        var puntuacionesRecientes = partidasRecientes
            .Where(p => p.Kpi != null)
            .Select(p => p.Kpi!.PuntuacionGlobal)
            .ToList();

        return new DashboardViewModel
        {
            TotalEstudiantes = totalEstudiantes,
            EstudiantesConProgreso = conProgreso,
            EstudiantesSinProgreso = Math.Max(0, totalEstudiantes - conProgreso),
            TotalPartidas = totalPartidas,
            PartidasFinalizadas = partidasFinalizadas,
            PromedioPuntuacionReciente = puntuacionesRecientes.Count > 0
                ? puntuacionesRecientes.Average()
                : null,
            EstudiantesDestacados = destacados,
            ActividadReciente = actividad,
            EvolucionGeneral = evolucion
        };
    }

    public async Task<EstudiantesIndexViewModel> ListarEstudiantesAsync(
        Guid docenteId, string? busqueda, int pagina = 1, CancellationToken cancellationToken = default)
    {
        var query = _db.Estudiantes
            .AsNoTracking()
            .Where(e => e.Matriculas.Any(m => m.Cohorte.DocenteId == docenteId && m.Estado == "activo"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim().ToLower();
            query = query.Where(e =>
                e.Usuario.NombreCompleto.ToLower().Contains(termino) ||
                e.Usuario.Correo.ToLower().Contains(termino) ||
                (e.CodigoInstitucional != null && e.CodigoInstitucional.ToLower().Contains(termino)));
        }

        const int tamanioPagina = 25;
        var totalEstudiantes = await query.CountAsync(cancellationToken);
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalEstudiantes / (double)tamanioPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        var estudiantes = await query
            .Include(e => e.Usuario)
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Cohorte)
                    .ThenInclude(c => c.Curso)
            .OrderBy(e => e.Usuario.NombreCompleto)
            .ThenBy(e => e.Id)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync(cancellationToken);

        var estudianteIds = estudiantes.Select(e => e.Id).ToList();

        var partidas = await _db.Partidas
            .AsNoTracking()
            .Include(p => p.Kpi)
            .Where(p => estudianteIds.Contains(p.EstudianteId))
            .ToListAsync(cancellationToken);
        var partidasPorEstudiante = partidas
            .GroupBy(partida => partida.EstudianteId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.OrderByDescending(partida => partida.FechaCreacion).ToList());

        var items = estudiantes.Select(e =>
        {
            var partidasEst = partidasPorEstudiante.GetValueOrDefault(e.Id) ?? [];

            var ultima = partidasEst.FirstOrDefault();
            var matricula = e.Matriculas
                .Where(m => m.Cohorte.DocenteId == docenteId && m.Estado == "activo")
                .OrderByDescending(m => m.MatriculadoEn)
                .FirstOrDefault();
            var cursoCohorte = matricula == null
                ? null
                : $"{matricula.Cohorte.Curso.Nombre} · {matricula.Cohorte.Nombre}";

            return new EstudianteListItemViewModel
            {
                Id = e.Id,
                NombreCompleto = e.Usuario.NombreCompleto,
                Correo = e.Usuario.Correo,
                CodigoInstitucional = e.CodigoInstitucional,
                CursoCohorte = cursoCohorte,
                UltimaActividad = ultima?.FechaCreacion,
                PartidasRegistradas = partidasEst.Count,
                EstadoProgreso = partidasEst.Count == 0 ? "Sin progreso" : "Con progreso",
                UltimaPuntuacion = ultima?.Kpi?.PuntuacionGlobal
            };
        }).ToList();

        return new EstudiantesIndexViewModel
        {
            Busqueda = busqueda,
            TotalEstudiantes = totalEstudiantes,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            TamanioPagina = tamanioPagina,
            Estudiantes = items
        };
    }

    public async Task<ProgresoEstudianteViewModel?> ObtenerProgresoAsync(
        Guid docenteId, Guid estudianteId, CancellationToken cancellationToken = default)
    {
        var estudiante = await _db.Estudiantes
            .AsNoTracking()
            .Where(e => e.Matriculas.Any(m => m.Cohorte.DocenteId == docenteId && m.Estado == "activo"))
            .Include(e => e.Usuario)
            .Include(e => e.Matriculas)
                .ThenInclude(m => m.Cohorte)
                    .ThenInclude(c => c.Curso)
            .FirstOrDefaultAsync(e => e.Id == estudianteId, cancellationToken);

        if (estudiante == null)
            return null;

        var partidas = await _db.Partidas
            .AsNoTracking()
            .Include(p => p.Kpi)
            .Include(p => p.Nivel)
                .ThenInclude(n => n.Escenario)
            .Where(p => p.EstudianteId == estudianteId)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync(cancellationToken);

        var matricula = estudiante.Matriculas
            .Where(m => m.Cohorte.DocenteId == docenteId && m.Estado == "activo")
            .OrderByDescending(m => m.MatriculadoEn)
            .FirstOrDefault();
        var cursoCohorte = matricula == null
            ? null
            : $"{matricula.Cohorte.Curso.Nombre} · {matricula.Cohorte.Nombre}";

        var vm = new ProgresoEstudianteViewModel
        {
            EstudianteId = estudiante.Id,
            NombreCompleto = estudiante.Usuario.NombreCompleto,
            Correo = estudiante.Usuario.Correo,
            CodigoInstitucional = estudiante.CodigoInstitucional,
            CursoCohorte = cursoCohorte,
            TieneProgreso = partidas.Count > 0,
            TotalPartidas = partidas.Count,
            PartidasFinalizadas = partidas.Count(p => p.Finalizada),
            UltimaActividad = partidas.FirstOrDefault()?.FechaCreacion
        };

        if (partidas.Count == 0)
            return vm;

        var ultimaConNivel = partidas.First();
        vm.NivelAlcanzado = $"Nivel {ultimaConNivel.Nivel.Numero}";
        vm.EscenarioAlcanzado = ultimaConNivel.Nivel.Escenario.Nombre;
        vm.Dificultad = ultimaConNivel.Nivel.Escenario.Dificultad;

        var ultimaPartidaConKpi = partidas.FirstOrDefault(p => p.Kpi != null);
        if (ultimaPartidaConKpi?.Kpi is { } ultimoKpi)
        {
            vm.UltimoResultadoEn = ultimaPartidaConKpi.FechaCreacion;
            vm.UltimoCostoTotal = ultimoKpi.CostoTotal;
            vm.UltimoPctEntregasATiempo = ultimoKpi.PctEntregasATiempo;
            vm.UltimaUtilizacionRecursos = ultimoKpi.UtilizacionRecursos;
            vm.UltimaEficienciaRutas = ultimoKpi.EficienciaRutas;
            vm.UltimaPuntuacionGlobal = ultimoKpi.PuntuacionGlobal;
        }

        var puntuaciones = partidas.Where(p => p.Kpi != null).Select(p => p.Kpi!.PuntuacionGlobal).ToList();
        if (puntuaciones.Count > 0)
        {
            vm.PromedioPuntuacion = Math.Round(puntuaciones.Average(), 2);
            vm.MejorPuntuacion = puntuaciones.Max();
        }

        vm.Historial = partidas.Select(p => new PartidaHistorialItemViewModel
        {
            PartidaId = p.Id,
            Fecha = p.FechaCreacion,
            NivelNumero = p.Nivel.Numero,
            EscenarioNombre = p.Nivel.Escenario.Nombre,
            Dificultad = p.Nivel.Escenario.Dificultad,
            Finalizada = p.Finalizada,
            CostoTotal = p.Kpi?.CostoTotal,
            PctEntregasATiempo = p.Kpi?.PctEntregasATiempo,
            UtilizacionRecursos = p.Kpi?.UtilizacionRecursos,
            EficienciaRutas = p.Kpi?.EficienciaRutas,
            PuntuacionGlobal = p.Kpi?.PuntuacionGlobal
        }).ToList();

        vm.Evolucion = partidas
            .Where(p => p.Kpi != null)
            .OrderBy(p => p.FechaCreacion)
            .TakeLast(10)
            .Select((p, index) => new EvolucionPuntoViewModel
            {
                Fecha = p.FechaCreacion,
                Puntuacion = p.Kpi!.PuntuacionGlobal,
                Etiqueta = p.FechaCreacion.ToLocalTime().ToString("dd/MM")
            })
            .ToList();

        return vm;
    }

    private static string ObtenerIniciales(string nombre)
    {
        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return "?";
        if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
        return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
    }
}
