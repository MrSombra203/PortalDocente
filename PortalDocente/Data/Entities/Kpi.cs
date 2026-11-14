namespace PortalDocente.Data.Entities;

/// <summary>Indicadores calculados para una partida y consultados en el progreso académico.</summary>
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
