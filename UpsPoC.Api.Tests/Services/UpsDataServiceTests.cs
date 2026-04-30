// UpsPoC.Api.Tests/Services/UpsDataServiceTests.cs
using FluentAssertions;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

namespace UpsPoC.Api.Tests.Services;

public class UpsDataServiceTests
{
    [Fact]
    public void GetLatestStatus_WhenNoDataYet_ReturnsDisconnectedStatus()
    {
        var service = new UpsDataService();
        var result = service.GetLatestStatus();
        result.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GetHistory_WhenEmpty_ReturnsEmptyList()
    {
        var service = new UpsDataService();
        var result = service.GetHistory();
        result.Should().BeEmpty();
    }

    [Fact]
    public void AddSnapshot_ThenGetHistory_ReturnsThatSnapshot()
    {
        var service = new UpsDataService();
        var status = new UpsStatus
        {
            BatteryStatus = 2,
            BatteryCapacityPercent = 98,
            BatteryRemainingMinutes = 42,
            OutputLoadPercent = 35,
            InputVoltage = 220,
            OutputVoltage = 220,
            IsConnected = true,
            Timestamp = DateTime.UtcNow
        };

        service.UpdateStatus(status);

        var history = service.GetHistory();
        history.Should().HaveCount(1);
        history[0].OutputLoadPercent.Should().Be(35);
        history[0].InputVoltage.Should().Be(220);
        history[0].BatteryPercent.Should().Be(98);
    }

    [Fact]
    public void UpdateStatus_ExceedsMaxSnapshots_OldestIsDropped()
    {
        var service = new UpsDataService();

        for (int i = 0; i < 721; i++)
        {
            service.UpdateStatus(new UpsStatus
            {
                BatteryCapacityPercent = i % 100,
                OutputLoadPercent = i,
                IsConnected = true,
                Timestamp = DateTime.UtcNow
            });
        }

        var history = service.GetHistory();
        history.Should().HaveCount(720);
        history[0].OutputLoadPercent.Should().Be(1); // index 0 (i=0) should be dropped
    }

    [Fact]
    public void UpdateStatus_WhenDisconnected_DoesNotAddSnapshot()
    {
        var service = new UpsDataService();
        service.UpdateStatus(new UpsStatus { IsConnected = false });

        service.GetHistory().Should().BeEmpty();
        service.GetLatestStatus().IsConnected.Should().BeFalse();
    }

    [Fact]
    public void UpdateStatus_SetsLatestStatus()
    {
        var service = new UpsDataService();
        var status = new UpsStatus { IsConnected = true, OutputLoadPercent = 42, Timestamp = DateTime.UtcNow };
        service.UpdateStatus(status);
        service.GetLatestStatus().OutputLoadPercent.Should().Be(42);
    }
}
