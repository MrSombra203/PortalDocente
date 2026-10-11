namespace PortalDocente.Data.Entities;

public class Kpi
{
    public Guid Id { get; set; }
    public Guid PartidaId { get; set; }
    public decimal CostoTotal { get; set; }
    public decimal PctEntregasATiempo { get; set; }
    public decimal UtilizacionRecursos { get; set; }
    public decimal EficienciaRutas { get; set; }
    public decimal PuntuacionGlobal { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }

    public Partida Partida { get; set; } = null!;
}
