using System.Text;
using FleetTrack.Api.Persistence;
using FleetTrack.Domain.Events;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace FleetTrack.Api.Messaging;

/// <summary>
/// Publica a RabbitMQ los eventos pendientes del outbox, en orden y con confirmacion del broker.
/// Si el broker cae, los eventos siguen en PostgreSQL y se publican al reconectar (entrega al-menos-una-vez; el consumidor deduplica por MessageId).
/// </summary>
public class OutboxPublisher(IServiceScopeFactory scopes, IConfiguration config, ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        IConnection? connection = null;
        IChannel? channel = null;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (channel is not { IsOpen: true })
                {
                    channel?.Dispose();
                    connection?.Dispose();
                    (connection, channel) = await ConnectAsync(ct);
                }

                var published = await PublishPendingAsync(channel, ct);
                if (published == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Outbox: no se pudo publicar, se reintenta en 3s.");
                channel = null;
                await Task.Delay(TimeSpan.FromSeconds(3), ct).ConfigureAwait(false);
            }
        }

        channel?.Dispose();
        connection?.Dispose();
    }

    private async Task<(IConnection, IChannel)> ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "guest",
            Password = config["RabbitMQ:Password"] ?? "guest"
        };
        var connection = await factory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);

        // Se declara la cola aqui tambien: si el consumidor esta caido, los eventos no deben perderse.
        await channel.ExchangeDeclareAsync(Topology.DeadExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(Topology.DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(Topology.DeadQueue, Topology.DeadExchange, string.Empty, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(Topology.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(Topology.Queue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = Topology.DeadExchange }, cancellationToken: ct);
        await channel.QueueBindAsync(Topology.Queue, Topology.Exchange, "#", cancellationToken: ct);

        logger.LogInformation("Outbox: conectado a RabbitMQ.");
        return (connection, channel);
    }

    private async Task<int> PublishPendingAsync(IChannel channel, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetTrackDbContext>();

        var batch = await db.OutboxMessages
            .Where(o => o.PublishedAt == null)
            .OrderBy(o => o.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                var properties = new BasicProperties
                {
                    MessageId = message.Id.ToString(),
                    Type = message.Type,
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    Timestamp = new AmqpTimestamp(message.OccurredAt.ToUnixTimeSeconds())
                };
                await channel.BasicPublishAsync(Topology.Exchange, message.Type, mandatory: false, properties,
                    Encoding.UTF8.GetBytes(message.Payload), ct);
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                await db.SaveChangesAsync(CancellationToken.None);
                throw; // se corta el lote para conservar el orden
            }

            message.PublishedAt = DateTimeOffset.UtcNow;
            message.Attempts++;
            await db.SaveChangesAsync(ct);
        }

        return batch.Count;
    }
}
