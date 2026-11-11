using System.ComponentModel.DataAnnotations;

namespace VeterinariaApp.Entidades.DTOs;

public class MascotaActualizarDto
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string Especie { get; set; } = string.Empty;
    [Required, StringLength(80)]
    public string Raza { get; set; } = string.Empty;
    [Required, Range(0, int.MaxValue)]
    public int? Edad { get; set; }
    [Required, Range(1, int.MaxValue)]
    public int? ClienteId { get; set; }
}
