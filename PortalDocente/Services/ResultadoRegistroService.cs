using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PortalDocente.Data;
using PortalDocente.Data.Entities;
using PortalDocente.Models.Api;

namespace PortalDocente.Services;

public sealed class ResultadoRegistroService : IResultadoRegistroService
{
    private readonly PortalDbContext _db;
    private readonly ILogger<ResultadoRegistroService> _logger;

    public ResultadoRegistroService(
        PortalDbContext db,
        ILogger<ResultadoRegistroService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RegistroResultadoOutcome> RegistrarAsync(
        Guid usuarioId,
        RegistrarResultadoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IdEstudiante == Guid.Empty || request.IdempotencyKey == Guid.Empty || request.Fecha == null)
            return new RegistroResultadoOutcome(RegistroResultadoEstado.SolicitudInvalida);

        var estudianteValido = await _db.Estudiantes
            .AsNoTracking()
            .AnyAsync(
                estudiante => estudiante.Id == request.IdEstudiante
                              && estudiante.UsuarioId == usuarioId
                              && estudiante.Usuario.Activo,
                cancellationToken);
        if (!estudianteValido)
            return new RegistroResultadoOutcome(RegistroResultadoEstado.EstudianteNoEncontrado);

        if (!TryParseNumeroNivel(request.Nivel, out var numeroNivel))
            return new RegistroResultadoOutcome(RegistroResultadoEstado.EscenarioNoDisponible);

        var niveles = await _db.Niveles
            .AsNoTracking()
            .Where(nivel =>
                nivel.Numero == numeroNivel
                && nivel.Escenario.Activo
                && nivel.Escenario.Nombre == request.Escenario
                && _db.CursosEstudiantes.Any(matricula =>
                    matricula.EstudianteId == request.IdEstudiante
                    && matricula.Estado == "activo"
                    && matricula.CohorteId == nivel.Escenario.CohorteId))
            .Select(nivel => new
            {
                nivel.Id,
                nivel.Numero,
                Escenario = nivel.Escenario.Nombre
            })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (niveles.Count == 0)
            return new RegistroResultadoOutcome(RegistroResultadoEstado.EscenarioNoDisponible);
        if (niveles.Count > 1)
            return new RegistroResultadoOutcome(RegistroResultadoEstado.EscenarioAmbiguo);

        var nivelSeleccionado = niveles[0];
        var fecha = request.Fecha.Value.ToUniversalTime();
        var clave = request.IdempotencyKey;
        var claveTexto = clave.ToString("D");

        var registroExistente = await BuscarPorClaveAsync(clave, claveTexto, cancellationToken);
        if (registroExistente != null)
            return CompararExistente(registroExistente, request, nivelSeleccionado.Id, fecha);

        var partida = new Partida
        {
            Id = clave,
            EstudianteId = request.IdEstudiante,
            NivelId = nivelSeleccionado.Id,
            IdempotencyKey = claveTexto,
            VersionRegistro = 1,
            OrigenRegistro = "unity",
            EstadoSincronizacion = "sincronizado",
            FechaCreacion = fecha,
            FechaRecepcion = DateTimeOffset.UtcNow,
            Finalizada = true,
            Eliminado = false
        };
        var kpi = new Kpi
        {
            Id = Guid.NewGuid(),
            PartidaId = partida.Id,
            CostoTotal = Redondear(request.CostoTotal),
            PctEntregasATiempo = Redondear(request.EntregasATiempo),
            UtilizacionRecursos = Redondear(request.UtilizacionRecursos),
            EficienciaRutas = Redondear(request.EficienciaRutas),
            PuntuacionGlobal = Redondear(request.PuntajeGlobal),
            FechaCreacion = partida.FechaRecepcion,
            Partida = partida
        };
        partida.Kpi = kpi;

        _db.Partidas.Add(partida);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (EsClaveDuplicada(exception))
        {
            _db.ChangeTracker.Clear();
            registroExistente = await BuscarPorClaveAsync(clave, claveTexto, cancellationToken);
            if (registroExistente == null)
                throw;

            return CompararExistente(registroExistente, request, nivelSeleccionado.Id, fecha);
        }

        var resultado = CrearRespuesta(partida, nivelSeleccionado.Escenario, nivelSeleccionado.Numero, false);
        _logger.LogInformation("Se registró la partida {PartidaId} recibida desde Unity.", partida.Id);
        return new RegistroResultadoOutcome(RegistroResultadoEstado.Registrada, resultado);
    }

    public async Task<ResultadoPartidaResponse?> ObtenerAsync(
        Guid estudianteId,
        Guid partidaId,
        CancellationToken cancellationToken)
    {
        var partida = await _db.Partidas
            .AsNoTracking()
            .Include(item => item.Kpi)
            .Include(item => item.Nivel)
                .ThenInclude(nivel => nivel.Escenario)
            .FirstOrDefaultAsync(
                item => item.Id == partidaId
                        && item.EstudianteId == estudianteId
                        && item.Estudiante.Usuario.Activo,
                cancellationToken);

        return partida?.Kpi == null
            ? null
            : CrearRespuesta(partida, partida.Nivel.Escenario.Nombre, partida.Nivel.Numero, false);
    }

    private async Task<Partida?> BuscarPorClaveAsync(
        Guid id,
        string claveTexto,
        CancellationToken cancellationToken) =>
        await _db.Partidas
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(partida => partida.Kpi)
            .Include(partida => partida.Nivel)
                .ThenInclude(nivel => nivel.Escenario)
            .FirstOrDefaultAsync(
                partida => partida.Id == id || partida.IdempotencyKey == claveTexto,
                cancellationToken);

    private static RegistroResultadoOutcome CompararExistente(
        Partida existente,
        RegistrarResultadoRequest request,
        Guid nivelId,
        DateTimeOffset fecha)
    {
        if (existente.Eliminado
            || existente.EstudianteId != request.IdEstudiante
            || existente.NivelId != nivelId
            || existente.Kpi == null
            || existente.FechaCreacion.ToUniversalTime() != fecha
            || existente.Kpi.CostoTotal != Redondear(request.CostoTotal)
            || existente.Kpi.PctEntregasATiempo != Redondear(request.EntregasATiempo)
            || existente.Kpi.UtilizacionRecursos != Redondear(request.UtilizacionRecursos)
            || existente.Kpi.EficienciaRutas != Redondear(request.EficienciaRutas)
            || existente.Kpi.PuntuacionGlobal != Redondear(request.PuntajeGlobal))
        {
            return new RegistroResultadoOutcome(RegistroResultadoEstado.ClaveIdempotenciaEnUso);
        }

        return new RegistroResultadoOutcome(
            RegistroResultadoEstado.Repetida,
            CrearRespuesta(existente, existente.Nivel.Escenario.Nombre, existente.Nivel.Numero, true));
    }

    private static ResultadoPartidaResponse CrearRespuesta(
        Partida partida,
        string escenario,
        int numeroNivel,
        bool duplicada)
    {
        var kpi = partida.Kpi!;
        return new ResultadoPartidaResponse(
            partida.Id,
            escenario,
            $"Nivel {numeroNivel}",
            partida.FechaCreacion,
            partida.FechaRecepcion,
            kpi.PuntuacionGlobal,
            kpi.PctEntregasATiempo,
            kpi.UtilizacionRecursos,
            kpi.EficienciaRutas,
            kpi.CostoTotal,
            duplicada);
    }

    private static bool TryParseNumeroNivel(string nivel, out int numero)
    {
        numero = 0;
        return nivel.StartsWith("Nivel ", StringComparison.Ordinal)
               && int.TryParse(
                   nivel.AsSpan("Nivel ".Length),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out numero)
               && numero > 0;
    }

    private static decimal Redondear(decimal valor) =>
        decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static bool EsClaveDuplicada(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
