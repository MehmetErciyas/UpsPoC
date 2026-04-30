// UpsPoC.Api/Controllers/UpsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsPoC.Api.Models;
using UpsPoC.Api.Services;

namespace UpsPoC.Api.Controllers;

[ApiController]
[Route("api/ups")]
[Authorize]
public class UpsController : ControllerBase
{
    private readonly IUpsDataService _upsData;
    private readonly ISnmpService _snmp;
    private readonly IConnectionState _connection;

    public UpsController(IUpsDataService upsData, ISnmpService snmp, IConnectionState connection)
    {
        _upsData = upsData;
        _snmp = snmp;
        _connection = connection;
    }

    [HttpGet("connection")]
    public IActionResult GetConnection() => Ok(_connection.Snapshot());

    [HttpPost("connection")]
    public IActionResult SetConnection([FromBody] ConnectionRequest req)
    {
        try
        {
            _connection.Update(req.Host, req.Port, req.ReadCommunity, req.WriteCommunity);
            return Ok(_connection.Snapshot());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("connection")]
    public IActionResult ClearConnection()
    {
        _connection.Clear();
        return Ok(_connection.Snapshot());
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _snmp.GetStatusAsync();
        _upsData.UpdateStatus(status);
        return Ok(status);
    }

    [HttpGet("history")]
    public IActionResult GetHistory() => Ok(_upsData.GetHistory());

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        try
        {
            var config = await _snmp.GetConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = ex.Message });
        }
    }

    // Bu cihazda yazılabilir config OID'leri doğrulanmadı; SET denemesi sessiz başarısız olabilir.
    [HttpPost("config")]
    public IActionResult SetConfig([FromBody] UpsConfig _)
        => StatusCode(501, new { error = "Konfigürasyon yazma bu cihaz için desteklenmiyor (OID'ler doğrulanmadı)." });

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] UpsCommand command)
    {
        try
        {
            switch (command.CommandName)
            {
                case "reboot":
                    await _snmp.RebootAsync();
                    break;
                case "shutdown":
                    await _snmp.ShutdownAsync();
                    break;

                // Aşağıdakilerin OID'leri bu cihazda doğrulanmadı.
                case "shutdown-type":
                case "shutdown-after-delay":
                case "startup-after-delay":
                case "abort-shutdown":
                case "abort-startup":
                case "auto-restart":
                case "battery-test":
                case "audible-alarm":
                case "set-name":
                    return StatusCode(501, new
                    {
                        error = $"'{command.CommandName}' komutu bu cihaz için desteklenmiyor."
                    });

                default:
                    return BadRequest(new { error = $"Bilinmeyen komut: {command.CommandName}" });
            }

            return Ok(new { success = true, command = command.CommandName });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = $"Komut gönderilemedi: {ex.Message}" });
        }
    }
}
