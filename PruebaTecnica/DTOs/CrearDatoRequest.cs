using System.ComponentModel.DataAnnotations;

namespace PruebaTecnica.DTOs;

/// <summary>
/// Información requerida para crear un registro.
/// </summary>
public class CrearDatoRequest
{
    /// <summary>
    /// Código asociado al registro.
    /// </summary>
    [Required]
    [StringLength(32)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Número de secuencia asociado al código.
    /// </summary>
    public required int Secuencia { get; set; }

    /// <summary>
    /// Fecha y hora de recepción del registro.
    /// </summary>
    public required DateTime FechaRecepcion { get; set; }

    /// <summary>
    /// Peso asociado al registro.
    /// </summary>
    public required decimal Peso { get; set; }

    /// <summary>
    /// Descripción opcional del registro.
    /// </summary>
    [StringLength(320)]
    public string? Descripcion { get; set; }
}