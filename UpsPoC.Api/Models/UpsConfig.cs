// UpsPoC.Api/Models/UpsConfig.cs
namespace UpsPoC.Api.Models;

public class UpsConfig
{
    public int InputVoltageNominal { get; set; }     // RMS Volt
    public int InputFreqNominal { get; set; }        // 0.1 Hz (500 = 50Hz)
    public int OutputVoltageNominal { get; set; }    // RMS Volt
    public int OutputFreqNominal { get; set; }       // 0.1 Hz
    public int LowBatteryMinutes { get; set; }       // dakika
    public int AudibleStatus { get; set; }           // 1=kapalı 2=açık 3=geçici sessiz
    public int LowVoltageTransferPoint { get; set; } // RMS Volt
    public int HighVoltageTransferPoint { get; set; }// RMS Volt
}
