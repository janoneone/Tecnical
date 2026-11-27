namespace PruebaTecnica.DTOs;

/// <summary>
/// Resultado de la creación de un registro.
/// </summary>
public class CrearDatoResponse
{
    /// <summary>
    /// Identificador generado para el registro.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Mensaje de confirmación de la operación.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;
}