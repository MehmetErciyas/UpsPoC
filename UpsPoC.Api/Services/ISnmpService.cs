// UpsPoC.Api/Services/ISnmpService.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface ISnmpService
{
    Task<UpsStatus> GetStatusAsync();
    Task<UpsConfig> GetConfigAsync();
    Task SetIntAsync(string oid, int value);
    Task SetStringAsync(string oid, string value);
    Task RunBatteryTestAsync();
}
