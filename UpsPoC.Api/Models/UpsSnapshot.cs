// UpsPoC.Api/Models/UpsSnapshot.cs
namespace UpsPoC.Api.Models;

public class UpsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int BatteryPercent { get; set; }
    public int OutputLoadPercent { get; set; }
    public double InputVoltage { get; set; }
    public double OutputVoltage { get; set; }
    public int BatteryRemainingMinutes { get; set; }
}
