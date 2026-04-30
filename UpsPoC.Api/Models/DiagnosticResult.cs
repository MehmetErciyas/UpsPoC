// UpsPoC.Api/Models/DiagnosticResult.cs
namespace UpsPoC.Api.Models;

public class DiagnosticLine
{
    public string Title { get; set; } = string.Empty;
    public string Oid { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public string? Value { get; set; }
    public string? Error { get; set; }
}

public class DiagnosticResult
{
    public List<DiagnosticLine> Lines { get; set; } = new();
    public List<string> Hints { get; set; } = new();
}

public class CustomSnmpSetRequest
{
    public string Oid { get; set; } = string.Empty;
    public int Value { get; set; }
}
