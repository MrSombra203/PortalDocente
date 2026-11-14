using PortalDocente.Models.ViewModels;

namespace PortalDocente.Services;

/// Contrato de lectura para el dashboard, el listado de estudiantes y el detalle de progreso.
public interface IProgresoConsultaService
{
    Task<DashboardViewModel> ObtenerDashboardAsync(Guid docenteId, CancellationToken cancellationToken = default);
    Task<EstudiantesIndexViewModel> ListarEstudiantesAsync(
        Guid docenteId, string? busqueda, int pagina = 1, CancellationToken cancellationToken = default);
    Task<ProgresoEstudianteViewModel?> ObtenerProgresoAsync(Guid docenteId, Guid estudianteId, CancellationToken cancellationToken = default);
}
