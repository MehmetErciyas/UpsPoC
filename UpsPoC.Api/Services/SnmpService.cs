// UpsPoC.Api/Services/SnmpService.cs
using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Options;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public class SnmpService : ISnmpService
{
    private readonly UpsSettings _settings;
    private readonly ILogger<SnmpService> _logger;

    // RFC 1628 Read OIDs
    private static class Oids
    {
        public const string ModelName             = "1.3.6.1.2.1.33.1.1.2.0";
        public const string FirmwareVersion       = "1.3.6.1.2.1.33.1.1.4.0";
        public const string AttachedDevices       = "1.3.6.1.2.1.33.1.1.6.0";

        public const string BatteryStatus         = "1.3.6.1.2.1.33.1.2.1.0";
        public const string BatteryCapacityPercent = "1.3.6.1.2.1.33.1.2.4.0";
        public const string BatteryRemainingMins  = "1.3.6.1.2.1.33.1.2.3.0";
        public const string BatteryVoltage        = "1.3.6.1.2.1.33.1.2.5.0";
        public const string BatteryTemperature    = "1.3.6.1.2.1.33.1.2.7.0";

        public const string InputFrequency        = "1.3.6.1.2.1.33.1.3.3.1.2.1";
        public const string InputVoltage          = "1.3.6.1.2.1.33.1.3.3.1.3.1";

        public const string OutputSource          = "1.3.6.1.2.1.33.1.4.1.0";
        public const string OutputFrequency       = "1.3.6.1.2.1.33.1.4.2.0";
        public const string OutputVoltage         = "1.3.6.1.2.1.33.1.4.4.1.2.1";
        public const string OutputCurrent         = "1.3.6.1.2.1.33.1.4.4.1.3.1";
        public const string OutputLoadPercent     = "1.3.6.1.2.1.33.1.4.4.1.5.1";
        public const string OutputPowerWatts      = "1.3.6.1.2.1.33.1.4.4.1.7.1";

        public const string ActiveAlarmCount      = "1.3.6.1.2.1.33.1.6.1.0";

        // Config OIDs
        public const string ConfigInputVoltage    = "1.3.6.1.2.1.33.1.9.1.0";
        public const string ConfigInputFreq       = "1.3.6.1.2.1.33.1.9.2.0";
        public const string ConfigOutputVoltage   = "1.3.6.1.2.1.33.1.9.3.0";
        public const string ConfigOutputFreq      = "1.3.6.1.2.1.33.1.9.4.0";
        public const string ConfigLowBattMins     = "1.3.6.1.2.1.33.1.9.7.0";
        public const string ConfigAudibleStatus   = "1.3.6.1.2.1.33.1.9.8.0";
        public const string ConfigLowVoltTrans    = "1.3.6.1.2.1.33.1.9.9.0";
        public const string ConfigHighVoltTrans   = "1.3.6.1.2.1.33.1.9.10.0";

        // Test OIDs
        public const string TestSpinLock          = "1.3.6.1.2.1.33.1.7.2.0";
    }

    public SnmpService(IOptions<AppSettings> options, ILogger<SnmpService> logger)
    {
        _settings = options.Value.Ups;
        _logger = logger;
    }

    private IPEndPoint Endpoint => new(IPAddress.Parse(_settings.Host), _settings.Port);
    private OctetString ReadCommunity => new(_settings.ReadCommunity);
    private OctetString WriteCommunity => new(_settings.WriteCommunity);

    private async Task<IList<Variable>> GetAsync(params string[] oids)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        return await Messenger.GetAsync(
            VersionCode.V2,
            Endpoint,
            ReadCommunity,
            variables);
    }

    private int GetInt(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        return data is NoSuchInstance or NoSuchObject ? 0 : int.Parse(data.ToString()!);
    }

    private string GetStr(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        return data is NoSuchInstance or NoSuchObject ? string.Empty : data.ToString()!;
    }

    public async Task<UpsStatus> GetStatusAsync()
    {
        try
        {
            var vars = await GetAsync(
                Oids.ModelName, Oids.FirmwareVersion, Oids.AttachedDevices,
                Oids.BatteryStatus, Oids.BatteryCapacityPercent, Oids.BatteryRemainingMins,
                Oids.BatteryVoltage, Oids.BatteryTemperature,
                Oids.InputFrequency, Oids.InputVoltage,
                Oids.OutputSource, Oids.OutputFrequency, Oids.OutputVoltage,
                Oids.OutputCurrent, Oids.OutputLoadPercent, Oids.OutputPowerWatts,
                Oids.ActiveAlarmCount);

            return new UpsStatus
            {
                ModelName               = GetStr(vars, 0),
                FirmwareVersion         = GetStr(vars, 1),
                AttachedDevices         = GetStr(vars, 2),
                BatteryStatus           = GetInt(vars, 3),
                BatteryCapacityPercent  = GetInt(vars, 4),
                BatteryRemainingMinutes = GetInt(vars, 5),
                BatteryVoltage          = GetInt(vars, 6) / 10.0,
                BatteryTemperature      = GetInt(vars, 7),
                InputFrequency          = GetInt(vars, 8) / 10.0,
                InputVoltage            = GetInt(vars, 9),
                OutputSource            = GetInt(vars, 10),
                OutputFrequency         = GetInt(vars, 11) / 10.0,
                OutputVoltage           = GetInt(vars, 12),
                OutputCurrent           = GetInt(vars, 13) / 10.0,
                OutputLoadPercent       = GetInt(vars, 14),
                OutputPowerWatts        = GetInt(vars, 15),
                ActiveAlarmCount        = GetInt(vars, 16),
                Timestamp               = DateTime.UtcNow,
                IsConnected             = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SNMP GET status failed");
            return new UpsStatus { IsConnected = false, ErrorMessage = ex.Message, Timestamp = DateTime.UtcNow };
        }
    }

    public async Task<UpsConfig> GetConfigAsync()
    {
        try
        {
            var vars = await GetAsync(
                Oids.ConfigInputVoltage, Oids.ConfigInputFreq,
                Oids.ConfigOutputVoltage, Oids.ConfigOutputFreq,
                Oids.ConfigLowBattMins, Oids.ConfigAudibleStatus,
                Oids.ConfigLowVoltTrans, Oids.ConfigHighVoltTrans);

            return new UpsConfig
            {
                InputVoltageNominal      = GetInt(vars, 0),
                InputFreqNominal         = GetInt(vars, 1),
                OutputVoltageNominal     = GetInt(vars, 2),
                OutputFreqNominal        = GetInt(vars, 3),
                LowBatteryMinutes        = GetInt(vars, 4),
                AudibleStatus            = GetInt(vars, 5),
                LowVoltageTransferPoint  = GetInt(vars, 6),
                HighVoltageTransferPoint = GetInt(vars, 7)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SNMP GET config failed");
            throw;
        }
    }

    public async Task SetIntAsync(string oid, int value)
    {
        var variables = new List<Variable>
        {
            new(new ObjectIdentifier(oid), new Integer32(value))
        };
        await Messenger.SetAsync(
            VersionCode.V2,
            Endpoint,
            WriteCommunity,
            variables);
    }

    public async Task SetStringAsync(string oid, string value)
    {
        var variables = new List<Variable>
        {
            new(new ObjectIdentifier(oid), new OctetString(value))
        };
        await Messenger.SetAsync(
            VersionCode.V2,
            Endpoint,
            WriteCommunity,
            variables);
    }

    public async Task RunBatteryTestAsync()
    {
        await SetIntAsync(Oids.TestSpinLock, 1);
    }
}
