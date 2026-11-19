namespace VeterinariaApp.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string ClaveHash { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
