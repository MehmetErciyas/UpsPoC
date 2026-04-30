// UpsPoC.Api/Models/ConnectionRequest.cs
namespace UpsPoC.Api.Models;

public class ConnectionRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 161;
    public string ReadCommunity { get; set; } = "public";
    public string? WriteCommunity { get; set; }
}

public class UpsConnectionInfo
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string ReadCommunity { get; set; } = string.Empty;
    public bool HasWriteCommunity { get; set; }
    public bool IsConfigured { get; set; }
}
