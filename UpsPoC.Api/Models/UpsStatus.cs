// UpsPoC.Api/Models/UpsStatus.cs
namespace UpsPoC.Api.Models;

public class UpsStatus
{
    // Kimlik
    public string ModelName { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public string AttachedDevices { get; set; } = string.Empty;

    // Batarya
    public int BatteryStatus { get; set; }          // 1=bilinmiyor 2=normal 3=düşük 4=kritik
    public string BatteryStatusText => BatteryStatus switch
    {
        2 => "Normal",
        3 => "Düşük",
        4 => "Kritik",
        _ => "Bilinmiyor"
    };
    public int BatteryRemainingMinutes { get; set; }
    public double BatteryVoltage { get; set; }      // 0.1V → V
    public int BatteryTemperature { get; set; }     // °C

    // Giriş
    public double InputVoltage { get; set; }        // VAC
    public double InputFrequency { get; set; }      // 0.1Hz → Hz

    // Çıkış
    public int OutputSource { get; set; }           // 1=diğer 2=normal 3=bypass 4=batarya 5=booster 6=reducer
    public string OutputSourceText => OutputSource switch
    {
        2 => "Normal",
        3 => "Bypass",
        4 => "Batarya",
        5 => "Booster",
        6 => "Reducer",
        _ => "Diğer"
    };
    public double OutputFrequency { get; set; }     // Hz
    public double OutputVoltage { get; set; }       // VAC
    public double OutputCurrent { get; set; }       // 0.1A → A
    public int OutputLoadPercent { get; set; }      // %
    public int OutputPowerWatts { get; set; }       // W

    // Alarm
    public int ActiveAlarmCount { get; set; }

    // Metadata
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
