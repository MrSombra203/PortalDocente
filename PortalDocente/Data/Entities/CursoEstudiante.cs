namespace PortalDocente.Data.Entities;

public class CursoEstudiante
{
    public Guid Id { get; set; }
    public Guid EstudianteId { get; set; }
    public Guid CohorteId { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTimeOffset MatriculadoEn { get; set; }

    public Estudiante Estudiante { get; set; } = null!;
    public Cohorte Cohorte { get; set; } = null!;
}
