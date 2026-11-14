namespace PortalDocente.Models.ViewModels;

/// <summary>Indicadores y colecciones que alimentan el resumen principal del docente.</summary>
public class DashboardViewModel
{
    public int TotalEstudiantes { get; set; }
    public int EstudiantesConProgreso { get; set; }
    public int EstudiantesSinProgreso { get; set; }
    public int TotalPartidas { get; set; }
    public int PartidasFinalizadas { get; set; }
    public decimal? PromedioPuntuacionReciente { get; set; }
    public IReadOnlyList<DashboardEstudianteDestacadoViewModel> EstudiantesDestacados { get; set; }
        = Array.Empty<DashboardEstudianteDestacadoViewModel>();

    public IReadOnlyList<DashboardActividadViewModel> ActividadReciente { get; set; }
        = Array.Empty<DashboardActividadViewModel>();

    public IReadOnlyList<EvolucionPuntoViewModel> EvolucionGeneral { get; set; }
        = Array.Empty<EvolucionPuntoViewModel>();
}

/// <summary>Resumen de la actividad reciente de un estudiante destacado en el dashboard.</summary>
public class DashboardEstudianteDestacadoViewModel
{
    public Guid Id { get; set; }
    public string Codigo { get; set; } = "—";
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal? Puntuacion { get; set; }
    public int Partidas { get; set; }
    public string Iniciales { get; set; } = "?";
}

/// Elemento de la lista de actividad reciente del dashboard.
public class DashboardActividadViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; }
    public string Icono { get; set; } = "activity";
}
