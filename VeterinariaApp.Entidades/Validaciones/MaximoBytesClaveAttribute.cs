using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VeterinariaApp.Entidades.Validaciones;

public sealed class MaximoBytesClaveAttribute : ValidationAttribute
{
    public MaximoBytesClaveAttribute() : base("La clave no puede superar 72 bytes UTF-8.") { }

    public override bool IsValid(object? value) =>
        value is null || value is string clave && Encoding.UTF8.GetByteCount(clave) <= 72;
}
