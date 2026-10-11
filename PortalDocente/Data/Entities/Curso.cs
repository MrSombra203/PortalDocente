namespace PortalDocente.Data.Entities;

public class Curso
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string PeriodoAcademico { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; }

    public ICollection<Cohorte> Cohortes { get; set; } = new List<Cohorte>();
}
