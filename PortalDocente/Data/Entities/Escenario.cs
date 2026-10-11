namespace PortalDocente.Data.Entities;

public class Escenario
{
    public Guid Id { get; set; }
    public Guid CohorteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Dificultad { get; set; } = "intermedio";
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Cohorte Cohorte { get; set; } = null!;
    public ICollection<Nivel> Niveles { get; set; } = new List<Nivel>();
}
