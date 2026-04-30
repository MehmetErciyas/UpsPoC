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
        public const string BatteryVoltagePerCell  = "1.3.6.1.4.1.935.1.1.1.2.2.2.0";
        public const string BatteryTemperature     = "1.3.6.1.4.1.935.1.1.1.2.2.3.0";
        public const string BatteryRemainingSec    = "1.3.6.1.4.1.935.1.1.1.2.2.4.0";
        public const string InputVoltage           = "1.3.6.1.4.1.935.1.1.1.3.2.1.0";
        public const string InputFrequency         = "1.3.6.1.4.1.935.1.1.1.3.2.4.0";
        public const string OutputVoltage          = "1.3.6.1.4.1.935.1.1.1.4.2.1.0";
        public const string OutputFrequency        = "1.3.6.1.4.1.935.1.1.1.4.2.2.0";
        public const string OutputLoadPercent      = "1.3.6.1.4.1.935.1.1.1.4.2.3.0";
        public const string NominalOutputVoltage   = "1.3.6.1.4.1.935.1.1.1.5.2.1.0";
        public const string NominalBatteryVoltage  = "1.3.6.1.4.1.935.1.1.1.2.2.6.0";

        // --- System info (sysX standard + 935 fallback) ---
        public const string SysDescr               = "1.3.6.1.2.1.1.1.0";
        public const string SysContact             = "1.3.6.1.2.1.1.4.0";
        public const string SysName                = "1.3.6.1.2.1.1.5.0";
        public const string SysLocation            = "1.3.6.1.2.1.1.6.0";
        public const string SysUptime              = "1.3.6.1.2.1.1.3.0";

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
        public static readonly string[] SystemTimeOids = {
            "1.3.6.1.4.1.935.1.1.1.9.1.0",
            "1.3.6.1.4.1.935.1.1.1.10.1.0",
            "1.3.6.1.4.1.935.1.1.1.8.1.0",
        };
        public static readonly string[] NextTestScheduleOids = {
            "1.3.6.1.4.1.935.1.1.1.7.2.4.0",
            "1.3.6.1.4.1.935.1.1.1.7.2.5.0",
        };
        public static readonly string[] ShutdownWarningOids = {
            "1.3.6.1.4.1.935.1.1.1.6.3.1.0",
            "1.3.6.1.4.1.935.1.1.1.6.3.2.0",
        };
        public static readonly string[] DailyReportEmailOids = {
            "1.3.6.1.4.1.935.1.1.1.9.3.1.0",
            "1.3.6.1.4.1.935.1.1.1.10.3.1.0",
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
        return (new IPEndPoint(ip, _connection.Port),
                new OctetString(_connection.ReadCommunity),
                new OctetString(_connection.WriteCommunity));
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

    // (rawValue, oid, ok, error) — ilk geçerli OID'yi döndür
    private async Task<(string raw, string oid, bool ok, string? error)> TryReadFirstWithOidAsync(IPEndPoint ep, OctetString community, string[] oids)
    {
        string? lastError = null;
        foreach (var oid in oids)
        {
            try
            {
                var vars = await GetAsync(ep, community, oid);
                var data = vars[0].Data;
                if (IsAbsent(data)) { lastError = "noSuchObject"; continue; }
                var s = data.ToString();
                if (string.IsNullOrWhiteSpace(s)) { lastError = "boş"; continue; }
                return (s, oid, true, null);
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogDebug(ex, "Fallback OID {Oid} okunamadı", oid);
            }
        }
        return (string.Empty, oids.FirstOrDefault() ?? string.Empty, false, lastError);
    }

    private async Task<int> TryReadIntAsync(IPEndPoint ep, OctetString community, string[] oids)
    {
        var r = await TryReadFirstWithOidAsync(ep, community, oids);
        if (!r.ok) return 0;
        return int.TryParse(r.raw, out var v) ? v : 0;
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

    private static string FormatYesNo(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw.Trim().ToLowerInvariant();
        if (s is "1" or "yes" or "true" or "evet" or "aktif" or "var") return "Evet";
        if (s is "0" or "no" or "false" or "hayir" or "hayır" or "pasif" or "yok") return "Hayır";
        return raw;
    }

    private int DetectBatteryBlockCount(double nominalBatteryVolts)
    {
        // Manuel override öncelikli
        var manual = _connection.ManualBatteryBlockCount;
        if (manual is >= 1 and <= 80) return manual.Value;

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

            // Fallback'li alanlar paralel.
            var hwTask        = TryReadFirstWithOidAsync(ep, read, Oids.HardwareVersionOids);
            var fwTask        = TryReadFirstWithOidAsync(ep, read, Oids.FirmwareVersionOids);
            var snTask        = TryReadFirstWithOidAsync(ep, read, Oids.SerialNumberOids);
            var lastTestTask  = TryReadIntAsync(ep, read, Oids.LastTestResultOids);
            var sysTimeTask   = TryReadFirstWithOidAsync(ep, read, Oids.SystemTimeOids);
            var nextTestTask  = TryReadFirstWithOidAsync(ep, read, Oids.NextTestScheduleOids);
            var shutdownWarnTask = TryReadFirstWithOidAsync(ep, read, Oids.ShutdownWarningOids);
            var dailyReportTask  = TryReadFirstWithOidAsync(ep, read, Oids.DailyReportEmailOids);
            await Task.WhenAll(hwTask, fwTask, snTask, lastTestTask, sysTimeTask, nextTestTask, shutdownWarnTask, dailyReportTask);

            return new UpsStatus
            {
                FirmwareVersion        = fwTask.Result.raw,
                HardwareVersion        = hwTask.Result.raw,
                SerialNumber           = snTask.Result.raw,
                SystemName             = GetStr(vars, 14),
                SystemDescription      = GetStr(vars, 13),
                Location               = GetStr(vars, 15),
                Contact                = GetStr(vars, 16),
                UptimeText             = FormatUptimeTicks(GetInt(vars, 17)),
                LastTestResultText     = FormatTestResult(lastTestTask.Result),
                SystemTime             = sysTimeTask.Result.raw,
                NextTestSchedule       = nextTestTask.Result.raw,
                ShutdownWarning        = shutdownWarnTask.Result.raw,
                DailyReportEmail       = FormatYesNo(dailyReportTask.Result.raw),

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

    // Python netagent_gui_v10 read_metric / read_info_item paritesi.
    public async Task<List<MetricDetail>> GetMetricsDetailAsync()
    {
        var output = new List<MetricDetail>();
        if (!_connection.IsConfigured) return output;

        var (ep, read, _) = Resolve();

        // Tek-OID metric tanımı: format'a göre değer üretir
        var defs = new (string Key, string Title, string Group, string[] Oids, Func<string, string> Format)[]
        {
            // live
            ("input_voltage",      "Giriş Voltajı",        "live",         new[]{Oids.InputVoltage},          FmtScaled10("V")),
            ("input_frequency",    "Giriş Frekansı",       "live",         new[]{Oids.InputFrequency},        FmtScaled10("Hz")),
            ("output_voltage",     "Çıkış Voltajı",        "live",         new[]{Oids.OutputVoltage},         FmtScaled10("V")),
            ("output_frequency",   "Çıkış Frekansı",       "live",         new[]{Oids.OutputFrequency},       FmtScaled10("Hz")),
            ("load_percent",       "Yük Oranı",            "live",         new[]{Oids.OutputLoadPercent},     FmtPlain("%")),
            ("battery_capacity",   "Batarya Kapasitesi",   "live",         new[]{Oids.BatteryCapacityPercent},FmtPlain("%")),
            ("remaining_time",     "Kalan Süre",           "live",         new[]{Oids.BatteryRemainingSec},   FmtPlain("sn")),
            ("battery_voltage",    "Batarya V/cell",       "live",         new[]{Oids.BatteryVoltagePerCell}, FmtScaled10("V/cell")),
            ("ups_temperature",    "UPS Sıcaklığı",        "live",         new[]{Oids.BatteryTemperature},    FmtScaled10("°C")),
            ("ups_status",         "UPS Durumu",           "live",         new[]{Oids.OutputSource},          FmtNetAgentStatus),
            ("battery_status",     "Batarya Durumu",       "live",         new[]{Oids.BatteryStatus},         FmtBatteryStatus),

            // f-equivalent
            ("f_rated_output_voltage",   "F: Nominal Çıkış Voltajı",      "f-equivalent", new[]{Oids.NominalOutputVoltage},  FmtScaled10("V")),
            ("f_battery_nominal_voltage","F: Nominal Batarya Voltajı",    "f-equivalent", new[]{Oids.NominalBatteryVoltage}, FmtScaled10("V")),
            ("f_rated_frequency",        "F: Nominal Frekans",            "f-equivalent", new[]{Oids.OutputFrequency, Oids.InputFrequency}, FmtScaled10("Hz")),

            // info
            ("hardware_version",       "Donanım Versiyonu",   "info", Oids.HardwareVersionOids,   FmtText),
            ("agent_software_version", "Aygıt Yazılım",       "info", Oids.FirmwareVersionOids,   FmtText),
            ("serial_number",          "Seri Numarası",       "info", Oids.SerialNumberOids,      FmtText),
            ("system_description",     "Sistem Açıklaması",   "info", new[]{Oids.SysDescr},       FmtText),
            ("system_name",            "Sistem İsmi",         "info", new[]{Oids.SysName},        FmtText),
            ("system_contact",         "Sistem Bağlantı",     "info", new[]{Oids.SysContact},     FmtText),
            ("location",               "Konum",               "info", new[]{Oids.SysLocation},    FmtText),
            ("uptime",                 "Çalışma Zamanı",      "info", new[]{Oids.SysUptime},      raw => FormatUptimeTicks(int.TryParse(raw, out var v) ? v : 0)),
            ("system_time",            "Sistem Saati",        "info", Oids.SystemTimeOids,        FmtText),
            ("last_ups_test",          "Son UPS Testi",       "info", Oids.LastTestResultOids,    raw => FormatTestResult(int.TryParse(raw, out var v) ? v : 0)),
            ("next_ups_test",          "Bir Sonraki UPS Testi","info", Oids.NextTestScheduleOids, FmtText),
            ("critical_load",          "UPS Kritik Yükü",     "info", Oids.CriticalLoadOids,      FmtPlain("%")),
            ("critical_temperature",   "UPS Kritik Sıcaklığı","info", Oids.CriticalTemperatureOids,FmtScaled10("°C")),
            ("critical_capacity",      "UPS Kritik Kapasitesi","info",Oids.CriticalCapacityOids,  FmtPlain("%")),
            ("shutdown_warning",       "Kapanma Uyarısı",     "info", Oids.ShutdownWarningOids,   FmtText),
            ("daily_report_email",     "Günlük Rapor Email",  "info", Oids.DailyReportEmailOids,  raw => FormatYesNo(raw)),
        };

        var tasks = defs.Select(async d =>
        {
            var r = await TryReadFirstWithOidAsync(ep, read, d.Oids);
            return new MetricDetail
            {
                Key = d.Key,
                Title = d.Title,
                Group = d.Group,
                ValueText = r.ok ? d.Format(r.raw) : "OKUNAMADI",
                RawValue = r.raw,
                Oid = r.oid,
                Ok = r.ok,
                Error = r.error
            };
        });

        var results = await Task.WhenAll(tasks);
        output.AddRange(results);
        return output;
    }

    // --- Format helpers ---
    private static Func<string, string> FmtScaled10(string unit) => raw =>
    {
        if (!double.TryParse(raw, out var v)) return raw;
        return $"{(v / 10.0):F1} {unit}".Trim();
    };
    private static Func<string, string> FmtPlain(string unit) => raw =>
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        return string.IsNullOrEmpty(unit) ? raw : $"{raw} {unit}";
    };
    private static string FmtText(string raw) => raw;
    private static string FmtNetAgentStatus(string raw)
    {
        if (!int.TryParse(raw, out var c)) return raw;
        return c switch
        {
            2 => "Online", 3 => "Aküde", 4 => "Boost", 5 => "Sleep",
            6 => "Bypass", 7 => "Rebooting", 8 => "Standby", 9 => "Buck",
            _ => $"Bilinmiyor ({c})"
        };
    }
    private static string FmtBatteryStatus(string raw)
    {
        if (!int.TryParse(raw, out var c)) return raw;
        return c switch { 2 => "Normal", 3 => "Düşük", _ => $"Bilinmiyor ({c})" };
    }

    // Python snmp_control_diagnostic paritesi
    public async Task<DiagnosticResult> DiagnoseAsync()
    {
        var result = new DiagnosticResult();

        if (!_connection.IsConfigured)
        {
            result.Lines.Add(new DiagnosticLine
            {
                Title = "Bağlantı kontrolü", Oid = "-", Ok = false,
                Error = "Önce bağlantı yapılandırılmalı."
            });
            return result;
        }

        var (ep, read, write) = Resolve();
        const string sysName = Oids.SysName;
        const string shutdownOid = Oids.ShutdownCmd;

        var tests = new (string Title, OctetString Community, string Oid)[]
        {
            ("Read community ile sysName okuma",  read,  sysName),
            ("Write community ile sysName okuma", write, sysName),
            ("Read community ile shutdown OID okuma",  read,  shutdownOid),
            ("Write community ile shutdown OID okuma", write, shutdownOid),
        };

        foreach (var t in tests)
        {
            try
            {
                var vars = await GetAsync(ep, t.Community, t.Oid);
                var data = vars[0].Data;
                if (IsAbsent(data))
                {
                    result.Lines.Add(new DiagnosticLine { Title = t.Title, Oid = t.Oid, Ok = false, Error = "noSuchObject" });
                }
                else
                {
                    result.Lines.Add(new DiagnosticLine { Title = t.Title, Oid = t.Oid, Ok = true, Value = data.ToString() });
                }
            }
            catch (Exception ex)
            {
                result.Lines.Add(new DiagnosticLine { Title = t.Title, Oid = t.Oid, Ok = false, Error = ex.Message });
            }
        }

        result.Hints.AddRange(new[]
        {
            "Read community OK ama Write community timeout veriyorsa: NetAgent üzerinde write community / ACL / Manager IP ayarı yanlıştır.",
            "Shutdown OID 'No Such' veriyorsa: bu model/firmware bu OID'yi desteklemiyor olabilir.",
            "Hepsi OK ama SET timeout veriyorsa: SNMP SET yetkisi kapalı veya UPS bu komutu reddediyor olabilir."
        });

        return result;
    }

    private void EnsureWriteCommunity()
    {
        if (string.IsNullOrWhiteSpace(_connection.WriteCommunity))
            throw new InvalidOperationException(
                "Write community tanımlı değil. Bağlantı çubuğundan 'Write' alanını doldurup yeniden Bağlan'a basın (genelde 'private').");
    }

    private static string TranslateSnmpException(Exception ex, string oid) => ex switch
    {
        Lextm.SharpSnmpLib.Messaging.TimeoutException =>
            $"UPS yanıt vermedi (timeout). OID: {oid}. " +
            "Olası sebepler: write community yanlış, NetAgent ACL/Manager IP listesi sınırlı, " +
            "veya cihaz bu komutu desteklemiyor. 'SET Yetki Testi'ni çalıştırın.",
        OperationCanceledException =>
            $"İstek zaman aşımına uğradı (timeout). OID: {oid}. Write community ve ACL ayarlarını kontrol edin.",
        Lextm.SharpSnmpLib.Messaging.ErrorException errEx =>
            $"SNMP cihazı SET'i reddetti: {errEx.Message}. OID: {oid}. " +
            "OID değer aralığı veya yazma izni uyumsuz olabilir.",
        _ => $"SNMP SET başarısız: {ex.Message}. OID: {oid}."
    };

    public async Task SetIntAsync(string oid, int value)
    {
        EnsureWriteCommunity();
        var (ep, _, write) = Resolve();

        // SET için GET'ten daha cömert timeout (Python tool 6 sn + 2 retry kullanıyor)
        var timeoutMs = Math.Max(_settings.TimeoutMs * 2, 6000);
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var variables = new List<Variable> { new(new ObjectIdentifier(oid), new Integer32(value)) };
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                await Messenger.SetAsync(VersionCode.V2, ep, write, variables, cts.Token);
                return;
            }
            catch (Lextm.SharpSnmpLib.Messaging.TimeoutException) when (attempt < maxAttempts)
            {
                _logger.LogWarning("SNMP SET timeout (deneme {Attempt}/{Max}) OID {Oid}", attempt, maxAttempts, oid);
            }
            catch (OperationCanceledException) when (attempt < maxAttempts)
            {
                _logger.LogWarning("SNMP SET iptal (deneme {Attempt}/{Max}) OID {Oid}", attempt, maxAttempts, oid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SNMP SET int failed for OID {Oid} (deneme {Attempt})", oid, attempt);
                throw new InvalidOperationException(TranslateSnmpException(ex, oid), ex);
            }
        }
    }

    public async Task SetStringAsync(string oid, string value)
    {
        EnsureWriteCommunity();
        var (ep, _, write) = Resolve();
        try
        {
            var variables = new List<Variable> { new(new ObjectIdentifier(oid), new OctetString(value)) };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(Math.Max(_settings.TimeoutMs * 2, 6000)));
            await Messenger.SetAsync(VersionCode.V2, ep, write, variables, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP SET string failed for OID {Oid}", oid);
            throw new InvalidOperationException(TranslateSnmpException(ex, oid), ex);
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
            return new RawOidResult { Oid = oid, Type = data.GetType().Name, Value = data.ToString() ?? string.Empty };
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
            await Messenger.WalkAsync(VersionCode.V2, ep, read, new ObjectIdentifier(startOid), walked,
                withinSubtree ? WalkMode.WithinSubtree : WalkMode.Default, cts.Token);

            foreach (var v in walked)
            {
                results.Add(new RawOidResult
                {
                    Oid = v.Id.ToString(),
                    Type = v.Data.GetType().Name,
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
