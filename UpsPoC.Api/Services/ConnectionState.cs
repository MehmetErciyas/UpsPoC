// UpsPoC.Api/Services/ConnectionState.cs
using System.Net;
using Microsoft.Extensions.Options;
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public class ConnectionState : IConnectionState
{
    private readonly object _lock = new();
    private string _host = string.Empty;
    private int _port = 161;
    private string _readCommunity = "public";
    private string _writeCommunity = string.Empty;
    private int? _manualBatteryBlockCount;
    private bool _isConfigured;

    public ConnectionState(IOptions<AppSettings> options)
    {
        var ups = options.Value.Ups;
        if (!string.IsNullOrWhiteSpace(ups.Host) && IPAddress.TryParse(ups.Host, out _))
        {
            _host = ups.Host;
            _port = ups.Port > 0 ? ups.Port : 161;
            _readCommunity = string.IsNullOrWhiteSpace(ups.ReadCommunity) ? "public" : ups.ReadCommunity;
            _writeCommunity = ups.WriteCommunity ?? string.Empty;
            _isConfigured = true;
        }
    }

    public bool IsConfigured { get { lock (_lock) return _isConfigured; } }
    public string Host { get { lock (_lock) return _host; } }
    public int Port { get { lock (_lock) return _port; } }
    public string ReadCommunity { get { lock (_lock) return _readCommunity; } }
    public string WriteCommunity { get { lock (_lock) return _writeCommunity; } }
    public int? ManualBatteryBlockCount { get { lock (_lock) return _manualBatteryBlockCount; } }

    public void Update(string host, int port, string readCommunity, string? writeCommunity, int? manualBatteryBlockCount)
    {
        if (string.IsNullOrWhiteSpace(host) || !IPAddress.TryParse(host, out _))
            throw new ArgumentException("Geçerli bir IP adresi gerekli.", nameof(host));
        if (port is <= 0 or > 65535)
            throw new ArgumentException("Port 1-65535 aralığında olmalıdır.", nameof(port));
        if (string.IsNullOrWhiteSpace(readCommunity))
            throw new ArgumentException("Read community boş olamaz.", nameof(readCommunity));
        if (manualBatteryBlockCount.HasValue && manualBatteryBlockCount.Value is not (>= 1 and <= 80))
            throw new ArgumentException("Akü adedi 1-80 aralığında olmalıdır.", nameof(manualBatteryBlockCount));

        lock (_lock)
        {
            _host = host;
            _port = port;
            _readCommunity = readCommunity;
            _writeCommunity = writeCommunity ?? string.Empty;
            _manualBatteryBlockCount = manualBatteryBlockCount;
            _isConfigured = true;
        }
    }

    public void Clear()
    {
        lock (_lock) _isConfigured = false;
    }

    public UpsConnectionInfo Snapshot()
    {
        lock (_lock)
        {
            return new UpsConnectionInfo
            {
                Host = _host,
                Port = _port,
                ReadCommunity = _readCommunity,
                HasWriteCommunity = !string.IsNullOrEmpty(_writeCommunity),
                IsConfigured = _isConfigured,
                ManualBatteryBlockCount = _manualBatteryBlockCount
            };
        }
    }
}
