using System.Text.Json;
using RabbitMQ.Client;
using PruebaTecnica.Services;

namespace PruebaTecnica.Infrastructure;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _queueName;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        var rabbitMq = configuration.GetSection("RabbitMQ");

        _queueName = rabbitMq["QueueName"] ?? throw new InvalidOperationException("No se configuró la cola RabbitMQ.");

        _connectionFactory = new ConnectionFactory
        {
            HostName = rabbitMq["HostName"] ?? "localhost",

            Port = rabbitMq.GetValue<int>("Port", 5672),

            UserName = rabbitMq["UserName"] ?? throw new InvalidOperationException("No se configuró el usuario RabbitMQ."),

            Password = rabbitMq["Password"] ?? throw new InvalidOperationException("No se configuró la contraseña RabbitMQ."),

            VirtualHost = rabbitMq["VirtualHost"] ?? "/",

            RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
        };
    }


    // PUBLICAR MENSAJE
    public async Task PublicarAsync(long id,CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(publisherConfirmationsEnabled: true,publisherConfirmationTrackingEnabled: true);

        await using var channel =await connection.CreateChannelAsync(channelOptions,cancellationToken);

        // Serializar el identificador del registro.
        var mensaje = JsonSerializer.SerializeToUtf8Bytes(new { id });

        // Configuración del mensaje.
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true
        };

        // Publicar directamente en la cola existente.
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: true,
            basicProperties: properties,
            body: mensaje,
            cancellationToken: cancellationToken);
    }
}