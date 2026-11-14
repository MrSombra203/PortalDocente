namespace PortalDocente.Data.Entities;

public class DocenteCohorte
{
    public Guid DocenteId { get; set; }
    public Guid CohorteId { get; set; }
    public DateTimeOffset AsignadoEn { get; set; }

    public Docente Docente { get; set; } = null!;
    public Cohorte Cohorte { get; set; } = null!;
}
