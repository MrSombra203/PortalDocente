namespace PortalDocente.Data.Entities;

/// Inscripción de un estudiante en una cohorte, con su estado y fecha de matrícula.</summ
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
