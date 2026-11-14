namespace PortalDocente.Data.Entities;

public class CohorteDocente
{
    public Guid DocenteId { get; set; }
    public Guid CohorteId { get; set; }
    public DateTimeOffset AsignadaEn { get; set; }

    public Docente Docente { get; set; } = null!;
    public Cohorte Cohorte { get; set; } = null!;
}
