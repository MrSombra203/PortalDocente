namespace PortalDocente.Models
{
    /// <summary>
    /// Proporciona a la vista de error un identificador de seguimiento sin mostrar detalles internos.
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
