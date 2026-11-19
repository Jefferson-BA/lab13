namespace VeterinariaApp.Datos;

// Traduce la restricción única sin exponer EF o SqlException al servicio.
public sealed class CorreoDuplicadoException(Exception innerException)
    : Exception("El correo ya se encuentra registrado.", innerException);
