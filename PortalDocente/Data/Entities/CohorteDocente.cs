namespace PortalDocente.Data.Entities;

/// Permiso explícito que permite a un docente adicional consultar una cohorte.
public class CohorteDocente
{
    public Guid DocenteId { get; set; }
    public Guid CohorteId { get; set; }
    public DateTimeOffset AsignadaEn { get; set; }

    public Docente Docente { get; set; } = null!;
    public Cohorte Cohorte { get; set; } = null!;
}
