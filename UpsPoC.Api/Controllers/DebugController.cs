// UpsPoC.Api/Controllers/DebugController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UpsPoC.Api.Services;

namespace UpsPoC.Api.Controllers;

[ApiController]
[Route("api/debug")]
[Authorize]
public class DebugController : ControllerBase
{
    private readonly ISnmpService _snmp;

    public DebugController(ISnmpService snmp)
    {
        _snmp = snmp;
    }

    /// <summary>
    /// Walk an OID subtree to discover what the device supports.
    /// Örnek: GET /api/debug/walk?oid=1.3.6.1.2.1.33
    /// </summary>
    [HttpGet("walk")]
    public async Task<IActionResult> Walk([FromQuery] string oid = "1.3.6.1.2.1.33", [FromQuery] bool withinSubtree = true)
    {
        var results = await _snmp.WalkAsync(oid, withinSubtree);
        return Ok(results);
    }

    /// <summary>
    /// Get raw value of a single OID.
    /// Örnek: GET /api/debug/get?oid=1.3.6.1.2.1.1.1.0
    /// </summary>
    [HttpGet("get")]
    public async Task<IActionResult> GetRaw([FromQuery] string oid)
    {
        var result = await _snmp.GetRawAsync(oid);
        return Ok(result);
    }
}
