using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;
using PruebaTecnica.Repositories;

namespace PruebaTecnica.Services;

public class DatosService : IDatosService
{
    private const decimal PesoEspecial = 100.64m;

    private readonly IDatosRepository _datosRepository;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;
    private readonly ILogger<DatosService> _logger;

    public DatosService(IDatosRepository datosRepository,IRabbitMqPublisher rabbitMqPublisher,ILogger<DatosService> logger)
    {
        _datosRepository = datosRepository;
        _rabbitMqPublisher = rabbitMqPublisher;
        _logger = logger;
    }


    // LISTAR PAGINADO
    public async Task<PaginacionResponse<DatoResponse>> ListarAsync(int pagina,CancellationToken cancellationToken = default)
    {
        if (pagina < 1)
        {
            throw new ArgumentException("El número de página debe ser mayor a cero.",nameof(pagina));
        }

        return await _datosRepository.ListarAsync(pagina,cancellationToken);
    }


    // OBTENER POR ID
    public async Task<Dato?> ObtenerAsync(long id,CancellationToken cancellationToken = default)
    {
        ValidarId(id);

        return await _datosRepository.ObtenerAsync(id,cancellationToken);
    }


    // INSERTAR
    public async Task<long> InsertarAsync(CrearDatoRequest request,CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            throw new ArgumentException("El código es obligatorio.");
        }

        if (request.FechaRecepcion == default)
        {
            throw new ArgumentException("La fecha de recepción es obligatoria.");
        }

        var dato = new Dato
        {
            Codigo = request.Codigo,
            Secuencia = request.Secuencia,
            FechaRecepcion = request.FechaRecepcion,
            Peso = request.Peso,
            Descripcion = request.Descripcion
        };

        long id = await _datosRepository.InsertarAsync(
    dato,
    cancellationToken);

        // PUBLICAR PESOS ESPECIALES
        if (dato.Peso > PesoEspecial)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                await _rabbitMqPublisher.PublicarAsync(id,timeout.Token);

                _logger.LogInformation("Mensaje publicado en RabbitMQ para el dato {Id}.",id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"El dato {Id} fue guardado en Oracle, pero no se pudo publicar en RabbitMQ.",id);
            }
        }

        return id;
    }


    // ACTUALIZAR DESCRIPCIÓN
    public async Task ActualizarDescripcionAsync(long id,ActualizarDescripcionRequest request,CancellationToken cancellationToken = default)
    {
        ValidarId(id);

        ArgumentNullException.ThrowIfNull(request);

        await _datosRepository.ActualizarDescripcionAsync(id,request.Descripcion,cancellationToken);
    }


    // ELIMINAR
    public async Task EliminarAsync(long id,CancellationToken cancellationToken = default)
    {
        ValidarId(id);

        await _datosRepository.EliminarAsync(id,cancellationToken);
    }

    // VALIDAR IDENTIFICADOR
    private static void ValidarId(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("El identificador debe ser mayor a cero.",nameof(id));
        }
    }
}