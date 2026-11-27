using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PruebaTecnica.DTOs;

/// <summary>
/// Solicitud para actualizar exclusivamente la descripción de un dato.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ActualizarDescripcionRequest
{
    /// <summary>
    /// Nueva descripción del dato.
    /// Puede ser nula y admite hasta 320 caracteres.
    /// </summary>
    [StringLength(320)]
    public string? Descripcion { get; set; }
}