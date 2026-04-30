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
    public string WriteCommunity { get; set; } = string.Empty;
    public int TimeoutMs { get; set; } = 3000;
    public int DefaultPollingIntervalSeconds { get; set; } = 5;

    // Otomatik akü adedi tespiti (nominal batarya voltajı / 12) başarısız olursa kullanılır.
    public int FallbackBatteryBlockCount { get; set; } = 3;
}

public class AuthSettings
{
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
}
