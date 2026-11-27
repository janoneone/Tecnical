using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;

namespace PruebaTecnica.Services;

public interface IDatosService
{
    Task<PaginacionResponse<DatoResponse>> ListarAsync(int pagina,CancellationToken cancellationToken = default);

    Task<Dato?> ObtenerAsync(long id,CancellationToken cancellationToken = default);

    Task<long> InsertarAsync(CrearDatoRequest request,CancellationToken cancellationToken = default);

    Task ActualizarDescripcionAsync(long id,ActualizarDescripcionRequest request,CancellationToken cancellationToken = default);

    Task EliminarAsync(long id,CancellationToken cancellationToken = default);
}