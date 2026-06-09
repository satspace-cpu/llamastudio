using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LlamaStudio.Infrastructure.Llama;

public class ServerManager : IServerManager, IDisposable
{
    readonly ILogService _log;
    readonly ISettings _settings;
    Process? _process;
    CancellationTokenSource? _cts;
    CancellationTokenSource? _healthCts;
    readonly object _lock = new();
    DateTime? _startedAt;
    ServerStatus _status = new() { State = ServerState.Stopped };
    string? _lastHost;
    int _lastPort;

    // TPS tracking from stdout parsing
    double _currentTps;
    DateTime _lastTpsUpdate = DateTime.UtcNow;
    int _tickCounter;
    // llama.cpp outputs: "n_decoded = 102, tg = 83.73 t/s" (interim) and "95.90 tokens per second" (final)
    static readonly Regex s_tpsRegexTg = new(@"tg\s*=\s*([\d.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex s_tpsRegexEval = new(@"([\d.]+)\s+tokens?\s+per\s+second", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Shared HttpClient to avoid socket exhaustion
    static readonly HttpClient s_sharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public event EventHandler<ServerStatus>? StatusChanged;
    public event EventHandler<LogEntry>? LogReceived;

    public ServerManager(ILogService log, ISettings settings)
    {
        _log = log;
        _settings = settings;
    }

    // Normalize wildcard bind addresses to localhost for client connections
    static string NormalizeHostForClient(string host) =>
        host == "0.0.0.0" || host == "::" ? "127.0.0.1" : host;

    async Task<ServerStatus> IServerManager.GetStatusAsync()
    {
        ServerStatus s;
        bool hasInternalProcess;
        lock (_lock)
        {
            hasInternalProcess = _process != null && !_process.HasExited;
            s = new ServerStatus
            {
                State = _status.State,
                Port = _status.Port,
                Host = _status.Host,
                ModelName = _status.ModelName,
                ContextSize = _status.ContextSize,
                Threads = _status.Threads,
                GpuLayers = _status.GpuLayers,
                VramUsedGb = _status.VramUsedGb,
                RamUsedGb = _status.RamUsedGb,
                TokensPerSecond = _status.TokensPerSecond,
                QueueSize = _status.QueueSize,
                ActiveSlots = _status.ActiveSlots,
                TotalTokensProcessed = _status.TotalTokensProcessed,
                Uptime = _startedAt.HasValue ? DateTime.Now - _startedAt.Value : TimeSpan.Zero,
                StartedAt = _startedAt,
                ErrorMessage = _status.ErrorMessage,
                ProcessId = _process?.Id
            };
        }

        // If no internal process but external llama-server might be running (KeepServerOnExit),
        // do a health check to get actual status
        if (!hasInternalProcess && s.State == ServerState.Stopped)
        {
            try
            {
                var externalProcs = Process.GetProcessesByName("llama-server")
                    .Where(p => !p.HasExited).ToList();

                if (externalProcs.Count > 0)
                {
                    // Attach to external process for RAM monitoring
                    lock (_lock)
                    {
                        _process = externalProcs[0];
                    }

                    // Try default settings port, then common ports (normalize for client connection)
                    var host = NormalizeHostForClient(_settings.DefaultHost ?? "127.0.0.1");
                    var port = _settings.DefaultPort > 0 ? _settings.DefaultPort : 8080;

                    var health = await HealthCheckInternalAsync(host, port);
                    if (health.State == ServerState.Running)
                    {
                        // Update internal state to reflect reality
                        lock (_lock)
                        {
                            _status.State = ServerState.Running;
                            _status.Port = health.Port;
                            _status.Host = health.Host;
                            _status.ModelName = health.ModelName;
                            _status.ContextSize = health.ContextSize;
                            _status.Threads = health.Threads;
                            _status.GpuLayers = health.GpuLayers;
                        }
                        RaiseStatusChanged();

                        // Start health polling for external server to get RAM/TPS
                        _healthCts?.Cancel();
                        _healthCts?.Dispose();
                        StartHealthPolling(host, port);

                        return new ServerStatus
                        {
                            State = ServerState.Running,
                            Port = health.Port,
                            Host = health.Host,
                            ModelName = health.ModelName,
                            ContextSize = health.ContextSize,
                            Threads = health.Threads,
                            GpuLayers = health.GpuLayers,
                            VramUsedGb = 0,
                            RamUsedGb = 0,
                            TokensPerSecond = 0,
                            QueueSize = 0,
                            ActiveSlots = health.ActiveSlots,
                            TotalTokensProcessed = 0,
                            Uptime = TimeSpan.Zero,
                            StartedAt = null,
                            ErrorMessage = null,
                            ProcessId = externalProcs[0].Id
                        };
                    }
                }
            }
            catch { /* Ignore — return internal state */ }
        }

        await Task.CompletedTask;
        return s;
    }

    async Task IServerManager.StartAsync(ServerProfile profile, CancellationToken cancellationToken)
    {
        if (_process != null && !_process.HasExited)
        {
            _log.Warning("Server already running", "ServerManager");
            return;
        }

        lock (_lock)
        {
            _status = new ServerStatus
            {
                State = ServerState.Starting,
                Port = profile.Port,
                Host = profile.Host,
                ModelName = Path.GetFileName(profile.ModelPath ?? string.Empty)
            };
        }

        RaiseStatusChanged();
        _log.Information($"Starting server with profile: {profile.Name}", "ServerManager");

        try
        {
            var exePath = ResolveExecutablePath(_settings.LlamaCppDirectory, _settings.ActiveLlamaCppVersion);
            if (exePath == null)
            {
                var errorMsg = $"llama-server executable not found in: {_settings.LlamaCppDirectory}";
                _log.Error(errorMsg, "ServerManager");
                lock (_lock)
                {
                    _status.State = ServerState.Error;
                    _status.ErrorMessage = errorMsg;
                }
                RaiseStatusChanged();
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.ModelPath) &&
                string.IsNullOrWhiteSpace(profile.HfRepo) &&
                string.IsNullOrWhiteSpace(profile.HfFile))
            {
                var errorMsg = "Model path or Hugging Face repo/file is not specified in profile";
                _log.Error(errorMsg, "ServerManager");
                lock (_lock)
                {
                    _status.State = ServerState.Error;
                    _status.ErrorMessage = errorMsg;
                }
                RaiseStatusChanged();
                return;
            }

            if (!string.IsNullOrWhiteSpace(profile.ModelPath) && !File.Exists(profile.ModelPath))
            {
                var errorMsg = $"Model file not found: {profile.ModelPath}";
                _log.Error(errorMsg, "ServerManager");
                lock (_lock)
                {
                    _status.State = ServerState.Error;
                    _status.ErrorMessage = errorMsg;
                }
                RaiseStatusChanged();
                return;
            }

            // Check if port is already in use
            if (IsPortInUse(profile.Port))
            {
                var errorMsg = $"Port {profile.Port} is already in use. Choose a different port or stop the process using it.";
                _log.Error(errorMsg, "ServerManager");
                lock (_lock)
                {
                    _status.State = ServerState.Error;
                    _status.ErrorMessage = errorMsg;
                }
                RaiseStatusChanged();
                return;
            }

            var args = BuildServerArgs(profile);
            _log.Information($"Command: {exePath} {args}", "ServerManager");

            _cts = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? _settings.LlamaCppDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Environment =
                {
                    ["LLAMA_NO_COLOR"] = "1"
                }
            };

            _process = new Process { StartInfo = startInfo };
            _process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _log.ServerOutput(e.Data);
                    ParseTpsFromOutput(e.Data, this);
                }
            };
            _process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _log.ServerOutput(e.Data);
                    ParseTpsFromOutput(e.Data, this);
                }
            };
            _process.Exited += (s, e) =>
            {
                _log.Information("Server process exited", "ServerManager");
                lock (_lock)
                {
                    if (_status.State != ServerState.Stopped)
                    {
                        _status.State = ServerState.Stopped;
                        _startedAt = null;
                    }
                }
                RaiseStatusChanged();
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            if (!string.IsNullOrEmpty(profile.ProcessPriority) && profile.ProcessPriority != "Normal")
            {
                try
                {
                    _process.PriorityClass = (ProcessPriorityClass)Enum.Parse(typeof(ProcessPriorityClass), profile.ProcessPriority);
                }
                catch
                {
                    _log.Warning($"Invalid process priority: {profile.ProcessPriority}", "ServerManager");
                }
            }

            lock (_lock)
            {
                _status.State = ServerState.Running;
                _status.ProcessId = _process.Id;
                _startedAt = DateTime.Now;
            }

            RaiseStatusChanged();
            _log.Information($"Server started (PID: {_process.Id}, WorkingSet: {_process.WorkingSet64 / (1024.0 * 1024.0):F1} MB)", "ServerManager");

            // Save host/port for health checks and graceful shutdown
            _lastHost = profile.Host;
            _lastPort = profile.Port;

            // Reset TPS tracking for new server instance
            lock (_lock)
            {
                _currentTps = 0;
                _lastTpsUpdate = DateTime.UtcNow;
            }

            // Cancel previous health polling before starting new one
            _healthCts?.Cancel();
            _healthCts?.Dispose();
            StartHealthPolling(_lastHost, _lastPort);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to start server", "ServerManager");
            lock (_lock)
            {
                _status.State = ServerState.Error;
                _status.ErrorMessage = ex.Message;
            }
            RaiseStatusChanged();
        }
    }

    async Task IServerManager.StopAsync(CancellationToken cancellationToken)
    {
        if (_process != null && !_process.HasExited)
        {
            // Normal stop: we own the process
            lock (_lock)
            {
                _status.State = ServerState.Stopping;
            }
            RaiseStatusChanged();
            _log.Information("Stopping server...", "ServerManager");

            _healthCts?.Cancel();
            _healthCts?.Dispose();
            _healthCts = null;
            _cts?.Cancel();

            if (!_process.HasExited)
            {
                if (!_process.WaitForExit(5000))
                {
                    _log.Warning("Server did not stop gracefully, forcing kill", "ServerManager");
                    try { _process.Kill(true); _process.WaitForExit(5000); } catch { }
                }
                else
                {
                    _log.Information("Server stopped gracefully", "ServerManager");
                }
            }

            lock (_lock)
            {
                _status.State = ServerState.Stopped;
                _startedAt = null;
                // Don't reset TPS - preserve last known value for UI persistence
                _status.RamUsedGb = 0;
                _status.ActiveSlots = 0;
            }
            RaiseStatusChanged();
            return;
        }

        // External server (KeepServerOnExit) — find and kill llama-server.exe processes
        try
        {
            var externalProcs = Process.GetProcessesByName("llama-server")
                .Where(p => !p.HasExited).ToList();

            if (externalProcs.Count > 0)
            {
                lock (_lock)
                {
                    _status.State = ServerState.Stopping;
                }
                RaiseStatusChanged();
                _log.Information($"Stopping {externalProcs.Count} external llama-server process(es)", "ServerManager");

                foreach (var proc in externalProcs)
                {
                    try
                    {
                        proc.Kill(true);
                        proc.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, $"Failed to kill external server PID={proc.Id}", "ServerManager");
                    }
                }

                lock (_lock)
                {
                    _status.State = ServerState.Stopped;
                    _startedAt = null;
                }
                RaiseStatusChanged();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to stop external server", "ServerManager");
        }

        lock (_lock)
        {
            _status.State = ServerState.Stopped;
        }
        RaiseStatusChanged();
    }

    void IServerManager.AttachExternalServer(string host, int port)
    {
        // Try to find the external llama-server process for RAM monitoring
        Process? externalProc = null;
        try
        {
            var procs = Process.GetProcessesByName("llama-server")
                .Where(p =>
                {
                    try { return !p.HasExited; } catch { return false; }
                })
                .ToList();
            if (procs.Count > 0)
                externalProc = procs[0];
        }
        catch { /* Can't enumerate processes */ }

        lock (_lock)
        {
            _process = externalProc;
            _lastHost = host;
            _lastPort = port;
            _status.State = ServerState.Running;
            _status.Host = host;
            _status.Port = port;
            _status.ProcessId = externalProc?.Id;
        }
        RaiseStatusChanged();

        // Cancel existing health polling if any, then start fresh
        _healthCts?.Cancel();
        _healthCts?.Dispose();
        StartHealthPolling(host, port);

        _log.Information($"Attached to external server at {host}:{port} (PID: {externalProc?.Id ?? -1})", "ServerManager");
    }

    void StartHealthPolling(string host, int port)
    {
        // Normalize wildcard bind addresses for client connections
        host = NormalizeHostForClient(host);

 
        _healthCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_healthCts.Token.IsCancellationRequested && _cts?.Token.IsCancellationRequested != true)
                {
                    await Task.Delay(1500, _healthCts.Token);

                    try
                    {
                        var health = await HealthCheckInternalAsync(host, port);
                        if (health.State != ServerState.Running)
                            continue; // Server not ready yet, skip this tick

// Get active slots + TPS from /slots API
                        var (activeSlots, apiGenTps, apiPromptTps) = await FetchActiveSlotsAsync(host, port);

                       // Prefer API TPS; fall back to stdout parsing
                          double tpsFromStdout;
                          lock (_lock)
                          {
                              tpsFromStdout = _currentTps;
                          }
                        double finalGenTps = apiGenTps > 0 ? apiGenTps : tpsFromStdout;

                        // Get RAM from process — try owned process first, then fallback to finding by name
                        double ramGb = GetProcessRamGb();

                        lock (_lock)
                        {
                            // Persist last known TPS when idle instead of resetting to 0
        _status.TokensPerSecond = finalGenTps > 0 ? finalGenTps : _status.TokensPerSecond;
                            _status.PromptTokensPerSecond = apiPromptTps > 0 ? apiPromptTps : _status.PromptTokensPerSecond;
                            _status.ActiveSlots = activeSlots;
                            _status.ModelName = health.ModelName;
                            _status.ContextSize = health.ContextSize;
                            _status.RamUsedGb = ramGb;
                        }

                        _tickCounter++;
                        RaiseStatusChanged();
                    }
                    catch (Exception ex)
                    {
                        _log.Debug($"Health poll error: {ex.Message}", "ServerManager");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, _healthCts.Token);
    }

    double GetProcessRamGb()
    {
        Process? target = null;

        // Try owned process first
        lock (_lock) { target = _process; }

        // Fallback: find llama-server by name if owned process is unavailable
        if (target == null || target.HasExited)
        {
            try
            {
                var procs = Process.GetProcessesByName("llama-server")
                    .Where(p =>
                    {
                        try { return !p.HasExited; } catch { return false; }
                    })
                    .ToList();

                if (procs.Count > 0)
                {
                    // Prefer the one matching our known PID
                    int? knownPid = null;
                    lock (_lock) { knownPid = _status.ProcessId; }

                    if (knownPid.HasValue)
                    {
                        target = procs.FirstOrDefault(p => p.Id == knownPid.Value) ?? procs[0];
                    }
                    else
                    {
                        target = procs[0];
                    }
                }
            }
            catch { /* Can't enumerate processes */ }
        }

        if (target != null && !target.HasExited)
        {
            try
                {
                    target.Refresh();
                    long privateMem = 0, workingSet = 0;
                    try { privateMem = target.PrivateMemorySize64; } catch { }
                    try { workingSet = target.WorkingSet64; } catch { }

                    // Use PrivateMemorySize64 — more reliable than WorkingSet64 on Windows
                    if (privateMem > 0)
                        return privateMem / (1024.0 * 1024.0 * 1024.0);

                    // Fallback to WorkingSet64
                    if (workingSet > 0)
                        return workingSet / (1024.0 * 1024.0 * 1024.0);
                }
           catch { /* Process access denied or exited */ }
        }

        return 0;
    }

    async Task<ServerStatus> IServerManager.HealthCheckAsync(string host, int port)
    {
        return await HealthCheckInternalAsync(host, port);
    }

    async Task<ServerStatus> HealthCheckInternalAsync(string host, int port)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await s_sharedHttpClient.GetAsync(
                $"http://{host}:{port}/health", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var status = new ServerStatus
                {
                    State = ServerState.Running,
                    Port = port,
                    Host = host,
                    ModelName = root.TryGetProperty("model_name", out var modelName) ? modelName.GetString() : null,
                    ContextSize = root.TryGetProperty("ctx_size", out var ctxSize) ? ctxSize.GetInt32() : 0,
                    Threads = root.TryGetProperty("n_threads", out var threads) ? threads.GetInt32() : 0,
                    GpuLayers = root.TryGetProperty("n_gpu_layers", out var gpuLayers) ? gpuLayers.GetInt32() : 0,
                    TokensPerSecond = root.TryGetProperty("tps", out var tps) ? tps.GetDouble() : 0,
                    ActiveSlots = root.TryGetProperty("load", out var load) ? load.TryGetProperty("current", out var current) ? current.GetInt32() : 0 : 0
                };

                // If /health doesn't provide model info, try /v1/models
                if (status.ModelName == null)
                {
                    try
                    {
                        var modelsResp = await s_sharedHttpClient.GetAsync(
                            $"http://{host}:{port}/v1/models", cts.Token);
                        if (modelsResp.IsSuccessStatusCode)
                        {
                            var modelsJson = await modelsResp.Content.ReadAsStringAsync();
                            using var modelsDoc = JsonDocument.Parse(modelsJson);
                            var modelsRoot = modelsDoc.RootElement;
                            if (modelsRoot.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
                            {
                                var firstModel = dataArray[0];
                                status.ModelName = firstModel.TryGetProperty("id", out var id) ? id.GetString() : null;
                                if (firstModel.TryGetProperty("meta", out var meta))
                                {
                                    status.ContextSize = meta.TryGetProperty("n_ctx", out var nctx) ? nctx.GetInt32() : 0;
                                }
                            }
                        }
                    }
                    catch { }
                }

                return status;
            }
        }
        catch (Exception ex)
        {
            _log.Debug($"Health check failed: {ex.Message}", "ServerManager");
        }

        return new ServerStatus { State = ServerState.Stopped };
    }

   async Task<(int ActiveSlots, double GenTps, double PromptTps)> FetchActiveSlotsAsync(string host, int port)
    {
        int activeSlots = 0;
        double genTps = 0;
        double promptTps = 0;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var resp = await s_sharedHttpClient.GetAsync(
                $"http://{host}:{port}/slots", cts.Token);
            if (!resp.IsSuccessStatusCode)
                return (activeSlots, genTps, promptTps);

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            JsonElement? slotsArray = null;
            if (root.TryGetProperty("value", out var val) && val.ValueKind == JsonValueKind.Array)
                slotsArray = val;
            else if (root.ValueKind == JsonValueKind.Array)
                slotsArray = root;

            if (slotsArray.HasValue)
            {
                foreach (var slot in slotsArray.Value.EnumerateArray())
                {
                    bool isProcessing = false;
                    if (slot.TryGetProperty("is_processing", out var isProc))
                        isProcessing = isProc.GetBoolean();

                    // Also check state field: "processing" means active
                    if (!isProcessing && slot.TryGetProperty("state", out var stateEl) && stateEl.GetString() == "processing")
                        isProcessing = true;

                    if (isProcessing)
                    {
                        activeSlots++;

                        // Calculate generation TPS from t_token_ms (ms per token → tokens/sec)
                        if (slot.TryGetProperty("t_token_ms", out var tTokenMs) && tTokenMs.GetDouble() > 0)
                        {
                            double slotTps = 1000.0 / tTokenMs.GetDouble();
                            // Use max TPS across all active slots
                            if (slotTps > genTps)
                                genTps = slotTps;
                        }

                        // Fallback: calculate generation TPS from n_eval / generation_t
                        if (genTps <= 0 &&
                            slot.TryGetProperty("n_eval", out var nEval) && nEval.GetInt32() > 0 &&
                            slot.TryGetProperty("generation_t", out var genT))
                        {
                            double genTime = genT.GetDouble();
                            if (genTime > 0.01) // avoid division by very small numbers
                                genTps = nEval.GetInt32() / genTime;
                        }

                        }
                }
            }
        }
        catch { }

        // Prompt TPS only comes from stdout logs, not /slots API
        return (activeSlots, genTps, 0);
    }

    string BuildServerArgs(ServerProfile profile)
    {
        return profile.BuildFullArgsString();
    }

    public void Dispose()
    {
        // Cancel health polling
        _healthCts?.Cancel();
        _healthCts?.Dispose();

        // Cancel main token
        _cts?.Cancel();
        _cts?.Dispose();

        // Kill process if still running
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(true);
            }
            catch { }
            _process.Dispose();
        }
    }

   void OnLogReceived(LogEntry entry)
    {
        LogReceived?.Invoke(this, entry);
    }
    
    bool IsPortInUse(int port)
    {
        try
        {
            using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return false;
        }
        catch
        {
            return true;
        }
    }

    static string? ResolveExecutablePath(string configuredPath, string? activeVersion)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return FindOnPath("llama-server");

          // Try versioned path first
        if (!string.IsNullOrWhiteSpace(activeVersion))
        {
            var versionedPath = Path.Combine(configuredPath, activeVersion);
            var resolved = ResolveSinglePath(versionedPath);
            if (resolved != null)
                return resolved;
        }

        // Try configured path as-is
        var resolvedBase = ResolveSinglePath(configuredPath);
        if (resolvedBase != null)
            return resolvedBase;

        return FindOnPath(configuredPath) ?? FindOnPath("llama-server");
    }

    static string? ResolveSinglePath(string path)
    {
        if (File.Exists(path))
            return Path.GetFullPath(path);

        if (Directory.Exists(path))
        {
            foreach (var name in GetExecutableNames("llama-server"))
            {
                var candidate = Path.Combine(path, name);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    static string? FindOnPath(string executableName)
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var name in GetExecutableNames(executableName))
            {
                var candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    static IEnumerable<string> GetExecutableNames(string executableName)
    {
        yield return executableName;

        if (OperatingSystem.IsWindows() && string.IsNullOrEmpty(Path.GetExtension(executableName)))
        {
            yield return executableName + ".exe";
            yield return executableName + ".cmd";
            yield return executableName + ".bat";
        }
    }

   static void ParseTpsFromOutput(string line, ServerManager manager)
    {
        if (string.IsNullOrEmpty(line))
            return;

        // Prompt processing speed: "prompt processing, n_tokens = 10240, progress = 0.11, t = 3.15 s / 3245.78 tokens per second"
        var promptMatch = System.Text.RegularExpressions.Regex.Match(line, @"prompt processing.*?([\d.]+)\s+tokens?\s+per\s+second");
        if (promptMatch.Success && double.TryParse(promptMatch.Groups[1].Value, CultureInfo.InvariantCulture, out double ptps) && ptps > 0)
        {
            lock (manager._lock)
            {
                manager._status.PromptTokensPerSecond = ptps;
            }
            manager.RaiseStatusChanged();
            return;
        }

        // Try "tg = 83.73 t/s" pattern first (interim, during generation — more frequent)
        var match = s_tpsRegexTg.Match(line);
        if (match.Success && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out double tps) && tps > 0)
        {
            lock (manager._lock)
            {
                manager._currentTps = tps;
                manager._lastTpsUpdate = DateTime.UtcNow;
                manager._status.TokensPerSecond = tps;
            }
            manager.RaiseStatusChanged();
            return;
        }

        // Try "95.90 tokens per second" pattern (final timing — less frequent, fallback)
        match = s_tpsRegexEval.Match(line);
        if (match.Success && double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out double tps2) && tps2 > 0)
        {
            lock (manager._lock)
            {
                manager._currentTps = tps2;
                manager._lastTpsUpdate = DateTime.UtcNow;
                manager._status.TokensPerSecond = tps2;
            }
            manager.RaiseStatusChanged();
            return;
        }

        // Try "130.62 tokens per second" pattern (eval time line)
        var match2 = System.Text.RegularExpressions.Regex.Match(line, @"([\d.]+)\s+tokens?\s+per\s+second");
        if (match2.Success && double.TryParse(match2.Groups[1].Value, CultureInfo.InvariantCulture, out double tps3) && tps3 > 0)
        {
            lock (manager._lock)
            {
                manager._currentTps = tps3;
                manager._lastTpsUpdate = DateTime.UtcNow;
                manager._status.TokensPerSecond = tps3;
            }
            manager.RaiseStatusChanged();
            return;
        }

  
    }

    void RaiseStatusChanged()
    {
        ServerStatus s;
        lock (_lock)
        {
            s = new ServerStatus
            {
                State = _status.State,
                Port = _status.Port,
                Host = _status.Host,
                ModelName = _status.ModelName,
                ContextSize = _status.ContextSize,
                Threads = _status.Threads,
                GpuLayers = _status.GpuLayers,
                VramUsedGb = _status.VramUsedGb,
                RamUsedGb = _status.RamUsedGb,
                TokensPerSecond = _status.TokensPerSecond,
                PromptTokensPerSecond = _status.PromptTokensPerSecond,
                QueueSize = _status.QueueSize,
                ActiveSlots = _status.ActiveSlots,
                TotalTokensProcessed = _status.TotalTokensProcessed,
                Uptime = _status.Uptime,
                StartedAt = _status.StartedAt,
                ErrorMessage = _status.ErrorMessage,
                ProcessId = _status.ProcessId
            };
        }
        StatusChanged?.Invoke(this, s);
    }
}
