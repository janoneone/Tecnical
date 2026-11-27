using Microsoft.AspNetCore.Mvc;
using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;
using PruebaTecnica.Services;

namespace PruebaTecnica.Controllers;

/// <summary>
/// Permite consultar, crear, actualizar y eliminar registros.
/// </summary>
[ApiController]
[Route("api/datos")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
public class DatosController : ControllerBase
{
    private readonly IDatosService _datosService;

    public DatosController(IDatosService datosService)
    {
        _datosService = datosService;
    }

    // GET: LISTAR PAGINADO

    /// <summary>
    /// Obtiene el listado paginado de registros.
    /// </summary>
    /// <remarks>
    /// Devuelve 20 registros por página, ordenados desde
    /// el más reciente al más antiguo.
    /// Incluye el total de registros asociados a cada código.
    /// </remarks>
    /// <param name="pagina">Número de página solicitada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Listado obtenido correctamente.</response>
    /// <response code="400">Número de página inválido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginacionResponse<DatoResponse>),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1,CancellationToken cancellationToken = default)
    {
        if (pagina < 1)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Página inválida",
                detail: "El número de página debe ser mayor a cero.",
                instance: HttpContext.Request.Path);
        }

        var resultado = await _datosService.ListarAsync(pagina,cancellationToken);

        return Ok(resultado);
    }

    // GET: OBTENER POR ID

    /// <summary>
    /// Obtiene un registro por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="200">Registro encontrado.</response>
    /// <response code="400">Identificador inválido.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(Dato),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(long id,CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Identificador inválido",
                detail: "El identificador debe ser mayor a cero.",
                instance: HttpContext.Request.Path);
        }

        var resultado = await _datosService.ObtenerAsync(id,cancellationToken);

        if (resultado is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Registro no encontrado",
                detail: $"No existe un registro con ID {id}.",
                instance: HttpContext.Request.Path);
        }

        return Ok(resultado);
    }

    // POST: INSERTAR

    /// <summary>
    /// Crea un nuevo registro.
    /// </summary>
    /// <remarks>
    /// El identificador se genera mediante la secuencia de Oracle.
    /// Si el peso es superior a 100,64, se intenta publicar
    /// una notificación en RabbitMQ después de guardar el registro.
    /// </remarks>
    /// <param name="request">Información del nuevo registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="201">Registro creado correctamente.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="409">Código y secuencia duplicados.</response>
    /// <response code="415">Formato de contenido no admitido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CrearDatoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Insertar([FromBody] CrearDatoRequest request,CancellationToken cancellationToken = default)
    {
        if (request.FechaRecepcion == default)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Fecha de recepción inválida",
                detail: "La fecha de recepción es obligatoria.",
                instance: HttpContext.Request.Path);
        }

        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Código inválido",
                detail: "El código es obligatorio.",
                instance: HttpContext.Request.Path);
        }

        var id = await _datosService.InsertarAsync(request,cancellationToken);

        var response = new CrearDatoResponse
        {
            Id = id,
            Mensaje = "Registro creado correctamente."
        };

        return CreatedAtAction(nameof(Obtener),new { id },response);
    }

    // PATCH: ACTUALIZAR DESCRIPCIÓN

    /// <summary>
    /// Actualiza exclusivamente la descripción de un registro.
    /// </summary>
    /// <remarks>
    /// La descripción admite hasta 320 caracteres.
    /// Puede establecerse como null para eliminar su contenido.
    /// No se permite modificar otros campos.
    /// </remarks>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="request">Nueva descripción del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Descripción actualizada correctamente.</response>
    /// <response code="400">Datos inválidos o campos no permitidos.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="415">Formato de contenido no admitido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPatch("{id:long}/descripcion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> ActualizarDescripcion(long id,[FromBody] ActualizarDescripcionRequest request,CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Identificador inválido",
                detail: "El identificador debe ser mayor a cero.",
                instance: HttpContext.Request.Path);
        }

        await _datosService.ActualizarDescripcionAsync(id,request,cancellationToken);

        return NoContent();
    }

    // DELETE: ELIMINAR

    /// <summary>
    /// Elimina un registro por su identificador.
    /// </summary>
    /// <param name="id">Identificador del registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <response code="204">Registro eliminado correctamente.</response>
    /// <response code="400">Identificador inválido.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(long id,CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Identificador inválido",
                detail: "El identificador debe ser mayor a cero.",
                instance: HttpContext.Request.Path);
        }

        await _datosService.EliminarAsync(id,cancellationToken);

        return NoContent();
    }
}