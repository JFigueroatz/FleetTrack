using System.Text;
using FleetTrack.Domain.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FleetTrack.Consumer;

/// <summary>
/// Consume fleettrack.events.q. Ante un fallo reintenta hasta MaxRetries (con espera creciente, reencolando con un contador)
/// y luego envia el mensaje a la cola de mensajes fallidos (dead-letter) con el error. Procesa de a uno para respetar el orden de llegada.
/// </summary>
public class Worker(EventProcessor processor, IConfiguration config, ILogger<Worker> logger) : BackgroundService
{
    private const int MaxRetries = 3;
    private const string RetryHeader = "x-retry";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };

        await processor.WaitForSchemaAsync(ct);

        IConnection connection;
        while (true)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(ct);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("RabbitMQ no disponible ({Message}), reintentando en 3s.", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        // La conexion se recupera sola si el broker reinicia (recuperacion automatica de RabbitMQ.Client).
        await using var _ = connection;
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await DeclareTopologyAsync(channel, ct);
        await channel.BasicQosAsync(0, 1, false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleAsync(channel, ea, ct);
        await channel.BasicConsumeAsync(Topology.Queue, autoAck: false, consumer, ct);
        logger.LogInformation("Consumidor escuchando {Queue}.", Topology.Queue);

        await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var retries = ea.BasicProperties.Headers?.TryGetValue(RetryHeader, out var h) == true ? Convert.ToInt32(h) : 0;
        try
        {
            var messageId = Guid.Parse(ea.BasicProperties.MessageId ?? throw new InvalidOperationException("Mensaje sin MessageId."));
            await processor.ProcessAsync(messageId, ea.BasicProperties.Type ?? ea.RoutingKey, Encoding.UTF8.GetString(ea.Body.Span), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (retries < MaxRetries)
            {
                logger.LogWarning(ex, "Fallo procesando mensaje; reintento {N}/{Max}.", retries + 1, MaxRetries);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, retries)), ct);
                await RepublishAsync(channel, ea, string.Empty, Topology.Queue, retries + 1, null, ct);
            }
            else
            {
                logger.LogError(ex, "Mensaje {Id} agotó los reintentos; va a la cola de fallidos.", ea.BasicProperties.MessageId);
                await RepublishAsync(channel, ea, Topology.DeadExchange, string.Empty, retries, ex.Message, ct);
            }
        }

        await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
    }

    private static Task RepublishAsync(IChannel channel, BasicDeliverEventArgs ea, string exchange, string routingKey,
        int retries, string? error, CancellationToken ct)
    {
        var props = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object?>())
        };
        props.Headers[RetryHeader] = retries;
        if (error is not null)
        {
            props.Headers["x-error"] = error;
        }
        return channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, ea.Body, ct).AsTask();
    }

    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(Topology.DeadExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(Topology.DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(Topology.DeadQueue, Topology.DeadExchange, string.Empty, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(Topology.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(Topology.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = Topology.DeadExchange }, cancellationToken: ct);
        await channel.QueueBindAsync(Topology.Queue, Topology.Exchange, "#", cancellationToken: ct);
    }
}
