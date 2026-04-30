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
    private readonly IConnectionState _connection;
    private readonly ILogger<SnmpService> _logger;

    // East EA900 / Powerware-MAKELSAN NetAgent IX — enterprise 935 + standard mib2 sys.
    // OID seti netagent_gui_v10.py referans alınarak hizalandı.
    private static class Oids
    {
        // --- Live metrics ---
        public const string OutputSource           = "1.3.6.1.4.1.935.1.1.1.4.1.1.0";
        public const string BatteryStatus          = "1.3.6.1.4.1.935.1.1.1.2.1.1.0";
        public const string BatteryCapacityPercent = "1.3.6.1.4.1.935.1.1.1.2.2.1.0";
        public const string BatteryVoltagePerCell  = "1.3.6.1.4.1.935.1.1.1.2.2.2.0"; // V/hücre × 10
        public const string BatteryTemperature     = "1.3.6.1.4.1.935.1.1.1.2.2.3.0"; // °C × 10
        public const string BatteryRemainingSec    = "1.3.6.1.4.1.935.1.1.1.2.2.4.0";
        public const string InputVoltage           = "1.3.6.1.4.1.935.1.1.1.3.2.1.0"; // V × 10
        public const string InputFrequency         = "1.3.6.1.4.1.935.1.1.1.3.2.4.0"; // Hz × 10
        public const string OutputVoltage          = "1.3.6.1.4.1.935.1.1.1.4.2.1.0"; // V × 10
        public const string OutputFrequency        = "1.3.6.1.4.1.935.1.1.1.4.2.2.0"; // Hz × 10
        public const string OutputLoadPercent      = "1.3.6.1.4.1.935.1.1.1.4.2.3.0";

        // --- Nominal / config ---
        public const string NominalOutputVoltage   = "1.3.6.1.4.1.935.1.1.1.5.2.1.0"; // V × 10
        public const string NominalBatteryVoltage  = "1.3.6.1.4.1.935.1.1.1.2.2.6.0"; // V × 10

        // --- System info (sysX standard + 935 fallback) ---
        public const string SysDescr               = "1.3.6.1.2.1.1.1.0";
        public const string SysContact             = "1.3.6.1.2.1.1.4.0";
        public const string SysName                = "1.3.6.1.2.1.1.5.0";
        public const string SysLocation            = "1.3.6.1.2.1.1.6.0";
        public const string SysUptime              = "1.3.6.1.2.1.1.3.0"; // TimeTicks (1/100 sn)

        public static readonly string[] HardwareVersionOids = {
            "1.3.6.1.4.1.935.1.1.1.1.2.2.0",
            "1.3.6.1.4.1.935.1.1.1.1.2.3.0",
            "1.3.6.1.4.1.935.1.1.1.1.2.5.0",
        };
        public static readonly string[] FirmwareVersionOids = {
            "1.3.6.1.4.1.935.1.1.1.1.2.4.0",
            "1.3.6.1.2.1.33.1.1.4.0",
        };
        public static readonly string[] SerialNumberOids = {
            "1.3.6.1.4.1.935.1.1.1.1.2.6.0",
            "1.3.6.1.4.1.935.1.1.1.1.2.7.0",
            "1.3.6.1.4.1.935.1.1.1.1.1.3.0",
        };
        public static readonly string[] LastTestResultOids = {
            "1.3.6.1.2.1.33.1.7.3.0",
            "1.3.6.1.4.1.935.1.1.1.7.2.1.0",
        };

        // --- Critical thresholds ---
        public static readonly string[] CriticalLoadOids = {
            "1.3.6.1.4.1.935.1.1.1.4.3.1.0",
            "1.3.6.1.4.1.935.1.1.1.4.3.2.0",
            "1.3.6.1.4.1.935.1.1.1.6.1.1.0",
        };
        public static readonly string[] CriticalTemperatureOids = {
            "1.3.6.1.4.1.935.1.1.1.2.3.1.0",
            "1.3.6.1.4.1.935.1.1.1.6.1.2.0",
        };
        public static readonly string[] CriticalCapacityOids = {
            "1.3.6.1.4.1.935.1.1.1.2.3.2.0",
            "1.3.6.1.4.1.935.1.1.1.6.1.3.0",
        };

        // --- Control (SET) ---
        public const string RebootCmd              = "1.3.6.1.4.1.935.1.1.1.6.2.2.0";
        public const string ShutdownCmd            = "1.3.6.1.4.1.935.1.1.1.6.2.3.0";
    }

    public SnmpService(IOptions<AppSettings> options, IConnectionState connection, ILogger<SnmpService> logger)
    {
        _settings = options.Value.Ups;
        _connection = connection;
        _logger = logger;
    }

    private (IPEndPoint endpoint, OctetString readCommunity, OctetString writeCommunity) Resolve()
    {
        if (!_connection.IsConfigured)
            throw new InvalidOperationException("UPS bağlantısı yapılandırılmadı. Önce IP ve community ayarlayın.");

        var ip = IPAddress.Parse(_connection.Host);
        var ep = new IPEndPoint(ip, _connection.Port);
        var read = new OctetString(_connection.ReadCommunity);
        var write = new OctetString(_connection.WriteCommunity);
        return (ep, read, write);
    }

    private async Task<IList<Variable>> GetAsync(IPEndPoint ep, OctetString community, params string[] oids)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
        return await Messenger.GetAsync(VersionCode.V2, ep, community, variables, cts.Token);
    }

    private static bool IsAbsent(ISnmpData data) => data is NoSuchInstance or NoSuchObject or Null;

    private static int GetInt(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        if (IsAbsent(data)) return 0;
        return int.TryParse(data.ToString(), out var result) ? result : 0;
    }

    private static string GetStr(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        return IsAbsent(data) ? string.Empty : data.ToString() ?? string.Empty;
    }

    private async Task<(string raw, bool ok)> TryReadFirstAsync(IPEndPoint ep, OctetString community, string[] oids)
    {
        foreach (var oid in oids)
        {
            try
            {
                var vars = await GetAsync(ep, community, oid);
                var data = vars[0].Data;
                if (IsAbsent(data)) continue;
                var s = data.ToString();
                if (string.IsNullOrWhiteSpace(s)) continue;
                return (s, true);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Fallback OID {Oid} okunamadı", oid);
            }
        }
        return (string.Empty, false);
    }

    private async Task<int> TryReadIntAsync(IPEndPoint ep, OctetString community, string[] oids)
    {
        var (raw, ok) = await TryReadFirstAsync(ep, community, oids);
        if (!ok) return 0;
        return int.TryParse(raw, out var v) ? v : 0;
    }

    private static string FormatUptimeTicks(int ticks)
    {
        if (ticks <= 0) return string.Empty;
        var totalSec = ticks / 100L;
        var days = totalSec / 86400;
        var hours = (totalSec % 86400) / 3600;
        var minutes = (totalSec % 3600) / 60;
        var seconds = totalSec % 60;
        return days > 0
            ? $"{days} gün {hours:D2}:{minutes:D2}:{seconds:D2}"
            : $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    private static string FormatTestResult(int code) => code switch
    {
        1 => "Başarılı",
        2 => "İptal",
        3 => "Devam ediyor",
        4 => "Test yok",
        0 => string.Empty,
        _ => $"Kod {code}"
    };

    private int DetectBatteryBlockCount(double nominalBatteryVolts)
    {
        if (nominalBatteryVolts > 0)
        {
            var count = (int)Math.Round(nominalBatteryVolts / 12.0);
            if (count is >= 1 and <= 80) return count;
        }
        return Math.Max(1, _settings.FallbackBatteryBlockCount);
    }

    public async Task<UpsStatus> GetStatusAsync()
    {
        if (!_connection.IsConfigured)
        {
            return new UpsStatus
            {
                IsConnected = false,
                ErrorMessage = "Bağlantı yapılandırılmadı.",
                Timestamp = DateTime.UtcNow
            };
        }

        try
        {
            var (ep, read, _) = Resolve();

            var vars = await GetAsync(ep, read,
                Oids.OutputSource,            // 0
                Oids.BatteryStatus,           // 1
                Oids.BatteryCapacityPercent,  // 2
                Oids.BatteryVoltagePerCell,   // 3
                Oids.BatteryTemperature,      // 4
                Oids.BatteryRemainingSec,     // 5
                Oids.InputVoltage,            // 6
                Oids.InputFrequency,          // 7
                Oids.OutputVoltage,           // 8
                Oids.OutputFrequency,         // 9
                Oids.OutputLoadPercent,       // 10
                Oids.NominalBatteryVoltage,   // 11
                Oids.NominalOutputVoltage,    // 12
                Oids.SysDescr,                // 13
                Oids.SysName,                 // 14
                Oids.SysLocation,             // 15
                Oids.SysContact,              // 16
                Oids.SysUptime                // 17
            );

            var nominalBattery = GetInt(vars, 11) / 10.0;
            var blockCount = DetectBatteryBlockCount(nominalBattery);
            var vPerCell = GetInt(vars, 3) / 10.0;
            var packVoltage = vPerCell * 6 * blockCount;

            var hwTask  = TryReadFirstAsync(ep, read, Oids.HardwareVersionOids);
            var fwTask  = TryReadFirstAsync(ep, read, Oids.FirmwareVersionOids);
            var snTask  = TryReadFirstAsync(ep, read, Oids.SerialNumberOids);
            var testTask = TryReadIntAsync(ep, read, Oids.LastTestResultOids);
            await Task.WhenAll(hwTask, fwTask, snTask, testTask);

            var uptimeTicks = GetInt(vars, 17);

            return new UpsStatus
            {
                FirmwareVersion        = fwTask.Result.raw,
                HardwareVersion        = hwTask.Result.raw,
                SerialNumber           = snTask.Result.raw,
                SystemName             = GetStr(vars, 14),
                SystemDescription      = GetStr(vars, 13),
                Location               = GetStr(vars, 15),
                Contact                = GetStr(vars, 16),
                UptimeText             = FormatUptimeTicks(uptimeTicks),
                LastTestResultText     = FormatTestResult(testTask.Result),

                BatteryStatus          = GetInt(vars, 1),
                BatteryCapacityPercent = GetInt(vars, 2),
                BatteryRemainingMinutes = GetInt(vars, 5) / 60,
                BatteryVoltagePerCell  = vPerCell,
                BatteryPackVoltage     = packVoltage,
                BatteryBlockCount      = blockCount,
                BatteryTemperature     = GetInt(vars, 4) / 10.0,

                InputVoltage           = GetInt(vars, 6) / 10.0,
                InputFrequency         = GetInt(vars, 7) / 10.0,

                OutputSource           = GetInt(vars, 0),
                OutputVoltage          = GetInt(vars, 8) / 10.0,
                OutputFrequency        = GetInt(vars, 9) / 10.0,
                OutputLoadPercent      = GetInt(vars, 10),

                Timestamp              = DateTime.UtcNow,
                IsConnected            = true
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
        if (!_connection.IsConfigured) return new UpsConfig();

        try
        {
            var (ep, read, _) = Resolve();
            var nominalVars = await GetAsync(ep, read, Oids.NominalOutputVoltage, Oids.NominalBatteryVoltage);

            var loadTask = TryReadIntAsync(ep, read, Oids.CriticalLoadOids);
            var tempTask = TryReadIntAsync(ep, read, Oids.CriticalTemperatureOids);
            var capTask  = TryReadIntAsync(ep, read, Oids.CriticalCapacityOids);
            await Task.WhenAll(loadTask, tempTask, capTask);

            return new UpsConfig
            {
                CriticalLoadPercent     = loadTask.Result,
                CriticalTemperatureC    = tempTask.Result / 10.0,
                CriticalCapacityPercent = capTask.Result,
                NominalOutputVoltage    = GetInt(nominalVars, 0) / 10.0,
                NominalBatteryVoltage   = GetInt(nominalVars, 1) / 10.0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SNMP GET config failed");
            return new UpsConfig();
        }
    }

    public async Task SetIntAsync(string oid, int value)
    {
        var (ep, _, write) = Resolve();
        try
        {
            var variables = new List<Variable>
            {
                new(new ObjectIdentifier(oid), new Integer32(value))
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            await Messenger.SetAsync(VersionCode.V2, ep, write, variables, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP SET int failed for OID {Oid}", oid);
            throw;
        }
    }

    public async Task SetStringAsync(string oid, string value)
    {
        var (ep, _, write) = Resolve();
        try
        {
            var variables = new List<Variable>
            {
                new(new ObjectIdentifier(oid), new OctetString(value))
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            await Messenger.SetAsync(VersionCode.V2, ep, write, variables, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP SET string failed for OID {Oid}", oid);
            throw;
        }
    }

    public Task RebootAsync()  => SetIntAsync(Oids.RebootCmd, 2);
    public Task ShutdownAsync() => SetIntAsync(Oids.ShutdownCmd, 2);

    public Task RunBatteryTestAsync()
        => throw new NotSupportedException("Batarya testi OID bu cihaz için doğrulanmadı.");

    public async Task<RawOidResult> GetRawAsync(string oid)
    {
        try
        {
            var (ep, read, _) = Resolve();
            var variables = new List<Variable> { new(new ObjectIdentifier(oid)) };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            var result = await Messenger.GetAsync(VersionCode.V2, ep, read, variables, cts.Token);
            var data = result[0].Data;
            return new RawOidResult
            {
                Oid   = oid,
                Type  = data.GetType().Name,
                Value = data.ToString() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            return new RawOidResult { Oid = oid, Type = "Error", Value = ex.Message };
        }
    }

    public async Task<List<RawOidResult>> WalkAsync(string startOid, bool withinSubtree = true)
    {
        var results = new List<RawOidResult>();
        try
        {
            var (ep, read, _) = Resolve();
            var walked = new List<Variable>();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30000));
            await Messenger.WalkAsync(
                VersionCode.V2,
                ep,
                read,
                new ObjectIdentifier(startOid),
                walked,
                withinSubtree ? WalkMode.WithinSubtree : WalkMode.Default,
                cts.Token);

            foreach (var v in walked)
            {
                results.Add(new RawOidResult
                {
                    Oid   = v.Id.ToString(),
                    Type  = v.Data.GetType().Name,
                    Value = v.Data.ToString() ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            results.Add(new RawOidResult { Oid = startOid, Type = "Error", Value = ex.Message });
        }
        return results;
    }
}
