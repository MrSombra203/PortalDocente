using System.ComponentModel.DataAnnotations;

namespace PortalDocente.Models.ViewModels;

public class UsuarioAdministracionItemViewModel
{
    public Guid Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class UsuarioAdministracionEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre completo.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(320, ErrorMessage = "El correo no puede superar los 320 caracteres.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;

    [StringLength(72, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 72 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [Display(Name = "Cuenta activa")]
    public bool Activo { get; set; } = true;

    [StringLength(30, ErrorMessage = "El código institucional no puede superar los 30 caracteres.")]
    [Display(Name = "Código institucional")]
    public string? CodigoInstitucional { get; set; }

    [StringLength(100, ErrorMessage = "El título no puede superar los 100 caracteres.")]
    [Display(Name = "Título académico")]
    public string? TituloAcademico { get; set; }
}
