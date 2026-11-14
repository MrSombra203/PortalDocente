using System.ComponentModel.DataAnnotations;

namespace PortalDocente.Models.ViewModels;

/// Recibe los datos que el formulario de inicio de sesión envía al controlador.
/// MVC valida sus atributos antes de que el servicio de autenticación compruebe
/// las credenciales; este modelo no crea la sesión ni autentica al usuario.
public class LoginViewModel
{
    /// Correo de la cuenta: obligatorio, con formato de correo y máximo de 320 caracteres.
    [Required(ErrorMessage = "Ingresa el correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(320, ErrorMessage = "El correo no puede superar los 320 caracteres.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    /// Contraseña del formulario, presentada como campo protegido y con mínimo de 8 caracteres.
    /// La comprobación de la contraseña corresponde al servicio de autenticación.

    [Required(ErrorMessage = "Ingresa la contraseña.")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}
