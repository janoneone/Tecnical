using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PruebaTecnica.DTOs;
using PruebaTecnica.Entities;
using PruebaTecnica.Repositories;
using PruebaTecnica.Services;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PruebaTecnica.Tests.Services;

public class DatosServiceTests
{
    private readonly Mock<IDatosRepository> _repository;
    private readonly Mock<IRabbitMqPublisher> _publisher;
    private readonly DatosService _service;

    public DatosServiceTests()
    {
        _repository = new Mock<IDatosRepository>();
        _publisher = new Mock<IRabbitMqPublisher>();

        _service = new DatosService(
            _repository.Object,
            _publisher.Object,
            NullLogger<DatosService>.Instance);
    }

    private static CrearDatoRequest CrearRequest(decimal peso)
    {
        return new CrearDatoRequest
        {
            Codigo = "COD-TEST-001",
            Secuencia = 1,
            FechaRecepcion = new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Unspecified),
            Peso = peso,
            Descripcion = "Registro de prueba"
        };
    }

    [Theory]
    [InlineData(100.63)]
    [InlineData(100.64)]
    public async Task InsertarAsync_PesoNoEspecial_NoPublicaMensaje(
        double peso)
    {
        var request = CrearRequest((decimal)peso);

        _repository
            .Setup(x => x.InsertarAsync(
                It.IsAny<Dato>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(222222L);

        var resultado = await _service.InsertarAsync(request);

        Assert.Equal(222222L, resultado);

        _publisher.Verify(
            x => x.PublicarAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(100.65)]
    [InlineData(150.50)]
    public async Task InsertarAsync_PesoEspecial_PublicaMensaje(
        double peso)
    {
        var request = CrearRequest((decimal)peso);

        _repository
            .Setup(x => x.InsertarAsync(
                It.IsAny<Dato>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(222222L);

        _publisher
            .Setup(x => x.PublicarAsync(
                222222L,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var resultado = await _service.InsertarAsync(request);

        Assert.Equal(222222L, resultado);

        _publisher.Verify(
            x => x.PublicarAsync(
                222222L,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InsertarAsync_RabbitMqNoDisponible_RetornaId()
    {
        var request = CrearRequest(150.75m);

        _repository
            .Setup(x => x.InsertarAsync(
                It.IsAny<Dato>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(222263L);

        _publisher
            .Setup(x => x.PublicarAsync(
                222263L,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "RabbitMQ no disponible."));

        var resultado = await _service.InsertarAsync(request);

        Assert.Equal(222263L, resultado);

        _repository.Verify(
            x => x.InsertarAsync(
                It.IsAny<Dato>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisher.Verify(
            x => x.PublicarAsync(
                222263L,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InsertarAsync_ErrorRepositorio_NoPublicaMensaje()
    {
        var request = CrearRequest(150.75m);

        _repository
            .Setup(x => x.InsertarAsync(
                It.IsAny<Dato>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Error al guardar el registro."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InsertarAsync(request));

        _publisher.Verify(
            x => x.PublicarAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ListarAsync_PaginaInvalida_GeneraExcepcion(
        int pagina)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ListarAsync(pagina));

        _repository.Verify(
            x => x.ListarAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ObtenerAsync_IdInvalido_GeneraExcepcion(
        long id)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ObtenerAsync(id));

        _repository.Verify(
            x => x.ObtenerAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Descripción actualizada")]
    [InlineData(null)]
    public async Task ActualizarDescripcionAsync_NoPublicaMensaje(
        string? descripcion)
    {
        var request = new ActualizarDescripcionRequest
        {
            Descripcion = descripcion
        };

        _repository
            .Setup(x => x.ActualizarDescripcionAsync(
                222222L,
                descripcion,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.ActualizarDescripcionAsync(
            222222L,
            request);

        _repository.Verify(
            x => x.ActualizarDescripcionAsync(
                222222L,
                descripcion,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _publisher.Verify(
            x => x.PublicarAsync(
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
