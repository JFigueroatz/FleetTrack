using FleetTrack.Consumer;

namespace FleetTrack.Api.Tests;

public class EventProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsNewer_true_when_nothing_cached() => Assert.True(EventProcessor.IsNewer(null, Now));

    [Fact]
    public void IsNewer_true_for_later_event() => Assert.True(EventProcessor.IsNewer(Now, Now.AddSeconds(1)));

    [Fact]
    public void IsNewer_false_for_late_event() => Assert.False(EventProcessor.IsNewer(Now, Now.AddMinutes(-5)));

    [Fact]
    public void IsNewer_false_for_same_instant() => Assert.False(EventProcessor.IsNewer(Now, Now));
}
