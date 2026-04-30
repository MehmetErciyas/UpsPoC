// UpsPoC.Api/Models/UpsConfig.cs
namespace UpsPoC.Api.Models;

// East EA900 / NetAgent IX cihazında SNMP üzerinden okunabilen statik kritik eşikler.
// SET destekleyen OID'ler bu cihazda doğrulanmadığı için yazma desteklenmez.
public class UpsConfig
{
    public int CriticalLoadPercent { get; set; }        // 935.1.1.1.4.3.1.0 (fallback .4.3.2.0, .6.1.1.0)
    public double CriticalTemperatureC { get; set; }    // 935.1.1.1.2.3.1.0 (fallback .6.1.2.0) — ham/10
    public int CriticalCapacityPercent { get; set; }    // 935.1.1.1.2.3.2.0 (fallback .6.1.3.0)
    public double NominalOutputVoltage { get; set; }    // 935.1.1.1.5.2.1.0 — ham/10
    public double NominalBatteryVoltage { get; set; }   // 935.1.1.1.2.2.6.0 — ham/10
}
