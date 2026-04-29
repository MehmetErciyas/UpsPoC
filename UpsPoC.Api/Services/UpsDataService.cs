// UpsPoC.Api/Services/UpsDataService.cs
using System.Collections.Concurrent;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public class UpsDataService : IUpsDataService
{
    private const int MaxSnapshots = 720; // 1 hour at 5-second polling intervals

    private readonly ConcurrentQueue<UpsSnapshot> _history = new();
    private volatile UpsStatus _latestStatus = new() { IsConnected = false };

    public UpsStatus GetLatestStatus() => _latestStatus;

    public List<UpsSnapshot> GetHistory() => _history.ToList();

    public void UpdateStatus(UpsStatus status)
    {
        _latestStatus = status;

        if (!status.IsConnected) return;

        _history.Enqueue(new UpsSnapshot
        {
            Timestamp               = status.Timestamp,
            BatteryPercent          = status.BatteryCapacityPercent,
            OutputLoadPercent       = status.OutputLoadPercent,
            InputVoltage            = status.InputVoltage,
            OutputVoltage           = status.OutputVoltage,
            BatteryRemainingMinutes = status.BatteryRemainingMinutes,
            OutputPowerWatts        = status.OutputPowerWatts
        });

        while (_history.Count > MaxSnapshots)
            _history.TryDequeue(out _);
    }
}
