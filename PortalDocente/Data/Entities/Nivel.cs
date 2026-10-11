namespace PortalDocente.Data.Entities;

public class Nivel
{
    public Guid Id { get; set; }
    public Guid EscenarioId { get; set; }
    public int Numero { get; set; }
    public decimal PresupuestoInicial { get; set; }

    public Escenario Escenario { get; set; } = null!;
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}
