using PortalDocente.Models.Api;

namespace PortalDocente.Services;

public interface IResultadoRegistroService
{
    Task<RegistroResultadoOutcome> RegistrarAsync(
        Guid usuarioId,
        RegistrarResultadoRequest request,
        CancellationToken cancellationToken);

    Task<ResultadoPartidaResponse?> ObtenerAsync(
        Guid estudianteId,
        Guid partidaId,
        CancellationToken cancellationToken);
}

public enum RegistroResultadoEstado
{
    Registrada,
    Repetida,
    EstudianteNoEncontrado,
    EscenarioNoDisponible,
    EscenarioAmbiguo,
    ClaveIdempotenciaEnUso,
    SolicitudInvalida
}

public sealed record RegistroResultadoOutcome(
    RegistroResultadoEstado Estado,
    ResultadoPartidaResponse? Resultado = null);
