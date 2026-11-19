namespace VeterinariaApp.Entidades.DTOs;

public class TokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}
