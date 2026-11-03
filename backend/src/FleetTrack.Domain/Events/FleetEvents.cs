namespace FleetTrack.Domain.Events;

public static class EventTypes
{
    public const string LocationUpdated = nameof(LocationUpdated);
    public const string RouteStarted = nameof(RouteStarted);
    public const string DeliveryStarted = nameof(DeliveryStarted);
    public const string DeliveryCompleted = nameof(DeliveryCompleted);
    public const string DeliveryFailed = nameof(DeliveryFailed);
}

public static class Topology
{
    public const string Exchange = "fleettrack.events";
    public const string Queue = "fleettrack.events.q";
    public const string DeadExchange = "fleettrack.events.dlx";
    public const string DeadQueue = "fleettrack.events.dead";

    public static string LastPositionKey(Guid driverId) => $"driver:{driverId}:lastposition";
}

// LocationUpdated se serializa igual que LastPositionResponse (API), asi el consumidor puede escribirlo directo en Redis.
public record LocationUpdated(Guid DriverId, string DriverName, double Latitude, double Longitude, DateTimeOffset RecordedAt);
public record RouteStarted(Guid RouteId, Guid DriverId, DateTimeOffset StartedAt);
public record DeliveryStarted(Guid DeliveryId, Guid RouteId, Guid DriverId, DateTimeOffset StartedAt);
public record DeliveryCompleted(Guid DeliveryId, Guid RouteId, Guid DriverId, DateTimeOffset CompletedAt);
public record DeliveryFailed(Guid DeliveryId, Guid RouteId, Guid DriverId, string Reason, DateTimeOffset FailedAt);
