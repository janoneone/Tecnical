namespace PruebaTecnica.DTOs;

/// <summary>
/// Representa el resultado de una consulta paginada.
/// </summary>
/// <typeparam name="T">
/// Tipo de registro incluido en la respuesta.
/// </typeparam>
public class PaginacionResponse<T>
{
    /// <summary>
    /// Número de página consultada.
    /// </summary>
    public int Pagina { get; set; }

    /// <summary>
    /// Cantidad máxima de registros por página.
    /// </summary>
    public int RegistrosPorPagina { get; set; } = 20;

    /// <summary>
    /// Cantidad total de registros disponibles.
    /// </summary>
    public long TotalRegistros { get; set; }

    /// <summary>
    /// Cantidad total de páginas disponibles.
    /// </summary>
    public long TotalPaginas =>
        (long)Math.Ceiling(
            (decimal)TotalRegistros / RegistrosPorPagina
        );

    /// <summary>
    /// Registros correspondientes a la página consultada.
    /// </summary>
    public IEnumerable<T> Datos { get; set; } =
        Enumerable.Empty<T>();
}