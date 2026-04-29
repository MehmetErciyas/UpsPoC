// UpsPoC.Api/Models/UpsCommand.cs
namespace UpsPoC.Api.Models;

public class UpsCommand
{
    public string CommandName { get; set; } = string.Empty;
    public int? IntValue { get; set; }
    public string? StringValue { get; set; }
}
