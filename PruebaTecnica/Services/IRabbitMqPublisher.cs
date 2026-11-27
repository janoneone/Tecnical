namespace PruebaTecnica.Services;

public interface IRabbitMqPublisher
{
    Task PublicarAsync(long id,CancellationToken cancellationToken = default);
}