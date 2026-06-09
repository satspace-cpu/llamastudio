using LlamaStudio.Core.Models;

namespace LlamaStudio.Core.Interfaces;

public interface IServerManager
{
    event EventHandler<ServerStatus>? StatusChanged;
    event EventHandler<LogEntry>? LogReceived;
    Task<ServerStatus> GetStatusAsync();
    Task StartAsync(ServerProfile profile, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<ServerStatus> HealthCheckAsync(string host = "127.0.0.1", int port = 8080);
    void AttachExternalServer(string host, int port);
}
