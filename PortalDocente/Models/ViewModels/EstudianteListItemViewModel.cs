namespace PortalDocente.Models.ViewModels;

public class EstudianteListItemViewModel
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? CodigoInstitucional { get; set; }
    public string? CursoCohorte { get; set; }
    public DateTimeOffset? UltimaActividad { get; set; }
    public int PartidasRegistradas { get; set; }
    public string EstadoProgreso { get; set; } = "Sin progreso";
    public decimal? UltimaPuntuacion { get; set; }
}

public class EstudiantesIndexViewModel
{
    public string? Busqueda { get; set; }
    public bool UsaDatosDemo { get; set; }
    public IReadOnlyList<EstudianteListItemViewModel> Estudiantes { get; set; } = Array.Empty<EstudianteListItemViewModel>();
}
