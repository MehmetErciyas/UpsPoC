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

    public UpsController(IUpsDataService upsData, ISnmpService snmp)
    {
        _upsData = upsData;
        _snmp = snmp;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _snmp.GetStatusAsync();
        // Update in-memory history with fresh reading (polling side effect on GET)
        _upsData.UpdateStatus(status);
        return Ok(status);
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        return Ok(_upsData.GetHistory());
    }

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

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] UpsCommand command)
    {
        try
        {
            // Validate IntValue ranges for commands that have known valid values
            if (command.CommandName == "shutdown-type" && command.IntValue.HasValue
                && command.IntValue is not (1 or 2))
                return BadRequest(new { error = "shutdown-type değeri 1 (output) veya 2 (sistem) olmalıdır." });

            if (command.CommandName == "audible-alarm" && command.IntValue.HasValue
                && command.IntValue is not (1 or 2 or 3))
                return BadRequest(new { error = "audible-alarm değeri 1 (kapalı), 2 (açık) veya 3 (geçici sessiz) olmalıdır." });

            if (command.CommandName == "auto-restart" && command.IntValue.HasValue
                && command.IntValue is not (1 or 2))
                return BadRequest(new { error = "auto-restart değeri 1 (açık) veya 2 (kapalı) olmalıdır." });

            if (command.CommandName == "reboot" && command.IntValue.HasValue
                && (command.IntValue < 0 || command.IntValue > 300))
                return BadRequest(new { error = "reboot gecikmesi 0-300 saniye arasında olmalıdır." });

            switch (command.CommandName)
            {
                case "shutdown-type":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.1.0", command.IntValue ?? 1);
                    break;
                case "shutdown-after-delay":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.2.0", command.IntValue ?? 60);
                    break;
                case "startup-after-delay":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.3.0", command.IntValue ?? 60);
                    break;
                case "reboot":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.4.0", command.IntValue ?? 60);
                    break;
                case "abort-shutdown":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.2.0", -1);
                    break;
                case "abort-startup":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.3.0", -1);
                    break;
                case "auto-restart":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.8.5.0", command.IntValue ?? 1);
                    break;
                case "battery-test":
                    await _snmp.RunBatteryTestAsync();
                    break;
                case "audible-alarm":
                    await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.8.0", command.IntValue ?? 2);
                    break;
                case "set-name":
                    if (!string.IsNullOrWhiteSpace(command.StringValue))
                        await _snmp.SetStringAsync("1.3.6.1.2.1.33.1.1.5.0", command.StringValue[..Math.Min(63, command.StringValue.Length)]);
                    break;
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

    [HttpPost("config")]
    public async Task<IActionResult> SetConfig([FromBody] UpsConfig config)
    {
        try
        {
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.1.0", config.InputVoltageNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.2.0", config.InputFreqNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.3.0", config.OutputVoltageNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.4.0", config.OutputFreqNominal);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.7.0", config.LowBatteryMinutes);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.8.0", config.AudibleStatus);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.9.0", config.LowVoltageTransferPoint);
            await _snmp.SetIntAsync("1.3.6.1.2.1.33.1.9.10.0", config.HighVoltageTransferPoint);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { error = $"Konfigürasyon kaydedilemedi: {ex.Message}" });
        }
    }
}
