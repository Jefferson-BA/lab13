namespace VeterinariaApp.Entidades;

public class Mascota
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public int Edad { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}
