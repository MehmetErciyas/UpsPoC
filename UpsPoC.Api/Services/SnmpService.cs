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
    private readonly IPEndPoint _endpoint;
    private readonly OctetString _readCommunity;
    private readonly OctetString _writeCommunity;

    // Powerware/MAKELSAN NetAgent IX vendor OIDs (enterprise 935)
    private static class Oids
    {
        public const string UpsStatus             = "1.3.6.1.4.1.935.1.1.1.4.1.1.0";

        public const string BatteryCapacityPercent = "1.3.6.1.4.1.935.1.1.1.2.2.1.0";
        public const string BatteryVoltage        = "1.3.6.1.4.1.935.1.1.1.2.2.2.0";

        public const string InputVoltage          = "1.3.6.1.4.1.935.1.1.1.3.2.1.0";

        public const string OutputVoltage         = "1.3.6.1.4.1.935.1.1.1.4.2.1.0";
        public const string OutputLoadPercent     = "1.3.6.1.4.1.935.1.1.1.4.2.3.0";
    }

    public SnmpService(IOptions<AppSettings> options, ILogger<SnmpService> logger)
    {
        _settings = options.Value.Ups;
        _logger = logger;
        _endpoint = new IPEndPoint(IPAddress.Parse(_settings.Host), _settings.Port);
        _readCommunity = new OctetString(_settings.ReadCommunity);
        _writeCommunity = new OctetString(_settings.WriteCommunity);
    }

    // Messenger.GetAsync/SetAsync in SharpSnmpLib 12.5.7 accept a CancellationToken (not an int timeout).
    // We create a CancellationTokenSource from TimeoutMs to honour the configured timeout.
    private async Task<IList<Variable>> GetAsync(params string[] oids)
    {
        var variables = oids.Select(o => new Variable(new ObjectIdentifier(o))).ToList();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
        return await Messenger.GetAsync(
            VersionCode.V2,
            _endpoint,
            _readCommunity,
            variables,
            cts.Token);
    }

    private int GetInt(IList<Variable> vars, int index)
    {
        var data = vars[index].Data;
        if (data is NoSuchInstance or NoSuchObject) return 0;
        return int.TryParse(data.ToString(), out var result) ? result : 0;
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
                Oids.UpsStatus,
                Oids.BatteryCapacityPercent,
                Oids.BatteryVoltage,
                Oids.InputVoltage,
                Oids.OutputVoltage,
                Oids.OutputLoadPercent);

            return new UpsStatus
            {
                OutputSource           = GetInt(vars, 0),  // 1=unknown,2=online,3=battery,4=boost,5=sleep,6=bypass,7=rebooting,8=standby,9=buck
                BatteryCapacityPercent = GetInt(vars, 1),
                BatteryVoltage         = GetInt(vars, 2) / 10.0,
                InputVoltage           = GetInt(vars, 3) / 10.0,
                OutputVoltage          = GetInt(vars, 4) / 10.0,
                OutputLoadPercent      = GetInt(vars, 5),
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
        // Config OIDs not yet discovered for this device — return empty config
        return await Task.FromResult(new UpsConfig());
    }

    public async Task SetIntAsync(string oid, int value)
    {
        try
        {
            var variables = new List<Variable>
            {
                new(new ObjectIdentifier(oid), new Integer32(value))
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            await Messenger.SetAsync(
                VersionCode.V2,
                _endpoint,
                _writeCommunity,
                variables,
                cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP SET int failed for OID {Oid}", oid);
            throw;
        }
    }

    public async Task SetStringAsync(string oid, string value)
    {
        try
        {
            var variables = new List<Variable>
            {
                new(new ObjectIdentifier(oid), new OctetString(value))
            };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            await Messenger.SetAsync(
                VersionCode.V2,
                _endpoint,
                _writeCommunity,
                variables,
                cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNMP SET string failed for OID {Oid}", oid);
            throw;
        }
    }

    public Task RunBatteryTestAsync()
    {
        throw new NotSupportedException("Battery test OID not yet discovered for this device.");
    }

    public async Task<RawOidResult> GetRawAsync(string oid)
    {
        try
        {
            var variables = new List<Variable> { new(new ObjectIdentifier(oid)) };
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_settings.TimeoutMs));
            var result = await Messenger.GetAsync(VersionCode.V2, _endpoint, _readCommunity, variables, cts.Token);
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
            var walked = new List<Variable>();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30000));
            await Messenger.WalkAsync(
                VersionCode.V2,
                _endpoint,
                _readCommunity,
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
