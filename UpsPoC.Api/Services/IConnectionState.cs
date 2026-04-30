// UpsPoC.Api/Services/IConnectionState.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface IConnectionState
{
    bool IsConfigured { get; }
    string Host { get; }
    int Port { get; }
    string ReadCommunity { get; }
    string WriteCommunity { get; }

    void Update(string host, int port, string readCommunity, string? writeCommunity);
    void Clear();
    UpsConnectionInfo Snapshot();
}
