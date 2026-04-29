// UpsPoC.Api/Models/AppSettings.cs
namespace UpsPoC.Api.Models;

public class AppSettings
{
    public UpsSettings Ups { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
}

public class UpsSettings
{
    public string Host { get; set; } = "192.168.143.246";
    public int Port { get; set; } = 161;
    public string ReadCommunity { get; set; } = "public";
    public string WriteCommunity { get; set; } = "private";
    public int TimeoutMs { get; set; } = 3000;
    public int DefaultPollingIntervalSeconds { get; set; } = 5;
}

public class AuthSettings
{
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
}
