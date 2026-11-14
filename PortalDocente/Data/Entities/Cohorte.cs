namespace PortalDocente.Data.Entities;

/// Grupo de un curso perteneciente a un docente; organiza matrículas y escenarios.
/// Otros docentes pueden consultarlo mediante CohorteDocente sin cambiar la propiedad.
public class Cohorte
{
    public Guid Id { get; set; }
    public Guid CursoId { get; set; }
    public Guid DocenteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; }

    public Curso Curso { get; set; } = null!;
    public Docente Docente { get; set; } = null!;
    public ICollection<CursoEstudiante> Matriculas { get; set; } = new List<CursoEstudiante>();
    public ICollection<Escenario> Escenarios { get; set; } = new List<Escenario>();
}
