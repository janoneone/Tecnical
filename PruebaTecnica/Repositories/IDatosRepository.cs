using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;

namespace PruebaTecnica.Repositories;

public interface IDatosRepository
{
    Task<PaginacionResponse<DatoResponse>> ListarAsync(int pagina,CancellationToken cancellationToken = default);

    Task<Dato?> ObtenerAsync(long id,CancellationToken cancellationToken = default);

    Task<long> InsertarAsync(Dato dato,CancellationToken cancellationToken = default);

    Task ActualizarDescripcionAsync(long id,string? descripcion,CancellationToken cancellationToken = default);

    Task EliminarAsync(long id,CancellationToken cancellationToken = default);
}