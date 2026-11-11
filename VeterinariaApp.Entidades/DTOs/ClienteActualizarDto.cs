using System.ComponentModel.DataAnnotations;

namespace VeterinariaApp.Entidades.DTOs;

public class ClienteActualizarDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener 8 dígitos.")]
    public string Dni { get; set; } = string.Empty;
    [Required, StringLength(20), Phone]
    public string Telefono { get; set; } = string.Empty;
    [Required, StringLength(120), EmailAddress]
    public string Email { get; set; } = string.Empty;
}
