namespace PortalDocente.Data.Entities;

/// <summary>
/// Registro de una partida de un estudiante en un nivel, con estado de sincronización
/// y eliminación lógica; puede tener un único conjunto de KPI.
/// </summary>
public class Partida
{
    public Guid Id { get; set; }
    public Guid EstudianteId { get; set; }
    public Guid NivelId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public int VersionRegistro { get; set; } = 1;
    public string OrigenRegistro { get; set; } = "local";
    public string EstadoSincronizacion { get; set; } = "sincronizado";
    public DateTimeOffset FechaCreacion { get; set; }
    public DateTimeOffset FechaRecepcion { get; set; }
    public bool Finalizada { get; set; }
    public bool Eliminado { get; set; }

    public Estudiante Estudiante { get; set; } = null!;
    public Nivel Nivel { get; set; } = null!;
    public Kpi? Kpi { get; set; }
}
