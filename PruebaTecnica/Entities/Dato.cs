namespace PruebaTecnica.Entities;

/// <summary>
/// Representa un registro almacenado en la tabla DATOS.
/// </summary>
public class Dato
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Código asociado al registro.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Número de secuencia asociado al código.
    /// </summary>
    public int Secuencia { get; set; }

    /// <summary>
    /// Fecha y hora de recepción del registro.
    /// </summary>
    public DateTime FechaRecepcion { get; set; }

    /// <summary>
    /// Peso asociado al registro.
    /// </summary>
    public decimal Peso { get; set; }

    /// <summary>
    /// Descripción opcional del registro.
    /// </summary>
    public string? Descripcion { get; set; }
}