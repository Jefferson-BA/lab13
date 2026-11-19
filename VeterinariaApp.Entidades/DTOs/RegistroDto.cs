using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VeterinariaApp.Entidades.Validaciones;

namespace VeterinariaApp.Entidades.DTOs;

// Rechaza campos adicionales como 'rol': los permisos los decide el servidor.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegistroDto
{
    [Required, EmailAddress, StringLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required, StringLength(72, MinimumLength = 8), MaximoBytesClave]
    public string Clave { get; set; } = string.Empty;
}
