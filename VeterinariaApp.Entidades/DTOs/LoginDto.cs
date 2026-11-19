using System.ComponentModel.DataAnnotations;
using VeterinariaApp.Entidades.Validaciones;

namespace VeterinariaApp.Entidades.DTOs;

public class LoginDto
{
    [Required, EmailAddress, StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(72), MaximoBytesClave]
    public string Clave { get; set; } = string.Empty;
}
