// UpsPoC.Api/Models/UpsStatus.cs
namespace UpsPoC.Api.Models;

public class UpsStatus
{
    // Kimlik / Sistem bilgisi
    public string FirmwareVersion { get; set; } = string.Empty;     // Agent firmware (935.1.1.1.1.2.4.0)
    public string HardwareVersion { get; set; } = string.Empty;     // 935.1.1.1.1.2.2.0 (fallback .2.3.0, .2.5.0)
    public string SerialNumber { get; set; } = string.Empty;        // 935.1.1.1.1.2.6.0 (fallback .2.7.0, .1.3.0)
    public string SystemName { get; set; } = string.Empty;          // sysName 1.3.6.1.2.1.1.5.0
    public string SystemDescription { get; set; } = string.Empty;   // sysDescr 1.3.6.1.2.1.1.1.0
    public string Location { get; set; } = string.Empty;            // sysLocation 1.3.6.1.2.1.1.6.0
    public string Contact { get; set; } = string.Empty;             // sysContact 1.3.6.1.2.1.1.4.0
    public string UptimeText { get; set; } = string.Empty;          // sysUpTime → "X gün HH:mm:ss"
    public string LastTestResultText { get; set; } = string.Empty;  // 1=Done, 2=Aborted, 3=InProgress, 4=NoTests
    public string SystemTime { get; set; } = string.Empty;          // 935.1.1.1.9.1.0 (fallback .10.1.0, .8.1.0)
    public string NextTestSchedule { get; set; } = string.Empty;    // 935.1.1.1.7.2.4.0 (fallback .7.2.5.0)
    public string ShutdownWarning { get; set; } = string.Empty;     // 935.1.1.1.6.3.1.0 (fallback .6.3.2.0)
    public string DailyReportEmail { get; set; } = string.Empty;    // 935.1.1.1.9.3.1.0 (fallback .10.3.1.0) → "Evet"/"Hayır"

    // Batarya
    public int BatteryStatus { get; set; }                  // 1=bilinmiyor 2=normal 3=düşük (NetAgent)
    public string BatteryStatusText => BatteryStatus switch
    {
        2 => "Normal",
        3 => "Düşük",
        _ => "Bilinmiyor"
    };
    public int BatteryCapacityPercent { get; set; }         // 0-100%
    public int BatteryRemainingMinutes { get; set; }        // ham saniye / 60
    public double BatteryVoltagePerCell { get; set; }       // V/hücre (ham/10)
    public double BatteryPackVoltage { get; set; }          // V/hücre × 6 × akü sayısı
    public int BatteryBlockCount { get; set; }              // Otomatik bulundu (nominal/12) ya da config fallback
    public double BatteryTemperature { get; set; }          // °C (ham/10)

    // Giriş
    public double InputVoltage { get; set; }                // VAC (ham/10)
    public double InputFrequency { get; set; }              // Hz (ham/10)

    // Çıkış
    public int OutputSource { get; set; }                   // NetAgent: 1=bilinmiyor 2=online 3=batarya 4=boost 5=sleep 6=bypass 7=rebooting 8=standby 9=buck
    public string OutputSourceText => OutputSource switch
    {
        2 => "Online",
        3 => "Aküde",
        4 => "Boost",
        5 => "Sleep",
        6 => "Bypass",
        7 => "Rebooting",
        8 => "Standby",
        9 => "Buck",
        _ => "Bilinmiyor"
    };
    public double OutputVoltage { get; set; }               // VAC (ham/10)
    public double OutputFrequency { get; set; }             // Hz (ham/10)
    public int OutputLoadPercent { get; set; }              // %

    // Metadata
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsConnected { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
