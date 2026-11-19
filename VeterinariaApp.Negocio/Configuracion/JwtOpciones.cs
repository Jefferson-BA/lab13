using System.ComponentModel.DataAnnotations;

namespace VeterinariaApp.Negocio.Configuracion;

public class JwtOpciones
{
    public const string Seccion = "Jwt";

    [Required, MinLength(32)]
    public string Clave { get; set; } = string.Empty;

    [Required]
    public string Emisor { get; set; } = string.Empty;

    [Required]
    public string Audiencia { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int MinutosExpiracion { get; set; }
}
