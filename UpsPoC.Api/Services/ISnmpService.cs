// UpsPoC.Api/Services/ISnmpService.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface ISnmpService
{
    Task<UpsStatus> GetStatusAsync();
    Task<UpsConfig> GetConfigAsync();
    Task<List<MetricDetail>> GetMetricsDetailAsync();
    Task<DiagnosticResult> DiagnoseAsync();
    Task SetIntAsync(string oid, int value);
    Task SetStringAsync(string oid, string value);
    Task RebootAsync();
    Task ShutdownAsync();
    Task RunBatteryTestAsync();
    Task<List<RawOidResult>> WalkAsync(string startOid, bool withinSubtree = true);
    Task<RawOidResult> GetRawAsync(string oid);
}
