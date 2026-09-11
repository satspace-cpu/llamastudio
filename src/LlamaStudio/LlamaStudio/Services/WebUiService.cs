using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Infrastructure.Llama;
using LlamaStudio.ViewModels;

namespace LlamaStudio.Services;

/// <summary>
/// Small embedded HTTP server for the Studio web control surface.
/// It deliberately does not proxy llama-server and does not depend on RPC.
/// </summary>
public sealed class WebUiService : IAsyncDisposable
{
	private readonly ISettings _settings;
	private readonly IServerManager _serverManager;
	private readonly IProfileManager _profileManager;
	private readonly IGpuMonitor _gpuMonitor;
	private readonly MonitoringViewModel _monitoringViewModel;
	private readonly IChatService _chatService;
	private readonly ILlamaUpdater _llamaUpdater;
	private readonly IModelScanner _modelScanner;
	private readonly IHuggingFaceDownloader _hfDownloader;
	private readonly IDialogService _dialog;
	private readonly ILogService _log;
	private readonly ServerViewModel _serverViewModel;
	private readonly LlamaReleasesViewModel _releasesViewModel;
	private readonly IVersionCache _versionCache;
	private readonly ILocalizationService _localization;
	private static readonly JsonSerializerOptions WebJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonNamingPolicy.CamelCase };
	private TcpListener? _listener;
	private CancellationTokenSource? _cts;
	private string? _activeProfileId;
	private string _llamaDownloadStatus = string.Empty;
	private bool _llamaInstallError;
	private CancellationTokenSource? _cudaToolkitInstallCts;
	private bool _cudaToolkitInstalling;
	private string _cudaToolkitInstallStatus = string.Empty;

public WebUiService(ISettings settings, IServerManager serverManager, IProfileManager profileManager, IGpuMonitor gpuMonitor, MonitoringViewModel monitoringViewModel, IChatService chatService, ILlamaUpdater llamaUpdater, IModelScanner modelScanner, IHuggingFaceDownloader hfDownloader, IDialogService dialog, ILogService log, ServerViewModel serverViewModel, IVersionCache versionCache, ILocalizationService localization, LlamaReleasesViewModel releasesViewModel)
	{
		_settings = settings;
		_serverManager = serverManager;
		_profileManager = profileManager;
		_gpuMonitor = gpuMonitor;
		_monitoringViewModel = monitoringViewModel;
		_chatService = chatService;
		_llamaUpdater = llamaUpdater;
		_modelScanner = modelScanner;
		_hfDownloader = hfDownloader;
		_dialog = dialog;
		_log = log;
		_serverViewModel = serverViewModel;
		_versionCache = versionCache;
		_localization = localization;
		_releasesViewModel = releasesViewModel;
		_llamaUpdater.StatusMessage += (_, status) => _llamaDownloadStatus = status;
		_releasesViewModel.InstallFailed += (_, message) =>
		{
			_llamaInstallError = true;
			_llamaDownloadStatus = _localization.T("web.llama.error") + message;
		};
	}

	public bool IsRunning => _listener != null;

	private async Task<ServerProfile?> ResolveCurrentProfileAsync()
	{
		ServerStatus status = await _serverManager.GetStatusAsync();
		List<ServerProfile> profiles = await _profileManager.GetAllProfilesAsync();
		if (status.State == LlamaStudio.Core.Enums.ServerState.Running && !string.IsNullOrWhiteSpace(status.ModelName))
		{
			ServerProfile? running = profiles.FirstOrDefault(p =>
				!string.IsNullOrWhiteSpace(p.ModelPath) &&
				string.Equals(Path.GetFileName(p.ModelPath), status.ModelName, StringComparison.OrdinalIgnoreCase));
			if (running != null)
				return running;
		}

		if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
		{
			ServerProfile? last = profiles.FirstOrDefault(p => string.Equals(p.Id, _settings.LastSelectedProfileId, StringComparison.OrdinalIgnoreCase));
			if (last != null)
				return last;
		}

		if (_serverViewModel.SelectedProfile != null)
		{
			ServerProfile? selected = profiles.FirstOrDefault(p => string.Equals(p.Id, _serverViewModel.SelectedProfile.Id, StringComparison.OrdinalIgnoreCase));
			if (selected != null)
				return selected;
		}

		return profiles.FirstOrDefault(p => p.IsDefault) ?? profiles.FirstOrDefault();
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		if (!_settings.WebUiEnabled || IsRunning)
			return;

		if (_settings.WebUiPort is < 1 or > 65535)
			throw new InvalidOperationException(_localization.T("web.api.invalid_web_port"));

		IPAddress address = ResolveBindAddress(_settings.WebUiHost);
		_listener = new TcpListener(address, _settings.WebUiPort);
		try
		{
			_listener.Start();
			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_log.Information($"Web UI listening on {address}:{_settings.WebUiPort}", "WebUI");
			_ = AcceptLoopAsync(_cts.Token);
		}
		catch
		{
			_listener.Stop();
			_listener = null;
			_cts?.Dispose();
			_cts = null;
			throw;
		}
		await Task.CompletedTask;
	}

	public async Task StopAsync()
	{
		CancellationTokenSource? cts = _cts;
		_cts = null;
		_listener?.Stop();
		_listener = null;
		if (cts != null)
		{
			cts.Cancel();
			cts.Dispose();
		}
		await Task.CompletedTask;
	}

	private async Task AcceptLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			TcpClient? client = null;
			try
			{
				TcpListener? listener = _listener;
				if (listener == null)
					return;
				client = await listener.AcceptTcpClientAsync(cancellationToken);
				_ = HandleClientAsync(client, cancellationToken);
			}
			catch (OperationCanceledException) { return; }
			catch (ObjectDisposedException) { return; }
			catch (Exception ex)
			{
				client?.Dispose();
				_log.Warning("Web UI accept failed: " + ex.Message, "WebUI");
			}
		}
	}

	private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
	{
		using (client)
		{
			using NetworkStream stream = client.GetStream();
			stream.ReadTimeout = 5000;
			stream.WriteTimeout = 5000;
			try
			{
				byte[] buffer = new byte[16384];
				int count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
				string request = Encoding.ASCII.GetString(buffer, 0, count);
			string[] lines = request.Split("\r\n", StringSplitOptions.None);
			string[] requestLine = lines.Length > 0 ? lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
			string target = requestLine.Length >= 2 ? requestLine[1] : "/";
			string path = target.Split('?')[0];
			Dictionary<string, string> query = ParseQuery(target);

			if (_settings.WebUiRequireLogin && !IsAuthorized(lines))
			{
				await WriteResponseAsync(stream, "401 Unauthorized", "text/plain; charset=utf-8", _localization.T("web.api.login_required"), cancellationToken, "WWW-Authenticate: Basic realm=\"Llama Studio\"");
				return;
			}

			if (path == "/api/status")
			{
				ServerStatus status = await _serverManager.GetStatusAsync();
				ServerProfile? current = await ResolveCurrentProfileAsync();
				string host = string.IsNullOrWhiteSpace(current?.Host) ? (string.IsNullOrWhiteSpace(_settings.DefaultHost) ? "127.0.0.1" : _settings.DefaultHost) : current.Host;
				int port = current?.Port > 0 ? current.Port : (_settings.DefaultPort > 0 ? _settings.DefaultPort : 8080);
				string browserHost = host is "0.0.0.0" or "::" ? "127.0.0.1" : host;
				string webUiUrl = $"http://{browserHost}:{port}/";
				string json = JsonSerializer.Serialize(new { studio = "running", llamaServer = status.State.ToString(), model = status.ModelName, activeProfileId = current?.Id, activeProfile = current?.Name, pid = status.ProcessId, context = status.ContextSize, llamaHost = host, llamaPort = port, llamaWebUiUrl = webUiUrl, webHost = _settings.WebUiHost, webPort = _settings.WebUiPort });
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", json, cancellationToken);
				return;
			}

			if (path == "/api/profiles")
			{
				// r79: hide profiles whose model file no longer exists — parity with the
				// desktop DashboardViewModel.RefreshAsync filter (they still exist in the
				// store; "Очистить без моделей" /api/profile/cleanup-missing removes them).
				List<ServerProfile> profiles = (await _profileManager.GetAllProfilesAsync()).Where(p => string.IsNullOrWhiteSpace(p.ModelPath) || File.Exists(p.ModelPath)).ToList();
				ServerProfile? current = await ResolveCurrentProfileAsync();
				ServerProfile? defaultProfile = await _profileManager.GetDefaultProfileAsync();
				string? selectedProfileId = current?.Id;
				string? defaultProfileId = defaultProfile?.Id;
				var items = profiles.ConvertAll(p => new { id = p.Id, name = string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name, model = p.ModelPath, isDefault = string.Equals(p.Id, defaultProfileId, StringComparison.OrdinalIgnoreCase), isSelected = string.Equals(p.Id, selectedProfileId, StringComparison.OrdinalIgnoreCase) });
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(items), cancellationToken);
				return;
			}

			if (path == "/api/profile")
			{
				ServerProfile? profile = null;
				if (query.TryGetValue("id", out string? id) && !string.IsNullOrWhiteSpace(id))
					profile = await _profileManager.GetProfileAsync(id);
				profile ??= await ResolveCurrentProfileAsync();
				if (profile == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"Профиль не найден\"}", cancellationToken);
					return;
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(ProfileToDictionary(profile), WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/gpu")
			{
				await _monitoringViewModel.RefreshNowAsync();
				IReadOnlyList<GpuInfo> gpus = _monitoringViewModel.GpuInfos;
				if (gpus.Count == 0)
					gpus = await _gpuMonitor.GetGpuInfosAsync();
				var gpuItems = gpus.Select(gpu => new
				{
					gpu.Index,
					gpu.IsRemote,
					gpu.RemoteDeviceId,
					gpu.Name,
					gpu.DisplayName,
					gpu.IsSelected,
					gpu.MemoryUsedGb,
					gpu.MemoryTotalGb,
					gpu.GpuUtilization,
					gpu.PowerDrawWatts,
					gpu.PowerLimitWatts,
					gpu.TemperatureCelsius,
					gpu.MemoryTemperatureCelsius,
					gpu.FanSpeed,
					gpu.ClockMhz,
					gpu.ComputeCapability
				}).ToArray();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(gpuItems, WebJsonOptions), cancellationToken);
				return;
			}

			// r94: GPU power limit (the desktop GpuPowerLimitControl over the web).
			if (path == "/api/gpu/power")
			{
				IReadOnlyList<GpuPowerLimitItem> powerItems = await GpuPowerLimitManager.GetItemsAsync();
				var powerList = powerItems.Select(item => new
				{
					item.Index,
					item.Name,
					item.CurrentWatts,
					item.MinWatts,
					item.MaxWatts,
					item.LimitWatts
				}).ToArray();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(powerList, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/gpu/power/set")
			{
				if (!query.TryGetValue("index", out string? powerIndexRaw) || !int.TryParse(powerIndexRaw, out int powerIndex)
					|| !query.TryGetValue("watts", out string? powerWattsRaw)
					|| !double.TryParse(powerWattsRaw.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double powerWatts))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"index и watts (число) обязательны\"}", cancellationToken);
					return;
				}
				IReadOnlyList<GpuPowerLimitItem> powerItems = await GpuPowerLimitManager.GetItemsAsync();
				GpuPowerLimitItem? powerItem = powerItems.FirstOrDefault(item => item.Index == powerIndex);
				(bool ok, string message) = await GpuPowerLimitManager.SetPowerLimitAsync(powerIndex, powerWatts, powerItem?.MinWatts, powerItem?.MaxWatts);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok, message }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/monitoring/speed")
			{
				if (!_monitoringViewModel.IsSpeedBenchmarking)
					await _monitoringViewModel.CheckSpeedCommand.ExecuteAsync(null);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new
				{
					ok = string.IsNullOrWhiteSpace(_monitoringViewModel.SpeedBenchmarkResult),
					tokensPerSecond = _monitoringViewModel.TokensPerSecond,
					promptTokensPerSecond = _monitoringViewModel.PromptTokensPerSecond,
					isBenchmarking = _monitoringViewModel.IsSpeedBenchmarking,
					result = _monitoringViewModel.SpeedBenchmarkResult
				}, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/monitoring")
			{
				await _monitoringViewModel.RefreshNowAsync();
				ServerProfile? monitoringProfile = null;
				if (query.TryGetValue("id", out string? monitoringId) && !string.IsNullOrWhiteSpace(monitoringId))
					monitoringProfile = await _profileManager.GetProfileAsync(monitoringId);
				monitoringProfile ??= await ResolveCurrentProfileAsync();
				IReadOnlyList<GpuInfo> monitoringGpus = _monitoringViewModel.GpuInfos;
				var profileGpu = monitoringProfile == null ? null : new
				{
					monitoringProfile.ContextSize,
					monitoringProfile.GpuLayers,
					monitoringProfile.GpuSplitMode,
					monitoringProfile.MainGpu,
					TensorSplit = string.Join(",", monitoringProfile.TensorSplit),
					SelectedGpuIndices = string.Join(",", monitoringProfile.SelectedGpuIndices),
					monitoringProfile.FlashAttention,
					monitoringProfile.Mmap,
					monitoringProfile.Mlock,
					monitoringProfile.MmqEnabled,
					monitoringProfile.KvOffloadEnabled,
					monitoringProfile.RpcEnabled,
					monitoringProfile.RpcRemoteTelemetry
				};
				ServerStatus monitoringStatus = await _serverManager.GetStatusAsync();
				var ram = new
				{
					usedGb = monitoringStatus.RamUsedGb,
					committedGb = monitoringStatus.RamCommittedGb,
					cacheLimitGb = monitoringProfile != null && monitoringProfile.CacheRam > 0 ? monitoringProfile.CacheRam / 1024.0 : 0.0,
					totalGb = _monitoringViewModel.TotalRamGb
				};
				var speed = new
				{
					tokensPerSecond = _monitoringViewModel.TokensPerSecond,
					promptTokensPerSecond = _monitoringViewModel.PromptTokensPerSecond,
					isBenchmarking = _monitoringViewModel.IsSpeedBenchmarking,
					result = _monitoringViewModel.SpeedBenchmarkResult
				};
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { profile = profileGpu, gpus = monitoringGpus, ram, speed }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/logs")
			{
				string consoleText = _log.GetConsoleText();
				string[] logLines = string.IsNullOrEmpty(consoleText) ? Array.Empty<string>() : consoleText.Split('\n');
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { lines = logLines }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/logs/clear")
			{
				_log.ClearConsole();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/logs/save")
			{
				string savePath = query.GetValueOrDefault("path")?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(savePath))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.path_required") + "\"}", cancellationToken);
					return;
				}
				await _log.ExportLogsAsync(savePath);
				string consoleText2 = _log.GetConsoleText();
				int lineCount = string.IsNullOrEmpty(consoleText2) ? 0 : consoleText2.Split('\n').Length;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true,\"path\":\"" + savePath.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"lines\":" + lineCount + "}", cancellationToken);
				return;
			}

			if (path == "/api/settings")
			{
				var settings = new { llamaHost = _settings.DefaultHost, llamaPort = _settings.DefaultPort, webEnabled = _settings.WebUiEnabled, webHost = _settings.WebUiHost, webPort = _settings.WebUiPort, loginRequired = _settings.WebUiRequireLogin, modelsDirectory = _settings.ModelsDirectory, showDesktopWindow = _settings.ShowDesktopWindow, autoCheckUpdates = _settings.AutoCheckUpdates, autoStartWithOs = _settings.AutoStartWithOS, keepServerOnExit = _settings.KeepServerOnExit, autoStartServer = _settings.AutoStartServer, language = _settings.Language, appVersion = (Assembly.GetEntryAssembly() ?? typeof(WebUiService).Assembly).GetName().Version?.ToString(3) ?? "0.0.0" };
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(settings, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/settings/save")
			{
				if (query.TryGetValue("webEnabled", out string? enabled) && bool.TryParse(enabled, out bool enabledValue)) _settings.WebUiEnabled = enabledValue;
				if (query.TryGetValue("webHost", out string? webHost)) _settings.WebUiHost = webHost;
				if (query.TryGetValue("webPort", out string? webPort) && int.TryParse(webPort, out int port)) _settings.WebUiPort = port;
				if (query.TryGetValue("loginRequired", out string? login) && bool.TryParse(login, out bool loginValue)) _settings.WebUiRequireLogin = loginValue;
				if (query.TryGetValue("username", out string? username)) _settings.WebUiUsername = username;
				if (query.TryGetValue("password", out string? password) && !string.IsNullOrEmpty(password)) _settings.WebUiPassword = password;
				if (query.TryGetValue("defaultHost", out string? defHost) && !string.IsNullOrEmpty(defHost)) _settings.DefaultHost = defHost;
				if (query.TryGetValue("defaultPort", out string? defPort) && int.TryParse(defPort, out int defPortValue)) _settings.DefaultPort = defPortValue;
			string? newLang = null;
			if (query.TryGetValue("language", out string? lang) && (lang == "en" || lang == "ru")) { _settings.Language = lang; newLang = lang; }
				if (query.TryGetValue("showDesktopWindow", out string? showDesktop) && bool.TryParse(showDesktop, out bool showDesktopValue)) _settings.ShowDesktopWindow = showDesktopValue;
				if (query.TryGetValue("autoCheckUpdates", out string? autoCheck) && bool.TryParse(autoCheck, out bool autoCheckValue)) _settings.AutoCheckUpdates = autoCheckValue;
				if (query.TryGetValue("autoStartWithOs", out string? autoStartOs) && bool.TryParse(autoStartOs, out bool autoStartOsValue)) { _settings.AutoStartWithOS = autoStartOsValue; SettingsViewModel.ApplyAutoStart(autoStartOsValue); }
				if (query.TryGetValue("keepServerOnExit", out string? keepServer) && bool.TryParse(keepServer, out bool keepServerValue)) _settings.KeepServerOnExit = keepServerValue;
				if (query.TryGetValue("autoStartServer", out string? autoStartServer) && bool.TryParse(autoStartServer, out bool autoStartServerValue)) _settings.AutoStartServer = autoStartServerValue;
				await _settings.SaveAsync();
				if (newLang != null && newLang != _localization.Language) { try { Dispatcher.UIThread.InvokeAsync(() => _localization.ChangeLanguage(newLang)); } catch { }
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true,\"restartRequired\":true}", cancellationToken);
				return;
			}

			if (path == "/api/desktop/window")
			{
				bool show = query.GetValueOrDefault("show") == "true";
				if (query.TryGetValue("showDesktopWindow", out string? showVal) && bool.TryParse(showVal, out bool showSetting))
				{
					_settings.ShowDesktopWindow = showSetting;
					await _settings.SaveAsync();
				}
				await Dispatcher.UIThread.InvokeAsync(() =>
				{
					if (show)
						(Avalonia.Application.Current as App)?.ShowWindow();
					else
						(Avalonia.Application.Current as App)?.HideWindow();
				});
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true,\"show\":" + (show ? "true" : "false") + "}", cancellationToken);
				return;
			}

			if (path == "/api/llama/releases")
			{
				string repo = query.GetValueOrDefault("repo")?.Trim() ?? string.Empty;
				bool latestOnly = query.TryGetValue("latestOnly", out string? latestText) && bool.TryParse(latestText, out bool latestValue) && latestValue;
				List<LlamaCppRelease> releases = string.IsNullOrWhiteSpace(repo)
					? await _llamaUpdater.FetchReleasesAsync(true, cancellationToken)
					: await _llamaUpdater.FetchReleasesFromRepoAsync(repo, cancellationToken);
				if (latestOnly && releases.Count > 1)
				{
					string latestTag = releases[0].TagName;
					releases = releases.Where(r => string.Equals(r.TagName, latestTag, StringComparison.OrdinalIgnoreCase)).ToList();
				}
				var result = releases.ConvertAll(r => new
				{
					tag = r.TagName,
					name = r.Name,
					publishedAt = r.PublishedAt,
					url = r.HtmlUrl,
					isPrerelease = r.IsPrerelease,
					body = r.Body,
					assets = r.Assets.ConvertAll(a => new { a.Name, url = a.BrowserDownloadUrl, size = a.Size, sizeDisplay = a.SizeDisplay, description = a.Description, buildType = a.BuildType.ToString(), cudaVersion = a.CudaVersion })
				});
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(result, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/browse")
			{
				string? selected = await Dispatcher.UIThread.InvokeAsync(() => _dialog.SelectFolderAsync("Выберите папку с серверами llama.cpp", GetLlamaBaseDirectory()));
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { path = selected ?? string.Empty }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/fs/list")
			{
				bool files = bool.TryParse(query.GetValueOrDefault("files"), out bool includeFiles) && includeFiles;
				string filter = query.GetValueOrDefault("filter")?.Trim() ?? string.Empty;
				string requested = query.GetValueOrDefault("path")?.Trim() ?? string.Empty;
				try
				{
					if (string.IsNullOrWhiteSpace(requested))
					{
						var roots = DriveInfo.GetDrives().Where(drive => drive.IsReady).Select(drive => new { name = drive.Name, path = drive.RootDirectory.FullName, isDirectory = true, sizeBytes = 0L }).ToArray();
						await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, path = string.Empty, parent = string.Empty, isRoot = true, entries = roots }, WebJsonOptions), cancellationToken);
						return;
					}

					string directory = Path.GetFullPath(requested);
					if (!Directory.Exists(directory))
					{
						await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = false, error = "Folder not found" }, WebJsonOptions), cancellationToken);
						return;
					}

					DirectoryInfo info = new(directory);
					var entries = new List<object>();
					foreach (DirectoryInfo item in info.EnumerateDirectories().OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Take(250))
						entries.Add(new { name = item.Name, path = item.FullName, isDirectory = true, sizeBytes = 0L });
					if (files)
					{
						foreach (FileInfo item in info.EnumerateFiles().Where(item => string.IsNullOrWhiteSpace(filter) || item.Extension.Equals(filter, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).Take(250))
							entries.Add(new { name = item.Name, path = item.FullName, isDirectory = false, sizeBytes = item.Length });
					}
					await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, path = info.FullName, parent = info.Parent?.FullName ?? string.Empty, isRoot = info.Parent == null, entries }, WebJsonOptions), cancellationToken);
					return;
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = false, error = ex.Message }, WebJsonOptions), cancellationToken);
					return;
				}
			}

			if (path == "/api/llama/set-directory")
			{
				string requested = query.GetValueOrDefault("path")?.Trim() ?? string.Empty;
				if (!TryValidateLlamaBaseDirectory(requested, out string directory, out string error))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", JsonSerializer.Serialize(new { error }, WebJsonOptions), cancellationToken);
					return;
				}
				_settings.LlamaCppBaseDirectory = directory;
				await _settings.SaveAsync();
				await _versionCache.ScanAsync(directory, cancellationToken);
				ServerProfile? currentProfile = await ResolveCurrentProfileAsync();
				if (currentProfile != null)
				{
					currentProfile.LlamaCppBaseDirectory = directory;
					await _profileManager.SaveProfileAsync(currentProfile);
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, path = directory }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/status")
			{
				string liveStatus = _releasesViewModel.IsDownloading
					? (!string.IsNullOrEmpty(_releasesViewModel.DownloadStatus) ? _releasesViewModel.DownloadStatus : _llamaDownloadStatus)
					: _llamaDownloadStatus;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new
				{
					isDownloading = _releasesViewModel.IsDownloading,
					progress = _releasesViewModel.DownloadProgress,
					status = liveStatus,
					detailStatus = _llamaDownloadStatus,
					isError = _llamaInstallError,
					baseDirectory = GetLlamaBaseDirectory()
				}, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/cuda/toolkit/status")
			{
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new
				{
					cuda12Installed = FindCudaToolkitDirectory(12) != null,
					cuda12Directory = FindCudaToolkitDirectory(12),
					// r83: the CUDA 12 Toolkit installer is a Linux runfile flow;
					// on Windows prebuilt CUDA 12 builds install like any other.
					toolkitSupported = !OperatingSystem.IsWindows(),
					isInstalling = _cudaToolkitInstalling,
					status = _cudaToolkitInstallStatus
				}, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/cuda/toolkit/install")
			{
				if (OperatingSystem.IsWindows())
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.cuda_linux_only") + "\"}", cancellationToken);
					return;
				}
				// r52: loopback-запрет удалён — установка разрешена для всех
				// авторизованных пользователей (Basic Auth обеспечивает доступ).
				if (FindCudaToolkitDirectory(12) != null)
				{
					await WriteResponseAsync(stream, "409 Conflict", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.cuda_already_installed") + "\"}", cancellationToken);
					return;
				}
				if (_cudaToolkitInstalling)
				{
					await WriteResponseAsync(stream, "409 Conflict", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.cuda_install_in_progress") + "\"}", cancellationToken);
					return;
				}
				_cudaToolkitInstallCts = new CancellationTokenSource();
				_cudaToolkitInstalling = true;
				_cudaToolkitInstallStatus = _localization.T("web.cuda.waiting_admin");
				_ = InstallCuda12ToolkitAsync(_cudaToolkitInstallCts);
				await WriteResponseAsync(stream, "202 Accepted", "application/json; charset=utf-8", "{\"ok\":true,\"started\":true}", cancellationToken);
				return;
			}

			if (path == "/api/llama/install")
			{
				if (_releasesViewModel.IsDownloading)
				{
					await WriteResponseAsync(stream, "409 Conflict", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.install_in_progress") + "\"}", cancellationToken);
					return;
				}
				string tag = query.GetValueOrDefault("version")?.Trim() ?? string.Empty;
				string assetName = query.GetValueOrDefault("asset")?.Trim() ?? string.Empty;
				string repo = query.GetValueOrDefault("repo")?.Trim() ?? string.Empty;
				bool nccl = bool.TryParse(query.GetValueOrDefault("nccl"), out bool ncclParsed) && ncclParsed;
				if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(assetName))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.version_asset_required") + "\"}", cancellationToken);
					return;
				}
				List<LlamaCppRelease> releases = string.IsNullOrWhiteSpace(repo)
					? await _llamaUpdater.FetchReleasesAsync(true, cancellationToken)
					: await _llamaUpdater.FetchReleasesFromRepoAsync(repo, cancellationToken);
				LlamaCppRelease? release = releases.FirstOrDefault(r => string.Equals(r.TagName, tag, StringComparison.OrdinalIgnoreCase));
				ReleaseAsset? asset = release?.Assets.FirstOrDefault(a => string.Equals(a.Name, assetName, StringComparison.OrdinalIgnoreCase));
				if (release == null || asset == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.release_build_not_found") + "\"}", cancellationToken);
					return;
				}
				ReleaseBuildCard? card = _releasesViewModel.ReleaseBuildCards.FirstOrDefault(c => string.Equals(c.ReleaseTag, release.TagName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.AssetName, asset.Name, StringComparison.OrdinalIgnoreCase))
					?? _releasesViewModel.ExperimentalBuildCards.FirstOrDefault(c => string.Equals(c.ReleaseTag, release.TagName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.AssetName, asset.Name, StringComparison.OrdinalIgnoreCase))
					?? new ReleaseBuildCard
					{
						ReleaseTag = release.TagName,
						Asset = asset,
						BuildType = asset.BuildType,
						BuildDescription = asset.Description ?? asset.Name,
						AssetName = asset.Name
					};
				if (card.IsInstalled)
				{
					await WriteResponseAsync(stream, "409 Conflict", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.already_installed") + "\"}", cancellationToken);
					return;
				}
				if (_releasesViewModel.NcclEnabled != nccl)
				{
					_releasesViewModel.NcclEnabled = nccl;
				}
				_llamaInstallError = false;
				_llamaDownloadStatus = _localization.T("web.llama.preparing");
				Task? installTask = _releasesViewModel.InstallBuildCommand?.ExecuteAsync(card);
				if (installTask == null)
				{
					await WriteResponseAsync(stream, "500 Internal Server Error", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.install_failed") + "\"}", cancellationToken);
					return;
				}
				_ = TrackInstallCompletionAsync(installTask);
				await WriteResponseAsync(stream, "202 Accepted", "application/json; charset=utf-8", "{\"ok\":true,\"started\":true}", cancellationToken);
				return;
			}

			if (path == "/api/llama/cancel")
			{
				_releasesViewModel.CancelDownloadCommand?.Execute(null);
				_llamaInstallError = false;
				_llamaDownloadStatus = _localization.T("web.llama.cancelled");
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/llama/update")
			{
				if (!query.TryGetValue("version", out string? updateVersion) || string.IsNullOrWhiteSpace(updateVersion))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"version is required\"}", cancellationToken);
					return;
				}
				List<LlamaCppRelease> available = await _llamaUpdater.FetchReleasesAsync(true, cancellationToken);
				LlamaCppRelease? release = available.FirstOrDefault(item => string.Equals(item.TagName, updateVersion, StringComparison.OrdinalIgnoreCase));
				ReleaseAsset? asset = release?.Assets.FirstOrDefault(item => string.IsNullOrWhiteSpace(query.GetValueOrDefault("asset")) || string.Equals(item.Name, query["asset"], StringComparison.OrdinalIgnoreCase));
				if (release == null || asset == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"release asset not found\"}", cancellationToken);
					return;
				}
				string installPath = await _llamaUpdater.DownloadAndExtractAsync(release.TagName, _settings.LlamaCppBaseDirectory, asset, cancellationToken);
				_settings.LlamaCppDirectory = installPath;
				_settings.ActiveLlamaCppVersion = Path.GetFileName(installPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, installed = _settings.ActiveLlamaCppVersion }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/installed")
			{
				string baseDir = GetLlamaBaseDirectory();
				if (Directory.Exists(baseDir))
					await _versionCache.ScanAsync(baseDir, cancellationToken);
				var versions = _versionCache.GetAll().Select(item => new
				{
					version = item.Version,
					versionKey = item.VersionKey,
					folderName = Path.GetFileName(item.Directory),
					// r71: full path + real commit/build of the binary in the folder
					directory = item.Directory,
					versionDetail = item.VersionDetail,
					buildType = item.BuildType,
					display = item.Display,
					isActive = item.Directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Equals((_settings.LlamaCppDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
				}).ToList();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { active = _settings.ActiveLlamaCppVersion, baseDirectory = baseDir, versions }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/check")
			{
				await _serverViewModel.CheckLlamaCppVersionCommand.ExecuteAsync(null);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new
				{
					ok = _serverViewModel.ExecutableFound,
					executablePath = _serverViewModel.ExecutablePath,
					executableFound = _serverViewModel.ExecutableFound,
					version = _serverViewModel.LlamaVersionInfo,
					cuda = _serverViewModel.CudaInfo,
					validation = _serverViewModel.ValidationSummary,
					validationDetails = _serverViewModel.ValidationDetails,
					unsupportedFlags = _serverViewModel.UnsupportedFlags
				}, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/validate")
			{
				await _serverViewModel.ValidateCliCommand.ExecuteAsync(null);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new
				{
					ok = _serverViewModel.ExecutableFound,
					validation = _serverViewModel.ValidationSummary,
					validationDetails = _serverViewModel.ValidationDetails,
					unsupportedFlags = _serverViewModel.UnsupportedFlags
				}, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/use")
			{
				string key = query.GetValueOrDefault("versionKey")?.Trim() ?? string.Empty;
				string folder = query.GetValueOrDefault("folder")?.Trim() ?? string.Empty;
				string baseDir = GetLlamaBaseDirectory();
				if (Directory.Exists(baseDir))
					await _versionCache.ScanAsync(baseDir, cancellationToken);
				VersionInfo? info = !string.IsNullOrWhiteSpace(key) ? _versionCache.GetByVersionKey(key) : null;
				info ??= _versionCache.GetAll().FirstOrDefault(item => string.Equals(Path.GetFileName(item.Directory), folder, StringComparison.OrdinalIgnoreCase));
				if (info == null || !File.Exists(Path.Combine(info.Directory, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server")))
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.version_not_found") + "\"}", cancellationToken);
					return;
				}
				ServerStatus oldStatus = await _serverManager.GetStatusAsync();
				bool wasRunning = oldStatus.State == LlamaStudio.Core.Enums.ServerState.Running || oldStatus.State == LlamaStudio.Core.Enums.ServerState.Starting;
				if (wasRunning)
					await _serverManager.StopAsync(cancellationToken);
				_settings.LlamaCppDirectory = info.Directory;
				_settings.ActiveLlamaCppVersion = info.VersionKey;
				await _settings.SaveAsync();
				ServerProfile? activeProfile = await ResolveCurrentProfileAsync();
				if (activeProfile != null)
				{
					activeProfile.SelectedServerVersion = info.VersionKey;
					await _profileManager.SaveProfileAsync(activeProfile);
				}
				if (wasRunning && activeProfile != null && !string.IsNullOrWhiteSpace(activeProfile.ModelPath))
					await _serverManager.StartAsync(activeProfile, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, active = info.VersionKey, restarted = wasRunning }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/llama/remove")
			{
				string folder = query.GetValueOrDefault("folder")?.Trim() ?? string.Empty;
				string baseDir = GetLlamaBaseDirectory();
				if (Directory.Exists(baseDir))
					await _versionCache.ScanAsync(baseDir, cancellationToken);
				VersionInfo? info = _versionCache.GetAll().FirstOrDefault(item => string.Equals(Path.GetFileName(item.Directory), folder, StringComparison.OrdinalIgnoreCase));
				if (info == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.version_not_found") + "\"}", cancellationToken);
					return;
				}
				if (info.Directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Equals((_settings.LlamaCppDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
				{
					await WriteResponseAsync(stream, "409 Conflict", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.cannot_delete_active") + "\"}", cancellationToken);
					return;
				}
				await _llamaUpdater.UninstallVersionAsync(baseDir, Path.GetFileName(info.Directory));
				await _versionCache.ScanAsync(baseDir, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/models")
			{
				List<object> models = new();
				foreach (string directory in (_settings.ModelsDirectory + ";" + _settings.AdditionalModelsDirectories).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (!Directory.Exists(directory)) continue;
					foreach (string file in Directory.EnumerateFiles(directory, "*.gguf", SearchOption.AllDirectories))
					{
						FileInfo info = new(file);
						models.Add(new { name = info.Name, path = info.FullName, sizeGb = Math.Round(info.Length / 1073741824d, 2) });
						if (models.Count >= 200) break;
					}
					if (models.Count >= 200) break;
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(models, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/models/directory")
			{
				string directory = query.GetValueOrDefault("path")?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.models_dir_not_found") + "\"}", cancellationToken);
					return;
				}
				_settings.ModelsDirectory = directory;
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, directory }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/models/browse")
			{
				string initialPath = query.GetValueOrDefault("path")?.Trim() ?? _settings.ModelsDirectory;
				string? directory = await Dispatcher.UIThread.InvokeAsync(() => _dialog.SelectFolderAsync("Выберите каталог моделей", initialPath));
				if (string.IsNullOrWhiteSpace(directory))
				{
					await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":false,\"cancelled\":true}", cancellationToken);
					return;
				}
				_settings.ModelsDirectory = directory;
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, directory }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/model/browse")
			{
				string kind = query.GetValueOrDefault("kind")?.Trim().ToLowerInvariant() ?? "model";
				string title = kind switch
				{
					"mmproj" => "Select mmproj GGUF model",
					"draft" => "Select draft GGUF model",
					_ => "Select GGUF model"
				};
				string initialPath = query.GetValueOrDefault("path")?.Trim() ?? _settings.ModelsDirectory;
				string? selected = await Dispatcher.UIThread.InvokeAsync(() => _dialog.SelectFileAsync(title, initialPath, "GGUF Files|*.gguf|All Files|*.*"));
				if (string.IsNullOrWhiteSpace(selected) || !File.Exists(selected))
				{
					await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":false,\"cancelled\":true}", cancellationToken);
					return;
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, kind, path = selected, name = Path.GetFileName(selected) }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/models/scan")
			{
				string directory = query.GetValueOrDefault("path")?.Trim() ?? _settings.ModelsDirectory;
				if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.models_dir_not_found") + "\"}", cancellationToken);
					return;
				}
				List<GgufModelInfo> scanned = await _modelScanner.ScanDirectoryAsync(directory, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(scanned, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/hf/login")
			{
				bool opened = await _hfDownloader.LoginViaBrowserAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = opened }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/hf/token/save")
			{
				_hfDownloader.SetAuthToken(query.GetValueOrDefault("token"));
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/hf/search")
			{
				string search = query.GetValueOrDefault("q")?.Trim() ?? string.Empty;
				if (search.Length < 2)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.search_too_short") + "\"}", cancellationToken);
					return;
				}
				List<HfSearchResult> results = await _hfDownloader.SearchModelsAsync(search, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(results, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/hf/repo")
			{
				string repo = query.GetValueOrDefault("repo")?.Trim() ?? string.Empty;
				if (string.IsNullOrWhiteSpace(repo))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.hf_repo_required") + "\"}", cancellationToken);
					return;
				}
				List<HfFileInfo> files = await _hfDownloader.ListRepoFilesAsync(repo, cancellationToken);
				string directory = query.GetValueOrDefault("dir")?.Trim() ?? _settings.ModelsDirectory;
				foreach (HfFileInfo file in files)
				{
					if (!string.IsNullOrWhiteSpace(directory))
					{
						string local = Path.Combine(directory, file.FileName);
						if (File.Exists(local) && new FileInfo(local).Length > 0)
						{
							file.IsCompleted = true;
							file.DownloadProgress = 100;
							file.LocalPath = local;
						}
					}
				}
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(files, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/hf/download")
			{
				string repo = query.GetValueOrDefault("repo")?.Trim() ?? string.Empty;
				string directory = query.GetValueOrDefault("dir")?.Trim() ?? _settings.ModelsDirectory;
				string[] names = (query.GetValueOrDefault("files") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				if (string.IsNullOrWhiteSpace(repo) || string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory) || names.Length == 0)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.hf_download_required") + "\"}", cancellationToken);
					return;
				}
				List<HfFileInfo> files = await _hfDownloader.ListRepoFilesAsync(repo, cancellationToken);
				HashSet<string> selected = new(names, StringComparer.OrdinalIgnoreCase);
				files = files.Where(file => selected.Contains(file.FileName)).ToList();
				await _hfDownloader.DownloadSelectedAsync(files, directory, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, downloaded = files.Count }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/hf/cancel")
			{
				_hfDownloader.CancelDownloads();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/hf/pause")
			{
				_hfDownloader.PauseDownloads();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/server/start")
			{
				ServerProfile? profile = null;
				if (query.TryGetValue("id", out string? id) && !string.IsNullOrWhiteSpace(id))
					profile = await _profileManager.GetProfileAsync(id);
				profile ??= await _profileManager.GetDefaultProfileAsync();
				if (profile == null)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"Профиль не найден\"}", cancellationToken);
					return;
				}
				_serverViewModel.SelectedProfile = profile;
				ServerStatus current = await _serverManager.GetStatusAsync();
				if (current.State == LlamaStudio.Core.Enums.ServerState.Running)
				{
					if (string.Equals(_activeProfileId, profile.Id, StringComparison.OrdinalIgnoreCase))
					{
						await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true,\"message\":\"" + _localization.T("web.api.server_already_running") + "\"}", cancellationToken);
						return;
					}
					await _serverManager.StopAsync(cancellationToken);
				}
				await _serverManager.StartAsync(profile, cancellationToken);
				_activeProfileId = profile.Id;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/rpc/check")
			{
				await _serverViewModel.CheckRpcSshAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = _serverViewModel.RpcSshConnected, status = _serverViewModel.RpcSshStatus }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/rpc/start")
			{
				await _serverViewModel.StartRpcServerAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, status = _serverViewModel.RpcSshStatus }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/rpc/stop")
			{
				await _serverViewModel.StopRpcServerAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, status = _serverViewModel.RpcSshStatus }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/profile/command")
			{
				ServerProfile? commandProfile = null;
				if (query.TryGetValue("id", out string? commandId) && !string.IsNullOrWhiteSpace(commandId))
					commandProfile = await _profileManager.GetProfileAsync(commandId);
				commandProfile ??= await _profileManager.GetDefaultProfileAsync();
				if (commandProfile == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"profile not found\"}", cancellationToken);
					return;
				}
				string args = commandProfile.BuildFullArgsString();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, args, command = "llama-server " + args }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/profile/save")
			{
				if (!query.TryGetValue("id", out string? id) || string.IsNullOrWhiteSpace(id))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.profile_required") + "\"}", cancellationToken);
					return;
				}
				ServerProfile? profile = await _profileManager.GetProfileAsync(id);
				if (profile == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"Профиль не найден\"}", cancellationToken);
					return;
				}
				ApplyProfileQuery(profile, query);
				await _profileManager.SaveProfileAsync(profile);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true,\"restartRequired\":true}", cancellationToken);
				return;
			}

			if (path == "/api/profile/create")
			{
				string name = query.TryGetValue("name", out string? requestedName) && !string.IsNullOrWhiteSpace(requestedName)
					? requestedName.Trim()
					: _localization.T("web.api.new_profile");
				ServerProfile created = _profileManager.CreateProfile(name);
				await _profileManager.SaveProfileAsync(created);
				_serverViewModel.SelectedProfile = created;
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, id = created.Id }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/profile/copy")
			{
				string targetId = query.GetValueOrDefault("id")?.Trim() ?? string.Empty;
				string sourceId = query.GetValueOrDefault("sourceId")?.Trim() ?? string.Empty;
				ServerProfile? targetProfile = await _profileManager.GetProfileAsync(targetId);
				ServerProfile? source = await _profileManager.GetProfileAsync(sourceId);
				if (targetProfile == null || source == null || string.Equals(targetProfile.Id, source.Id, StringComparison.OrdinalIgnoreCase))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.two_profiles_required") + "\"}", cancellationToken);
					return;
				}
				ServerProfile copied = JsonSerializer.Deserialize<ServerProfile>(JsonSerializer.Serialize(source)) ?? new ServerProfile();
				copied.Id = targetProfile.Id;
				copied.Name = targetProfile.Name;
				copied.Description = targetProfile.Description;
				copied.IsDefault = targetProfile.IsDefault;
				copied.Color = targetProfile.Color;
				await _profileManager.SaveProfileAsync(copied);
				_serverViewModel.SelectedProfile = copied;
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/profile/cleanup-missing")
			{
				List<ServerProfile> allProfiles = await _profileManager.GetAllProfilesAsync();
				List<ServerProfile> missing = allProfiles.Where(p => !string.IsNullOrWhiteSpace(p.ModelPath) && !File.Exists(p.ModelPath)).ToList();
				foreach (ServerProfile missingProfile in missing)
				{
					if (string.Equals(_activeProfileId, missingProfile.Id, StringComparison.OrdinalIgnoreCase))
						await _serverManager.StopAsync(cancellationToken);
					await _profileManager.DeleteProfileAsync(missingProfile.Id);
				}
				if (missing.Any(p => string.Equals(_activeProfileId, p.Id, StringComparison.OrdinalIgnoreCase)))
					_activeProfileId = null;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, removed = missing.Count }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/profile/import-bat")
			{
				string importId = query.GetValueOrDefault("id")?.Trim() ?? string.Empty;
				string content = query.GetValueOrDefault("content") ?? string.Empty;
				if (string.IsNullOrWhiteSpace(content))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.bat_empty") + "\"}", cancellationToken);
					return;
				}
				try
				{
					int imported = await _serverViewModel.ImportBatTextAsync(content, importId);
					await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { ok = true, imported }, WebJsonOptions), cancellationToken);
				}
				catch (Exception ex)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", JsonSerializer.Serialize(new { error = ex.Message }, WebJsonOptions), cancellationToken);
				}
				return;
			}

			if (path == "/api/profile/rename")
			{
				if (!query.TryGetValue("id", out string? renameId) || string.IsNullOrWhiteSpace(renameId) || !query.TryGetValue("name", out string? newName) || string.IsNullOrWhiteSpace(newName))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"profile id and name are required\"}", cancellationToken);
					return;
				}
				ServerProfile? renamed = await _profileManager.GetProfileAsync(renameId);
				if (renamed == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"profile not found\"}", cancellationToken);
					return;
				}
				renamed.Name = newName.Trim();
				await _profileManager.SaveProfileAsync(renamed);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/profile/delete")
			{
				if (!query.TryGetValue("id", out string? deleteId) || string.IsNullOrWhiteSpace(deleteId))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"profile id is required\"}", cancellationToken);
					return;
				}
				if (string.Equals(_activeProfileId, deleteId, StringComparison.OrdinalIgnoreCase))
					await _serverManager.StopAsync(cancellationToken);
				bool deleted = await _profileManager.DeleteProfileAsync(deleteId);
				if (!deleted)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"profile not found\"}", cancellationToken);
					return;
				}
				if (string.Equals(_activeProfileId, deleteId, StringComparison.OrdinalIgnoreCase))
					_activeProfileId = null;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/profile/default")
			{
				if (!query.TryGetValue("id", out string? defaultId) || string.IsNullOrWhiteSpace(defaultId))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"profile id is required\"}", cancellationToken);
					return;
				}
				await _profileManager.SetDefaultProfileAsync(defaultId);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/profile/select")
			{
				if (!query.TryGetValue("id", out string? selectId) || string.IsNullOrWhiteSpace(selectId))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"profile id is required\"}", cancellationToken);
					return;
				}
				ServerProfile? selected = await _profileManager.GetProfileAsync(selectId);
				if (selected == null)
				{
					await WriteResponseAsync(stream, "404 Not Found", "application/json; charset=utf-8", "{\"error\":\"Профиль не найден\"}", cancellationToken);
					return;
				}
				_serverViewModel.SelectedProfile = selected;
				await _settings.SaveAsync();
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/chat")
			{
				if (!query.TryGetValue("message", out string? message) || string.IsNullOrWhiteSpace(message))
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"" + _localization.T("web.api.empty_message") + "\"}", cancellationToken);
					return;
				}
				ServerProfile? profile = null;
				if (query.TryGetValue("id", out string? profileId)) profile = await _profileManager.GetProfileAsync(profileId);
				profile ??= await _profileManager.GetDefaultProfileAsync();
				string host = profile?.Host ?? _settings.DefaultHost;
				int port = profile?.Port > 0 ? profile.Port : _settings.DefaultPort;
				StringBuilder answer = new();
				await _chatService.SendChatAsync(host, port, string.Empty, new List<ChatMessage>(), message, new List<string>(), false, null, profile?.Temperature ?? 0.8f, profile?.TopP ?? 0.95f, profile?.TopK ?? 40, profile?.MinP ?? 0.05f, profile?.RepeatPenalty ?? 1.1f, profile?.PresencePenalty ?? 0f, profile?.FrequencyPenalty ?? 0f, profile?.Seed ?? -1, profile?.MaxTokens ?? -1, new List<string>(), token => answer.Append(token), _ => { }, null, cancellationToken);
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", JsonSerializer.Serialize(new { answer = answer.ToString() }, WebJsonOptions), cancellationToken);
				return;
			}

			if (path == "/api/server/restart")
			{
				ServerProfile? profile = null;
				if (query.TryGetValue("id", out string? id) && !string.IsNullOrWhiteSpace(id))
					profile = await _profileManager.GetProfileAsync(id);
				profile ??= await _profileManager.GetDefaultProfileAsync();
				if (profile == null)
				{
					await WriteResponseAsync(stream, "400 Bad Request", "application/json; charset=utf-8", "{\"error\":\"Профиль не найден\"}", cancellationToken);
					return;
				}
				_serverViewModel.SelectedProfile = profile;
				await _serverManager.StopAsync(cancellationToken);
				await _serverManager.StartAsync(profile, cancellationToken);
				_activeProfileId = profile.Id;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/api/server/stop")
			{
				await _serverManager.StopAsync(cancellationToken);
				_activeProfileId = null;
				await WriteResponseAsync(stream, "200 OK", "application/json; charset=utf-8", "{\"ok\":true}", cancellationToken);
				return;
			}

			if (path == "/health")
			{
				await WriteResponseAsync(stream, "200 OK", "text/plain; charset=utf-8", "OK", cancellationToken);
				return;
			}

				await WriteResponseAsync(stream, "200 OK", "text/html; charset=utf-8", GetHtmlV2(), cancellationToken, "Cache-Control: no-store\r\n");
			}
			catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException)
			{
				// Client disconnected; this is normal for browser refreshes.
			}
		}
	}

	private bool IsAuthorized(string[] lines)
	{
		if (string.IsNullOrWhiteSpace(_settings.WebUiUsername) || string.IsNullOrEmpty(_settings.WebUiPassword))
			return false;
		foreach (string line in lines)
		{
			if (!line.StartsWith("Authorization: Basic ", StringComparison.OrdinalIgnoreCase))
				continue;
			try
			{
				string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(line[21..].Trim()));
				return decoded == _settings.WebUiUsername + ":" + _settings.WebUiPassword;
			}
			catch { return false; }
		}
		return false;
	}

	private static Dictionary<string, string> ParseQuery(string target)
	{
		Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
		int separator = target.IndexOf('?');
		if (separator < 0 || separator == target.Length - 1)
			return result;
		foreach (string item in target[(separator + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] pair = item.Split('=', 2);
			string key = Uri.UnescapeDataString(pair[0]);
			string value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;
			result[key] = value;
		}
		return result;
	}

	private static void ApplyProfileQuery(ServerProfile profile, Dictionary<string, string> query)
	{
		if (query.TryGetValue("customArguments", out string? customArguments))
		{
			profile.CustomArguments = customArguments
				.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(line => line.Trim())
				.Where(line => line.Length > 0)
				.Select(line => line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries))
				.Where(parts => parts.Length > 0)
				.ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1].Trim() : string.Empty, StringComparer.OrdinalIgnoreCase);
		}
		foreach (var pair in query)
		{
			if (pair.Key is "id" or "message" or "password" || pair.Key.Equals("RpcSshPassword", StringComparison.OrdinalIgnoreCase)) continue;
			PropertyInfo? property = typeof(ServerProfile).GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.CanWrite && p.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
			if (property == null || typeof(System.Collections.IDictionary).IsAssignableFrom(property.PropertyType)) continue;
			try
			{
				if (property.PropertyType == typeof(int[]))
				{
					property.SetValue(profile, pair.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(int.Parse).ToArray());
					continue;
				}
				Type target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
				object? value = target.IsEnum ? Enum.Parse(target, pair.Value, true) : target == typeof(string) ? pair.Value : target == typeof(bool) ? bool.Parse(pair.Value) : Convert.ChangeType(pair.Value, target, System.Globalization.CultureInfo.InvariantCulture);
				property.SetValue(profile, value);
			}
			catch { }
		}
		if (query.TryGetValue("contextSize", out string? context) && int.TryParse(context, out int contextSize)) profile.ContextSize = contextSize;
		if (query.TryGetValue("temperature", out string? temperature) && float.TryParse(temperature, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float temp)) profile.Temperature = temp;
		if (query.TryGetValue("topK", out string? topK) && int.TryParse(topK, out int topKValue)) profile.TopK = topKValue;
		if (query.TryGetValue("topP", out string? topP) && float.TryParse(topP, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float topPValue)) profile.TopP = topPValue;
		if (query.TryGetValue("minP", out string? minP) && float.TryParse(minP, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minPValue)) profile.MinP = minPValue;
		if (query.TryGetValue("rpcEnabled", out string? rpcEnabled) && bool.TryParse(rpcEnabled, out bool enabled)) profile.RpcEnabled = enabled;
		if (query.TryGetValue("rpcEndpoint", out string? rpcEndpoint)) profile.RpcEndpoint = rpcEndpoint;
		if (query.TryGetValue("rpcUseCache", out string? rpcCache) && bool.TryParse(rpcCache, out bool cache)) profile.RpcUseCache = cache;
		if (query.TryGetValue("rpcRemoteTelemetry", out string? telemetry) && bool.TryParse(telemetry, out bool remoteTelemetry)) profile.RpcRemoteTelemetry = remoteTelemetry;
		if (query.TryGetValue("rpcServerPath", out string? serverPath)) profile.RpcServerPath = serverPath;
		// r94: per-GPU power limits "0:250,1:300" (the generic loop skips dictionaries).
		if (query.TryGetValue("gpuPowerLimits", out string? gpuPowerLimitsRaw))
		{
			if (string.IsNullOrWhiteSpace(gpuPowerLimitsRaw))
			{
				profile.GpuPowerLimits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
			else
			{
				profile.GpuPowerLimits = gpuPowerLimitsRaw
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(part => part.Split(':', 2))
				.Where(parts => parts.Length == 2 && int.TryParse(parts[0].Trim(), out _))
				.ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
			}
		}
	}

	private static Dictionary<string, object?> ProfileToDictionary(ServerProfile profile)
	{
		Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
		foreach (PropertyInfo property in typeof(ServerProfile).GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!property.CanRead || property.Name is "RpcSshPassword" or "ApiKey" or "CustomArgumentToggleStates" || typeof(System.Collections.IDictionary).IsAssignableFrom(property.PropertyType)) continue;
			object? value = property.GetValue(profile);
			if (property.PropertyType == typeof(int[]))
				value = string.Join(",", (int[]?)value ?? Array.Empty<int>());
			result[property.Name] = value;
		}
		if (profile.CustomArguments is not null)
			result["CustomArguments"] = string.Join(Environment.NewLine, profile.CustomArguments.Select(pair => string.IsNullOrWhiteSpace(pair.Value) ? pair.Key : pair.Key + " " + pair.Value));
		// r94: per-GPU power limits (the generic loop skips dictionaries).
		if (profile.GpuPowerLimits is { Count: > 0 } gpuPowerLimits)
			result["GpuPowerLimits"] = string.Join(",", gpuPowerLimits.Select(pair => pair.Key + ":" + pair.Value));
		result["RpcSshPasswordSet"] = !string.IsNullOrEmpty(profile.RpcSshPassword);
		return result;
	}

	private static async Task WriteResponseAsync(NetworkStream stream, string status, string contentType, string body, CancellationToken cancellationToken, string? extraHeader = null)
	{
		byte[] payload = Encoding.UTF8.GetBytes(body);
		string headers = $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n" + (extraHeader ?? string.Empty) + "\r\n";
		byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
		await stream.WriteAsync(headerBytes.AsMemory(), cancellationToken);
		await stream.WriteAsync(payload.AsMemory(), cancellationToken);
	}

	private static IPAddress ResolveBindAddress(string? host)
	{
		if (string.IsNullOrWhiteSpace(host) || host is "0.0.0.0" or "*" or "+")
			return IPAddress.Any;
		if (host == "::")
			return IPAddress.IPv6Any;
		if (IPAddress.TryParse(host, out IPAddress? address))
			return address;
		return Dns.GetHostAddresses(host)[0];
	}

	public ValueTask DisposeAsync() => new(StopAsync());

	private string GetLlamaBaseDirectory()
	{
		if (!string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory))
			return _settings.LlamaCppBaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string defaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
		if (Directory.Exists(defaultDirectory))
			return defaultDirectory;
		if (!string.IsNullOrWhiteSpace(_settings.LlamaCppDirectory))
		{
			string selected = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (File.Exists(Path.Combine(selected, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server")))
				return Path.GetDirectoryName(selected) ?? selected;
			return selected;
		}
		return defaultDirectory;
	}

	private bool TryValidateLlamaBaseDirectory(string requested, out string directory, out string error)
	{
		directory = string.Empty;
		error = string.Empty;
		if (string.IsNullOrWhiteSpace(requested))
		{
			error = _localization.T("web.api.llama_folder_required");
			return false;
		}
		try
		{
			directory = Path.GetFullPath(requested).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string root = (Path.GetPathRoot(directory) ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
			{
				error = _localization.T("web.api.llama_root_disk");
				return false;
			}
			if (!Directory.Exists(directory))
			{
				error = _localization.T("web.api.folder_not_exists");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private async Task TrackInstallCompletionAsync(Task installTask)
	{
		try
		{
			await installTask;
		}
		catch (OperationCanceledException)
		{
			_llamaDownloadStatus = _localization.T("web.llama.cancelled");
		}
		catch (Exception ex)
		{
			_llamaInstallError = true;
			_llamaDownloadStatus = _localization.T("web.llama.error") + ex.Message;
			_log.Error(ex, "Web llama.cpp installation failed", "LlamaReleases");
		}
	}

	private static string? FindCudaToolkitDirectory(int major)
	{
		if (OperatingSystem.IsWindows())
			return null;
		foreach (string candidate in Directory.EnumerateDirectories("/usr/local", $"cuda-{major}*", SearchOption.TopDirectoryOnly).OrderByDescending(x => x, StringComparer.Ordinal))
		{
			if (File.Exists(Path.Combine(candidate, "bin", "nvcc")))
				return candidate;
		}
		return null;
	}

	private async Task InstallCuda12ToolkitAsync(CancellationTokenSource source)
	{
		const string installerUrl = "https://developer.download.nvidia.com/compute/cuda/12.8.1/local_installers/cuda_12.8.1_570.124.06_linux.run";
		string scriptPath = Path.Combine(Path.GetTempPath(), "llamastudio-cuda12-toolkit-" + Guid.NewGuid().ToString("N") + ".sh");
		try
		{
			_cudaToolkitInstallStatus = _localization.T("web.cuda.downloading_official");
			string script = $"#!/bin/sh\nset -eu\nwork=$(mktemp -d)\ntrap 'rm -rf \"$work\"' EXIT\ncurl -fL --retry 3 --connect-timeout 30 '{installerUrl}' -o \"$work/cuda.run\"\nchmod +x \"$work/cuda.run\"\n\"$work/cuda.run\" --silent --toolkit --toolkitpath=/usr/local/cuda-12.8 --override\ntest -x /usr/local/cuda-12.8/bin/nvcc\n";
			await File.WriteAllTextAsync(scriptPath, script, source.Token);
			// r52: sudo вместо pkexec — работает headless (без GUI-окна пароля)
			// и удалённо через веб (passwordless sudo на сервере).
			ProcessStartInfo startInfo = new ProcessStartInfo("sudo") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
			startInfo.ArgumentList.Add("-n");
			startInfo.ArgumentList.Add("/bin/sh");
			startInfo.ArgumentList.Add(scriptPath);
			using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException(_localization.T("web.cuda.launch_failed"));
			_cudaToolkitInstallStatus = _localization.T("web.cuda.downloading_installing");
			Task<string> stdout = process.StandardOutput.ReadToEndAsync(source.Token);
			Task<string> stderr = process.StandardError.ReadToEndAsync(source.Token);
			await process.WaitForExitAsync(source.Token);
			string error = await stderr;
			_ = await stdout;
			if (process.ExitCode != 0 || FindCudaToolkitDirectory(12) == null)
				throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? _localization.T("web.cuda.official_failed") : error.Trim());
			_cudaToolkitInstallStatus = _localization.T("web.cuda.done");
			_log.Information("CUDA Toolkit 12.8 installed from the official NVIDIA runfile without driver installation.", "CudaToolkit");
		}
		catch (OperationCanceledException)
		{
			_cudaToolkitInstallStatus = _localization.T("web.cuda.cancelled");
		}
		catch (Exception ex)
		{
			_cudaToolkitInstallStatus = _localization.T("web.cuda.error") + ex.Message;
			_log.Error(ex, "CUDA Toolkit 12 installation failed", "CudaToolkit");
		}
		finally
		{
			try { File.Delete(scriptPath); } catch { }
			_cudaToolkitInstalling = false;
			_cudaToolkitInstallCts?.Dispose();
			_cudaToolkitInstallCts = null;
		}
	}

	private const string webI18nScript = """
(function(){
window.WEB_LANG='ru';
window.WEB_EN={"Все":"All","Своё число":"Custom","Для режима Tensor слои GPU установлены автоматически: All.":"For Tensor mode GPU layers were set automatically: All.","Язык / Language":"Language","Установка: ":"Installing: ","Установлена":"Installed","Ошибка установки":"Installation error"," моделей":" models"," сохранено":" saved","GPU не обнаружены":"No GPUs detected","KV Кэш":"KV Cache","MTP и Спекулятивное":"MTP & Speculative","llama-server.exe не найден":"llama-server.exe not found","mmproj (Зрение) (--mmproj)":"mmproj (Vision) (--mmproj)","Автозапуск с системой":"Auto-start with system","Автоматическое распределение":"Automatic distribution","Автопроверка обновлений":"Auto-check for updates","Автопрокрутка":"Auto-scroll","Активна":"Active","Активный профиль: ":"Active profile: ","Веб-интерфейс":"Web interface","Веб-интерфейс включён":"Web interface enabled","Вентилятор":"Fan","Версия llama.cpp":"llama.cpp version","Войти на HF":"Log in to HF","Выбрать все":"Select all","Высокий приоритет":"High priority","Дополнительные опции":"Advanced options","Загрузка ":"Loading ","Загрузка GPU":"GPU load","Загрузка завершена":"Download complete","Загрузка отменена":"Download cancelled","Запустить":"Start","Запустить сервер":"Start server","Запустить удалённый сервер":"Start remote server","Измерить скорость":"Measure speed","Импорт .bat":"Import .bat","Использовать":"Use","Использовать mmproj":"Use mmproj","Использовать кэш":"Use cache","Использовать кэш RPC":"Use RPC cache","Использовать удалённый RPC":"Use remote RPC","Использовать черновую модель (-md)":"Use draft model (-md)","Используемые GPU":"GPUs in use","Карта памяти (--mmap)":"Memory map (--mmap)","Команда скопирована":"Command copied","Контекст":"Context","Контекст (-c)":"Context (-c)","Копировать":"Copy","Копировать команду":"Copy command","Кэш RAM":"Cache RAM","Кэш промпта":"Prompt cache","Кэш промпта (--cache-prompt)":"Prompt cache (--cache-prompt)","Логи":"Logs","Логи отсутствуют":"No logs","Метрики (--metrics)":"Metrics (--metrics)","Модели":"Models","Модели не найдены":"No models found","Модель":"Model","Модель сервера":"Server model","Модель сервера назначена":"Server model assigned","Мониторинг GPU":"GPU monitoring","Мониторинг недоступен":"Monitoring unavailable","Мощность":"Power","Мощность GPU":"GPU power","Найдено файлов: ":"Files found: ","Настройки":"Settings","Настройки подключения":"Connection settings","Не выгружать сервер при закрытии":"Keep server running on exit","Необязательно":"Optional","Непрерывный батч":"Continuous batching","Непрерывный батч (-cb)":"Continuous batching (-cb)","Нет связи":"No connection","Новый профиль":"New profile","ОЗУ":"RAM","Обновить":"Refresh","Обновить (":"Refresh (","Обновить команду":"Refresh command","Обновить список":"Refresh list","Обсуждение и поддержка":"Discussion & support","Основная модель":"Main model","Основная модель (-m)":"Main model (-m)","Остановить":"Stop","Остановить удалённый сервер":"Stop remote server","Отмена":"Cancel","Отправить":"Send","Офлайн режим (--offline)":"Offline mode (--offline)","Очистить":"Clear","Очистить без моделей":"Clear without models","Ошибка HuggingFace: ":"HuggingFace error: ","Ошибка автосохранения: ":"Autosave error: ","Ошибка входа HuggingFace: ":"HuggingFace login error: ","Ошибка загрузки":"Loading error","Ошибка запуска Toolkit: ":"Toolkit launch error: ","Ошибка запуска: ":"Launch error: ","Ошибка измерения скорости: ":"Speed test error: ","Ошибка импорта":"Import error","Ошибка обновления: ":"Update error: ","Ошибка очистки":"Clear error","Ошибка очистки: ":"Clear error: ","Ошибка проверки: ":"Check error: ","Ошибка сканирования: ":"Scan error: ","Ошибка сохранения версии":"Error saving version","Ошибка сохранения каталога: ":"Error saving directory: ","Ошибка удаления: ":"Delete error: ","Параметры RPC сохранены.":"RPC parameters saved.","Параметры мониторинга из профиля":"Monitoring parameters from profile","Параметры профиля":"Profile parameters","Перезапуск сервера с выбранным профилем…":"Restarting the server with the selected profile…","Перезапуск сервера…":"Restarting the server…","Перезапустить":"Restart","Переименовать":"Rename","Подключение SSH":"SSH connection","Подключение SSH и управление RPC":"SSH connection and RPC management","Подключение SSH…":"Connecting via SSH…","Подключение ещё не проверялось.":"The connection has not been checked yet.","Поиск":"Search","Показать команду":"Show command","Показать пароль":"Show password","Получение команды…":"Getting the command…","Получение списка файлов…":"Getting the file list…","Пользовательские аргументы":"Custom arguments","Пользовательские аргументы (по одному на строку)":"Custom arguments (one per line)","Порт RPC":"RPC port","Потоки (-t)":"Threads (-t)","Потоки batch (-tb)":"Batch threads (-tb)","Потоки в batch (-tb)":"Threads in batch (-tb)","Проверить версию":"Check version","Проверить обновления":"Check for updates","Проверить флаги":"Validate flags","Проверка версии…":"Checking the version…","Проверка обновлений…":"Checking for updates…","Проверка флагов…":"Validating flags…","Продвинуто":"Advanced","Промпт ток/с: ":"Prompt tok/s: ","Профиль и команда запуска":"Profile and launch command","Профиль назначен дефолтным":"Profile set as default","Профиль не найден":"Profile not found","Путь к llama.cpp серверу":"Path to the llama.cpp server","Путь к mmproj:":"mmproj path:","Путь к модели:":"Model path:","Путь к черновой модели .gguf":"Path to the draft .gguf model","Работа с multi-GPU":"Multi-GPU","Раздел сэмплинга":"Sampling section","Распределение модели":"Model distribution","Распределение тензоров (--tensor-split)":"Tensor distribution (--tensor-split)","Рассуждение":"Reasoning","Рассуждение (-rea)":"Reasoning (-rea)","Релизы не найдены":"No releases found","Сдвиг контекста (--context-shift)":"Context shift (--context-shift)","Сделать дефолтным":"Make default","Сервер":"Server","Сервер llama-server":"llama-server","Сервер перезапущен.":"The server has been restarted.","Сканировать модели":"Scan models","Скачать выбранные":"Download selected","Скорость генерации":"Generation speed","Слои GPU":"GPU layers","Слои GPU (-ngl)":"GPU layers (-ngl)","Слои GPU draft (-ngld)":"Draft GPU layers (-ngld)","Снять выбор":"Deselect","Создать профиль":"Create profile","Состояние:":"Status:","Сохранить":"Save","Сохранить RPC":"Save RPC","Сохранить каталог":"Save directory","Сохранить настройки":"Save settings","Сохранить профиль":"Save profile","Сохранить токен":"Save token","Сэмплинг":"Sampling","Температура":"Temperature","Температура (--temp)":"Temperature (--temp)","Температура GPU":"GPU temperature","Темп. памяти GPU":"GPU Memory Temp","°C":"°C"," · Темп. памяти: ":" · GPU mem temp: "," · Мощность: ":" · Power: ","Тип KV-кэша K (-ctk)":"KV cache type K (-ctk)","Тип KV-кэша V (-ctv)":"KV cache type V (-ctv)","Удалить":"Delete","Удалить выбранный профиль?":"Delete the selected profile?","Удалённый RPC":"Remote RPC","Удалённый компьютер":"Remote computer","Ускорение GPU":"GPU acceleration","Установить":"Install","Установить CUDA 12 Toolkit":"Install CUDA 12 Toolkit","Установленные версии не найдены":"No installed versions found","Установлено":"Installed","Чат llama-server":"llama-server chat","Черновая модель не выбрана":"No draft model selected","Экспорт .bat":"Export .bat","✓ Запущен":"✓ Running"," (не используется при tensor-split)":" (not used with tensor-split)"," · выделено: ":" · allocated: "," · кэш: до ":" · cache: up to "," · прошло ":" · elapsed "," доступных сборок":" available builds"," и сервер перезапущен":" and the server has been restarted"," строк)":" lines)"," установлено":" installed","% · Температура: ":"% · Temperature: ",". Значение сохраняется в текущем профиле.":". The value is saved in the current profile.","Draft только на локальную GPU (не DFlash/EAGLE) (--spec-draft-device)":"Draft only on the local GPU (not DFlash/EAGLE) (--spec-draft-device)","Draft-модель":"Draft model","Draft-модель (-md)":"Draft model (-md)","Draft-модель только на локальную GPU":"Draft model only on the local GPU","GGUF-файл модели, используемый llama-server":"GGUF model file used by llama-server","HuggingFace репозиторий":"HuggingFace repository","HuggingFace репозиторий (-hf)":"HuggingFace repository (-hf)","Locally typical sampling. 1.0 отключает параметр.":"Locally typical sampling. 1.0 disables the parameter.","Min-P sampling: отсеивает маловероятные токены. 0 отключает ограничение.":"Min-P sampling: filters out unlikely tokens. 0 disables the limit.","Mirostat: 0 — отключён, 1 — Mirostat, 2 — Mirostat 2.0. При включении заменяет Top-K, Top-P и Typical-P.":"Mirostat: 0 — disabled, 1 — Mirostat, 2 — Mirostat 2.0. When enabled, it replaces Top-K, Top-P and Typical-P.","Nucleus sampling: ограничивает выбор токенов накопленной вероятностью. 1.0 отключает ограничение.":"Nucleus sampling: limits token choice by cumulative probability. 1.0 disables the limit.","RPC сохранён. Для применения перезапустите сервер.":"RPC saved. Restart the server to apply.","RPC-телеметрия":"RPC telemetry","SSH используется для управления удалённым ggml-rpc-server.exe и отдельного запроса телеметрии GPU.":"SSH is used to manage the remote ggml-rpc-server.exe and for a separate GPU telemetry request.","SSH подключается отдельно от запуска llama-server. Пароль не отображается после сохранения.":"SSH connects separately from starting llama-server. The password is not shown after saving.","Seed генератора случайных чисел. -1 — случайное значение.":"Random number generator seed. -1 — a random value.","Top-K sampling: число наиболее вероятных токенов. 0 отключает ограничение.":"Top-K sampling: the number of most probable tokens. 0 disables the limit.","mmproj назначен":"mmproj assigned","°C · Мощность: ":"°C · Power: ","Авто":"Auto","Автоматически":"Automatically","Адрес llama-server":"llama-server address","Адрес llama-server (--host)":"llama-server address (--host)","Адрес веб-интерфейса":"Web interface address","Адрес сервера недоступен":"Server address unavailable","Активна: ":"Active: ","Активная":"Active","Активная версия не выбрана":"No active version selected","Блокировка памяти (--mlock)":"Memory lock (--mlock)","Введите сообщение":"Enter a message","Вверх":"Up","Версия llama.cpp сохранена. Для применения перезапустите сервер.":"llama.cpp version saved. Restart the server to apply.","Версия активирована":"Version activated","Версия сохранена":"Version saved","Версия удалена":"Version deleted","Включена":"Enabled","Включить слоты (--slots)":"Enable slots (--slots)","Встроенный Web UI (--ui)":"Built-in Web UI (--ui)","Вход HuggingFace запущен в браузере":"HuggingFace login opened in the browser","Выберите локальные и RPC GPU. Значения сохраняются в SelectedGpuIndices и участвуют в построении команды.":"Select local and RPC GPUs. The values are saved in SelectedGpuIndices and are used when building the command.","Выберите локальные и RPC GPU. Значения сохраняются в профиле и используются при построении команды.":"Select local and RPC GPUs. The values are saved in the profile and are used when building the command.","Выбор папки":"Select a folder","Выбранные GPU":"Selected GPUs","Выбрать":"Select","Выбрать из найденных моделей…":"Select from the found models…","Выбрать каталог":"Select a directory","Выбрать папку":"Select a folder","Выбрать файл":"Select a file","Выключена":"Disabled","Выполняется · ":"Running · ","Выполняется…":"In progress…","Генерация…":"Generating…","Глобальные настройки Studio":"Global Studio settings","Готово":"Done","Данные GPU недоступны":"GPU data unavailable","Дефолтный профиль":"Default profile","Диски":"Drives","Для основной GPU нужна выбранная локальная карта.":"A selected local card is required for the main GPU.","Для трёх и более GPU используйте автоматическое распределение по памяти карт.":"For three or more GPUs, use automatic distribution by card memory.","Для этой сборки нужен CUDA Toolkit 12.x. Установщик добавит только Toolkit 12.8 и не меняет драйвер NVIDIA.":"This build requires CUDA Toolkit 12.x. The installer will add only Toolkit 12.8 and will not replace the NVIDIA driver.","Документация и обсуждение проекта доступны пользователю через указанные ссылки.":"Project documentation and discussion are available to the user through the links provided.","Дополнительные параметры модели":"Additional model parameters","Журнал событий":"Event log","Загрузка…":"Loading…","Запуск удалённого RPC-сервера…":"Starting the remote RPC server…","Запуск установки ":"Starting the installation of ","Запуск установщика CUDA Toolkit 12.8…":"Starting the CUDA Toolkit 12.8 installer…","Запускать сервер при старте":"Start the server on launch","Измерение…":"Measuring…","Импортировано аргументов: ":"Arguments imported: ","Имя API / alias":"API name / alias","Имя API / alias (-a)":"API name / alias (-a)","Имя модели / alias (-a)":"Model name / alias (-a)","Исполняемый файл сервера llama-server.exe находится в выбранной установленной версии llama.cpp.":"The llama-server executable is located in the selected installed version of llama.cpp.","Используемые GPU (например 0,1,100000) (--device)":"GPUs in use (e.g. 0,1,100000) (--device)","Каталог llama.cpp":"llama.cpp directory","Каталог выбран: ":"Directory selected: ","Каталог моделей":"Models directory","Каталог моделей сохранён":"Models directory saved","Каталог не найден":"Directory not found","Количество последних токенов для штрафа повторов. -1 использует размер контекста.":"Number of last tokens for the repeat penalty. -1 uses the context size.","Количество слотов (-np)":"Number of slots (-np)","Команда ещё не загружена":"The command has not been loaded yet","Команда запуска":"Launch command","Команда не сформирована":"The command has not been formed","Кэш RAM (--cache-ram)":"Cache RAM (--cache-ram)","Логи отсутствуют (сервер не запущен или вывод ещё не получен)":"No logs (the server is not started or output has not been received yet)","Логин":"Login","Логин SSH":"SSH login","Макс. токенов (-n)":"Max. tokens (-n)","Максимум токенов (-n)":"Maximum tokens (-n)","Масштабирование Rope (--rope-scaling)":"Rope scaling (--rope-scaling)","Мониторинг GPU загружается…":"Loading GPU monitoring…","На удалённом компьютере должна быть включена служба OpenSSH Server.":"The OpenSSH Server service must be enabled on the remote computer.","Нагрузка обновляется автоматически и не перезапускает llama-server.":"The load is updated automatically and does not restart llama-server.","Нажмите «Проверить версию», чтобы выполнить проверку вручную.":"Click \"Check version\" to run the check manually.","Название нового профиля":"Name of the new profile","Настройки сохранены. Перезапустите Studio для смены адреса или порта.":"Settings saved. Restart Studio to change the address or port.","Настройки сохранены. Перезапустите Studio при изменении адреса или порта.":"Settings saved. Restart Studio if the address or port was changed.","Не выбрано":"Not selected","Не выполнено":"Not performed","Не удалось выбрать каталог: ":"Failed to select the directory: ","Не удалось загрузить журнал: ":"Failed to load the log: ","Не удалось загрузить настройки: ":"Failed to load the settings: ","Не удалось назначить":"Failed to assign","Не удалось открыть вход HuggingFace":"Failed to open the HuggingFace login","Не удалось открыть выбор файла: ":"Failed to open the file picker: ","Не удалось получить состояние llama.cpp":"Failed to get the llama.cpp status","Не удалось получить список GPU":"Failed to get the GPU list","Не удалось проверить CUDA Toolkit":"Failed to check the CUDA Toolkit","Не удалось прочитать сборки":"Failed to read the builds","Не удалось прочитать установленные сборки":"Failed to read the installed builds","Не удалось создать профиль":"Failed to create the profile","Не удалось сохранить версию":"Failed to save the version","Не удалось сохранить каталог":"Failed to save the directory","Не удалось сохранить профиль":"Failed to save the profile","Не удалось сохранить токен":"Failed to save the token","Необязательная модель для speculative decoding":"An optional model for speculative decoding","Непрерывный batch (-cb)":"Continuous batching (-cb)","Ничего не найдено":"Nothing found","Новое название профиля":"New profile name","Обзор…":"Browsing…","Обновления не найдены":"No updates found","Обработка prompt":"Prompt processing","Определение адреса сервера…":"Determining the server address…","Основная GGUF-модель":"Main GGUF model","Основной GPU":"Main GPU","Основной GPU (-mg)":"Main GPU (-mg)","Остановка удалённого RPC-сервера…":"Stopping the remote RPC server…","Отключено":"Disabled","Открывается вход HuggingFace…":"Opening the HuggingFace login…","Открывается выбор каталога…":"Opening the directory picker…","Открыть":"Open","Открыть чат llama-server":"Open the llama-server chat","Ошибка: ":"Error: ","Папка пуста.":"The folder is empty.","Папка с серверами llama.cpp":"Folder with llama.cpp servers","Папка сохранена":"Folder saved","Параметр llama-server: ":"llama-server parameter: ","Параметр сохранён автоматически.":"The parameter was saved automatically.","Пароль":"Password","Пароль SSH":"SSH password","Пауза":"Pause","Пауза загрузки":"Pause the download","Передавать телеметрию":"Send telemetry","Передавать телеметрию с RPC":"Send telemetry from RPC","Переключение версии…":"Switching the version…","По VRAM":"By VRAM","Повторное использование кэша (--cache-reuse)":"Cache reuse (--cache-reuse)","Подготовка":"Preparing","Подключение и управление RPC-сервером":"Connecting to and managing the RPC server","Подключить SSH":"Connect via SSH","Подробный журнал (--verbose)":"Detailed log (--verbose)","Подходящая CUDA-сборка не найдена":"No suitable CUDA build found","Поиск…":"Searching…","Получить релизы":"Get releases","Получить файлы":"Get files","Помощь по Llama Studio":"Llama Studio help","Поровну":"Evenly","Порт (--port)":"Port (--port)","Порт llama-server":"llama-server port","Порт веб-интерфейса":"Web interface port","Последние токены":"Last tokens","Последние токены (--repeat-last-n)":"Last tokens (--repeat-last-n)","Последняя: ":"Latest: ","Привязка CPU (-C)":"CPU affinity (-C)","Приложение":"Application","Приоритет процесса":"Process priority","Профиль сохранён. Для применения параметров перезапустите сервер.":"Profile saved. Restart the server to apply the parameters.","Профиль сохранён. Для применения перезапустите сервер.":"Profile saved. Restart the server to apply.","Профиль сохранён. Перезапустите сервер для применения.":"Profile saved. Restart the server to apply.","Пути к моделям относятся к локальному компьютеру. Путь ggml-rpc-server.exe задаётся отдельно только во вкладке RPC.":"Model paths refer to the local computer. The ggml-rpc-server.exe path is set separately, only in the RPC tab.","Путь ggml-rpc-server.exe":"ggml-rpc-server.exe path","Путь для сохранения журнала:":"Path to save the log:","Путь к draft":"Path to the draft","Путь к ggml-rpc-server.exe на удалённом компьютере":"Path to ggml-rpc-server.exe on the remote computer","Распределение":"Distribution","Распределение GPU (-ts)":"GPU distribution (-ts)","Режим embeddings (--embedding)":"Embeddings mode (--embedding)","Режим распределения":"Distribution mode","Режим распределения (-sm)":"Distribution mode (-sm)","Релиз":"Release","Репозиторий draft":"Draft repository","Репозиторий draft (-hfd)":"Draft repository (-hfd)","Сборка":"Build","Сборки не найдены":"No builds found","Сканирование завершено: ":"Scan finished: ","Скорость":"Speed","Скорость обучения Mirostat (eta). По умолчанию 0.1.":"Mirostat learning rate (eta). Default 0.1.","Слои":"Layers","Сначала сохраните параметры профиля, затем проверьте SSH. SSH остаётся доступным независимо от состояния ggml-rpc-server.":"First save the profile parameters, then check SSH. SSH remains available regardless of the state of ggml-rpc-server.","Сообщение пока отправляется через desktop API…":"The message is sent through the desktop API for now…","Сохранено: ":"Saved: ","Сохранять пароль":"Save the password","Таймаут (-to)":"Timeout (-to)","Температура сэмплирования. По умолчанию 0.8; меньше — более детерминированный ответ.":"Sampling temperature. Default 0.8; lower values give a more deterministic answer.","Тензоры":"Tensors","Тип speculative (--spec-type)":"Speculative type (--spec-type)","Токен HuggingFace сохранён":"HuggingFace token saved","Токены MTP (-n)":"MTP tokens (-n)","Токены генерации":"Generation tokens","Требовать логин и пароль":"Require login and password","Удалено профилей: ":"Profiles deleted: ","Удалить профили, у которых отсутствуют файлы моделей?":"Delete profiles whose model files are missing?","Удалить установленную версию llama.cpp?":"Delete the installed llama.cpp version?","Уже скачан":"Already downloaded","Укажите имя или ID профиля-источника:\n":"Specify the name or ID of the source profile:\n","Укажите каталог моделей":"Specify the models directory","Укажите полный путь к ggml-rpc-server.exe на удалённом компьютере. Желательно, чтобы версии llama.cpp на обоих компьютерах совпадали.":"Specify the full path to ggml-rpc-server.exe on the remote computer. The llama.cpp versions on both computers should ideally match.","Укажите репозиторий, каталог и выберите файлы":"Specify the repository, the directory and select the files","Управление RPC-сервером":"RPC server management","Управление llama-server":"llama-server management","Установить официальный NVIDIA CUDA Toolkit 12.8? Будет загружено около 5 ГБ. Драйвер NVIDIA не будет заменён.":"Install the official NVIDIA CUDA Toolkit 12.8? About 5 GB will be downloaded. The NVIDIA driver will not be replaced.","Установка CUDA Toolkit…":"Installing CUDA Toolkit…","Установка Toolkit…":"Installing Toolkit…","Установка завершена":"Installation finished","Установленные сборки не найдены":"No installed builds found","Установлено: ":"Installed: ","Файл HuggingFace":"HuggingFace file","Файл HuggingFace (-hff)":"HuggingFace file (-hff)","Файл mmproj":"mmproj file","Файл mmproj (--mmproj)":"mmproj file (--mmproj)","Файл vision-проектора для мультимодальных моделей":"Vision projector file for multimodal models","Файл журнала (--log-file)":"Log file (--log-file)","Файл не выбран":"No file selected","Файлы не найдены":"No files found","Целевая энтропия Mirostat (tau). По умолчанию 5.":"Mirostat target entropy (tau). Default 5.","Чат работает во встроенном веб-интерфейсе llama-server. Откройте его в новой вкладке браузера.":"The chat works in the llama-server built-in web interface. Open it in a new browser tab.","Черновая GGUF-модель":"Draft GGUF model","Черновая модель (спекулятивное декодирование) (-md)":"Draft model (speculative decoding) (-md)","Штраф за повторяющиеся последовательности. 1.0 отключает штраф.":"Penalty for repeating sequences. 1.0 disables the penalty.","Штраф за уже встречавшиеся токены. 0 отключает параметр.":"Penalty for already encountered tokens. 0 disables the parameter.","Штраф за частые токены. 0 отключает параметр.":"Penalty for frequent tokens. 0 disables the parameter.","Штраф повторов":"Repeat penalty","Штраф повторов (--repeat-penalty)":"Repeat penalty (--repeat-penalty)","ток/с: ":"tok/s: ","↑ Доступно обновление, ":"↑ An update is available, ","Не удалось открыть путь.":"Failed to open the path.","Не удалось получить скорость: ":"Failed to get the speed: ","Не удалось получить состояние":"Failed to get the status","GGUF-модели":"GGUF models","GitHub проекта":"Project on GitHub","Telegram проекта":"Project on Telegram","Активный профиль":"Active profile","Главная":"Home","Доступные релизы":"Available releases","Зрение (mmproj)":"Vision (mmproj)","Использование GPU":"GPU usage","Локальные и удалённые GPU":"Local and remote GPUs","Мониторинг":"Monitoring","Найдено":"Found","Официальные":"Official","Подготовка установки…":"Preparing installation…","Профили":"Profiles","Профиль":"Profile","Релизы llama.cpp":"llama.cpp releases","Репозиторий":"Repository","Сообщение":"Message","Только последняя версия":"Latest version only","Установленные версии":"Installed versions","Чат":"Chat","Черновик":"Draft","Экспериментальные":"Experimental","Экспериментальные сборки":"Experimental builds","✓ Версия актуальна":"✓ Version is up to date","Введите название модели для поиска HuggingFace":"Enter model name to search HuggingFace","Каталог загрузки и моделей":"Download and models directory","Поиск моделей…":"Search models…","Путь на компьютере со Studio":"Path on the computer running Studio","Репозиторий, например TheBloke/model-GGUF":"Repository, e.g. TheBloke/model-GGUF","Токен авторизации HF (для приватных репозиториев)":"HF auth token (for private repositories)"};
window.WEB_TT_RU={"server.tip.additional_args":"Что это:\\nПоле для ввода любых дополнительных аргументов командной строки llama-server, которых нет в графическом интерфейсе.\\n\\nЗначения:\\n• Любой валидный флаг llama-server, по одному на строку\\n• Примеры: --flash-attn-best-fit, --ctx-size-ext 65536\\n\\nРекомендации:\\n• Используйте для экспериментальных флагов из документации llama.cpp\\n• Каждый аргумент с новой строки","server.tip.batch_size":"Флаг: -b / --batch-size\\n\\nЧто это:\\nМаксимальное количество токенов промпта, обрабатываемых за раз. Влияет на скорость чтения входного текста моделью.\\n\\nЗначения:\\n• 64–131072 (по умолчанию: 2048)\\n\\nПреимущества:\\n• Больший батч = быстрее обработка длинных промптов\\n• Эффективнее использование GPU\\n\\nРекомендации:\\n• Оставьте 2048 для большинства задач\\n• Увеличьте до 4096-8192 если медленно обрабатываются длинные сообщения","server.tip.cache_prompt":"Флаг: --cache-prompt\\n\\nЧто это:\\nКэширование промптов для повторных запросов. Сохраняет промежуточные результаты вычислений для ускорения.\\n\\nПреимущества:\\n• Значительно ускоряет работу в диалогах (повторные сообщения)\\n• Экономит время при редактировании промпта\\n\\nРекомендации:\\n• Оставьте включённым — значительно ускоряет работу в диалогах","server.tip.cache_ram":"Флаг: --cache-ram\\n\\nЧто это:\\nКоличество RAM в мегабайтах, выделенное для кэширования KV-кэша.\\n\\nРекомендации:\\n• Оставьте пустым для автоматического определения\\n• Укажите если нужно ограничить использование RAM","server.tip.cache_reuse":"Флаг: --cache-reuse\\n\\nЧто это:\\nРазмер буфера переиспользования кэша в токенах. Позволяет переиспользовать вычисления для общих префиксов запросов.\\n\\nЗначения:\\n• Число токенов (по умолчанию: 0 = отключено)\\n\\nРекомендации:\\n• Включите для ускорения повторных запросов с общими префиксами\\n• 0.6-0.8 от размера контекста — хороший баланс","server.tip.cache_type_k":"Флаг: -ctk\\n\\nЧто это:\\nТип данных для кэша ключей (K) в механизме внимания. Влияет на потребление памяти и скорость.\\n\\nЗначения:\\n• f16 — полная точность, больше VRAM\\n• q8_0 — 8-бит квантование, баланс\\n• q4_0 — 4-бит квантование, экономия VRAM\\n\\nРекомендации:\\n• f16 для лучшего качества\\n• q8_0 для баланса качество/память","server.tip.cache_type_v":"Флаг: -ctv\\n\\nЧто это:\\nТип данных для кэша значений (V) в механизме внимания. Влияет на потребление памяти и скорость.\\n\\nЗначения:\\n• f16 — полная точность, больше VRAM\\n• q8_0 — 8-бит квантование, баланс\\n• q4_0 — 4-бит квантование, экономия VRAM\\n\\nРекомендации:\\n• f16 для лучшего качества\\n• q8_0 для баланса качество/память","server.tip.cont_batching":"Флаг: -cb / --no-cb\\n\\nЧто это:\\nПозволяет серверу обрабатывать новые запросы, не дожидаясь завершения текущих. Критически важный параметр для многопользовательской работы!\\n\\nПреимущества:\\n• Лучшая пропускная способность при множестве одновременных запросов\\n• Меньше задержек для новых клиентов\\n• Плавная работа сервера под нагрузкой\\n\\nРекомендации:\\n• Оставьте включённым для лучшей производительности\\n• Отключайте только если возникают проблемы со стабильностью","server.tip.context_shift":"Флаг: --context-shift\\n\\nЧто это:\\nАвтоматический сдвиг контекста при переполнении. Позволяет продолжать диалог без потери истории.\\n\\nРекомендации:\\n• Оставьте включённым (по умолчанию)","server.tip.ctx_size":"Флаг: -c / --ctx-size\\n\\nЧто это:\\nМаксимальная длина диалога в токенах. 1 токен ≈ 0.75 слова. Определяет, сколько текста модель может запомнить за раз.\\n\\nЗначения:\\n• 256–131072 (по умолчанию: 4096)\\n• Примеры: 4096 ≈ 3000 слов, 8192 ≈ 6000 слов, 32768 ≈ 24000 слов\\n\\nРекомендации:\\n• Обычный чат: 4096-8192\\n• Длинные документы: 16384+\\n• Больше контекст = больше VRAM/RAM (пропорционально)","server.tip.draft_model_path":"Флаг: --spec-draft-model\\n\\nЧто это:\\nПуть к файлу черновой (draft) модели для спекулятивного декодирования.\\n\\nРекомендации:\\n• Используйте лёгкую модель (Phi-2, Qwen2-1.5B) как draft\\n• Draft модель должна быть значительно меньше основной\\n• Формат: .gguf","server.tip.embedding_mode":"Флаг: --embedding\\n\\nЧто это:\\nОграничивает сервер режимом эмбеддинга. Модель генерирует только векторные представления текста, а не ответы.\\n\\nКогда включать:\\n• Только для моделей эмбеддингов (text-embedding-3-small и т.д.)\\n• Для поиска похожих текстов, классификации, RAG-систем\\n\\nРекомендации:\\n• Оставьте выключенным для обычных языковых моделей","server.tip.flash_attention":"Флаг: -fa / --flash-attn\\n\\nЧто это:\\nОптимизированный алгоритм вычисления внимания (attention). Значительно ускоряет работу и экономит память.\\n\\nПреимущества:\\n• Ускорение обработки промпта в 2-4 раза\\n• Меньше потребление VRAM при том же контексте\\n• Позволяет использовать больший размер контекста\\n\\nРекомендации:\\n• Оставьте включённым для максимальной производительности\\n• Если возникают ошибки — попробуйте отключить (редко нужно)","server.tip.frequency_penalty":"Флаг: --frequency-penalty\\n\\nЧто это:\\nШтраф за частоту появления токена. Чем чаще слово встречалось, тем меньше вероятность его повторения (как в GPT-4).\\n\\nЗначения:\\n• -2.0–2.0 (по умолчанию: 0.0)\\n• 0.1-0.5 — уменьшение повторяющихся фраз\\n\\nРекомендации:\\n• Оставьте 0.0 для большинства задач","server.tip.gpu_layers":"Флаг: -ngl\\n\\nЧто это:\\nОпределяет, сколько слоёв нейросети будет работать на видеокарте. Больше слоёв на GPU = быстрее генерация текста.\\n\\nЗначения:\\n• Число (например 32) — точно столько слоёв на GPU\\n• all — все слои на GPU (максимальная скорость)\\n• auto — автоопределение по доступной VRAM\\n• 0 — всё на CPU (медленно, но работает без видеокарты)\\n\\nРекомендации:\\n• Для большинства задач ставьте all\\n• Если не хватает VRAM — уменьшите число или используйте auto","server.tip.host":"Флаг: --host\\n\\nЧто это:\\nIP-адрес, на котором сервер будет слушать входящие подключения.\\n\\nЗначения:\\n• 127.0.0.1 — только локальный доступ (по умолчанию, безопасно)\\n• 0.0.0.0 — доступ из сети (все интерфейсы)\\n• Конкретный IP — только с определённого адреса\\n\\nРекомендации:\\n• Оставьте 127.0.0.1 для безопасности\\n• Используйте 0.0.0.0 если нужно подключаться с других устройств в сети","server.tip.kv_offload":"Флаг: --kv-offload\\n\\nЧто это:\\nВыгрузка KV-кэша (промежуточных результатов) на видеокарту.\\n\\nПреимущества:\\n• Быстрее генерация при наличии VRAM\\n• Меньше нагрузка на CPU и RAM\\n\\nРекомендации:\\n• Оставьте включённым для лучшей производительности\\n• Отключите только при критической нехватке VRAM","server.tip.main_gpu":"Флаг: --main-gpu\\n\\nЧто это:\\nИндекс основной видеокарты для вычислений при наличии нескольких GPU.\\n\\nЗначения:\\n• 0 — первая видеокарта (по умолчанию)\\n• 1, 2, 3... — вторая, третья и т.д.\\n\\nРекомендации:\\n• Оставьте 0 если у вас одна видеокарта\\n• При двух картах: поставьте индекс более мощной карты","server.tip.metrics":"Флаг: --metrics\\n\\nЧто это:\\nОткрывает endpoint /metrics с данными в формате Prometheus для мониторинга производительности сервера.\\n\\nПреимущества:\\n• Отслеживание использования VRAM/RAM в реальном времени\\n• Мониторинг скорости генерации (токенов/сек)\\n• Интеграция с системами визуализации\\n\\nКогда включать:\\n• Для интеграции с Grafana, Zabbix или другими системами мониторинга\\n\\nРекомендации:\\n• Оставьте отключённым если не используете мониторинг","server.tip.min_p":"Флаг: --min-p\\n\\nЧто это:\\nМинимальная вероятность токена относительно самого вероятного. Отсеивает токены, которые значительно хуже лучшего варианта.\\n\\nЗначения:\\n• 0.0 — отключено (по умолчанию)\\n• 0.05-0.1 — мягкая фильтрация (рекомендуется)\\n• 0.2+ — строгая фильтрация\\n\\nРекомендации:\\n• 0.05-0.1 хорошо работает для большинства задач","server.tip.mirostat":"Флаг: --mirostat / --mirostat2\\n\\nЧто это:\\nАдаптивный алгоритм сэмплирования, который автоматически подстраивает случайность ответов для поддержания заданного уровня perplexity (непредсказуемости текста).\\n\\nПреимущества:\\n• Автоматическая подстройка температуры в процессе генерации\\n• Может улучшить качество при неправильных настройках temp/top-p\\n\\nРекомендации:\\n• По умолчанию выключено — работает и без него\\n• Попробуйте включить если хотите умную подстройку параметров","server.tip.mirostat_eta":"Флаг: --mirostat-eta\\n\\nЧто это:\\nСкорость адаптации алгоритма Mirostat. Как быстро он подстраивает параметры под целевую perplexity.\\n\\nЗначения:\\n• 0.0–1.0 (по умолчанию: 0.1)\\n\\nРекомендации:\\n• Оставьте 0.1 — оптимальное значение","server.tip.mirostat_tau":"Флаг: --mirostat-tau\\n\\nЧто это:\\nЦелевая perplexity для Mirostat. Определяет, насколько удивительным должен быть текст.\\n\\nЗначения:\\n• 1.0 — очень предсказуемый текст\\n• 5.0 — баланс (по умолчанию)\\n• 10.0 — более разнообразный и неожиданный текст\\n\\nРекомендации:\\n• Оставьте 5.0 для большинства задач","server.tip.mlock":"Флаг: --mlock\\n\\nЧто это:\\nУдерживает модель в оперативной памяти, предотвращая своппинг (перемещение на диск) при нехватке RAM.\\n\\nПреимущества:\\n• Стабильная скорость работы без замираний\\n• Модель не замедляется когда система загружена\\n\\nРекомендации:\\n• Включите если у вас достаточно свободной RAM (в 1.5-2 раза больше модели)\\n• На системах с 16GB+ RAM для моделей до 7B — обязательно включите","server.tip.mmap":"Флаг: --mmap\\n\\nЧто это:\\nЗагрузка модели через memory-mapped файлы. Операционная система сама управляет кэшированием данных модели в памяти.\\n\\nПреимущества:\\n• Быстрая загрузка модели\\n• ОС может выгрузить неиспользуемые части в swap при необходимости\\n• Меньше пиковое потребление RAM при старте\\n\\nРекомендации:\\n• Оставьте включённым (по умолчанию)\\n• Отключите только если модель вылетает из памяти при многозадачности","server.tip.mmproj":"Флаг: --mmproj\\n\\nЧто это:\\nФайл мультимодального проектора. Позволяет текстовой модели видеть и анализировать изображения.\\n\\nКогда включать:\\n• Только для vision-моделей (LLaVA, Bakllava и т.д.)\\n• Обычным языковым моделям НЕ нужен\\n\\nРекомендации:\\n• Файл проектора идёт вместе с vision-моделью при скачивании\\n• Без этого файла модель не сможет обрабатывать картинки","server.tip.mmq":"Флаг: --no-mmq\\n\\nЧто это:\\nОтключает многоуровневое квантование (MMQ). При включении MMQ позволяет смешанное квантование слоёв модели.\\n\\nРекомендации:\\n• Оставьте отключённым для лучшей совместимости","server.tip.model_path":"Флаг: -m\\n\\nЧто это:\\nПуть к файлу GGUF модели для загрузки. Это основная нейросеть, которая будет отвечать на ваши запросы.\\n\\nЗначения:\\n• Полный путь к .gguf файлу\\n• Поддерживается перетаскивание файла в поле\\n\\nРекомендации:\\n• Используйте модели с Instruct в названии для чата\\n• Размер модели зависит от VRAM/RAM: 7B (~4GB), 13B (~8GB), 70B (~40GB)","server.tip.mtp_predict":"Флаг: -n / --predict\\n\\nЧто это:\\nМаксимальная длина генерируемого ответа в токенах.\\n\\nЗначения:\\n• -1 — без ограничений (по умолчанию)\\n• 256–131072 — точное ограничение в токенах\\n\\nРекомендации:\\n• Для чата: 256-512 (короткие ответы)\\n• Для длинных ответов и статей: 1024+","server.tip.numa":"Флаг: --numa\\n\\nЧто это:\\nNUMA-aware распределение памяти. Оптимизирует работу с памятью на серверах с несколькими CPU сокетами.\\n\\nКогда включать:\\n• Только на серверах с 2+ CPU сокетами (многопроцессорные системы)\\n\\nРекомендации:\\n• На обычных ПК и ноутбуках НЕ нужно включать\\n• На серверном оборудовании может дать прирост производительности","server.tip.parallel":"Флаг: --parallel / -np\\n\\nЧто это:\\nКоличество параллельных слотов запросов. Определяет, сколько запросов сервер может обрабатывать одновременно.\\n\\nЗначения:\\n• Число — точное количество слотов (по умолчанию: 4)\\n• -1 — автоопределение\\n\\nПреимущества:\\n• Больше слотов = больше одновременных запросов\\n• Каждый слот потребляет VRAM/RAM пропорционально размеру контекста\\n\\nРекомендации:\\n• Для одного пользователя: 2-4 достаточно\\n• Увеличьте если работаете с несколькими клиентами одновременно","server.tip.port":"Флаг: --port\\n\\nЧто это:\\nTCP-порт, на котором будет работать сервер.\\n\\nЗначения:\\n• 1–65535 (по умолчанию: 8080)\\n\\nРекомендации:\\n• Оставьте 8080 или измените если порт занят другой программой","server.tip.predict":"Флаг: -n / --predict\\n\\nЧто это:\\nМаксимальная длина генерируемого ответа в токенах. Ограничивает, сколько текста модель может написать за один раз.\\n\\nЗначения:\\n• -1 — без ограничений (по умолчанию)\\n• 256–131072 — точное ограничение в токенах\\n\\nРекомендации:\\n• Для чата: 256-512 (короткие ответы)\\n• Для длинных ответов и статей: 1024+\\n• -1 = без ограничений, модель остановится сама","server.tip.predict_count":"Флаг: -np / --predict-count\\n\\nЧто это:\\nКоличество предсказаний на запрос в режиме MTP.\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического определения","server.tip.presence_penalty":"Флаг: --presence-penalty\\n\\nЧто это:\\nШтраф за присутствие токена в контексте. Поощряет модель использовать новые слова и темы (как в GPT-4).\\n\\nЗначения:\\n• -2.0–2.0 (по умолчанию: 0.0)\\n• 0.1-0.5 — лёгкое разнообразие тем\\n\\nРекомендации:\\n• Оставьте 0.0 для большинства задач\\n• 0.1-0.3 если хотите более разнообразные ответы","server.tip.priority_high":"Флаг: process priority\\n\\nЧто это:\\nУстанавливает высокий приоритет процесса сервера в операционной системе.\\n\\nПреимущества:\\n• Лучший отклик при высокой загрузке системы\\n• Приоритетное распределение CPU времени\\n\\nРекомендации:\\n• Включите если сервер работает на выделенной машине\\n• На рабочих станциях может замедлить другие приложения","server.tip.repeat_last_n":"Флаг: --repeat-last-n\\n\\nЧто это:\\nОкно токенов для проверки повторений. Определяет, сколько последних слов модель будет проверять на дублирование.\\n\\nЗначения:\\n• 64 — по умолчанию в llama.cpp\\n• -1 — весь контекст\\n• 0 — отключено\\n• Число — только последние N токенов\\n\\nРекомендации:\\n• Оставьте 64 для большинства задач\\n• Поставьте -1 для проверки всего контекста","server.tip.repeat_penalty":"Флаг: --repeat-penalty / -rep-pen\\n\\nЧто это:\\nШтраф за повторение одинаковых токенов. Предотвращает зацикливание модели на одних и тех же фразах.\\n\\nЗначения:\\n• 1.0 — отключено (нет штрафа)\\n• 1.1 — лёгкий штраф (по умолчанию, рекомендуется)\\n• 1.2-1.3 — умеренный штраф\\n• 1.5+ — сильный штраф (может исказить текст)\\n\\nРекомендации:\\n• Оставьте 1.1 для большинства задач\\n• Увеличьте до 1.2-1.3 если модель повторяется","server.tip.rope_freq_base":"Флаг: --rope-freq-base\\n\\nЧто это:\\nБазовая частота позиционного кодирования RoPE. Влияет на то, как модель понимает порядок слов в тексте.\\n\\nЗначения:\\n• Число с плавающей точкой (по умолчанию: из модели)\\n\\nРекомендации:\\n• Не меняйте без необходимости — используется для fine-tuning под другие длины контекста","server.tip.rope_freq_scale":"Флаг: --rope-freq-scale\\n\\nЧто это:\\nМасштаб частоты RoPE. Позволяет растянуть контекст модели за пределы оригинального размера.\\n\\nЗначения:\\n• Число > 0 (по умолчанию: из модели)\\n• 2.0 — удвоение эффективного контекста\\n\\nПреимущества:\\n• Позволяет использовать модель с большим контекстом чем она обучена\\n• Альтернатива YARN для экстраполяции контекста","server.tip.rope_scaling":"Флаг: --rope-scaling\\n\\nЧто это:\\nТип масштабирования позиционного кодирования RoPE. Определяет, как модель обрабатывает контекст за пределами тренировочного размера.\\n\\nЗначения:\\n• none — без масштабирования\\n• linear — линейное масштабирование\\n• yarn — YARN (более качественная экстраполяция)\\n\\nРекомендации:\\n• Используйте yarn для лучшего качества при увеличении контекста\\n• linear — простая альтернатива","server.tip.seed":"Флаг: --seed / -s\\n\\nЧто это:\\nНачальное число генератора случайных чисел. Определяет точку старта для случайности ответов.\\n\\nЗначения:\\n• Любое число (0, 42, 123...) — воспроизводимые результаты\\n• -1 или не задано — случайный seed каждый раз\\n\\nПреимущества:\\n• Один и тот же seed = один и тот же ответ на тот же промпт\\n• Полезно для тестирования и отладки","server.tip.slots_monitoring":"Флаг: --slots / --no-slots\\n\\nЧто это:\\nМониторинг слотов запросов. При включении сервер отдаёт информацию о слотах через endpoint /slots.\\n\\nПреимущества:\\n• Возможность отслеживать состояние слотов через API\\n• Управление параллельными запросами\\n• Интеграция с внешними системами\\n\\nРекомендации:\\n• Включите по умолчанию для полноценного мониторинга","server.tip.spec_draft_gpu_layers":"Флаг: -ngld / --draft-ngl\\n\\nЧто это:\\nСколько слоёв черновой (draft) модели выгрузить на видеокарту.\\n\\nЗначения:\\n• Число — точно столько слоёв\\n• auto — автоопределение по VRAM (по умолчанию)\\n• all — все слои на GPU\\n\\nРекомендации:\\n• Ставьте all для максимальной скорости спекулятивного декодирования","server.tip.spec_draft_n_max":"Флаг: --spec-draft-n-max\\n\\nЧто это:\\nМаксимальное число токенов, которые draft модель генерирует за шаг.\\n\\nЗначения:\\n• 1–20 (по умолчанию: 3)\\n\\nРекомендации:\\n• 3-5 хорошо работает для большинства случаев\\n• Больше = быстрее, но может быть меньше принятия основной моделью","server.tip.spec_draft_n_min":"Флаг: --spec-draft-n-min\\n\\nЧто это:\\nМинимальное число токенов для draft за шаг.\\n\\nЗначения:\\n• 0–10 (по умолчанию: 0)\\n\\nРекомендации:\\n• Оставьте 0 — оптимально","server.tip.spec_draft_p_min":"Флаг: --spec-draft-p-min\\n\\nЧто это:\\nМинимальная вероятность (greedy threshold) для принятия draft токенов.\\n\\nЗначения:\\n• 0.0–1.0 (по умолчанию: 0.00)\\n\\nРекомендации:\\n• Оставьте 0 — оптимально","server.tip.spec_draft_p_split":"Флаг: --spec-draft-p-split\\n\\nЧто это:\\nВероятность разделения для спекулятивного декодирования. Экспериментальная настройка.\\n\\nЗначения:\\n• 0.0–1.0 (по умолчанию: 0.10)\\n\\nРекомендации:\\n• Оставьте по умолчанию — экспериментальный параметр","server.tip.spec_type":"Флаг: --spec-type\\n\\nЧто это:\\nТип спекулятивного декодирования. Определяет метод ускорения генерации.\\n\\nЗначения:\\n• draft — использование черновой модели\\n• mtp — много-токеновое предсказание (MTP)\\n\\nРекомендации:\\n• draft — для большинства случаев с отдельной draft моделью\\n• mtp — для моделей с встроенной поддержкой MTP","server.tip.speculative_decoding":"Флаг: --spec-draft-model / -md\\n\\nЧто это:\\nВторая, маленькая модель-ассистент. Она быстро генерирует черновик ответа, а основная модель его проверяет и исправляет.\\n\\nПреимущества:\\n• Ускорение генерации в 1.5-3 раза при правильных настройках\\n• Основная модель работает эффективнее\\n\\nКогда включать:\\n• При наличии мощного GPU (8+ GB VRAM)\\n• Для больших моделей (13B+) где скорость важна\\n\\nРекомендации:\\n• Используйте лёгкую модель (Phi-2, Qwen2-1.5B) как draft\\n• Draft модель должна быть значительно меньше основной","server.tip.temperature":"Флаг: --temp / -temp\\n\\nЧто это:\\nУправляет креативностью и случайностью ответов модели. Самый важный параметр для контроля стиля ответов.\\n\\nЗначения:\\n• 0.0 — полностью детерминированный ответ\\n• 0.1-0.3 — очень точные, фактические ответы\\n• 0.4-0.6 — баланс между точностью и разнообразием\\n• 0.7-1.0 — креативные, разнообразные ответы\\n• 1.5+ — очень случайные\\n\\nРекомендации:\\n• Для работы с кодом и фактами: 0.2-0.4\\n• Для обычного чата: 0.6-0.8\\n• Для творчества: 0.8-1.2","server.tip.tensor_split":"Флаг: --tensor-split\\n\\nЧто это:\\nРаспределение вычислительной нагрузки между несколькими видеокартами.\\n\\nЗначения:\\n• Формат: 50:50 — равное распределение на 2 GPU\\n• 70:30 — 70% на первый, 30% на второй\\n\\nРекомендации:\\n• Для двух одинаковых карт: 50:50\\n• Для разных карт: пропорционально их мощности","server.tip.threads":"Флаг: -t\\n\\nЧто это:\\nКоличество потоков процессора для генерации текста. Влияет на скорость работы при использовании CPU.\\n\\nЗначения:\\n• Число (1-128) — точное количество потоков\\n• 0 или не задано — автоопределение\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического выбора\\n• Для CPU-генерации ставьте число равное ядрам процессора","server.tip.threads_batch":"Флаг: -tb\\n\\nЧто это:\\nКоличество потоков для обработки входящего промпта (текста, который вы отправляете модели).\\n\\nЗначения:\\n• Число (1-128) — точное количество потоков\\n• 0 или не задано — равно значению -t\\n\\nРекомендации:\\n• Обычно оставляйте по умолчанию (равно -t)\\n• Увеличьте если медленная обработка длинных сообщений","server.tip.timeout":"Флаг: -to / --timeout\\n\\nЧто это:\\nТаймаут чтения/записи в секундах. Время ожидания ответа от сервера.\\n\\nЗначения:\\n• Число секунд (по умолчанию: 600 = 10 минут)\\n\\nРекомендации:\\n• Для коротких ответов: 60-300 секунд достаточно\\n• Для длинных генераций увеличьте до 1200+","server.tip.top_k":"Флаг: --top-k\\n\\nЧто это:\\nОграничивает выбор следующего токена только K наиболее вероятными вариантами из всего словаря модели.\\n\\nЗначения:\\n• 0 — отключено (рассматриваются все токены)\\n• 10-20 — очень ограниченный выбор, более предсказуемо\\n• 40 — баланс (по умолчанию)\\n• 100+ — почти без ограничений\\n\\nРекомендации:\\n• Оставьте 40 для большинства задач\\n• Уменьшите до 20 если ответы слишком случайные","server.tip.top_p":"Флаг: --top-p\\n\\nЧто это:\\nNucleus сэмплинг — оставляет только токены, чья суммарная вероятность не превышает заданное значение P.\\n\\nЗначения:\\n• 0.0–1.0 (по умолчанию: 0.95)\\n• 0.9 = более фокусированные ответы\\n• 0.98 = больше разнообразия\\n\\nРекомендации:\\n• 0.9-0.95 для большинства задач (рекомендуется)\\n• Уменьшите до 0.8 если ответы слишком размытые","server.tip.typical_p":"Флаг: --typical-p\\n\\nЧто это:\\nЛокально типичное сэмплирование — оставляет токены, которые типичны для данного контекста.\\n\\nЗначения:\\n• 1.0 — отключено (по умолчанию)\\n• 0.2-0.5 — более естественный текст\\n\\nРекомендации:\\n• Оставьте 1.0 (отключено) для большинства задач\\n• Попробуйте 0.3-0.5 если хотите более человеческий стиль текста","server.tip.ubatch_size":"Флаг: -ubatch / --u-batch-size\\n\\nЧто это:\\nФизический размер батча на уровне GPU. Должен быть ≤ размера батча (-b).\\n\\nЗначения:\\n• 1–131072 (по умолчанию: 512)\\n\\nРекомендации:\\n• Обычно оставляйте 512 или равным значению Batch\\n• Не увеличивайте без необходимости — может вызвать ошибки памяти","server.tip.verbose_logging":"Флаг: --verbose / -lv 4\\n\\nЧто это:\\nВключает максимальный уровень детализации логов сервера.\\n\\n⚠\\ufe0f КРИТИЧЕСКИ ВАЖНО:\\n• При включении сервер выводит огромный объём отладочных сообщений в stdout/stderr\\n• Это может заблокировать буфер вывода и привести к ПУСТОМУ ОТВЕТУ при запросах\\n• Логи перемешиваются с потоком ответов SSE\\n\\nРекомендации:\\n• ❌ НИКОГДА не включайте для продакшн-работы!\\n• Включайте ТОЛЬКО для отладки проблем с сервером\\n• После диагностики обязательно выключите","server.tip.webui":"Флаг: --webui\\n\\nЧто это:\\nВстроенный веб-чат, доступный из браузера по адресу http://localhost:8080 (или вашему порту).\\n\\nПреимущества:\\n• Удобная альтернатива API для тестирования модели\\n• Не нужно устанавливать дополнительные программы\\n• Работает прямо в браузере\\n\\nРекомендации:\\n• Включите для удобного тестирования и демонстрации","server.tip.yarn_attn_factor":"Флаг: --yarn-attn-factor\\n\\nЧто это:\\nФактор масштабирования внимания для YARN. Корректирует веса внимания при экстраполяции контекста.\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического определения\\n• Нужен только при тонкой настройке YARN","server.tip.yarn_beta_fast":"Флаг: --yarn-beta-fast\\n\\nЧто это:\\nБыстрый параметр бета для YARN-интерполяции. Влияет на качество экстраполяции контекста.\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического определения\\n• Нужен только при тонкой настройке YARN","server.tip.yarn_beta_slow":"Флаг: --yarn-beta-slow\\n\\nЧто это:\\nМедленный параметр бета для YARN-интерполяции. Влияет на качество экстраполяции контекста.\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического определения\\n• Нужен только при тонкой настройке YARN","server.tip.yarn_ext_factor":"Флаг: --yarn-ext-factor\\n\\nЧто это:\\nКоэффициент расширения контекста для YARN. Определяет, насколько можно расширить контекст за пределы оригинального размера.\\n\\nРекомендации:\\n• Оставьте по умолчанию для автоматического определения\\n• Нужен только при тонкой настройке YARN","server.tip.yarn_orig_ctx":"Флаг: --yarn-orig-ctx\\n\\nЧто это:\\nОригинальный размер контекста модели для YARN-экстраполяции. Необходим для корректной работы YARN при увеличении контекста.\\n\\nЗначения:\\n• Число (по умолчанию: из модели)\\n• Например, 8192 для Llama 3, 4096 для Llama 2\\n\\nРекомендации:\\n• Ставьте оригинальный размер контекста модели\\n• Нужен только при использовании YARN для увеличения контекста","tt.auto_check_updates":"Автоматически проверять новые версии llama.cpp при запуске (отправляет сетевой запрос к GitHub)","tt.auto_start_server":"Автоматически запускать llama сервер с дефолтным профилем при старте приложения.","tt.auto_start_with_os":"Автоматически запускать Llama Studio при входе в систему. Используется штатный механизм автозапуска ОС.","tt.host":"Адрес сервера. 127.0.0.1 = только локально, 0.0.0.0 = все интерфейсы.","tt.keep_server_on_exit":"При закрытии приложения не останавливать llama сервер. Полезно для непрерывных сессий чата.","tt.language":"Выберите язык приложения (применяется сразу)","tt.llama_cpp_base_dir":"Корневая папка для всех версий llama.cpp. Оставьте пустым для использования AppData.","tt.llama_cpp_dir":"Папка с бинарниками llama.cpp. Заполняется автоматически после установки.","tt.models_dir":"Папка для хранения файлов моделей GGUF.","tt.port":"Порт сервера (1-65535). По умолчанию: 8080","tt.theme":"Тема оформления приложения"};
window.WEB_TT_EN={"server.tip.additional_args":"What is this:\\nA field for entering any additional llama-server command-line arguments not available in the GUI.\\n\\nValues:\\n• Any valid llama-server flag, one per line\\n• Examples: --flash-attn-best-fit, --ctx-size-ext 65536\\n\\nRecommendations:\\n• Use for experimental flags from the llama.cpp documentation\\n• Each argument on a new line","server.tip.batch_size":"Flag: -b / --batch-size\\n\\nWhat is this:\\nMaximum number of prompt tokens processed at once. Affects the speed of reading input text by the model.\\n\\nValues:\\n• 64–131072 (default: 2048)\\n\\nBenefits:\\n• Larger batch = faster processing of long prompts\\n• More efficient GPU utilization\\n\\nRecommendations:\\n• Leave 2048 for most tasks\\n• Increase to 4096-8192 if long messages are processed slowly","server.tip.cache_prompt":"Flag: --cache-prompt\\n\\nWhat is this:\\nPrompt caching for repeated queries. Saves intermediate computation results for speedup.\\n\\nBenefits:\\n• Significantly speeds up dialog operation (repeated messages)\\n• Saves time when editing prompts\\n\\nRecommendations:\\n• Keep enabled — significantly speeds up dialog operation","server.tip.cache_ram":"Flag: --cache-ram\\n\\nWhat is this:\\nAmount of RAM in megabytes allocated for KV cache.\\n\\nRecommendations:\\n• Leave empty for automatic detection\\n• Specify if you need to limit RAM usage","server.tip.cache_reuse":"Flag: --cache-reuse\\n\\nWhat is this:\\nCache reuse buffer size in tokens. Allows reusing computations for common request prefixes.\\n\\nValues:\\n• Number of tokens (default: 0 = disabled)\\n\\nRecommendations:\\n• Enable for faster repeated queries with common prefixes\\n• 0.6-0.8 of context size — good balance","server.tip.cache_type_k":"Flag: -ctk\\n\\nWhat is this:\\nData type for key (K) cache in the attention mechanism. Affects memory usage and speed.\\n\\nValues:\\n• f16 — full precision, more VRAM\\n• q8_0 — 8-bit quantization, balance\\n• q4_0 — 4-bit quantization, VRAM savings\\n\\nRecommendations:\\n• f16 for best quality\\n• q8_0 for quality/memory balance","server.tip.cache_type_v":"Flag: -ctv\\n\\nWhat is this:\\nData type for value (V) cache in the attention mechanism. Affects memory usage and speed.\\n\\nValues:\\n• f16 — full precision, more VRAM\\n• q8_0 — 8-bit quantization, balance\\n• q4_0 — 4-bit quantization, VRAM savings\\n\\nRecommendations:\\n• f16 for best quality\\n• q8_0 for quality/memory balance","server.tip.cont_batching":"Flag: -cb / --no-cb\\n\\nWhat is this:\\nAllows the server to process new requests without waiting for current ones to finish. Critically important for multi-user operation!\\n\\nBenefits:\\n• Better throughput with many simultaneous requests\\n• Less latency for new clients\\n• Smooth server operation under load\\n\\nRecommendations:\\n• Keep enabled for better performance\\n• Only disable if stability issues arise","server.tip.context_shift":"Flag: --context-shift\\n\\nWhat is this:\\nAutomatic context shift on overflow. Allows continuing the dialog without losing history.\\n\\nRecommendations:\\n• Keep enabled (default)","server.tip.ctx_size":"Flag: -c / --ctx-size\\n\\nWhat is this:\\nMaximum dialog length in tokens. 1 token ≈ 0.75 words. Determines how much text the model can remember at once.\\n\\nValues:\\n• 256–131072 (default: 4096)\\n• Examples: 4096 ≈ 3000 words, 8192 ≈ 6000 words, 32768 ≈ 24000 words\\n\\nRecommendations:\\n• Normal chat: 4096-8192\\n• Long documents: 16384+\\n• More context = more VRAM/RAM (proportional)","server.tip.draft_model_path":"Flag: --spec-draft-model\\n\\nWhat is this:\\nPath to the draft model file for speculative decoding.\\n\\nRecommendations:\\n• Use a light model (Phi-2, Qwen2-1.5B) as draft\\n• Draft model should be significantly smaller than main\\n• Format: .gguf","server.tip.embedding_mode":"Flag: --embedding\\n\\nWhat is this:\\nLimits the server to embedding mode only. The model generates only vector representations of text, not responses.\\n\\nWhen to enable:\\n• Only for embedding models (text-embedding-3-small, etc.)\\n• For text similarity search, classification, RAG systems\\n\\nRecommendations:\\n• Leave disabled for normal language models","server.tip.flash_attention":"Flag: -fa / --flash-attn\\n\\nWhat is this:\\nOptimized attention computation algorithm. Significantly speeds up processing and saves memory.\\n\\nBenefits:\\n• 2-4x faster prompt processing\\n• Less VRAM usage for the same context\\n• Allows larger context sizes\\n\\nRecommendations:\\n• Keep enabled for maximum performance\\n• Only disable if you encounter errors (rare)","server.tip.frequency_penalty":"Flag: --frequency-penalty\\n\\nWhat is this:\\nPenalty for token frequency. The more often a word appeared, the less likely it is to repeat (like GPT-4).\\n\\nValues:\\n• -2.0–2.0 (default: 0.0)\\n• 0.1-0.5 — reducing repetitive phrases\\n\\nRecommendations:\\n• Leave 0.0 for most tasks","server.tip.gpu_layers":"Flag: -ngl\\n\\nWhat is this:\\nDetermines how many neural network layers will run on the GPU. More layers on GPU = faster text generation.\\n\\nValues:\\n• Number (e.g. 32) — exactly that many layers on GPU\\n• all — all layers on GPU (maximum speed)\\n• auto — auto-detect based on available VRAM\\n• 0 — all on CPU (slow, but works without a GPU)\\n\\nRecommendations:\\n• For most tasks, set to all\\n• If VRAM is insufficient — decrease the number or use auto","server.tip.host":"Flag: --host\\n\\nWhat is this:\\nIP address on which the server will listen for incoming connections.\\n\\nValues:\\n• 127.0.0.1 — local access only (default, safe)\\n• 0.0.0.0 — network access (all interfaces)\\n• Specific IP — only from a certain address\\n\\nRecommendations:\\n• Leave 127.0.0.1 for security\\n• Use 0.0.0.0 if you need to connect from other devices on the network","server.tip.kv_offload":"Flag: --kv-offload\\n\\nWhat is this:\\nOffloading KV cache (intermediate results) to the GPU.\\n\\nBenefits:\\n• Faster generation when VRAM is available\\n• Less CPU and RAM load\\n\\nRecommendations:\\n• Keep enabled for better performance\\n• Only disable with critical VRAM shortage","server.tip.main_gpu":"Flag: --main-gpu\\n\\nWhat is this:\\nIndex of the primary GPU for computation when multiple GPUs are present.\\n\\nValues:\\n• 0 — first GPU (default)\\n• 1, 2, 3... — second, third, etc.\\n\\nRecommendations:\\n• Leave 0 if you have one GPU\\n• With two cards: set the index of the more powerful card","server.tip.metrics":"Flag: --metrics\\n\\nWhat is this:\\nOpens /metrics endpoint with data in Prometheus format for server performance monitoring.\\n\\nBenefits:\\n• Track VRAM/RAM usage in real time\\n• Monitor generation speed (tokens/sec)\\n• Integration with visualization systems\\n\\nWhen to enable:\\n• For integration with Grafana, Zabbix, or other monitoring systems\\n\\nRecommendations:\\n• Leave disabled if not using monitoring","server.tip.min_p":"Flag: --min-p\\n\\nWhat is this:\\nMinimum token probability relative to the most probable. Filters out tokens significantly worse than the best option.\\n\\nValues:\\n• 0.0 — disabled (default)\\n• 0.05-0.1 — mild filtering (recommended)\\n• 0.2+ — strict filtering\\n\\nRecommendations:\\n• 0.05-0.1 works well for most tasks","server.tip.mirostat":"Flag: --mirostat / --mirostat2\\n\\nWhat is this:\\nAdaptive sampling algorithm that automatically adjusts response randomness to maintain a given perplexity level.\\n\\nBenefits:\\n• Automatic temperature adjustment during generation\\n• May improve quality with incorrect temp/top-p settings\\n\\nRecommendations:\\n• Disabled by default — works fine without it\\n• Try enabling if you want smart parameter adjustment","server.tip.mirostat_eta":"Flag: --mirostat-eta\\n\\nWhat is this:\\nMirostat algorithm adaptation speed. How quickly it adjusts parameters toward target perplexity.\\n\\nValues:\\n• 0.0–1.0 (default: 0.1)\\n\\nRecommendations:\\n• Leave 0.1 — optimal value","server.tip.mirostat_tau":"Flag: --mirostat-tau\\n\\nWhat is this:\\nTarget perplexity for Mirostat. Determines how surprising the text should be.\\n\\nValues:\\n• 1.0 — very predictable text\\n• 5.0 — balance (default)\\n• 10.0 — more diverse and unexpected text\\n\\nRecommendations:\\n• Leave 5.0 for most tasks","server.tip.mlock":"Flag: --mlock\\n\\nWhat is this:\\nKeeps the model in RAM, preventing swapping to disk when memory is low.\\n\\nBenefits:\\n• Stable performance without stuttering\\n• Model doesn't slow down under system load\\n\\nRecommendations:\\n• Enable if you have enough free RAM (1.5-2x the model size)\\n• On systems with 16GB+ RAM for models up to 7B — definitely enable","server.tip.mmap":"Flag: --mmap\\n\\nWhat is this:\\nModel loading via memory-mapped files. The OS manages caching of model data in memory.\\n\\nBenefits:\\n• Fast model loading\\n• OS can swap out unused parts when needed\\n• Less peak RAM usage on startup\\n\\nRecommendations:\\n• Keep enabled (default)\\n• Only disable if the model gets evicted from memory during multitasking","server.tip.mmproj":"Flag: --mmproj\\n\\nWhat is this:\\nMultimodal projector file. Allows a text model to see and analyze images.\\n\\nWhen to enable:\\n• Only for vision models (LLaVA, Bakllava, etc.)\\n• Normal language models do NOT need this\\n\\nRecommendations:\\n• The projector file comes with the vision model when downloading\\n• Without this file, the model cannot process images","server.tip.mmq":"Flag: --no-mmq\\n\\nWhat is this:\\nDisables Multi-Modal Quantization (MMQ). When enabled, MMQ allows mixed quantization of model layers.\\n\\nRecommendations:\\n• Keep disabled for best compatibility","server.tip.model_path":"Flag: -m\\n\\nWhat is this:\\nPath to the GGUF model file to load. This is the main neural network that will respond to your requests.\\n\\nValues:\\n• Full path to .gguf file\\n• Drag and drop file into the field is supported\\n\\nRecommendations:\\n• Use models with Instruct in the name for chat\\n• Model size depends on VRAM/RAM: 7B (~4GB), 13B (~8GB), 70B (~40GB)","server.tip.mtp_predict":"Flag: -n / --predict\\n\\nWhat is this:\\nMaximum length of generated response in tokens.\\n\\nValues:\\n• -1 — unlimited (default)\\n• 256–131072 — exact limit in tokens\\n\\nRecommendations:\\n• For chat: 256-512 (short answers)\\n• For long answers and articles: 1024+","server.tip.numa":"Flag: --numa\\n\\nWhat is this:\\nNUMA-aware memory distribution. Optimizes memory operations on servers with multiple CPU sockets.\\n\\nWhen to enable:\\n• Only on servers with 2+ CPU sockets (multi-processor systems)\\n\\nRecommendations:\\n• On regular PCs and laptops — do NOT enable\\n• On server hardware, may provide performance boost","server.tip.parallel":"Flag: --parallel / -np\\n\\nWhat is this:\\nNumber of parallel request slots. Determines how many requests the server can process simultaneously.\\n\\nValues:\\n• Number — exact slot count (default: 4)\\n• -1 — auto-detect\\n\\nBenefits:\\n• More slots = more simultaneous requests\\n• Each slot consumes VRAM/RAM proportional to context size\\n\\nRecommendations:\\n• For one user: 2-4 is enough\\n• Increase if working with multiple clients simultaneously","server.tip.port":"Flag: --port\\n\\nWhat is this:\\nTCP port on which the server will run.\\n\\nValues:\\n• 1–65535 (default: 8080)\\n\\nRecommendations:\\n• Leave 8080 or change if the port is occupied by another program","server.tip.predict":"Flag: -n / --predict\\n\\nWhat is this:\\nMaximum length of generated response in tokens. Limits how much text the model can write at once.\\n\\nValues:\\n• -1 — unlimited (default)\\n• 256–131072 — exact limit in tokens\\n\\nRecommendations:\\n• For chat: 256-512 (short answers)\\n• For long answers and articles: 1024+\\n• -1 = no limit, the model will stop on its own","server.tip.predict_count":"Flag: -np / --predict-count\\n\\nWhat is this:\\nNumber of predictions to generate per request in MTP mode.\\n\\nRecommendations:\\n• Leave default for automatic detection","server.tip.presence_penalty":"Flag: --presence-penalty\\n\\nWhat is this:\\nPenalty for the presence of a token in context. Encourages the model to use new words and topics (like GPT-4).\\n\\nValues:\\n• -2.0–2.0 (default: 0.0)\\n• 0.1-0.5 — mild topic diversity\\n\\nRecommendations:\\n• Leave 0.0 for most tasks\\n• 0.1-0.3 if you want more diverse answers","server.tip.priority_high":"Flag: process priority\\n\\nWhat is this:\\nSets high process priority for the server in the operating system.\\n\\nBenefits:\\n• Better response under high system load\\n• Priority CPU time allocation\\n\\nRecommendations:\\n• Enable if the server runs on a dedicated machine\\n• On workstations, may slow down other applications","server.tip.repeat_last_n":"Flag: --repeat-last-n\\n\\nWhat is this:\\nToken window for checking repetitions. Determines how many last words the model will check for duplication.\\n\\nValues:\\n• 64 — default in llama.cpp\\n• -1 — entire context\\n• 0 — disabled\\n• Number — only the last N tokens\\n\\nRecommendations:\\n• Leave 64 for most tasks\\n• Set -1 to check the entire context","server.tip.repeat_penalty":"Flag: --repeat-penalty / -rep-pen\\n\\nWhat is this:\\nPenalty for repeating the same tokens. Prevents the model from looping on the same phrases.\\n\\nValues:\\n• 1.0 — disabled (no penalty)\\n• 1.1 — mild penalty (default, recommended)\\n• 1.2-1.3 — moderate penalty\\n• 1.5+ — strong penalty (may distort text)\\n\\nRecommendations:\\n• Leave 1.1 for most tasks\\n• Increase to 1.2-1.3 if the model repeats","server.tip.rope_freq_base":"Flag: --rope-freq-base\\n\\nWhat is this:\\nBase frequency of RoPE positional encoding. Affects how the model understands word order in text.\\n\\nValues:\\n• Floating point number (default: from model)\\n\\nRecommendations:\\n• Don't change unless necessary — used for fine-tuning to other context lengths","server.tip.rope_freq_scale":"Flag: --rope-freq-scale\\n\\nWhat is this:\\nRoPE frequency scale. Allows extending model context beyond original size.\\n\\nValues:\\n• Number > 0 (default: from model)\\n• 2.0 — doubles effective context\\n\\nBenefits:\\n• Allows using a model with more context than it was trained on\\n• Alternative to YARN for context extrapolation","server.tip.rope_scaling":"Flag: --rope-scaling\\n\\nWhat is this:\\nType of RoPE positional encoding scaling. Determines how the model handles context beyond training size.\\n\\nValues:\\n• none — no scaling\\n• linear — linear scaling\\n• yarn — YARN (better quality extrapolation)\\n\\nRecommendations:\\n• Use yarn for best quality when increasing context\\n• linear — simple alternative","server.tip.seed":"Flag: --seed / -s\\n\\nWhat is this:\\nInitial random number generator seed. Determines the starting point for response randomness.\\n\\nValues:\\n• Any number (0, 42, 123...) — reproducible results\\n• -1 or not set — random seed each time\\n\\nBenefits:\\n• Same seed = same response to the same prompt\\n• Useful for testing and debugging","server.tip.slots_monitoring":"Flag: --slots / --no-slots\\n\\nWhat is this:\\nRequest slot monitoring. When enabled, the server provides slot information via the /slots endpoint.\\n\\nBenefits:\\n• Ability to monitor slot status via API\\n• Manage parallel requests\\n• Integration with external systems\\n\\nRecommendations:\\n• Enable by default for full monitoring","server.tip.spec_draft_gpu_layers":"Flag: -ngld / --draft-ngl\\n\\nWhat is this:\\nHow many layers of the draft model to offload to GPU.\\n\\nValues:\\n• Number — exactly that many layers\\n• auto — auto-detect based on VRAM (default)\\n• all — all layers on GPU\\n\\nRecommendations:\\n• Set to all for maximum speculative decoding speed","server.tip.spec_draft_n_max":"Flag: --spec-draft-n-max\\n\\nWhat is this:\\nMaximum number of tokens the draft model generates per step.\\n\\nValues:\\n• 1–20 (default: 3)\\n\\nRecommendations:\\n• 3-5 works well for most cases\\n• More = faster, but may be accepted less by the main model","server.tip.spec_draft_n_min":"Flag: --spec-draft-n-min\\n\\nWhat is this:\\nMinimum number of tokens for draft per step.\\n\\nValues:\\n• 0–10 (default: 0)\\n\\nRecommendations:\\n• Leave 0 — optimal","server.tip.spec_draft_p_min":"Flag: --spec-draft-p-min\\n\\nWhat is this:\\nMinimum probability (greedy threshold) for accepting draft tokens.\\n\\nValues:\\n• 0.0–1.0 (default: 0.00)\\n\\nRecommendations:\\n• Leave 0 — optimal","server.tip.spec_draft_p_split":"Flag: --spec-draft-p-split\\n\\nWhat is this:\\nSplit probability for speculative decoding. Experimental setting.\\n\\nValues:\\n• 0.0–1.0 (default: 0.10)\\n\\nRecommendations:\\n• Leave default — experimental parameter","server.tip.spec_type":"Flag: --spec-type\\n\\nWhat is this:\\nType of speculative decoding. Determines the method for acceleration.\\n\\nValues:\\n• draft — using a draft model\\n• mtp — multi-token prediction (MTP)\\n\\nRecommendations:\\n• draft — for most cases with a separate draft model\\n• mtp — for models with built-in MTP support","server.tip.speculative_decoding":"Flag: --spec-draft-model / -md\\n\\nWhat is this:\\nA second, small assistant model. It quickly generates a draft response, and the main model checks and corrects it.\\n\\nBenefits:\\n• 1.5-3x speedup with proper settings\\n• Main model works more efficiently\\n\\nWhen to enable:\\n• With a powerful GPU (8+ GB VRAM)\\n• For large models (13B+) where speed matters\\n\\nRecommendations:\\n• Use a light model (Phi-2, Qwen2-1.5B) as draft\\n• Draft model should be significantly smaller than main","server.tip.temperature":"Flag: --temp / -temp\\n\\nWhat is this:\\nControls creativity and randomness of model responses. The most important parameter for controlling response style.\\n\\nValues:\\n• 0.0 — fully deterministic\\n• 0.1-0.3 — very precise, factual answers\\n• 0.4-0.6 — balance between accuracy and variety\\n• 0.7-1.0 — creative, diverse answers\\n• 1.5+ — very random\\n\\nRecommendations:\\n• For code and facts: 0.2-0.4\\n• For normal chat: 0.6-0.8\\n• For creativity: 0.8-1.2","server.tip.tensor_split":"Flag: --tensor-split\\n\\nWhat is this:\\nDistribution of compute load across multiple GPUs.\\n\\nValues:\\n• Format: 50:50 — equal distribution on 2 GPUs\\n• 70:30 — 70% on first, 30% on second\\n\\nRecommendations:\\n• For two identical cards: 50:50\\n• For different cards: proportional to their power","server.tip.threads":"Flag: -t\\n\\nWhat is this:\\nNumber of CPU threads for text generation. Affects speed when using CPU.\\n\\nValues:\\n• Number (1-128) — exact thread count\\n• 0 or not set — auto-detect\\n\\nRecommendations:\\n• Leave default for automatic selection\\n• For CPU generation, set to the number of CPU cores","server.tip.threads_batch":"Flag: -tb\\n\\nWhat is this:\\nNumber of threads for processing incoming prompts (text you send to the model).\\n\\nValues:\\n• Number (1-128) — exact thread count\\n• 0 or not set — equals -t value\\n\\nRecommendations:\\n• Usually leave default (equals -t)\\n• Increase if long messages are processed slowly","server.tip.timeout":"Flag: -to / --timeout\\n\\nWhat is this:\\nRead/write timeout in seconds. How long to wait for a server response.\\n\\nValues:\\n• Number of seconds (default: 600 = 10 minutes)\\n\\nRecommendations:\\n• For short responses: 60-300 seconds is enough\\n• For long generations, increase to 1200+","server.tip.top_k":"Flag: --top-k\\n\\nWhat is this:\\nLimits the next token choice to only the K most probable options from the entire model vocabulary.\\n\\nValues:\\n• 0 — disabled (all tokens considered)\\n• 10-20 — very limited choice, more predictable\\n• 40 — balance (default)\\n• 100+ — almost unlimited\\n\\nRecommendations:\\n• Leave 40 for most tasks\\n• Decrease to 20 if answers are too random","server.tip.top_p":"Flag: --top-p\\n\\nWhat is this:\\nNucleus sampling — keeps only tokens whose cumulative probability does not exceed the given value P.\\n\\nValues:\\n• 0.0–1.0 (default: 0.95)\\n• 0.9 = more focused answers\\n• 0.98 = more variety\\n\\nRecommendations:\\n• 0.9-0.95 for most tasks (recommended)\\n• Decrease to 0.8 if answers are too vague","server.tip.typical_p":"Flag: --typical-p\\n\\nWhat is this:\\nLocally typical sampling — keeps tokens that are typical for the given context.\\n\\nValues:\\n• 1.0 — disabled (default)\\n• 0.2-0.5 — more natural text\\n\\nRecommendations:\\n• Leave 1.0 (disabled) for most tasks\\n• Try 0.3-0.5 if you want a more human-like text style","server.tip.ubatch_size":"Flag: -ubatch / --u-batch-size\\n\\nWhat is this:\\nPhysical batch size at the GPU level. Should be ≤ batch size (-b).\\n\\nValues:\\n• 1–131072 (default: 512)\\n\\nRecommendations:\\n• Usually leave 512 or equal to Batch value\\n• Don't increase unnecessarily — may cause memory errors","server.tip.verbose_logging":"Flag: --verbose / -lv 4\\n\\nWhat is this:\\nEnables maximum log detail level for the server.\\n\\n⚠\\ufe0f CRITICALLY IMPORTANT:\\n• When enabled, the server outputs a huge amount of debug messages to stdout/stderr\\n• This can block the output buffer and result in EMPTY RESPONSES for requests\\n• Logs get mixed with SSE response stream\\n\\nRecommendations:\\n• ❌ NEVER enable for production!\\n• Enable ONLY for debugging server issues\\n• After diagnostics, definitely disable","server.tip.webui":"Flag: --webui\\n\\nWhat is this:\\nBuilt-in web chat accessible from browser at http://localhost:8080 (or your port).\\n\\nBenefits:\\n• Convenient API alternative for model testing\\n• No need to install additional software\\n• Works directly in the browser\\n\\nRecommendations:\\n• Enable for convenient testing and demonstration","server.tip.yarn_attn_factor":"Flag: --yarn-attn-factor\\n\\nWhat is this:\\nAttention scaling factor for YARN. Corrects attention weights during context extrapolation.\\n\\nRecommendations:\\n• Leave default for automatic detection\\n• Only needed for fine YARN tuning","server.tip.yarn_beta_fast":"Flag: --yarn-beta-fast\\n\\nWhat is this:\\nFast beta parameter for YARN interpolation. Affects context extrapolation quality.\\n\\nRecommendations:\\n• Leave default for automatic detection\\n• Only needed for fine YARN tuning","server.tip.yarn_beta_slow":"Flag: --yarn-beta-slow\\n\\nWhat is this:\\nSlow beta parameter for YARN interpolation. Affects context extrapolation quality.\\n\\nRecommendations:\\n• Leave default for automatic detection\\n• Only needed for fine YARN tuning","server.tip.yarn_ext_factor":"Flag: --yarn-ext-factor\\n\\nWhat is this:\\nContext expansion factor for YARN. Determines how much context can be extended beyond original size.\\n\\nRecommendations:\\n• Leave default for automatic detection\\n• Only needed for fine YARN tuning","server.tip.yarn_orig_ctx":"Flag: --yarn-orig-ctx\\n\\nWhat is this:\\nOriginal model context size for YARN extrapolation. Required for correct YARN operation when increasing context.\\n\\nValues:\\n• Number (default: from model)\\n• For example, 8192 for Llama 3, 4096 for Llama 2\\n\\nRecommendations:\\n• Set the model's original context size\\n• Only needed when using YARN to increase context","tt.auto_check_updates":"Automatically check for new llama.cpp releases on startup (sends a network request to GitHub)","tt.auto_start_server":"Automatically start the llama server with the default profile when the application launches.","tt.auto_start_with_os":"Automatically start Llama Studio when you sign in. Uses the system startup mechanism.","tt.host":"Server listen address. 127.0.0.1 = local only, 0.0.0.0 = all interfaces.","tt.keep_server_on_exit":"When closing the application, keep the llama server running in the background. Useful for continuous chat sessions.","tt.language":"Select application language (changes apply immediately)","tt.llama_cpp_base_dir":"Root directory where all llama.cpp versions are stored. Leave empty for default (AppData).","tt.llama_cpp_dir":"Directory where llama.cpp binaries are installed. Auto-set after download.","tt.models_dir":"Directory where GGUF model files are stored.","tt.port":"Server port number (1-65535). Default: 8080","tt.theme":"Application appearance theme"};
window.WEB_TIP_BY_ID={"llamaBaseDirectory":"tt.llama_cpp_base_dir","modelsDir":"tt.models_dir","set_defaultHost":"tt.host","set_defaultPort":"tt.port","set_autoCheckUpdates":"tt.auto_check_updates","set_autoStartWithOs":"tt.auto_start_with_os","set_keepServerOnExit":"tt.keep_server_on_exit","set_autoStartServer":"tt.auto_start_server","set_language":"tt.language","set_theme":"tt.theme","contextSize":"server.tip.ctx_size","temperature":"server.tip.temperature","topK":"server.tip.top_k","topP":"server.tip.top_p","minP":"server.tip.min_p"};
window.applyTips=function(){
try{
var dict=window.WEB_LANG==='en'?window.WEB_TT_EN:window.WEB_TT_RU;
if(!dict)return;
var els=document.querySelectorAll('[data-tt]');
for(var i=0;i<els.length;i++){
var el=els[i],k=el.getAttribute('data-tt'),v=dict[k];
if(v)el.setAttribute('data-tip',v);
}
var byId=window.WEB_TIP_BY_ID;
for(var id in byId){var e2=document.getElementById(id);if(e2&&dict[byId[id]])e2.setAttribute('data-tip',dict[byId[id]]);}
}catch(e){}
};
window.t=function(s){if(typeof s!=='string'||!s)return s;if(window.WEB_LANG==='en'){var v=window.WEB_EN[s];if(v!==undefined)return v}return s};
var I18N_ATTRS=['placeholder','title','aria-label','alt'];
var _orig=new WeakMap();
function trTextNode(n){
if(n.nodeType!==3)return;
var txt=n.nodeValue;if(!txt)return;
var p=n.parentElement;if(!p)return;
var tag=(p.tagName||'').toLowerCase();
if(tag==='script'||tag==='style')return;
var key=txt.trim();if(!key)return;
if(window.WEB_LANG==='en'){
var en=window.WEB_EN[key];
if(en!==undefined){
if(!_orig.has(n))_orig.set(n,txt);
var lead=txt.match(/^\s*/)[0],trail=txt.match(/\s*$/)[0];
n.nodeValue=lead+en+trail;
}}else{
var orig=_orig.get(n);
if(orig!==undefined){
var en2=window.WEB_EN[orig];
if(en2!==undefined&&txt.trim()===en2){
var lead=txt.match(/^\s*/)[0],trail=txt.match(/\s*$/)[0];
n.nodeValue=lead+orig+trail;
_orig.delete(n);
}}}
}
function trAttr(el){
for(var i=0;i<I18N_ATTRS.length;i++){
var a=I18N_ATTRS[i];
if(!el.hasAttribute(a))continue;
var val=el.getAttribute(a);if(!val)continue;
var key=val.trim();if(!key)continue;
var saveAttr='data-wi18n-'+a;
if(window.WEB_LANG==='en'){
var en=window.WEB_EN[key];
if(en!==undefined){
if(!el.hasAttribute(saveAttr))el.setAttribute(saveAttr,key);
el.setAttribute(a,en);
}}else if(el.hasAttribute(saveAttr)){
var orig=el.getAttribute(saveAttr);
var en2=window.WEB_EN[orig];
if(en2!==undefined&&val.trim()===en2){el.setAttribute(a,orig);el.removeAttribute(saveAttr);}
}}
}
function trNode(node){
if(!node)return;
if(node.nodeType===3){trTextNode(node);return;}
if(node.nodeType!==1)return;
trAttr(node);
if(node.querySelectorAll){
var els=node.querySelectorAll('*');
for(var i=0;i<els.length;i++)trAttr(els[i]);
}
var w=document.createTreeWalker(node,NodeFilter.SHOW_TEXT,null);
while(w.nextNode())trTextNode(w.currentNode);
}
window.applyWebI18n=function(){
try{
document.documentElement.lang=window.WEB_LANG==='en'?'en':'ru';
var sel=document.getElementById('set_language');
if(sel&&sel.value!==window.WEB_LANG)sel.value=window.WEB_LANG;
trNode(document.body);try{window.applyTips()}catch(_){}
}catch(e){try{console.warn('applyWebI18n',e)}catch(_){}}
};
var _observer=null;
function ensureObserver(){
if(_observer)return;
try{
_observer=new MutationObserver(function(muts){
var roots=[];
muts.forEach(function(m){
if(m.type==='childList')m.addedNodes.forEach(function(n){if(n.nodeType===1||n.nodeType===3)roots.push(n);});
if(m.type==='characterData'&&m.target&&m.target.nodeType===3)roots.push(m.target);
});
roots=roots.filter(function(n){for(var i=0;i<roots.length;i++){var o=roots[i];if(o!==n&&n.nodeType===1&&o.nodeType===1&&o.contains&&o.contains(n))return false;}return true;});
for(var i=0;i<roots.length;i++)trNode(roots[i]);
try{for(var q=0;q<roots.length;q++){var rq=roots[q];if((rq.nodeType===1&&rq.hasAttribute&&rq.hasAttribute('data-tt'))||(rq.nodeType===1&&rq.querySelectorAll&&rq.querySelectorAll('[data-tt]').length)){window.applyTips();break;}}}catch(_){}
});
_observer.observe(document.body,{childList:true,subtree:true,characterData:true});
}catch(e){_observer=null;try{console.warn('i18n observer',e)}catch(_){}}
}
window.rebuildWebTabs=function(){
try{
if(window.profileCache&&typeof window.buildServerTabs==='function')window.buildServerTabs(window.profileCache);
if(window._arrangeParity)setTimeout(function(){try{window._arrangeParity()}catch(e){}},50);
setTimeout(function(){try{window.applyTips()}catch(e){}},120);
}catch(e){}
};
window.setWebLang=function(lang,save){
if(lang!=='en'&&lang!=='ru')return;
window.WEB_LANG=lang;
window.rebuildWebTabs();
window.applyWebI18n();
ensureObserver();
if(save){try{fetch('/api/settings/save?language='+encodeURIComponent(lang)).catch(function(){})}catch(e){}}
};
function pollLanguage(){
try{
fetch('/api/settings').then(function(r){return r.json()}).then(function(d){
var lang=d&&d.language;
if((lang==='en'||lang==='ru')&&lang!==window.WEB_LANG)window.setWebLang(lang,false);
}).catch(function(){});
}catch(e){}
}
setInterval(pollLanguage,4000);
document.addEventListener('DOMContentLoaded',function(){
fetch('/api/settings').then(function(r){return r.json()}).then(function(d){
if(d&&d.language&&(d.language==='en'||d.language==='ru'))window.WEB_LANG=d.language;
window.applyWebI18n();
window.rebuildWebTabs();
ensureObserver();
}).catch(function(){});
});
})();
""";

	private static string GetHtmlV2()
	{
		const string injectedScript = """
(function(){
const pageKey='llamaStudio.web.page';
const normalizeVersion=function(value){
 if(value&&typeof value==='object'){
  const key=value.versionKey||value.key||((value.version||'')+'|'+(value.buildType||''));
  const label=value.display||((value.version||'')+(value.buildType?' ('+value.buildType+')':''));
  return {key:String(key),label:String(label)};
 }
 const raw=String(value||'');
 return {key:raw.replace(/-cuda12x$/i,'|Cuda12x').replace(/-cuda13x$/i,'|Cuda13x'),label:raw};
};
window.loadHomeReleases=async function(){
 try{
  const data=await json('/api/llama/installed');
  const items=(Array.isArray(data.versions)?data.versions:[]).map(normalizeVersion).filter(x=>x.key);
  const select=document.getElementById('homeReleaseSelect');
  if(select){
   const current=profileCache&&profileCache.selectedServerVersion?String(profileCache.selectedServerVersion):String(data.active||'');
   select.innerHTML=items.map(x=>'<option value="'+esc(x.key)+'">'+esc(x.label)+'</option>').join('');
   if(current&&items.some(x=>x.key===current))select.value=current;
  }
  const current=profileCache&&profileCache.selectedServerVersion?String(profileCache.selectedServerVersion):String(data.active||'');
  const info=document.getElementById('homeReleaseInfo'),button=document.getElementById('homeReleaseUpdate');
  try{
   const releases=await json('/api/llama/releases'),latest=releases[0],latestTag=latest&&latest.tag?String(latest.tag):'';
   if(latestTag&&latestTag!==current.split('|')[0]){if(info)info.textContent=t('↑ Доступно обновление, ')+latestTag;if(button)button.textContent=t('Обновить (')+latestTag+')';}
   else{if(info)info.textContent=items.length?items.length+t(' установлено'):t('Установленные сборки не найдены');if(button)button.textContent=t('Проверить обновления');}
  }catch(error){if(info)info.textContent=items.length?items.length+t(' установлено'):t('Установленные сборки не найдены');}
 }catch(error){const info=document.getElementById('homeReleaseInfo');if(info)info.textContent=t('Не удалось прочитать установленные сборки');}
};
document.querySelectorAll('.nav').forEach(function(button){button.addEventListener('click',function(){localStorage.setItem(pageKey,button.dataset.page);});});
const savedPage=localStorage.getItem(pageKey);
if(savedPage){const button=Array.from(document.querySelectorAll('.nav')).find(x=>x.dataset.page===savedPage);if(button)setTimeout(function(){button.click();},0);}

const serverTabNames={model:t('Модель'),gpu:'GPU',context:t('Контекст'),sampling:t('Сэмплинг'),kv:t('KV Кэш'),mtp:t('MTP и Спекулятивное'),rope:'Rope/YARN',server:t('Сервер'),args:t('Пользовательские аргументы'),rpc:t('Удалённый RPC')};
const serverSchema={
 model:[['modelPath',t('Основная модель (-m)'),'text','server.tip.model_path'],['mmprojPath',t('Файл mmproj (--mmproj)'),'text','server.tip.mmproj'],['draftModelPath',t('Draft-модель (-md)'),'text','server.tip.draft_model_path'],['llamaCppBaseDirectory',t('Каталог llama.cpp'),'text','tt.llama_cpp_dir'],['hfRepo',t('HuggingFace репозиторий (-hf)'),'text'],['hfFile',t('Файл HuggingFace (-hff)'),'text'],['hfRepoDraft',t('Репозиторий draft (-hfd)'),'text'],['hfUrl','URL HuggingFace','text'],['hfOffline',t('Офлайн режим (--offline)'),'bool'],['alias',t('Имя API / alias (-a)'),'text'],['embeddingMode',t('Режим embeddings (--embedding)'),'bool','server.tip.embedding_mode'],['poolingType','Pooling (--pooling)','select',['','mean','cls','last','rank']]],
 gpu:[['gpuLayers',t('Слои GPU (-ngl)'),'ngl','server.tip.gpu_layers'],['gpuSplitMode',t('Режим распределения (-sm)'),'select',['none','layer','row','tensor']],['tensorSplit',t('Распределение GPU (-ts)'),'text','server.tip.tensor_split'],['mainGpu',t('Основной GPU (-mg)'),'number','server.tip.main_gpu'],['selectedGpuIndices',t('Используемые GPU (например 0,1,100000) (--device)'),'text'],['flashAttention','Flash Attention (-fa)','bool','server.tip.flash_attention'],['mmap',t('Карта памяти (--mmap)'),'bool','server.tip.mmap'],['mlock',t('Блокировка памяти (--mlock)'),'bool','server.tip.mlock'],['mmqEnabled','MMQ (--no-mmq)','bool','server.tip.mmq'],['kvOffloadEnabled','KV Offload (-nkvo)','bool','server.tip.kv_offload']],
 context:[['contextSize',t('Контекст (-c)'),'number','server.tip.ctx_size'],['batchSize','Batch size (-b)','number','server.tip.batch_size'],['ubatchSize','UBatch size (-ub)','number','server.tip.ubatch_size'],['maxTokens',t('Максимум токенов (-n)'),'number','server.tip.predict'],['threads',t('Потоки (-t)'),'number','server.tip.threads'],['threadsBatch',t('Потоки batch (-tb)'),'number','server.tip.threads_batch'],['predictCount',t('Токены MTP (-n)'),'number','server.tip.predict_count'],['timeout',t('Таймаут (-to)'),'number','server.tip.timeout'],['slots',t('Количество слотов (-np)'),'number','server.tip.parallel']],
 sampling:[['temperature',t('Температура (--temp)'),'number','0.01','server.tip.temperature'],['topK','Top K (--top-k)','number','server.tip.top_k'],['topP','Top P (--top-p)','number','0.01','server.tip.top_p'],['minP','Min P (--min-p)','number','0.01','server.tip.min_p'],['typicalP','Typical P (--typical-p)','number','0.01','server.tip.typical_p'],['repeatPenalty',t('Штраф повторов (--repeat-penalty)'),'number','0.01','server.tip.repeat_penalty'],['repeatLastN',t('Последние токены (--repeat-last-n)'),'number','server.tip.repeat_last_n'],['presencePenalty','Presence penalty (--presence-penalty)','number','0.01','server.tip.presence_penalty'],['frequencyPenalty','Frequency penalty (--frequency-penalty)','number','0.01','server.tip.frequency_penalty'],['seed','Seed (-s)','number','server.tip.seed'],['mirostat','Mirostat (--mirostat)','select',['disabled','mode1','mode2'],'server.tip.mirostat'],['mirostatTau','Mirostat tau (--mirostat-ent)','number','0.01','server.tip.mirostat_tau'],['mirostatEta','Mirostat eta (--mirostat-lr)','number','0.01','server.tip.mirostat_eta'],['dryMultiplier','DRY multiplier (--dry-multiplier)','number','0.01'],['dryBase','DRY base (--dry-base)','number','0.01'],['dynatempStddev','Dynamic temperature stddev (--dynatemp-range)','number','0.01'],['xtcProbability','XTC probability (--xtc-probability)','number','0.01'],['xtcThreshold','XTC threshold (--xtc-threshold)','number','0.01']],
 kv:[['cacheTypeK',t('Тип KV-кэша K (-ctk)'),'select',['f32','f16','bf16','q8_0','q4_0','q4_1','iq4_nl','q5_0','q5_1'],'server.tip.cache_type_k'],['cacheTypeV',t('Тип KV-кэша V (-ctv)'),'select',['f32','f16','bf16','q8_0','q4_0','q4_1','iq4_nl','q5_0','q5_1'],'server.tip.cache_type_v'],['cacheRam',t('Кэш RAM (--cache-ram)'),'number','server.tip.cache_ram'],['cacheReuse',t('Повторное использование кэша (--cache-reuse)'),'number','server.tip.cache_reuse'],['contBatching',t('Непрерывный batch (-cb)'),'bool','server.tip.cont_batching'],['contextShift',t('Сдвиг контекста (--context-shift)'),'bool','server.tip.context_shift'],['cachePrompt',t('Кэш промпта (--cache-prompt)'),'bool','server.tip.cache_prompt']],
 mtp:[['speculativeDecoding',t('Использовать черновую модель (-md)'),'bool','server.tip.speculative_decoding'],['draftModelLocalOnly',t('Draft только на локальную GPU (не DFlash/EAGLE) (--spec-draft-device)'),'bool'],['jinja','Jinja / Chat Template (--jinja)','bool'],['specType',t('Тип speculative (--spec-type)'),'select',['none','draft-simple','draft-eagle3','draft-mtp','draft-dflash','ngram-simple','ngram-map-k','ngram-map-kdv','ngram-mod','ngram-cache','draft'],'server.tip.spec_type'],['draftModelPath',t('Draft-модель (-md)'),'text','server.tip.draft_model_path'],['specDraftGpuLayers',t('Слои GPU draft (-ngld)'),'number','server.tip.spec_draft_gpu_layers'],['specDraftNMax','Draft N max (--spec-draft-n-max)','number','server.tip.spec_draft_n_max'],['specDraftNMin','Draft N min (--spec-draft-n-min)','number','server.tip.spec_draft_n_min'],['specDraftPSplit','Draft p-split (--spec-draft-p-split)','number','0.01','server.tip.spec_draft_p_split'],['specDraftPMin','Draft p-min (--spec-draft-p-min)','number','0.01','server.tip.spec_draft_p_min'],['predictCount',t('Макс. токенов (-n)'),'number','server.tip.mtp_predict']],
 rope:[['ropeScaling',t('Масштабирование Rope (--rope-scaling)'),'select',['','none','linear','yarn'],'server.tip.rope_scaling'],['ropeFreqBase','Rope base freq (--rope-freq-base)','number','0.01','server.tip.rope_freq_base'],['ropeFreqScale','Rope scale freq (--rope-freq-scale)','number','0.0001','server.tip.rope_freq_scale'],['yarnOriginalContext','YARN original context (--yarn-orig-ctx)','number','server.tip.yarn_orig_ctx'],['yarnExtFactor','YARN ext factor (--yarn-ext-factor)','number','0.01','server.tip.yarn_ext_factor'],['yarnAttnFactor','YARN attn factor (--yarn-attn-factor)','number','0.01','server.tip.yarn_attn_factor'],['yarnBetaFast','YARN beta fast (--yarn-beta-fast)','number','0.01','server.tip.yarn_beta_fast'],['yarnBetaSlow','YARN beta slow (--yarn-beta-slow)','number','0.01','server.tip.yarn_beta_slow']],
 server:[['host',t('Адрес llama-server (--host)'),'text','server.tip.host'],['port',t('Порт (--port)'),'number','server.tip.port'],['timeout',t('Таймаут (-to)'),'number','server.tip.timeout'],['slots',t('Количество слотов (-np)'),'number','server.tip.parallel'],['enableSlots',t('Включить слоты (--slots)'),'bool','server.tip.slots_monitoring'],['enableMetrics',t('Метрики (--metrics)'),'bool','server.tip.metrics'],['enableWebUI',t('Встроенный Web UI (--ui)'),'bool','server.tip.webui'],['alias',t('Имя модели / alias (-a)'),'text'],['apiKey','API key (--api-key)','password'],['logFilePath',t('Файл журнала (--log-file)'),'text'],['verboseLogging',t('Подробный журнал (--verbose)'),'bool','server.tip.verbose_logging'],['embeddingMode',t('Режим embeddings (--embedding)'),'bool','server.tip.embedding_mode'],['poolingType','Pooling (--pooling)','select',['','mean','cls','last','rank']],['priorityHigh',t('Высокий приоритет'),'bool','server.tip.priority_high'],['processPriority',t('Приоритет процесса'),'select',['Normal','AboveNormal','High','Realtime'],'server.tip.priority_high'],['numa','NUMA (--numa)','bool','server.tip.numa'],['cpuAffinity',t('Привязка CPU (-C)'),'text']],
 args:[['customArguments',t('Пользовательские аргументы (по одному на строку)'),'textarea','server.tip.additional_args']],
 rpc:[['rpcEnabled',t('Использовать удалённый RPC'),'bool'],['rpcEndpoint','RPC endpoint (--rpc)','text'],['rpcSshUsername',t('Логин SSH'),'text'],['rpcSshPassword',t('Пароль SSH'),'password'],['rpcSavePassword',t('Сохранять пароль'),'bool'],['rpcUseCache',t('Использовать кэш RPC'),'bool'],['rpcRemoteTelemetry',t('Передавать телеметрию с RPC'),'bool'],['rpcServerPath',t('Путь к ggml-rpc-server.exe на удалённом компьютере'),'text']]
};
 function serverField(spec,p){let k=spec[0],label=spec[1],type=spec[2]||'text',tt=spec[4]?' data-tt="'+spec[4]+'"':'',source=Object.keys(p||{}).find(x=>x.toLowerCase()===k.toLowerCase()),value=source?p[source]:p[k],passwordSetKey=Object.keys(p||{}).find(x=>x.toLowerCase()==='rpcsshpasswordset'),passwordMasked=k==='rpcSshPassword'&&!!(passwordSetKey?p[passwordSetKey]:false);if(k==='customArguments'&&value&&typeof value==='object')value=Object.entries(value).map(x=>x[0]+(x[1]?' '+x[1]:'')).join('\\n');if(type==='ngl'){let canon=String(value??'').trim();let nglMode=canon==='all'?'all':(/^\d+$/.test(canon)?'custom':'auto');let nglNum=/^\d+$/.test(canon)?canon:'';return '<label class="field"'+tt+'><span class="label">'+esc(label)+'</span><div class="ngl-wrap"><input type="hidden" data-server-key="'+k+'" value="'+esc(canon)+'"><select data-ngl-mode><option value="auto" '+(nglMode==='auto'?'selected':'')+'>'+t('Авто')+'</option><option value="all" '+(nglMode==='all'?'selected':'')+'>'+t('Все')+'</option><option value="custom" '+(nglMode==='custom'?'selected':'')+'>'+t('Своё число')+'</option></select><input data-ngl-number type="number" min="0" step="1" value="'+esc(nglNum)+'" '+(nglMode==='custom'?'':'disabled')+' style="width:92px"></div></label>'}
if(type==='bool')return '<label class="check server-check"'+tt+'><input data-server-key="'+k+'" type="checkbox" '+(value?'checked':'')+'><span>'+esc(label)+'</span></label>';if(type==='select'){let normalized=String(value??'');if(k==='gpuSplitMode'&&/^\\d+$/.test(normalized))normalized=['none','layer','row','tensor'][Number(normalized)]||'none';if(k==='cacheTypeK'||k==='cacheTypeV')normalized=['f32','f16','bf16','q8_0','q4_0','q4_1','iq4_nl','q5_0','q5_1'][Number(normalized)]||normalized.toLowerCase();if(k==='mirostat'&&/^\\d+$/.test(normalized))normalized=['disabled','mode1','mode2'][Number(normalized)]||'disabled';return '<label class="field"'+tt+'><span class="label">'+esc(label)+'</span><select data-server-key="'+k+'">'+spec[3].map(x=>'<option value="'+esc(x)+'" '+(normalized.toLowerCase()===String(x).toLowerCase()?'selected':'')+'>'+esc(x||t('Авто'))+'</option>').join('')+'</select></label>'}let step=spec[3]?' step="'+spec[3]+'"':'';let wide=type==='textarea'||k.toLowerCase().includes('path')||k==='rpcEndpoint'||k==='rpcServerPath';if(type==='textarea')return '<label class="field '+(wide?'wide':'')+'"'+tt+'><span class="label">'+esc(label)+'</span><textarea data-server-key="'+k+'" rows="5">'+esc(value??'')+'</textarea></label>';return '<label class="field '+(wide?'wide':'')+'"'+tt+'><span class="label">'+esc(label)+'</span><input data-server-key="'+k+'" type="'+type+'"'+step+' value="'+(type==='password'?(passwordMasked?'********':''):esc(value??''))+'"'+(passwordMasked?' data-password-masked="true"':'')+' autocomplete="new-password"></label>'}
 function serverFields(fields,p){let html='<div class="formgrid server-formgrid">'+fields.map(spec=>serverField(spec,p)).join('');return html+'</div>'}
 function enhanceServerShell(){let page=document.getElementById('server');if(!page)return;let first=page.querySelector('section');if(first&&!document.getElementById('serverProfileTools')){let title=first.querySelector('h2');if(title)title.textContent=t('Управление llama-server');let row=first.querySelector('.row');if(row){row.classList.add('server-top-row');row.insertAdjacentHTML('beforeend','<div id="serverProfileTools" class="server-profile-tools"><button class="secondary" onclick="createWebProfile()">Создать профиль</button><button class="secondary" onclick="renameWebProfile()">Переименовать</button><button class="stop" onclick="deleteWebProfile()">Удалить</button><button class="orange" onclick="makeDefaultProfile()">Сделать дефолтным</button></div>')}}let section=page.querySelectorAll('section')[1];if(section&&!document.getElementById('serverBuildRow')){let tab=section.querySelector('#serverTabs');if(tab)tab.insertAdjacentHTML('beforebegin','<div id="serverBuildRow" class="server-build-row"><label class="field"><span class="label">Версия llama.cpp</span><select id="serverBuildSelect" onchange="saveServerBuild(this.value)"></select></label><span id="serverBuildInfo" class="muted"></span></div>')}}
 function renderServerTabs(p){enhanceServerShell();let root=document.getElementById('serverBodies'),tabsHost=document.getElementById('serverTabs');if(!root||!tabsHost)return;let active=tabsHost.querySelector('.tab.active')?.dataset.tab||tabsHost.querySelector('.tab.active')?.dataset.serverTab||'model';tabsHost.innerHTML=Object.keys(serverSchema).map(k=>'<button class="tab '+(k===active?'active':'')+'" data-server-tab="'+k+'">'+serverTabNames[k]+'</button>').join('');root.innerHTML=Object.entries(serverSchema).map(([key,fields])=>'<div class="tabbody '+(key===active?'active':'')+'" data-server-body="'+key+'">'+serverFields(fields,p)+(key==='gpu'?'<div id="serverGpuChooser" class="server-gpu-chooser"></div><div class="diag">Выберите локальные и RPC GPU. Значения сохраняются в SelectedGpuIndices и участвуют в построении команды.</div>':'')+(key==='model'?'<div class="diag">Пути к моделям относятся к локальному компьютеру. Путь ggml-rpc-server.exe задаётся отдельно только во вкладке RPC.</div>':'')+(key==='rpc'?'<div class="diag">SSH подключается отдельно от запуска llama-server. Пароль не отображается после сохранения.</div>':'')+'</div>').join('');tabsHost.querySelectorAll('[data-server-tab]').forEach(b=>b.onclick=()=>{tabsHost.querySelectorAll('.tab').forEach(x=>x.classList.remove('active'));root.querySelectorAll('.tabbody').forEach(x=>x.classList.remove('active'));b.classList.add('active');root.querySelector('[data-server-body="'+b.dataset.serverTab+'"]')?.classList.add('active')});loadServerBuilds(p);loadServerGpuChooser(p)}
 async function loadServerGpuChooser(p){if(window.normalizeServerGpuTab)return window.normalizeServerGpuTab();let host=document.getElementById('serverGpuChooser');if(!host)return;try{let raw=await json('/api/gpu');let list=Array.isArray(raw)?raw:(raw.value||[]);let selected=String(p.selectedGpuIndices||'').split(',').map(x=>x.trim()).filter(Boolean);host.innerHTML='<div class="label">Используемые GPU</div><div class="gpu-choice-grid">'+list.map(g=>{let id=String(g.index??'');let checked=selected.length?selected.includes(id):!!g.isSelected;return '<label class="check gpu-choice"><input type="checkbox" data-gpu-index="'+esc(id)+'" '+(checked?'checked':'')+' onchange="syncServerGpuSelection()"><span>'+esc(g.displayName||g.name||('GPU '+id))+'</span></label>'}).join('')+'</div>';if(!list.length)host.innerHTML='<div class="muted">GPU не обнаружены</div>'}catch(e){host.innerHTML='<div class="muted">Не удалось получить список GPU</div>'}}
 function syncServerGpuSelection(){let hidden=document.querySelector('#serverBodies [data-server-key="selectedGpuIndices"]');if(!hidden)return;hidden.value=Array.from(document.querySelectorAll('#serverGpuChooser [data-gpu-index]:checked')).map(x=>x.dataset.gpuIndex).join(',')}
 async function loadServerBuilds(p){let select=document.getElementById('serverBuildSelect');if(!select)return;try{let d=await json('/api/llama/installed');let list=Array.isArray(d.versions)?d.versions:[];select.innerHTML=list.map(v=>'<option value="'+esc(v.versionKey||v.folderName||'')+'">'+esc(v.display||v.versionKey||v.folderName||'')+'</option>').join('');let selected=p?.selectedServerVersion||d.active||'';if(selected&&list.some(v=>String(v.versionKey||v.folderName)===String(selected)))select.value=selected;document.getElementById('serverBuildInfo').textContent=list.length?list.length+t(' установлено'):t('Сборки не найдены')}catch(e){document.getElementById('serverBuildInfo').textContent=t('Не удалось прочитать сборки')}}
 async function saveServerBuild(value){if(!value||!currentId)return;let q=new URLSearchParams({id:currentId,selectedServerVersion:value});let r=await fetch('/api/profile/save?'+q);let e=document.getElementById('message');if(e)e.textContent=r.ok?t('Версия llama.cpp сохранена. Для применения перезапустите сервер.'):t('Ошибка сохранения версии');if(r.ok)await profileForm()}
 function serverValues(){let q=new URLSearchParams({id:currentId});document.querySelectorAll('#serverBodies [data-server-key]').forEach(e=>{if(e.dataset.passwordMasked==='true'||((e.type==='password'||e.dataset.serverKey==='apiKey'||e.dataset.serverKey==='rpcSshPassword')&&!e.value))return;if(e.dataset.serverKey==='customArguments')q.set('customArguments',e.value);else q.set(e.dataset.serverKey,e.type==='checkbox'?String(e.checked):e.value)});return q}

 function nglApply(mode,wrap){let hidden=wrap.querySelector('[data-server-key="gpuLayers"]');let num=wrap.querySelector('[data-ngl-number]');if(mode==='auto'){if(hidden)hidden.value='';if(num){num.disabled=true;num.value='';}}else if(mode==='all'){if(hidden)hidden.value='all';if(num){num.disabled=true;num.value='';}}else{if(num){num.disabled=false;if(!/^\d+$/.test(num.value))num.value='32';}if(hidden)hidden.value=String(num.value||'').trim();}}
 function nglSyncFromValue(wrap){let hidden=wrap.querySelector('[data-server-key="gpuLayers"]');if(!hidden)return;let canon=String(hidden.value||'').trim();let mode=canon==='all'?'all':(/^\d+$/.test(canon)?'custom':'auto');let sel=wrap.querySelector('[data-ngl-mode]');if(sel)sel.value=mode;let num=wrap.querySelector('[data-ngl-number]');if(num){num.value=/^\d+$/.test(canon)?canon:'';num.disabled=mode!=='custom';}}
 window.nglAutoAllForTensor=function(msg){let hidden=document.querySelector('#serverBodies [data-server-key="gpuLayers"]');if(!hidden)return false;let v=String(hidden.value||'').trim();if(v!==''&&v!=='-1')return false;hidden.value='all';let wrap=hidden.closest('.ngl-wrap');if(wrap)nglSyncFromValue(wrap);if(msg){let m=document.getElementById('message');if(m)m.textContent=t(msg)}return true};
 window.enhanceNglControls=function(){document.querySelectorAll('#serverBodies .ngl-wrap').forEach(wrap=>nglSyncFromValue(wrap))};
 async function saveNglProfile(){try{await window.saveWebProfileValues();let m=document.getElementById('message');if(m)m.textContent=t('Параметр сохранён автоматически.')}catch(err){let m=document.getElementById('message');if(m)m.textContent=t('Ошибка автосохранения: ')+err.message}}
 document.addEventListener('change',function(e){let el=e.target;if(!el||!el.hasAttribute)return;if(el.hasAttribute('data-ngl-mode')){let wrap=el.closest('.ngl-wrap');if(!wrap)return;nglApply(el.value,wrap);saveNglProfile()}else if(el.hasAttribute('data-ngl-number')){let wrap2=el.closest('.ngl-wrap');if(!wrap2)return;let h2=wrap2.querySelector('[data-server-key="gpuLayers"]');if(h2)h2.value=String(el.value||'').trim();saveNglProfile()}else if(el.hasAttribute('data-server-key')&&el.dataset.serverKey==='gpuSplitMode'&&el.value==='tensor'){if(window.nglAutoAllForTensor&&window.nglAutoAllForTensor('Для режима Tensor слои GPU установлены автоматически: All.')){window.saveWebProfileValues&&window.saveWebProfileValues().catch(function(){})}}},true); async function saveFullProfile(){if(!currentId)return;let r=await fetch('/api/profile/save?'+serverValues());let e=document.getElementById('message');if(e)e.textContent=r.ok?t('Профиль сохранён. Для применения параметров перезапустите сервер.'):await r.text();if(r.ok)await profileForm()}
 async function createWebProfile(){let name=prompt(t('Название нового профиля'),t('Новый профиль'));if(!name)return;let r=await fetch('/api/profile/create?name='+encodeURIComponent(name));let d=await r.json().catch(()=>({}));if(!r.ok){alert(d.error||t('Не удалось создать профиль'));return}currentId=d.id;await profiles();await profileForm()}
 async function renameWebProfile(){if(!currentId)return;let p=await json('/api/profile?id='+encodeURIComponent(currentId));let name=prompt(t('Новое название профиля'),p.name||'');if(!name||name===p.name)return;let r=await fetch('/api/profile/rename?id='+encodeURIComponent(currentId)+'&name='+encodeURIComponent(name));if(!r.ok)alert(await r.text());await profiles();await profileForm()}
 async function deleteWebProfile(){if(!currentId||!confirm(t('Удалить выбранный профиль?')))return;let r=await fetch('/api/profile/delete?id='+encodeURIComponent(currentId));if(!r.ok){alert(await r.text());return}let p=await profiles();currentId=p.find(x=>x.isDefault)?.id||p[0]?.id||'';if(currentId)await profileForm()}
 buildServerTabs=renderServerTabs;profileChanged=function(){return selectProfile(document.getElementById('profiles')?.value||currentId)};enhanceServerShell();
 let serverStyle=document.createElement('style');serverStyle.textContent='.server-top-row{align-items:flex-end}.server-profile-tools{display:flex;gap:6px;flex-wrap:wrap;align-items:center}.server-profile-tools button{font-size:12px}.server-build-row{display:flex;gap:14px;align-items:flex-end;flex-wrap:wrap;margin:0 0 16px;padding:12px;border:1px solid #334155;border-radius:8px;background:#172236}.server-build-row .field{max-width:360px}.server-formgrid{grid-template-columns:repeat(auto-fit,minmax(250px,1fr))}.server-check{min-height:42px;padding:10px;border:1px solid #26364f;border-radius:6px;background:#172236}.server-check input{margin-right:5px}.tabbody .diag{grid-column:1/-1;margin-top:12px;padding:10px;color:#94a3b8;background:#111827;border-radius:6px}';document.head.appendChild(serverStyle);
})();
""";
		const string desktopServerScript = """
(function(){
 const getId=()=>document.getElementById('profiles')?.value||'';
 const safe=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
 const num=v=>Number.isFinite(Number(v))?Number(v):0;
 function metric(label,value,pct,color){return '<div class="server-metric"><div class="server-metric-line"><span>'+safe(label)+'</span><span>'+safe(value)+'</span></div><div class="server-bar"><i class="'+color+'" style="width:'+Math.max(0,Math.min(100,num(pct)))+'%"></i></div></div>'}
 function gpuSummaryCardsS(g){if(!g||!g.length)return '';let used=g.reduce((a,x)=>a+num(x.memoryUsedGb),0),total=g.reduce((a,x)=>a+num(x.memoryTotalGb),0),load=g.map(x=>'GPU'+(x.index??'')+' '+num(x.gpuUtilization).toFixed(0)+'%').join('  '),draw=g.reduce((a,x)=>a+num(x.powerDrawWatts),0),limit=g.reduce((a,x)=>a+((num(x.powerLimitWatts)||num(x.powerLimit)||0)),0);let h='<div class="server-gpu-card"><h3>'+t('Все GPU')+'</h3>'+metric('VRAM',used.toFixed(1)+' / '+total.toFixed(1)+' GB',total?used/total*100:0,'blue')+'<div class="server-metric-line"><span>'+t('Загрузка GPU')+'</span><span>'+load+'</span></div><div class="server-metric-line"><span>'+t('Мощность')+'</span><span>'+draw.toFixed(0)+' W</span></div></div>';if(limit>0)h+='<div class="server-gpu-card"><h3>'+t('Мощность всех GPU')+'</h3>'+metric(t('Мощность'),draw.toFixed(0)+' / '+limit.toFixed(0)+' W',limit?draw/limit*100:0,'orange')+'</div>';return h}function gpuMonitorHtml(data){let ram=data?.ram||{},used=num(ram.usedGb),total=num(ram.totalGb),committed=num(ram.committedGb),cache=num(ram.cacheLimitGb),ramVal=(total?used.toFixed(1)+' / '+total.toFixed(1)+' GB':'— / — GB')+(committed>0?t(' · выделено: ')+committed.toFixed(1)+' GB':'')+(cache>0?t(' · кэш: до ')+cache.toFixed(1)+' GB':''),html='<div class="server-monitor-title">Мониторинг GPU <span>• Live</span></div>';html+=gpuSummaryCardsS(data?.gpus||[]);html+='<div class="server-ram-card"><h3>RAM</h3>'+metric(t('ОЗУ'),ramVal,total?used/total*100:0,'green')+'</div>';for(const g of (data?.gpus||[])){let vu=num(g.memoryUsedGb),vt=num(g.memoryTotalGb),pw=num(g.powerDrawWatts),pl=num(g.powerLimitWatts)||num(g.powerLimit)||1000,temp=num(g.temperatureCelsius),mtemp=num(g.memoryTemperatureCelsius),util=num(g.gpuUtilization),fan=num(g.fanSpeed);html+='<div class="server-gpu-card"><h3>'+safe(g.displayName||g.name||('GPU '+(g.index??'')))+'</h3>'+metric('VRAM',vu.toFixed(1)+' / '+vt.toFixed(1)+' GB',vt?vu/vt*100:0,'blue')+metric(t('Загрузка GPU'),util.toFixed(0)+'%',util,'blue')+metric(t('Мощность'),pw.toFixed(0)+' / '+pl.toFixed(0)+' W',pl?pw/pl*100:0,'orange')+metric(t('Температура GPU'),temp.toFixed(0)+' °C',temp,'green')+(mtemp>0?metric(t('Темп. памяти GPU'),mtemp.toFixed(0)+' °C',110,'red'):'')+metric(t('Вентилятор'),fan.toFixed(0)+'%',fan,'blue')+'<div class="server-gpu-footer"><span>'+safe(g.clockMhz?g.clockMhz.toFixed(0)+' MHz':'—')+'</span><span>'+safe(g.computeCapability||'')+'</span></div></div>'}return html||'<div class="muted">Данные GPU недоступны</div>'}
 async function updateServerTelemetry(){let box=document.getElementById('serverStatusMonitor'),gpu=document.getElementById('serverGpuMonitor');if(!box&&!gpu)return;try{let d=await json('/api/monitoring?id='+encodeURIComponent(currentId)),s=await json('/api/status'),status=s.llamaServer||'Stopped',used=(d.gpus||[]).reduce((a,g)=>a+num(g.memoryUsedGb),0),total=(d.gpus||[]).reduce((a,g)=>a+num(g.memoryTotalGb),0),ram=d.ram||{},speed=d.speed||{};if(box)box.innerHTML='<div class="server-status-main"><span class="server-status-label">Состояние:</span><b class="'+(status==='Running'?'is-running':'is-stopped')+'">'+safe(status)+'</b></div><div class="server-status-stats"><span>VRAM: '+(total?used.toFixed(1)+' / '+total.toFixed(1)+' GB':'— / — GB')+'</span><span>RAM: '+(num(ram.totalGb)?num(ram.usedGb).toFixed(1)+' / '+num(ram.totalGb).toFixed(1)+' GB':'— / — GB')+'</span><span>ток/с: '+num(speed.tokensPerSecond).toFixed(1)+'</span><span>Промпт ток/с: '+num(speed.promptTokensPerSecond).toFixed(1)+'</span><button class="speed-button" onclick="measureSpeedWeb()">Измерить скорость</button></div>';if(gpu)gpu.innerHTML=gpuMonitorHtml(d)}catch(e){if(box)box.innerHTML='<span class="is-stopped">Мониторинг недоступен: '+safe(e.message)+'</span>'}}
		const updateServerTelemetryBase=updateServerTelemetry;
		updateServerTelemetry=async function(){await updateServerTelemetryBase();try{let d=await json('/api/monitoring?id='+encodeURIComponent(currentId)),r=d.ram||{},used=num(r.usedGb),total=num(r.totalGb),committed=num(r.committedGb),cacheLimit=num(r.cacheLimitGb),value=(total?used.toFixed(1)+' / '+total.toFixed(1)+' GB':'— / — GB')+(committed>0?t(' · выделено: ')+committed.toFixed(1)+' GB':'')+(cacheLimit>0?t(' · кэш: до ')+cacheLimit.toFixed(1)+' GB':'');let ramLine=document.querySelector('#serverGpuMonitor .server-ram-card .server-metric-line span:last-child');if(ramLine)ramLine.textContent=value;let statusRam=document.querySelector('#serverStatusMonitor .server-status-stats span:nth-child(2)');if(statusRam)statusRam.textContent='RAM: '+value}catch(e){}};
		// Keep the status strip DOM stable. Replacing its innerHTML on every poll caused a visible flash,
		// especially while the cache value was appended after the first telemetry request.
		updateServerTelemetry=async function(){let box=document.getElementById('serverStatusMonitor'),gpu=document.getElementById('serverGpuMonitor');if(!box&&!gpu)return;try{let d=await json('/api/monitoring?id='+encodeURIComponent(currentId)),s=await json('/api/status'),status=s.llamaServer||'Stopped',used=(d.gpus||[]).reduce((a,g)=>a+num(g.memoryUsedGb),0),total=(d.gpus||[]).reduce((a,g)=>a+num(g.memoryTotalGb),0),ram=d.ram||{},speed=d.speed||{},ramUsed=num(ram.usedGb),ramTotal=num(ram.totalGb),committed=num(ram.committedGb),cacheLimit=num(ram.cacheLimitGb),ramText=(ramTotal?ramUsed.toFixed(1)+' / '+ramTotal.toFixed(1)+' GB':'— / — GB')+(committed>0?t(' · выделено: ')+committed.toFixed(1)+' GB':'')+(cacheLimit>0?t(' · кэш: до ')+cacheLimit.toFixed(1)+' GB':'');if(box){if(!document.getElementById('serverStatusValue'))box.innerHTML='<div class="server-status-main"><span class="server-status-label">Состояние:</span><b id="serverStatusValue"></b></div><div class="server-status-stats"><span id="serverStatusVram"></span><span id="serverStatusRam"></span><span id="serverStatusTokens"></span><span id="serverStatusPrompt"></span><button class="speed-button" onclick="measureSpeedWeb()">Измерить скорость</button></div>';let state=document.getElementById('serverStatusValue');state.textContent=status;state.className=status==='Running'?'is-running':'is-stopped';document.getElementById('serverStatusVram').textContent='VRAM: '+(total?used.toFixed(1)+' / '+total.toFixed(1)+' GB':'— / — GB');document.getElementById('serverStatusRam').textContent='RAM: '+ramText;document.getElementById('serverStatusTokens').textContent=t('ток/с: ')+num(speed.tokensPerSecond).toFixed(1);document.getElementById('serverStatusPrompt').textContent=t('Промпт ток/с: ')+num(speed.promptTokensPerSecond).toFixed(1)}if(gpu)gpu.innerHTML=gpuMonitorHtml(d)}catch(e){let state=document.getElementById('serverStatusValue');if(state){state.textContent=t('Мониторинг недоступен');state.className='is-stopped'}}};
		window.measureSpeedWeb=async function(){try{await json('/api/monitoring/speed')}finally{await updateServerTelemetry()}};
 window.showServerCommand=async function(){let box=document.getElementById('serverCommand');if(!box||!currentId)return;try{let d=await json('/api/profile/command?id='+encodeURIComponent(currentId));box.textContent=d.command||d.args||t('Команда не сформирована')}catch(e){box.textContent=t('Ошибка: ')+e.message}};
 window.copyServerCommand=async function(){let d=await json('/api/profile/command?id='+encodeURIComponent(currentId));await navigator.clipboard?.writeText(d.command||d.args||'');let e=document.getElementById('serverMessage');if(e)e.textContent=t('Команда скопирована')};
 window.exportServerBat=async function(){let d=await json('/api/profile/command?id='+encodeURIComponent(currentId));let name=(profileCache?.name||'llama-server').replace(/[^\wа-яё-]+/gi,'_'),content='@echo off\r\nllama-server '+(d.args||'')+'\r\npause\r\n',a=document.createElement('a');a.href=URL.createObjectURL(new Blob([content],{type:'application/octet-stream'}));a.download=name+'.bat';a.click();setTimeout(()=>URL.revokeObjectURL(a.href),1000)};
 window.importServerBat=function(){let input=document.createElement('input');input.type='file';input.accept='.bat,.cmd,text/plain';input.onchange=async()=>{let file=input.files?.[0];if(!file)return;let text=await file.text(),r=await fetch('/api/profile/import-bat?id='+encodeURIComponent(currentId)+'&content='+encodeURIComponent(text)),d=await r.json().catch(()=>({})),e=document.getElementById('serverMessage');if(e)e.textContent=r.ok?t('Импортировано аргументов: ')+(d.imported||0):(d.error||t('Ошибка импорта'));if(r.ok)await profileForm()};input.click()};
 window.copyFromWebProfile=async function(){let list=await json('/api/profiles'),names=list.filter(x=>x.id!==currentId).map(x=>x.name+' ['+x.id+']').join('\n'),chosen=prompt(t('Укажите имя или ID профиля-источника:\n')+names);if(!chosen)return;let source=list.find(x=>x.id!==currentId&&(chosen===x.id||chosen.startsWith(x.name+' [')));if(!source){alert(t('Профиль не найден'));return}let r=await fetch('/api/profile/copy?id='+encodeURIComponent(currentId)+'&sourceId='+encodeURIComponent(source.id));if(!r.ok)alert(await r.text());await profileForm()};
 window.cleanupMissingWebProfiles=async function(){if(!confirm(t('Удалить профили, у которых отсутствуют файлы моделей?')))return;let r=await fetch('/api/profile/cleanup-missing'),d=await r.json().catch(()=>({})),e=document.getElementById('serverMessage');if(e)e.textContent=r.ok?t('Удалено профилей: ')+(d.removed||0):(d.error||t('Ошибка очистки'));if(r.ok){await profiles();await profileForm()}};
 function field(key,label,value,type='text',wide=false,placeholder=''){let val=Array.isArray(value)?value.join(','):value??'';return '<label class="field '+(wide?'wide':'')+'"><span class="label">'+label+'</span><input data-server-key="'+key+'" type="'+type+'" value="'+safe(val)+'" placeholder="'+safe(placeholder)+'" autocomplete="new-password"></label>'}
 function check(key,label,value){return '<label class="server-check"><input data-server-key="'+key+'" type="checkbox" '+(value?'checked':'')+'><span>'+label+'</span></label>'}
 function renderServerGpu(){let body=document.querySelector('[data-server-body="gpu"]');if(!body)return;let p=window.profileCache||{};body.innerHTML='<div class="server-gpu-section"><h3>Ускорение GPU</h3><div class="server-formgrid">'+field('gpuLayers',t('Слои GPU (-ngl)'),p.gpuLayers,'text',false,'all')+field('threads',t('Потоки (-t)'),p.threads,'number',false,'-1')+field('threadsBatch',t('Потоки в batch (-tb)'),p.threadsBatch,'number',false,'-1')+'</div><div class="server-toggle-grid">'+check('flashAttention','Flash Attention (-fa)',p.flashAttention)+check('mmap',t('Карта памяти (--mmap)'),p.mmap)+check('mlock',t('Блокировка памяти (--mlock)'),p.mlock)+check('kvOffloadEnabled','KV Offload (-nkvo)',p.kvOffloadEnabled)+'</div></div><div class="server-gpu-section"><h3>Работа с multi-GPU</h3><div class="server-formgrid">'+field('mainGpu',t('Основной GPU (-mg)'),p.mainGpu,'number')+field('tensorSplit',t('Распределение тензоров (--tensor-split)'),p.tensorSplit,'text',true,'89,11')+'</div><div id="serverGpuChooser" class="server-gpu-chooser"></div><div class="diag">Выберите локальные и RPC GPU. Значения сохраняются в профиле и используются при построении команды.</div></div><div class="server-gpu-section"><h3>Мониторинг GPU</h3><div class="muted">Нагрузка обновляется автоматически и не перезапускает llama-server.</div></div>';loadServerGpuChoices(p)}
 async function loadServerGpuChoices(p){if(window.normalizeServerGpuTab)return window.normalizeServerGpuTab();let host=document.getElementById('serverGpuChooser');if(!host)return;try{let list=await json('/api/gpu'),selected=String(p.selectedGpuIndices||'').split(',').map(x=>x.trim()).filter(Boolean);host.innerHTML='<div class="label">Используемые GPU</div><div class="gpu-choice-grid">'+(list||[]).map(g=>{let id=String(g.index??'');return '<label class="gpu-choice"><input type="checkbox" data-gpu-index="'+safe(id)+'" '+(selected.length?selected.includes(id):!!g.isSelected?'checked':'')+' onchange="syncWebGpuSelection()"><span>'+safe(g.displayName||g.name||('GPU '+id))+'</span></label>'}).join('')+'</div>';if(!list?.length)host.innerHTML='<div class="muted">GPU не обнаружены</div>'}catch{host.innerHTML='<div class="muted">Не удалось получить список GPU</div>'}}
 window.syncWebGpuSelection=function(){let hidden=document.querySelector('#serverBodies [data-server-key="selectedGpuIndices"]');if(hidden)hidden.value=Array.from(document.querySelectorAll('#serverGpuChooser [data-gpu-index]:checked')).map(x=>x.dataset.gpuIndex).join(',')};
 function ensureServerChrome(){let page=document.getElementById('server');if(!page)return;let first=page.querySelector('section'),h=page.querySelector('h1');if(first&&!document.getElementById('serverStatusMonitor')&&h)h.insertAdjacentHTML('afterend','<section id="serverStatusMonitor" class="server-status-strip"></section>');if(first&&!document.getElementById('serverMessage')){let e=document.createElement('div');e.id='serverMessage';e.className='server-message';first.appendChild(e)}let tools=document.getElementById('serverProfileTools');if(tools&&!tools.querySelector('[data-desktop-extra]'))tools.insertAdjacentHTML('beforeend','<button data-desktop-extra class="secondary" onclick="copyFromWebProfile()">Копировать</button><button data-desktop-extra class="secondary" onclick="cleanupMissingWebProfiles()">Очистить без моделей</button>');let body=document.getElementById('serverBodies');if(body&&!document.getElementById('serverGpuMonitor')){let m=document.createElement('section');m.id='serverGpuMonitor';m.className='server-monitor-section';m.innerHTML='<div class="muted">Мониторинг GPU загружается…</div>';body.parentElement.insertBefore(m,body.nextElementSibling)}let old=document.getElementById('serverCommandSection');if(old)old.style.display='none';if(first&&!document.getElementById('serverDesktopTools'))first.insertAdjacentHTML('afterend','<section id="serverDesktopTools"><div class="row"><h2 style="flex:1">Профиль и команда запуска</h2><button class="secondary" onclick="showServerCommand()">Обновить команду</button></div><pre id="serverCommand" class="command-preview">Команда ещё не загружена</pre><div class="actions"><button class="secondary" onclick="exportServerBat()">Экспорт .bat</button><button class="secondary" onclick="copyServerCommand()">Копировать команду</button><button class="cyan" onclick="importServerBat()">Импорт .bat</button></div></section>')}showServerCommand();updateServerTelemetry()}
 const oldBuild=window.buildServerTabs;if(oldBuild&&!window._desktopServerBuildWrapped){window._desktopServerBuildWrapped=true;window.buildServerTabs=function(p){let r=oldBuild(p);setTimeout(()=>{renderServerGpu();ensureServerChrome();updateServerTelemetry()},0);return r}}
 let style=document.createElement('style');style.textContent='.server-status-strip{background:#211853;border:1px solid #4938b9;border-radius:8px;padding:12px 16px;margin:12px 0}.server-status-main{display:flex;gap:10px;align-items:center}.server-status-label{color:#93c5fd}.is-running{color:#34d399}.is-stopped{color:#f87171}.server-status-stats{display:flex;gap:18px;align-items:center;flex-wrap:wrap;margin-top:8px;color:#93c5fd;font-size:13px}.speed-button{background:#4f46e5!important}.server-message{color:#93c5fd;min-height:20px}.server-monitor-section{background:#111827;border:1px solid #334155;border-radius:8px;padding:16px;margin-top:14px}.server-monitor-title{font-weight:700;font-size:18px;color:#c4b5fd;margin-bottom:12px}.server-monitor-title span{font-size:12px;color:#34d399}.server-ram-card,.server-gpu-card{background:#101827;border:1px solid #334155;border-radius:8px;padding:12px;margin:8px 0}.server-ram-card h3{color:#10b981}.server-gpu-card h3{color:#f8fafc}.server-metric{margin:8px 0}.server-metric-line{display:flex;justify-content:space-between;color:#bfdbfe;font-size:13px}.server-bar{height:8px;border-radius:5px;background:#374151;overflow:hidden;margin-top:4px}.server-bar i{display:block;height:100%;min-width:2px}.server-bar i.blue{background:#3b82f6}.server-bar i.green{background:#10b981}.server-bar i.orange{background:#f59e0b}.server-gpu-footer{display:flex;justify-content:space-between;color:#94a3b8;font-size:12px;margin-top:8px}.server-gpu-section{background:#101827;border:1px solid #26364f;border-radius:8px;padding:14px;margin:10px 0}.server-gpu-section h3{margin:0 0 12px;color:#dbeafe}.server-toggle-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(210px,1fr));gap:8px;margin-top:10px}.server-formgrid{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:10px}.server-formgrid .wide{grid-column:1/-1}.server-check{display:flex;gap:8px;align-items:center;padding:10px;border:1px solid #334155;border-radius:6px;background:#172236}.gpu-choice-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:8px;margin:8px 0 12px}.gpu-choice{display:flex;gap:8px;align-items:center;border:1px solid #334155;border-radius:6px;background:#172236;padding:10px}.command-preview{white-space:pre-wrap;word-break:break-word;background:#101827;border:1px solid #374151;border-radius:6px;padding:12px;min-height:48px;color:#bfdbfe;font:12px ui-monospace,Consolas,monospace}';document.head.appendChild(style);setTimeout(ensureServerChrome,0);setInterval(()=>{if(document.getElementById('server')?.classList.contains('active')){ensureServerChrome();updateServerTelemetry()}},2500)
})();
""";
		const string desktopModelScript = """
(function(){
 const id=()=>document.getElementById('profiles')?.value||'';
 const text=v=>String(v??'').replace(/[&<>\"']/g,x=>({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#39;'}[x]));
 const n=v=>Number.parseFloat(String(v??''))||0;
 function fileName(path){let s=String(path||'');return s?s.split(/[\\/]/).pop():t('Файл не выбран')}
 function metric(label,value,max,kind){let p=max?Math.max(0,Math.min(100,n(value)/n(max)*100)):0;return '<div class="server-metric"><div class="server-metric-line"><span>'+text(label)+'</span><span>'+text(value)+'</span></div><div class="server-bar"><i class="'+(kind||'blue')+'" style="width:'+p+'%"></i></div></div>'}
 function monitorHtml(d){let r=d?.ram||{},u=n(r.usedGb),t=n(r.totalGb),html='<div class="server-monitor-title">Мониторинг GPU <span>• Live</span></div>';html+='<div class="server-ram-card"><h3>RAM</h3>'+metric(t('ОЗУ'),t?u.toFixed(1)+' / '+t.toFixed(1)+' GB':'— / — GB',t,'green')+'</div>';for(const g of (d?.gpus||[])){let vu=n(g.memoryUsedGb),vt=n(g.memoryTotalGb),pw=n(g.powerDrawWatts),pl=n(g.powerLimitWatts)||n(g.powerLimit)||1000,gu=n(g.gpuUtilization),temp=n(g.temperatureCelsius),mtemp=n(g.memoryTemperatureCelsius),fan=n(g.fanSpeed);html+='<div class="server-gpu-card"><h3>'+text(g.displayName||g.name||'GPU')+'</h3>'+metric('VRAM',vu.toFixed(1)+' / '+vt.toFixed(1)+' GB',vt,'blue')+metric(t('Загрузка GPU'),gu.toFixed(0)+'%',100,'blue')+metric(t('Мощность'),pw.toFixed(0)+' / '+pl.toFixed(0)+' W',pl,'orange')+metric(t('Температура GPU'),temp.toFixed(0)+' °C',100,'green')+(mtemp>0?metric(t('Темп. памяти GPU'),mtemp.toFixed(0)+' °C',110,'red'):'')+metric(t('Вентилятор'),fan.toFixed(0)+'%',100,'blue')+'</div>'}return html||'<div class="muted">Данные GPU недоступны</div>'}
 function modelCard(key,title,subtitle,optional,p){let path=String(p[key]||'');let kind=key==='mmprojPath'?'mmproj':key==='draftModelPath'?'draft':'model';return '<section class="server-model-card"><div class="server-model-card-title"><h3>'+title+'</h3>'+(optional?'<span class="server-optional">Необязательно</span>':'')+'</div><div class="server-model-subtitle">'+subtitle+'</div><div class="server-path-row"><input data-server-key="'+key+'" id="serverModel_'+key+'" value="'+text(path)+'" autocomplete="off"><button class="secondary" onclick="browseServerModel(\''+kind+'\')">Обзор…</button></div><div class="server-path-name" id="serverModelName_'+key+'">'+text(fileName(path))+'</div><div class="server-model-picker-row"><select id="serverModelPicker_'+key+'"><option value="">Выбрать из найденных моделей…</option></select><button class="secondary" onclick="loadServerModelPicker()">Обновить список</button></div></section>';}
 function advancedModelFields(p){let val=(k)=>text(p[k]??'');return '<details class="server-model-advanced"><summary>Дополнительные параметры модели</summary><div class="server-formgrid"><label class="field wide"><span class="label">Каталог llama.cpp</span><input data-server-key="llamaCppBaseDirectory" value="'+val('llamaCppBaseDirectory')+'"></label><label class="field"><span class="label">Имя API / alias</span><input data-server-key="alias" value="'+val('alias')+'"></label><label class="field"><span class="label">HuggingFace репозиторий</span><input data-server-key="hfRepo" value="'+val('hfRepo')+'"></label><label class="field"><span class="label">Файл HuggingFace</span><input data-server-key="hfFile" value="'+val('hfFile')+'"></label><label class="field"><span class="label">Репозиторий draft</span><input data-server-key="hfRepoDraft" value="'+val('hfRepoDraft')+'"></label><label class="field"><span class="label">URL HuggingFace</span><input data-server-key="hfUrl" value="'+val('hfUrl')+'"></label><label class="check server-check"><input data-server-key="hfOffline" type="checkbox" '+(p.hfOffline?'checked':'')+'><span>Офлайн режим (--offline)</span></label><label class="check server-check"><input data-server-key="embeddingMode" type="checkbox" '+(p.embeddingMode?'checked':'')+'><span>Режим embeddings (--embedding)</span></label><label class="field"><span class="label">Pooling</span><select data-server-key="poolingType"><option value="">Авто</option><option value="mean" '+(p.poolingType==='mean'?'selected':'')+'>mean</option><option value="cls" '+(p.poolingType==='cls'?'selected':'')+'>cls</option><option value="last" '+(p.poolingType==='last'?'selected':'')+'>last</option><option value="rank" '+(p.poolingType==='rank'?'selected':'')+'>rank</option></select></label></div></details>'}
 async function webJson(url){let r=await fetch(url);if(!r.ok)throw new Error(await r.text());return r.json()}
 async function loadServerModelPicker(p){try{let list=await webJson('/api/models');for(const key of ['modelPath','mmprojPath','draftModelPath']){let s=document.getElementById('serverModelPicker_'+key);if(!s)continue;let current=String(p?.[key]||document.getElementById('serverModel_'+key)?.value||'');s.innerHTML='<option value="">Выбрать из найденных моделей…</option>'+list.map(x=>'<option value="'+text(x.path)+'">'+text(x.name)+' ('+n(x.sizeGb).toFixed(2)+' GB)</option>').join('');if(current&&list.some(x=>String(x.path).toLowerCase()===current.toLowerCase()))s.value=current;s.onchange=()=>chooseServerModel(key,s.value)} }catch(e){}}
 async function refreshServerModel(){let p=await webJson('/api/profile?id='+encodeURIComponent(id()));renderServerModel(p);return p}
 async function saveModelValue(key,path){if(!path)return;let q=new URLSearchParams({id:id()});q.set(key,path);let r=await fetch('/api/profile/save?'+q);if(!r.ok){alert(await r.text());return}await refreshServerModel()}
 window.chooseServerModel=async function(key,path){if(path)await saveModelValue(key,path)};
 window.browseServerModel=async function(kind){let key=kind==='mmproj'?'mmprojPath':kind==='draft'?'draftModelPath':'modelPath';let current=document.getElementById('serverModel_'+key)?.value||'';try{let d=await webJson('/api/model/browse?kind='+encodeURIComponent(kind)+'&path='+encodeURIComponent(current));if(d.ok&&d.path)await saveModelValue(key,d.path)}catch(e){alert(t('Не удалось открыть выбор файла: ')+e.message)}};
 window.checkServerLlama=async function(validate){let out=document.getElementById('serverLlamaInfo');if(out)out.textContent=validate?t('Проверка флагов…'):t('Проверка версии…');try{let d=await webJson(validate?'/api/llama/validate':'/api/llama/check');if(out)out.innerHTML=(d.executableFound?'Найден: <code>'+text(d.executablePath)+'</code>':t('llama-server.exe не найден'))+'<br>'+text(d.version||d.validation||'')+(d.cuda?' · '+text(d.cuda):'')+(d.validationDetails?'<br>'+text(d.validationDetails):'');}catch(e){if(out)out.textContent=t('Ошибка проверки: ')+e.message}};
 window.renderServerModel=renderServerModel;
 function renderServerModel(p){let body=document.querySelector('#serverBodies [data-server-body="model"]');if(!body)return;body.innerHTML='<div class="server-model-intro"><h3>Путь к llama.cpp серверу</h3><div class="diag">Исполняемый файл сервера llama-server.exe находится в выбранной установленной версии llama.cpp.</div><div id="serverLlamaInfo" class="server-executable-info">Нажмите «Проверить версию», чтобы выполнить проверку вручную.</div><div class="actions"><button class="secondary" onclick="checkServerLlama(false)">Проверить версию</button><button class="secondary" onclick="checkServerLlama(true)">Проверить флаги</button></div></div>'+modelCard('modelPath',t('Основная модель (-m)'),t('GGUF-файл модели, используемый llama-server'),false,p)+modelCard('mmprojPath',t('mmproj (Зрение) (--mmproj)'),t('Файл vision-проектора для мультимодальных моделей'),true,p)+modelCard('draftModelPath',t('Draft-модель (-md)'),t('Необязательная модель для speculative decoding'),true,p)+advancedModelFields(p)+'<section id="serverModelMonitor" class="server-monitor-section"></section>';loadServerModelPicker(p);updateServerModelMonitor()}
 async function loadServerLlamaSummary(){let out=document.getElementById('serverLlamaInfo');if(!out)return;try{let d=await webJson('/api/llama/check');out.innerHTML=(d.executableFound?'Исполняемый файл найден: <code>'+text(d.executablePath)+'</code>':t('llama-server.exe не найден'))+'<br>'+text(d.version||'')+(d.cuda?'<br>'+text(d.cuda):'')}catch(e){out.textContent=t('Не удалось получить состояние llama.cpp')}}
 async function updateServerModelMonitor(){let box=document.getElementById('serverModelMonitor');if(!box)return;try{box.innerHTML=monitorHtml(await webJson('/api/monitoring?id='+encodeURIComponent(id())))}catch(e){box.innerHTML='<div class="muted">Мониторинг недоступен</div>'}}
 const oldBuild=window.buildServerTabs;if(oldBuild&&!window._desktopModelBuildWrapped){window._desktopModelBuildWrapped=true;window.buildServerTabs=function(p){let result=oldBuild(p);setTimeout(()=>renderServerModel(p),0);return result}}
 let style=document.createElement('style');style.textContent='.server-model-intro,.server-model-card{background:#101827;border:1px solid #26364f;border-radius:8px;padding:14px;margin:10px 0}.server-model-intro h3,.server-model-card h3{margin:0 0 8px;color:#dbeafe;font-size:15px}.server-model-card-title{display:flex;gap:10px;align-items:center}.server-model-subtitle,.server-executable-info{color:#94a3b8;font-size:12px;margin:6px 0 10px}.server-executable-info code{color:#93c5fd;word-break:break-all}.server-optional{background:#059669;color:#fff;border-radius:5px;padding:3px 7px;font-size:11px}.server-path-row{display:flex;gap:8px;align-items:center}.server-path-row input{flex:1;min-width:0}.server-path-name{color:#93c5fd;font-size:12px;margin-top:7px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.server-model-picker-row{display:flex;gap:8px;margin-top:10px}.server-model-picker-row select{flex:1}.server-model-advanced{background:#172236;border:1px solid #26364f;border-radius:8px;padding:12px;margin:10px 0}.server-model-advanced summary{cursor:pointer;color:#bfdbfe;font-weight:600}.server-model-advanced .server-formgrid{margin-top:12px}.server-model-card .server-monitor-section{margin-top:0}';document.head.appendChild(style);setTimeout(()=>{let p=window.profileCache;if(p)renderServerModel(p)},0);setInterval(()=>{if(document.getElementById('server')?.classList.contains('active')&&document.getElementById('serverModelMonitor'))updateServerModelMonitor()},2500)
})();
""";
		const string desktopModelTrimScript = """
(function(){
 function normalizeDesktopModelTab(){
  const body=document.querySelector('#serverBodies [data-server-body="model"]');
  if(!body)return;
  body.querySelectorAll('.server-model-picker-row,.server-model-advanced,#serverModelMonitor').forEach(x=>x.remove());
  const draft=document.getElementById('serverModel_draftModelPath');
  draft?.closest('.server-model-card')?.remove();
  body.querySelectorAll('.server-model-card').forEach(card=>{
   const title=card.querySelector('h3')?.textContent||'';
   const subtitle=card.querySelector('.server-model-subtitle');
   if(subtitle)subtitle.textContent=title.includes('mmproj')?t('Путь к mmproj:'):t('Путь к модели:');
  });
 }
 const oldRender=window.renderServerModel;if(oldRender&&!window._desktopModelNormalizeWrapped){window._desktopModelNormalizeWrapped=true;window.renderServerModel=function(p){let result=oldRender(p);setTimeout(normalizeDesktopModelTab,0);return result}}
 const oldBuild=window.buildServerTabs;if(oldBuild&&!window._desktopModelBuildNormalizeWrapped){window._desktopModelBuildNormalizeWrapped=true;window.buildServerTabs=function(p){let result=oldBuild(p);setTimeout(normalizeDesktopModelTab,0);return result}}
 setTimeout(normalizeDesktopModelTab,0)
})();
""";
		const string webConsistencyScript = """
(function(){
 let webFileBrowserState=null;
 const webDefaults={host:'127.0.0.1',port:'8080',timeout:'3600',slots:'-1',gpuLayers:'all',threads:'0',threadsBatch:'0',mainGpu:'0',cacheRam:'-1',cacheReuse:'32',contextSize:'0',batchSize:'2048',ubatchSize:'512',maxTokens:'-1',temperature:'0.8',topK:'40',topP:'0.95',minP:'0.05',typicalP:'1.0 (disabled)',repeatPenalty:'1.0',repeatLastN:'64',presencePenalty:'0',frequencyPenalty:'0',seed:'-1 (random)',mirostatTau:'5',mirostatEta:'0.1',specDraftGpuLayers:'auto',specDraftNMax:'3',specDraftNMin:'0',specDraftPSplit:'0.1',specDraftPMin:'0',reasoningBudget:'0',ropeFreqBase:'0',ropeFreqScale:'0',yarnOriginalContext:'0',yarnExtFactor:'-1',yarnAttnFactor:'-1',yarnBetaFast:'-1',yarnBetaSlow:'-1'};
 const webTips={temperature:t('Температура сэмплирования. По умолчанию 0.8; меньше — более детерминированный ответ.'),topP:t('Nucleus sampling: ограничивает выбор токенов накопленной вероятностью. 1.0 отключает ограничение.'),topK:t('Top-K sampling: число наиболее вероятных токенов. 0 отключает ограничение.'),minP:t('Min-P sampling: отсеивает маловероятные токены. 0 отключает ограничение.'),typicalP:t('Locally typical sampling. 1.0 отключает параметр.'),repeatPenalty:t('Штраф за повторяющиеся последовательности. 1.0 отключает штраф.'),repeatLastN:t('Количество последних токенов для штрафа повторов. -1 использует размер контекста.'),presencePenalty:t('Штраф за уже встречавшиеся токены. 0 отключает параметр.'),frequencyPenalty:t('Штраф за частые токены. 0 отключает параметр.'),seed:t('Seed генератора случайных чисел. -1 — случайное значение.'),mirostat:t('Mirostat: 0 — отключён, 1 — Mirostat, 2 — Mirostat 2.0. При включении заменяет Top-K, Top-P и Typical-P.'),mirostatTau:t('Целевая энтропия Mirostat (tau). По умолчанию 5.'),mirostatEta:t('Скорость обучения Mirostat (eta). По умолчанию 0.1.')};
 function webTip(key,input){let label=input.closest('label')?.querySelector('.label')?.textContent?.trim()||key;return webTips[key]||(t('Параметр llama-server: ')+label+t('. Значение сохраняется в текущем профиле.'))}
 function enhanceWebFieldHints(root=document){root.querySelectorAll('[data-server-key]').forEach(input=>{let key=input.dataset.serverKey,tip=webTip(key,input);if(!input.title)input.title=tip;if((input.tagName==='INPUT'||input.tagName==='TEXTAREA')&&input.type!=='password'&&!input.placeholder&&webDefaults[key]!=null)input.placeholder=webDefaults[key];let caption=input.closest('label')?.querySelector('.label');if(caption&&!caption.querySelector('.web-help')){let help=document.createElement('span');help.className='web-help';help.tabIndex=0;help.dataset.tooltip=tip;help.textContent='?';caption.appendChild(document.createTextNode(' '));caption.appendChild(help)}})}
 function enhanceSamplingLayout(){const body=document.querySelector('#serverBodies [data-server-body="sampling"]');if(!body||body.dataset.webSamplingReady==='1')return;const mainKeys=['temperature','topP','topK','minP','typicalP','repeatPenalty','repeatLastN','presencePenalty','frequencyPenalty','seed'],miroKeys=['mirostat','mirostatTau','mirostatEta'],field=key=>body.querySelector('[data-server-key="'+key+'"]')?.closest('.field'),main=mainKeys.map(field).filter(Boolean),miro=miroKeys.map(field).filter(Boolean);if(!main.length)return;body.querySelectorAll('[data-server-key="dryMultiplier"],[data-server-key="dryBase"],[data-server-key="dynatempStddev"],[data-server-key="xtcProbability"],[data-server-key="xtcThreshold"]').forEach(x=>x.closest('.field')?.remove());const layout=document.createElement('div');layout.className='server-sampling-layout';const primary=document.createElement('div');primary.className='server-sampling-card';primary.innerHTML='<h3>Раздел сэмплинга</h3><div class="server-sampling-fields"></div>';const secondary=document.createElement('div');secondary.className='server-sampling-card server-mirostat-card';secondary.innerHTML='<div class="server-sampling-fields"></div>';main.forEach(x=>{x.classList.add('sampling-field');primary.querySelector('.server-sampling-fields').appendChild(x)});miro.forEach(x=>{x.classList.add('sampling-field');secondary.querySelector('.server-sampling-fields').appendChild(x)});body.replaceChildren(layout);layout.append(primary,secondary);body.dataset.webSamplingReady='1';enhanceWebFieldHints(body)}
 function enhanceWebServerFields(){enhanceWebFieldHints();enhanceSamplingLayout()}
 function ensureWebFileBrowser(){
  if(document.getElementById('webFileBrowser'))return;
  const style=document.createElement('style');
  style.textContent='.web-file-modal{position:fixed;inset:0;z-index:10050;background:#020617bb;display:flex;align-items:center;justify-content:center;padding:20px}.web-file-dialog{width:min(920px,100%);max-height:86vh;display:flex;flex-direction:column;background:#1f2937;border:1px solid #475569;border-radius:10px;box-shadow:0 24px 70px #000;padding:18px}.web-file-head{display:flex;align-items:center;gap:10px;margin-bottom:12px}.web-file-head h2{flex:1;margin:0}.web-file-path{margin:0 0 10px}.web-file-list{overflow:auto;min-height:180px;border:1px solid #334155;border-radius:7px;background:#101827;padding:6px}.web-file-entry{display:flex;width:100%;text-align:left;background:transparent;color:#e5e7eb;border:0;border-radius:5px;padding:9px 10px}.web-file-entry:hover,.web-file-entry.selected{background:#263b5c}.web-file-entry .kind{width:32px;color:#93c5fd}.web-file-foot{display:flex;justify-content:flex-end;gap:8px;margin-top:12px;flex-wrap:wrap}';
  document.head.appendChild(style);
  document.body.insertAdjacentHTML('beforeend','<div id="webFileBrowser" class="web-file-modal" style="display:none"><div class="web-file-dialog"><div class="web-file-head"><h2 id="webFileBrowserTitle">Выбор папки</h2><button type="button" class="secondary" id="webFileBrowserRoots">Диски</button><button type="button" class="secondary" id="webFileBrowserUp">Вверх</button></div><input id="webFileBrowserPath" class="web-file-path" placeholder="Путь на компьютере со Studio"><div id="webFileBrowserStatus" class="notice"></div><div id="webFileBrowserList" class="web-file-list"></div><div class="web-file-foot"><button type="button" class="secondary" id="webFileBrowserCancel">Отмена</button><button type="button" class="green" id="webFileBrowserChoose">Выбрать папку</button></div></div></div>');
  document.getElementById('webFileBrowserRoots').onclick=()=>webFileBrowserLoad('');
  document.getElementById('webFileBrowserUp').onclick=()=>webFileBrowserLoad(webFileBrowserState?.parent||'');
  document.getElementById('webFileBrowserCancel').onclick=webFileBrowserClose;
  document.getElementById('webFileBrowserChoose').onclick=webFileBrowserChoose;
  document.getElementById('webFileBrowserPath').onkeydown=e=>{if(e.key==='Enter')webFileBrowserLoad(e.target.value)};
 }
 function webFileBrowserClose(){const modal=document.getElementById('webFileBrowser');if(modal)modal.style.display='none';webFileBrowserState=null}
 async function webFileBrowserOpen(options){
  ensureWebFileBrowser();
  webFileBrowserState={...options,path:'',parent:'',selected:''};
  document.getElementById('webFileBrowserTitle').textContent=options.title;
  document.getElementById('webFileBrowserChoose').textContent=options.files?t('Выбрать файл'):t('Выбрать папку');
  document.getElementById('webFileBrowser').style.display='flex';
  await webFileBrowserLoad(options.path||'');
 }
 async function webFileBrowserLoad(path){
  const state=webFileBrowserState;if(!state)return;
  const list=document.getElementById('webFileBrowserList'),status=document.getElementById('webFileBrowserStatus'),choose=document.getElementById('webFileBrowserChoose');
  list.textContent=t('Загрузка…');status.textContent='';choose.disabled=!!state.files&&!state.selected;
  try{
   const data=await json('/api/fs/list?path='+encodeURIComponent(path||'')+'&files='+String(!!state.files)+'&filter='+encodeURIComponent(state.filter||''));
   state.path=data.path||'';state.parent=data.parent||'';state.entries=Array.isArray(data.entries)?data.entries:[];
   document.getElementById('webFileBrowserPath').value=state.path;
   document.getElementById('webFileBrowserUp').disabled=!state.parent;
   list.innerHTML='';
   if(!state.entries.length){list.textContent=t('Папка пуста.');return;}
   state.entries.forEach((entry,index)=>{const button=document.createElement('button');button.type='button';button.className='web-file-entry';button.innerHTML='<span class="kind">'+(entry.isDirectory?'📁':'📄')+'</span><span>'+esc(entry.name)+'</span>';button.onclick=()=>{if(entry.isDirectory){state.selected='';webFileBrowserLoad(entry.path)}else{state.selected=entry.path;list.querySelectorAll('.web-file-entry').forEach(x=>x.classList.remove('selected'));button.classList.add('selected');status.textContent=entry.name;choose.disabled=false}};list.appendChild(button)});
  }catch(error){list.textContent=t('Не удалось открыть путь.');status.textContent=t('Ошибка: ')+error.message;status.className='notice bad'}
 }
 async function webFileBrowserChoose(){
  const state=webFileBrowserState;if(!state)return;
  const value=state.files?state.selected:state.path;
  if(!value)return;
  const callback=state.onChoose;webFileBrowserClose();
  await callback(value);
 }
 function openWebFolderBrowser(title,path,onChoose){return webFileBrowserOpen({title,path,files:false,onChoose})}
 function openWebGgufBrowser(title,path,onChoose){return webFileBrowserOpen({title,path: String(path||'').replace(/[\\/][^\\/]*$/, ''),files:true,filter:'.gguf',onChoose})}
 window.browseLlamaDirectory=function(){const input=document.getElementById('llamaBaseDirectory');return openWebFolderBrowser(t('Папка с серверами llama.cpp'),input?.value||'',async path=>{if(input)input.value=path;await setLlamaDirectory(path)})};
 window.browseModelsDirectory=function(targetId){const input=document.getElementById(targetId),status=document.getElementById('hfStatus');return openWebFolderBrowser(t('Каталог моделей'),input?.value||'',async path=>{for(const id of ['hfDir','modelsDir']){const item=document.getElementById(id);if(item)item.value=path}try{const response=await fetch('/api/models/directory?path='+encodeURIComponent(path));if(status)status.textContent=response.ok?t('Каталог моделей сохранён'):t('Не удалось сохранить каталог');if(response.ok)await scanModelsWeb()}catch(error){if(status)status.textContent=t('Ошибка сохранения каталога: ')+error.message}})};
 window.browseServerModel=function(kind){const key=kind==='main'?'modelPath':kind==='mmproj'?'mmprojPath':'draftModelPath',input=document.querySelector('[data-server-key="'+key+'"]');return openWebGgufBrowser(kind==='main'?t('Основная GGUF-модель'):kind==='mmproj'?t('Файл mmproj'):t('Draft-модель'),input?.value||'',async path=>{if(input)input.value=path;await saveModelValue(key,path)})};
 function normalizeHomeVersion(value){
  if(value&&typeof value==='object'){
   return {key:String(value.versionKey||value.key||value.folderName||''),label:String(value.display||value.versionKey||value.folderName||'')};
  }
  const raw=String(value||'');
  return {key:raw.replace(/-cuda12x$/i,'|Cuda12x').replace(/-cuda13x$/i,'|Cuda13x'),label:raw};
 }
 async function fixedHomeReleases(){
  try{
   const data=await json('/api/llama/installed');
   const items=(Array.isArray(data.versions)?data.versions:[]).map(normalizeHomeVersion).filter(x=>x.key);
   const select=document.getElementById('homeReleaseSelect');
   const current=window.profileCache?.selectedServerVersion?String(window.profileCache.selectedServerVersion):String(data.active||'');
   if(select){select.innerHTML=items.map(x=>'<option value="'+esc(x.key)+'">'+esc(x.label)+'</option>').join('');if(current&&items.some(x=>x.key===current))select.value=current;}
   const info=document.getElementById('homeReleaseInfo'),button=document.getElementById('homeReleaseUpdate');
   try{const releases=await json('/api/llama/releases'),latest=releases[0],tag=latest?.tag?String(latest.tag):'';if(tag&&tag!==current.split('|')[0]){if(info)info.textContent=t('↑ Доступно обновление, ')+tag;if(button)button.textContent=t('Обновить (')+tag+')'}else{if(info)info.textContent=items.length?items.length+t(' установлено'):t('Установленные сборки не найдены');if(button)button.textContent=t('Проверить обновления')}}catch{if(info)info.textContent=items.length?items.length+t(' установлено'):t('Установленные сборки не найдены')}
  }catch{const info=document.getElementById('homeReleaseInfo');if(info)info.textContent=t('Не удалось прочитать установленные сборки')}
 }
 window.loadHomeReleases=fixedHomeReleases;
 async function hfSaveDir(){
  const dir=document.getElementById('hfDir')?.value.trim(),status=document.getElementById('hfStatus');
  if(!dir){if(status)status.textContent=t('Укажите каталог моделей');return;}
  try{const response=await fetch('/api/models/directory?path='+encodeURIComponent(dir));if(status)status.textContent=response.ok?t('Каталог моделей сохранён'):t('Каталог не найден');if(response.ok){const modelsDir=document.getElementById('modelsDir');if(modelsDir)modelsDir.value=dir;await scanModelsWeb()}}catch(error){if(status)status.textContent=t('Ошибка сохранения каталога: ')+error.message}
 }
 window.hfSaveDir=hfSaveDir;
 async function hfLoginWeb(){const status=document.getElementById('hfStatus');if(status)status.textContent=t('Открывается вход HuggingFace…');try{const response=await fetch('/api/hf/login'),data=await response.json().catch(()=>({}));if(status)status.textContent=response.ok&&data.ok?t('Вход HuggingFace запущен в браузере'):t('Не удалось открыть вход HuggingFace')}catch(error){if(status)status.textContent=t('Ошибка входа HuggingFace: ')+error.message}}
 window.hfLoginWeb=hfLoginWeb;
 function ensureHfLoginButton(){const token=document.getElementById('hfToken');if(!token||document.getElementById('hfLoginButton'))return;const button=document.createElement('button');button.id='hfLoginButton';button.textContent=t('Войти на HF');button.onclick=hfLoginWeb;token.parentElement?.insertBefore(button,token.nextElementSibling)}
 function gpuField(body,key){return body.querySelector('[data-server-key="'+key+'"]').closest('label')}
 function splitValue(value){let n=Number(String(value||'').split(',')[0]);return Number.isFinite(n)?Math.max(1,Math.min(99,n)):50}
 function setGpuSelection(host){const selected=Array.from(host.querySelectorAll('[data-gpu-index]:checked')).map(x=>x.dataset.gpuIndex);const hidden=host.querySelector('[data-server-key="selectedGpuIndices"]');if(hidden)hidden.value=selected.join(',');return selected}
 function renderWebMultiGpu(body,host,list,selected){
  const profile=window.profileCache||{};
  const mainInput=body.querySelector('[data-server-key="mainGpu"]');
  const tensorInput=body.querySelector('[data-server-key="tensorSplit"]');
  const chosen=list.filter(g=>selected.includes(String(g.index??'')));
  const splitActive=chosen.length>1&&String(tensorInput?.value??profile.tensorSplit??'').trim()!=='';
  const local=chosen.filter(g=>!g.isRemote);
  const display=g=>safe(g.displayName||g.name||('GPU '+g.index));
  let extra='<div class="label">Основной GPU'+(splitActive?t(' (не используется при tensor-split)'):'')+'</div>';
  extra+=local.map(g=>'<label class="gpu-choice"><input type="radio" name="web-main-gpu" value="'+safe(g.index)+'" '+(Number(mainInput?.value)===Number(g.index)?'checked':'')+' '+(splitActive?'disabled':'')+' onchange="webMainGpuChanged(this.value)"><span>'+display(g)+'</span></label>').join('')||'<div class="muted">Для основной GPU нужна выбранная локальная карта.</div>';
  if(chosen.length===2){const first=splitValue(tensorInput?.value||profile.tensorSplit),second=100-first;extra+='<div class="web-gpu-distribution"><span class="label">Распределение модели</span><input id="webGpuSplitSlider" type="range" min="1" max="99" value="'+first+'" oninput="webGpuSplitChanged(this.value)"><div class="web-gpu-ruler"><span>0%</span><span>10%</span><span>20%</span><span>30%</span><span>40%</span><span>50%</span><span>60%</span><span>70%</span><span>80%</span><span>90%</span><span>100%</span></div><div class="web-gpu-adjust"><button type="button" class="secondary" onclick="webGpuSplitDelta(-5)">−5%</button><button type="button" class="secondary" onclick="webGpuSplitDelta(-1)">−1%</button><button type="button" class="secondary" onclick="webGpuSplitDelta(1)">+1%</button><button type="button" class="secondary" onclick="webGpuSplitDelta(5)">+5%</button></div><div id="webGpuSplitText" class="web-gpu-split-text">'+display(chosen[0])+': '+first+'%   '+display(chosen[1])+': '+second+'%</div></div>'}
  extra+='<button type="button" class="secondary web-gpu-auto" onclick="webGpuAutoSplit()">Автоматическое распределение</button>';
  host.querySelector('.web-gpu-extra').innerHTML=extra;
 }
 window.webMainGpuChanged=function(value){const input=document.querySelector('#serverBodies [data-server-key="mainGpu"]');if(input)input.value=value};
 window.webGpuSplitChanged=function(value){const host=document.getElementById('serverGpuChooser'),input=document.querySelector('#serverBodies [data-server-key="tensorSplit"]');if(!host||!input)return;const n=Math.max(1,Math.min(99,Number(value)||50));input.value=n+','+(100-n);const list=window._webGpuList||[],chosen=list.filter(g=>Array.from(host.querySelectorAll('[data-gpu-index]:checked')).map(x=>x.dataset.gpuIndex).includes(String(g.index??'')));const text=document.getElementById('webGpuSplitText');if(text&&chosen.length===2)text.textContent=(chosen[0].displayName||chosen[0].name)+': '+n+'%   '+(chosen[1].displayName||chosen[1].name)+': '+(100-n)+'%'};
 window.webGpuSplitDelta=function(delta){const slider=document.getElementById('webGpuSplitSlider');if(!slider)return;slider.value=Math.max(1,Math.min(99,Number(slider.value)+delta));window.webGpuSplitChanged(slider.value)};
 window.webGpuAutoSplit=function(){const host=document.getElementById('serverGpuChooser'),list=window._webGpuList||[];if(!host)return;const selected=Array.from(host.querySelectorAll('[data-gpu-index]:checked')).map(x=>x.dataset.gpuIndex),chosen=list.filter(g=>selected.includes(String(g.index??'')));if(chosen.length===2){const a=Number(chosen[0].memoryTotalGb)||0,b=Number(chosen[1].memoryTotalGb)||0;const n=a+b?Math.round(a/(a+b)*100):50;const slider=document.getElementById('webGpuSplitSlider');if(slider)slider.value=Math.max(1,Math.min(99,n));window.webGpuSplitChanged(n)}};
 window.webGpuChoiceChanged=function(){const host=document.getElementById('serverGpuChooser'),body=document.querySelector('#serverBodies [data-server-body="gpu"]');if(!host||!body)return;const selected=setGpuSelection(host);renderWebMultiGpu(body,host,window._webGpuList||[],selected)};
 async function normalizeServerGpuTab(){
  const body=document.querySelector('#serverBodies [data-server-body="gpu"]');
  if(!body)return;
  body.querySelectorAll('[data-server-key="gpuSplitMode"],[data-server-key="mmqEnabled"]').forEach(x=>x.closest('label')?.remove());
  const main=gpuField(body,'mainGpu'),tensor=gpuField(body,'tensorSplit'),host=document.getElementById('serverGpuChooser');
  if(!host)return;
  try{
   let raw,list;
   try{raw=await json('/api/gpu');list=Array.isArray(raw)?raw:(raw.value||[])}catch{const monitoring=await json('/api/monitoring?id='+encodeURIComponent(currentId||''));list=Array.isArray(monitoring.gpus)?monitoring.gpus:[]}
   if(!list.length){const monitoring=await json('/api/monitoring?id='+encodeURIComponent(currentId||''));list=Array.isArray(monitoring.gpus)?monitoring.gpus:[]}
   window._webGpuList=list;
   if(list.length<=1){main&&(main.style.display='');tensor&&(tensor.style.display='');host.remove();body.querySelector('.server-gpu-section .diag')?.remove();return;}
   main&&(main.style.display='none');tensor&&(tensor.style.display='none');
   const profile=window.profileCache||{},profileSelectedKey=Object.keys(profile).find(key=>key.toLowerCase()==='selectedgpuindices'),saved=String(profileSelectedKey?profile[profileSelectedKey]:'').split(',').map(x=>x.trim()).filter(Boolean),selected=saved.length?saved:list.filter(g=>g.isSelected).map(g=>String(g.index??''));
   host.innerHTML='<div class="label">Используемые GPU</div><div class="gpu-choice-grid">'+list.map(g=>{const id=String(g.index??'');return '<label class="gpu-choice"><input type="checkbox" data-gpu-index="'+safe(id)+'" '+(selected.includes(id)?'checked':'')+' onchange="webGpuChoiceChanged()"><span>'+safe(g.displayName||g.name||('GPU '+id))+'</span></label>'}).join('')+'</div><input type="hidden" data-server-key="selectedGpuIndices" value="'+safe(selected.join(','))+'"><div class="web-gpu-extra"></div>';
   renderWebMultiGpu(body,host,list,selected);
  }catch{host.innerHTML='<div class="muted">Не удалось получить список GPU</div>'}
 }
 window.normalizeServerGpuTab=normalizeServerGpuTab;
 function normalizeMtpDraftModel(){
  const body=document.querySelector('#serverBodies [data-server-body="mtp"]');
  if(!body||body.dataset.webMtpDraftReady==='1')return;
  const form=body.querySelector('.server-formgrid');
  const draftInput=body.querySelector('[data-server-key="draftModelPath"]');
  const enabled=body.querySelector('[data-server-key="speculativeDecoding"]');
  const draftLocalOnly=body.querySelector('[data-server-key="draftModelLocalOnly"]');
	if(!form||!draftInput||!enabled)return;
  const enabledField=enabled.closest('.field')||enabled.closest('label');
  const draftField=draftInput.closest('.field')||draftInput.closest('label');
  const card=document.createElement('section');
  card.className='web-mtp-draft-card';
  card.innerHTML='<div class="web-mtp-title"><h3>Черновая модель (спекулятивное декодирование) (-md)</h3><span>Продвинуто</span></div><div class="web-mtp-enable"></div><div class="web-mtp-path"><span class="label">Путь к draft</span><div class="web-mtp-path-row"></div><div class="server-path-name" id="webMtpDraftName"></div></div>';
  if(enabledField)card.querySelector('.web-mtp-enable').appendChild(enabledField);
  draftInput.remove();
  draftField?.remove();
  const row=card.querySelector('.web-mtp-path-row');
  draftInput.id='webMtpDraftPath';
  draftInput.placeholder=t('Путь к черновой модели .gguf');
  row.appendChild(draftInput);
  const browse=document.createElement('button');
  browse.type='button';browse.className='secondary';browse.textContent=t('Обзор…');
  browse.onclick=()=>window.browseMtpDraftModel();
  row.appendChild(browse);
  const name=document.getElementById('webMtpDraftName');
  const updateName=()=>{if(name)name.textContent=draftInput.value?fileName(draftInput.value):t('Черновая модель не выбрана')};
  draftInput.addEventListener('input',updateName);updateName();
  form.querySelectorAll('[data-server-key="reasoning"],[data-server-key="reasoningBudget"],[data-key="Reasoning"],[data-key="ReasoningBudget"]').forEach(input=>(input.closest('.field')||input.closest('label'))?.remove());
  form.querySelectorAll('[data-key="Jinja"]').forEach(input=>(input.closest('.field')||input.closest('label'))?.remove());
  form.querySelectorAll('.web-mtp-quick').forEach(node=>node.remove());
  const jinja=document.createElement('label');jinja.className='server-check web-mtp-jinja';const jinjaEnabled=(window.profileCache?.jinja??window.profileCache?.Jinja??true);jinja.innerHTML='<input data-server-key="jinja" type="checkbox"'+(jinjaEnabled?' checked':'')+'><span>Jinja / Chat Template (--jinja)</span>';card.appendChild(jinja);
  const draftLocalField=draftLocalOnly?.closest('.field')||draftLocalOnly?.closest('label');
  const specTypeInput=body.querySelector('[data-server-key="specType"]');
  const specTypeField=specTypeInput?.closest('.field')||specTypeInput?.closest('label');
  if(draftLocalField){form.insertBefore(draftLocalField,specTypeField?.nextSibling||form.firstChild);const updateDraftPlacement=()=>{const type=String(specTypeInput?.value||'').trim().toLowerCase();draftLocalField.hidden=type!=='draft'&&type!=='draft-simple'};specTypeInput?.addEventListener('change',updateDraftPlacement);updateDraftPlacement()}
  form.parentElement.insertBefore(card,form);
  body.dataset.webMtpDraftReady='1';
  enhanceWebFieldHints(card);
 }
 window.browseMtpDraftModel=function(){
  const input=document.getElementById('webMtpDraftPath');
  return openWebGgufBrowser(t('Черновая GGUF-модель'),input?.value||'',async path=>{
   if(input)input.value=path;
   const name=document.getElementById('webMtpDraftName');if(name)name.textContent=fileName(path);
   await saveModelValue('draftModelPath',path);
  });
 };
 const oldBuild=window.buildServerTabs;if(oldBuild&&!window._webConsistencyBuildWrapped){window._webConsistencyBuildWrapped=true;window.buildServerTabs=function(p){const result=oldBuild(p);setTimeout(()=>{enhanceWebServerFields();normalizeServerGpuTab();normalizeMtpDraftModel();window.normalizeDesktopModelTab?.()},250);return result}}
 const layoutStyle=document.createElement('style');layoutStyle.textContent='#server:not(:has([data-server-body="gpu"].active)) #serverGpuMonitor{display:none!important}';document.head.appendChild(layoutStyle);
 const gpuConsistencyStyle=document.createElement('style');gpuConsistencyStyle.textContent='.web-gpu-extra{display:grid;gap:8px;margin-top:10px}.web-gpu-distribution{margin-top:6px}.web-gpu-distribution input[type=range]{width:100%;accent-color:#60a5fa}.web-gpu-ruler{display:flex;justify-content:space-between;color:#a8afbf;font-size:10px}.web-gpu-adjust{display:flex;justify-content:center;gap:6px;margin:7px 0}.web-gpu-adjust button{padding:4px 9px;font-size:12px}.web-gpu-split-text{font-size:13px;color:#dbeafe}.web-gpu-auto{justify-self:start}.server-sampling-layout{display:grid;gap:12px}.server-sampling-card{background:#101827;border:1px solid #334155;border-radius:7px;padding:14px}.server-sampling-card h3{margin:0 0 12px;font-size:15px;color:#dbeafe}.server-sampling-fields{display:grid;gap:8px}.sampling-field{display:grid;grid-template-columns:245px minmax(0,1fr);align-items:center;gap:10px;min-width:0}.sampling-field .label{margin:0;color:#93a4bf}.sampling-field input,.sampling-field select{grid-column:2}.web-help{position:relative;display:inline-flex;align-items:center;justify-content:center;width:16px;height:16px;border-radius:50%;background:#334155;color:#bfdbfe;font-size:11px;font-weight:700;cursor:help;vertical-align:middle}.web-help:hover:after,.web-help:focus:after{content:attr(data-tooltip);position:absolute;z-index:10060;left:22px;top:-7px;width:min(360px,70vw);padding:9px 10px;border:1px solid #64748b;border-radius:6px;background:#0b1220;color:#e5e7eb;font-weight:400;line-height:1.35;box-shadow:0 8px 24px #000;text-align:left}.web-help:focus{outline:2px solid #60a5fa}@media(max-width:760px){.sampling-field{grid-template-columns:1fr}.sampling-field input,.sampling-field select{grid-column:1}}';document.head.appendChild(gpuConsistencyStyle);
 const desktopFormLayout=document.createElement('style');desktopFormLayout.textContent='#serverBodies .server-formgrid{grid-template-columns:1fr;gap:9px;background:#101827;border:1px solid #334155;border-radius:7px;padding:14px}#serverBodies .server-formgrid>.field{display:grid;grid-template-columns:265px minmax(0,1fr);align-items:center;gap:12px;min-width:0}#serverBodies .server-formgrid>.field>.label{margin:0;color:#93a4bf}#serverBodies .server-formgrid>.field>input,#serverBodies .server-formgrid>.field>select,#serverBodies .server-formgrid>.field>textarea{grid-column:2}#serverBodies .server-formgrid>.field>textarea{min-height:120px}#serverBodies .server-formgrid .server-check{display:flex;align-items:center;gap:9px;min-height:38px;padding:6px 0;color:#dbeafe}#serverBodies .server-formgrid .server-check input[type=checkbox]{appearance:none;width:32px;height:18px;margin:0;border:1px solid #64748b;border-radius:999px;background:#334155;position:relative;cursor:pointer;transition:.16s}#serverBodies .server-formgrid .server-check input[type=checkbox]::after{content:"";position:absolute;top:2px;left:2px;width:12px;height:12px;border-radius:50%;background:#f8fafc;transition:.16s}#serverBodies .server-formgrid .server-check input[type=checkbox]:checked{background:#4f8bc4;border-color:#60a5fa}#serverBodies .server-formgrid .server-check input[type=checkbox]:checked::after{transform:translateX(14px)}#serverBodies .server-formgrid .server-check span::after{content:" Off";color:#9ca3af;font-size:12px;margin-left:5px}#serverBodies .server-formgrid .server-check input:checked+span::after{content:" On";color:#bfdbfe}@media(max-width:760px){#serverBodies .server-formgrid>.field{grid-template-columns:1fr}#serverBodies .server-formgrid>.field>input,#serverBodies .server-formgrid>.field>select,#serverBodies .server-formgrid>.field>textarea{grid-column:1}}';document.head.appendChild(desktopFormLayout);
 const mtpDraftStyle=document.createElement('style');mtpDraftStyle.textContent='.web-mtp-draft-card{margin:0 0 12px;padding:14px;background:#101827;border:1px solid #5b4ae6;border-radius:7px}.web-mtp-title{display:flex;align-items:center;gap:8px;margin-bottom:10px}.web-mtp-title h3{margin:0;font-size:15px;color:#dbeafe}.web-mtp-title span{background:#7c3aed;color:#fff;border-radius:4px;padding:3px 6px;font-size:10px}.web-mtp-enable{margin:0 0 10px}.web-mtp-enable .field,.web-mtp-enable .server-check{display:flex;align-items:center;gap:9px;color:#dbeafe}.web-mtp-enable .label{margin:0}.web-mtp-enable .server-check input[type=checkbox]{appearance:none;width:32px;height:18px;margin:0;border:1px solid #64748b;border-radius:999px;background:#334155;position:relative;cursor:pointer}.web-mtp-enable .server-check input[type=checkbox]::after{content:"";position:absolute;top:2px;left:2px;width:12px;height:12px;border-radius:50%;background:#f8fafc}.web-mtp-enable .server-check input[type=checkbox]:checked{background:#4f8bc4;border-color:#60a5fa}.web-mtp-enable .server-check input[type=checkbox]:checked::after{transform:translateX(14px)}.web-mtp-enable .server-check span::after{content:" Off";color:#9ca3af;font-size:12px;margin-left:5px}.web-mtp-enable .server-check input:checked+span::after{content:" On";color:#bfdbfe}.web-mtp-path{display:grid;grid-template-columns:265px minmax(0,1fr);align-items:center;gap:8px 12px}.web-mtp-path .label{margin:0;color:#93a4bf}.web-mtp-path-row{grid-column:2;display:flex;gap:8px}.web-mtp-path-row input{min-width:0;flex:1}.web-mtp-path-row button{white-space:nowrap}.web-mtp-path .server-path-name{grid-column:2;color:#93c5fd;font-size:12px}@media(max-width:760px){.web-mtp-path{grid-template-columns:1fr}.web-mtp-path-row,.web-mtp-path .server-path-name{grid-column:1}}';document.head.appendChild(mtpDraftStyle);
 setTimeout(()=>{enhanceWebServerFields();normalizeServerGpuTab();window.normalizeDesktopModelTab?.();ensureHfLoginButton()},350);
 setInterval(ensureHfLoginButton,1000);
})();
""";
		const string oldReleaseMapping = "let items=versions.map(v=>{let key=String(v).replace(/-cuda12x$/i,'|Cuda12x').replace(/-cuda13x$/i,'|Cuda13x');return {key:key,label:v}});";
		const string fixedReleaseMapping = "let items=versions.map(v=>{if(v&&typeof v==='object'){return {key:String(v.versionKey||v.key||''),label:String(v.display||v.versionKey||v.key||'')}}let raw=String(v||'');return {key:raw.replace(/-cuda12x$/i,'|Cuda12x').replace(/-cuda13x$/i,'|Cuda13x'),label:raw}});";
		const string serverCommandScript = """
(function(){
 function ensureServerCommand(){let page=document.getElementById('server'),anchor=page?.querySelectorAll('section')[1];if(page&&anchor&&!document.getElementById('serverCommandSection'))anchor.insertAdjacentHTML('afterend','<section id="serverCommandSection"><div class="row"><h2 style="flex:1">Команда запуска</h2><button class="secondary" onclick="showServerCommand()">Показать команду</button></div><pre id="serverCommand" class="command-preview">Команда ещё не загружена</pre></section>')}
 window.showServerCommand=async function(){let box=document.getElementById('serverCommand');if(!box||!currentId)return;box.textContent=t('Получение команды…');try{let d=await json('/api/profile/command?id='+encodeURIComponent(currentId));box.textContent=d.command||d.args||t('Команда не сформирована')}catch(e){box.textContent=t('Ошибка: ')+e.message}};
 let style=document.createElement('style');style.textContent='.gpu-choice-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:8px;margin:8px 0 12px}.gpu-choice{border:1px solid #334155;border-radius:6px;background:#172236;padding:10px}.command-preview{white-space:pre-wrap;word-break:break-word;background:#101827;border:1px solid #374151;border-radius:6px;padding:12px;min-height:48px;color:#bfdbfe;font:12px ui-monospace,Consolas,monospace}';document.head.appendChild(style);setTimeout(ensureServerCommand,0);
})();
""";
		string desktopServerScriptFinal = desktopServerScript
			.Replace("currentId", "getId()")
			.Replace("profileCache?.name", "'llama-server'")
			.Replace("function renderServerGpu(){", "function renderServerGpu(p){")
			.Replace("let p=window.profileCache||{};", "p=p||{};")
			.Replace("renderServerGpu();", "renderServerGpu(p);")
			.Replace("</section>')}showServerCommand();", "</section>');showServerCommand();");
		const string rpcServerIntegrationScript = """
(function () {
    // Remote RPC is a Server tab in the desktop application, not an extra page in the left navigation.
    function hideLegacyRpcPage() {
        const nav = document.querySelector('.nav[data-page="rpc"]');
        if (nav) nav.remove();
        const page = document.getElementById('rpc');
        if (page) page.style.display = 'none';
        try {
            if (localStorage.getItem('llamaStudio.web.page') === 'rpc')
                localStorage.setItem('llamaStudio.web.page', 'server');
        } catch (_) { }
    }

    function setRpcStatus(text, ok) {
        const status = document.getElementById('serverRpcStatus');
        const message = document.getElementById('serverRpcMessage');
        if (status) {
            status.textContent = text;
            status.className = ok === true ? 'is-running' : ok === false ? 'is-stopped' : 'muted';
        }
        if (message) message.textContent = text;
    }

    async function saveRpcProfile() {
        if (!window.serverValues) return true;
        const result = await fetch('/api/profile/save?' + window.serverValues());
        if (!result.ok) {
            setRpcStatus(await result.text(), false);
            return false;
        }
        return true;
    }

    async function rpcAction(path, pending) {
        setRpcStatus(pending, null);
        try {
            if (!await saveRpcProfile()) return;
            const response = await fetch(path);
            const payload = await response.json().catch(() => ({}));
            const text = payload.status || (payload.ok ? t('Готово') : t('Отключено'));
            setRpcStatus(text, response.ok && payload.ok !== false);
        } catch (error) {
            setRpcStatus(t('Ошибка: ') + error.message, false);
        }
    }

    window.checkEmbeddedRpcSsh = () => rpcAction('/api/rpc/check', t('Подключение SSH…'));
    window.startEmbeddedRpc = () => rpcAction('/api/rpc/start', t('Запуск удалённого RPC-сервера…'));
    window.stopEmbeddedRpc = () => rpcAction('/api/rpc/stop', t('Остановка удалённого RPC-сервера…'));

    window.ensureEmbeddedRpcControls = function () {
        const body = document.querySelector('[data-server-body="rpc"]');
        if (!body || document.getElementById('serverRpcActions')) return;
        const panel = document.createElement('section');
        panel.id = 'serverRpcActions';
        panel.className = 'server-rpc-panel';
        panel.innerHTML = '<h3>Подключение SSH и управление RPC</h3>' +
            '<p class="muted">Сначала сохраните параметры профиля, затем проверьте SSH. SSH остаётся доступным независимо от состояния ggml-rpc-server.</p>' +
            '<div class="actions server-rpc-buttons">' +
            '<button class="secondary" onclick="checkEmbeddedRpcSsh()">Подключить SSH</button>' +
            '<button class="green" onclick="startEmbeddedRpc()">Запустить удалённый сервер</button>' +
            '<button class="stop" onclick="stopEmbeddedRpc()">Остановить удалённый сервер</button></div>' +
            '<div id="serverRpcStatus" class="muted">Подключение ещё не проверялось.</div>' +
            '<div id="serverRpcMessage" class="server-message"></div>';
        body.appendChild(panel);
    };

    const previousBuild = window.buildServerTabs;
    if (previousBuild && !window._rpcServerTabWrapped) {
        window._rpcServerTabWrapped = true;
        window.buildServerTabs = function (profile) {
            const result = previousBuild(profile);
            setTimeout(window.ensureEmbeddedRpcControls, 0);
            return result;
        };
    }

    const style = document.createElement('style');
    style.textContent = '.server-rpc-panel{margin-top:14px;padding:14px;border:1px solid #334155;border-radius:8px;background:#101827}.server-rpc-panel h3{margin:0 0 8px;color:#dbeafe}.server-rpc-buttons{margin:12px 0}.server-rpc-panel #serverRpcStatus{display:block;min-height:20px;font-weight:600}';
    document.head.appendChild(style);
    hideLegacyRpcPage();
    setTimeout(() => {
        hideLegacyRpcPage();
        if (document.getElementById('rpc')?.classList.contains('active'))
            document.querySelector('.nav[data-page="server"]')?.click();
        window.ensureEmbeddedRpcControls();
    }, 0);
})();
""";
		const string desktopLayoutParityScript = """
(function () {
    const find = (body, key) => body.querySelector('[data-server-key="' + key + '"]')?.closest('label');
    const move = (host, body, key) => { const item = find(body, key); if (item) host.appendChild(item); };
    const card = title => { const item = document.createElement('section'); item.className = 'desktop-parity-card'; item.innerHTML = '<h3>' + title + '</h3>'; return item; };
    const toggle = (key, label, value) => '<label class="server-check desktop-option"><input data-server-key="' + key + '" type="checkbox" ' + (value ? 'checked' : '') + '><span>' + label + '</span></label>';

    function arrangeServerTab() {
        const body = document.querySelector('[data-server-body="server"]');
        if (!body) return;
        const profile = profileCache || {};
        const profileValue = key => { const hit = Object.keys(profile).find(name => name.toLowerCase() === key.toLowerCase()); return hit ? profile[hit] : undefined; };
        // The 4 toggles created by arrange (not part of serverSchema) must be
        // re-synced whenever the profile (re)loads, because arrange can run
        // before profileForm() has populated profileCache.
        const dynamicKeys = ['cachePrompt', 'contBatching', 'contextShift', 'reasoning'];
        if (body.dataset.desktopParity === '1') {
            dynamicKeys.forEach(key => { const el = body.querySelector('[data-server-key="' + key + '"]'); if (el) el.checked = !!profileValue(key); });
            return;
        }
        if (!body.querySelector('.server-formgrid')) return;
        const connection = card(t('Настройки подключения'));
        const connectionFields = document.createElement('div'); connectionFields.className = 'desktop-parity-fields';
        ['host', 'port', 'timeout', 'slots'].forEach(key => move(connectionFields, body, key));
        connection.appendChild(connectionFields);
        connection.insertAdjacentHTML('beforeend', '<div class="desktop-launch-actions"><button class="green" onclick="startServer()">Запустить</button><button class="stop" onclick="stopServer()">Остановить</button></div>');
        const options = card(t('Дополнительные опции'));
        const optionGrid = document.createElement('div'); optionGrid.className = 'desktop-options-grid';
        ['numa', 'cachePrompt', 'contBatching', 'contextShift', 'verboseLogging', 'enableSlots', 'enableWebUI', 'enableMetrics', 'reasoning', 'embeddingMode', 'priorityHigh'].forEach(key => {
            const item = find(body, key);
            if (item) optionGrid.appendChild(item);
            else {
                const names = { numa: 'NUMA (--numa)', cachePrompt: t('Кэш промпта (--cache-prompt)'), contBatching: t('Непрерывный батч (-cb)'), contextShift: t('Сдвиг контекста (--context-shift)'), reasoning: t('Рассуждение (-rea)') };
                if (names[key]) optionGrid.insertAdjacentHTML('beforeend', toggle(key, names[key], !!profileValue(key)));
            }
        });
        options.appendChild(optionGrid);
        body.replaceChildren(connection, options);
        body.dataset.desktopParity = '1';
    }

    function arrangeRpcTab() {
        const body = document.querySelector('[data-server-body="rpc"]');
        if (!body || body.dataset.desktopParity === '1') return;
		window.ensureEmbeddedRpcControls?.();
        const endpoint = body.querySelector('[data-server-key="rpcEndpoint"]');
        const value = endpoint?.value || '', split = value.lastIndexOf(':');
        const remoteHost = split > 0 ? value.slice(0, split) : value;
        const remotePort = split > 0 ? value.slice(split + 1) : '50052';
        const ssh = card(t('Подключение SSH'));
        ssh.insertAdjacentHTML('beforeend', '<p class="muted">На удалённом компьютере должна быть включена служба OpenSSH Server.</p>');
        const sshFields = document.createElement('div'); sshFields.className = 'desktop-rpc-grid';
        sshFields.innerHTML = '<label class="field"><span class="label">Удалённый компьютер</span><input id="rpcRemoteComputer"></label><label class="field"><span class="label">Порт RPC</span><input id="rpcRemotePort" type="number"></label>';
        sshFields.querySelector('#rpcRemoteComputer').value = remoteHost;
        sshFields.querySelector('#rpcRemotePort').value = remotePort;
        ['rpcSshUsername', 'rpcSshPassword', 'rpcSavePassword'].forEach(key => move(sshFields, body, key));
        ssh.appendChild(sshFields);
        ssh.insertAdjacentHTML('beforeend', '<div class="desktop-launch-actions"><button class="secondary" onclick="checkEmbeddedRpcSsh()">Подключить SSH</button></div>');
        const manager = card(t('Управление RPC-сервером'));
        manager.insertAdjacentHTML('beforeend', '<p class="muted">Укажите полный путь к ggml-rpc-server.exe на удалённом компьютере. Желательно, чтобы версии llama.cpp на обоих компьютерах совпадали.</p>');
        const managerFields = document.createElement('div'); managerFields.className = 'desktop-parity-fields';
        ['rpcServerPath', 'rpcEnabled', 'rpcUseCache'].forEach(key => move(managerFields, body, key));
        manager.appendChild(managerFields);
        const controls = document.getElementById('serverRpcActions'); if (controls) manager.appendChild(controls);
        const syncEndpoint = () => { if (endpoint) endpoint.value = (document.getElementById('rpcRemoteComputer')?.value || '').trim() + ':' + (document.getElementById('rpcRemotePort')?.value || '50052').trim(); };
        sshFields.querySelector('#rpcRemoteComputer')?.addEventListener('input', syncEndpoint);
        sshFields.querySelector('#rpcRemotePort')?.addEventListener('input', syncEndpoint);
        const hiddenEndpoint = endpoint?.closest('label'); if (hiddenEndpoint) hiddenEndpoint.style.display = 'none';
        body.replaceChildren(ssh, manager); if (hiddenEndpoint) body.appendChild(hiddenEndpoint);
        body.dataset.desktopParity = '1';
    }

    function arrange() { arrangeServerTab(); arrangeRpcTab(); } window._arrangeParity = arrange;
    const previousBuild = window.buildServerTabs;
    if (previousBuild && !window._desktopLayoutParityWrapped) {
        window._desktopLayoutParityWrapped = true;
        window.buildServerTabs = function (profile) { const result = previousBuild(profile); setTimeout(arrange, 25); return result; };
    }

    window.renderLogsBody = function (lines) {
        if (!document.getElementById('webLogText')) {
            const host = document.getElementById('logsList');
            host.innerHTML = '<div class="desktop-log-toolbar"><label class="desktop-log-check"><input id="webLogAuto" type="checkbox" checked> Автопрокрутка</label><button class="secondary" onclick="loadLogs()">Обновить</button><button class="secondary" onclick="saveWebLogs()">Сохранить</button><button class="secondary" onclick="clearWebLogs()">Очистить</button></div><pre id="webLogText" class="desktop-log-output"></pre>';
        }
        const output = document.getElementById('webLogText');
        const auto = document.getElementById('webLogAuto')?.checked;
        const text = (Array.isArray(lines) ? lines : []).map(x => typeof x === 'string' ? x : [x.timestamp || x.time || '', x.level || x.category || '', x.message || x.text || JSON.stringify(x)].filter(Boolean).join('  ')).join('\n');
        output.textContent = text || t('Логи отсутствуют (сервер не запущен или вывод ещё не получен)');
        if (auto) output.scrollTop = output.scrollHeight;
    };
    window.loadLogs = async function () {
        const host = document.getElementById('logsList'); if (!host) return;
        try {
            const data = await json('/api/logs');
            window.renderLogsBody(data && Array.isArray(data.lines) ? data.lines : (Array.isArray(data) ? data : []));
        } catch (error) { host.textContent = t('Не удалось загрузить журнал: ') + error.message; }
    };
    window.saveWebLogs = async function () {
        const path = prompt(t('Путь для сохранения журнала:'), '/tmp/llama-studio-logs.txt'); if (!path) return;
        try { const r = await fetch('/api/logs/save?path=' + encodeURIComponent(path)); const d = await r.json().catch(() => ({})); if (r.ok) alert(t('Сохранено: ') + (d.path || path) + ' (' + (d.lines ?? 0) + t(' строк)')); else alert(t('Ошибка: ') + (d.error || await r.text())); } catch (e) { alert(t('Ошибка: ') + e.message); }
    };
    window.clearWebLogs = async function () {
        try { await json('/api/logs/clear'); await window.loadLogs(); } catch (e) { alert(t('Ошибка очистки: ') + e.message); }
    };
    setInterval(() => { if (document.getElementById('logs')?.classList.contains('active')) window.loadLogs(); }, 3000);

    window.loadSettings = async function () {
        const host = document.getElementById('settingsForm') || document.getElementById('settingsInfo'); if (!host) return;
        try {
            const s = await json('/api/settings');
            host.innerHTML = [
'<section class="desktop-settings-card"><h2>Глобальные настройки Studio</h2>',
'<div class="desktop-parity-fields">',
'<h3 style="margin:0 0 8px;color:#93c5fd;font-size:13px;text-transform:uppercase;letter-spacing:.5px">Веб-интерфейс</h3>',
'<label class="field"><span class="label">Адрес веб-интерфейса</span><input id="set_webHost"></label>',
'<label class="field"><span class="label">Порт веб-интерфейса</span><input id="set_webPort" type="number"></label>',
'<label class="server-check"><input id="set_webEnabled" type="checkbox"><span>Веб-интерфейс включён</span></label>',
'<label class="server-check"><input id="set_loginRequired" type="checkbox"><span>Требовать логин и пароль</span></label>',
'<label class="field"><span class="label">Логин</span><input id="set_username"></label>',
'<label class="field"><span class="label">Пароль</span><input id="set_password" type="password" autocomplete="new-password"></label>',
'<h3 style="margin:14px 0 8px;color:#93c5fd;font-size:13px;text-transform:uppercase;letter-spacing:.5px">Сервер llama-server</h3>',
'<label class="field"><span class="label">Адрес llama-server</span><input id="set_defaultHost"></label>',
'<label class="field"><span class="label">Порт llama-server</span><input id="set_defaultPort" type="number"></label>',
'<h3 style="margin:14px 0 8px;color:#93c5fd;font-size:13px;text-transform:uppercase;letter-spacing:.5px">Приложение</h3>',
'<label class="server-check"><input id="set_autoCheckUpdates" type="checkbox"><span>Автопроверка обновлений</span></label>',
'<label class="server-check"><input id="set_autoStartWithOs" type="checkbox"><span>Автозапуск с системой</span></label>',
'<label class="server-check"><input id="set_keepServerOnExit" type="checkbox"><span>Не выгружать сервер при закрытии</span></label>',
'<label class="server-check"><input id="set_autoStartServer" type="checkbox"><span>Запускать сервер при старте</span></label>',
'<label class="field" style="margin-top:14px"><span class="label">Язык / Language</span><select id="set_language"><option value="ru">Русский</option><option value="en">English</option></select></label>',
'</div></section>'
            ].join('');
            document.getElementById('set_webHost').value = s.webHost || ''; document.getElementById('set_webPort').value = s.webPort || ''; document.getElementById('set_webEnabled').checked = !!s.webEnabled; document.getElementById('set_loginRequired').checked = !!s.loginRequired; document.getElementById('set_username').value = s.username || ''; document.getElementById('set_defaultHost').value = s.defaultHost || s.llamaHost || ''; document.getElementById('set_defaultPort').value = s.defaultPort || s.llamaPort || ''; document.getElementById('set_autoCheckUpdates').checked = !!s.autoCheckUpdates; document.getElementById('set_autoStartWithOs').checked = !!s.autoStartWithOs; document.getElementById('set_keepServerOnExit').checked = !!s.keepServerOnExit; document.getElementById('set_autoStartServer').checked = !!s.autoStartServer; const langSel = document.getElementById('set_language'); if (langSel) { langSel.value = s.language || 'ru'; langSel.onchange = function () { if (window.setWebLang) window.setWebLang(this.value, true); } }
                    } catch (error) { host.textContent = t('Не удалось загрузить настройки: ') + error.message; }
    };
    window.saveSettings = async function () {
        const q = new URLSearchParams();
        for (const k of ['webHost','webPort','webEnabled','loginRequired','username','password','defaultHost','defaultPort','autoCheckUpdates','autoStartWithOs','keepServerOnExit','autoStartServer']) { const e = document.getElementById('set_'+k); if (e) q.set(k, e.type==='checkbox' ? String(e.checked) : e.value); }
        const result = await fetch('/api/settings/save?' + q), message = document.getElementById('settingsMessage'); if (message) message.textContent = result.ok ? t('Настройки сохранены. Перезапустите Studio для смены адреса или порта.') : await result.text();
    };
    const style = document.createElement('style');
    style.textContent = '.desktop-parity-card{margin:0 0 10px;padding:14px;border:1px solid #26364f;border-radius:7px;background:#101827}.desktop-parity-card h3{margin:0 0 12px;color:#dbeafe;font-size:15px}.desktop-parity-fields{display:grid;grid-template-columns:1fr;gap:9px}.desktop-parity-fields>.field{display:grid;grid-template-columns:265px minmax(0,1fr);align-items:center;gap:12px}.desktop-parity-fields>.field>.label{margin:0;color:#93a4bf}.desktop-launch-actions{display:flex;gap:8px;flex-wrap:wrap;margin-top:12px}.desktop-options-grid{display:flex;gap:14px;flex-wrap:wrap;align-items:center}.desktop-options-grid .desktop-option{border:0!important;background:transparent!important;padding:3px 0!important;min-height:auto!important;display:flex!important;align-items:center;gap:8px}.desktop-options-grid .desktop-option input[type=checkbox]{appearance:none!important;width:32px!important;height:18px!important;margin:0!important;border:1px solid #64748b!important;border-radius:999px!important;background:#334155!important;position:relative!important;cursor:pointer}.desktop-options-grid .desktop-option input[type=checkbox]::after{content:"";position:absolute;top:2px;left:2px;width:12px;height:12px;border-radius:50%;background:#f8fafc;transition:.16s}.desktop-options-grid .desktop-option input[type=checkbox]:checked{background:#4f8bc4!important;border-color:#60a5fa!important}.desktop-options-grid .desktop-option input[type=checkbox]:checked::after{transform:translateX(14px)}.desktop-options-grid .desktop-option span::after{content:" Off";color:#9ca3af;font-size:12px;margin-left:5px}.desktop-options-grid .desktop-option input:checked+span::after{content:" On";color:#bfdbfe}.desktop-rpc-grid{display:grid;grid-template-columns:1fr 180px;gap:10px}.desktop-rpc-grid .field{display:grid;grid-template-columns:180px minmax(0,1fr);align-items:center;gap:10px}.desktop-rpc-grid .server-check{grid-column:1/-1}.desktop-log-toolbar{display:flex;gap:8px;margin-bottom:10px}.desktop-log-toolbar input{flex:1}.desktop-log-output{min-height:420px;max-height:70vh;overflow:auto;white-space:pre-wrap;word-break:break-word;background:#101827;border:1px solid #334155;border-radius:7px;padding:12px;color:#dbeafe}.desktop-settings-card{background:#101827;border:1px solid #334155;border-radius:7px;padding:14px}.desktop-settings-card h2{margin:0 0 12px}@media(max-width:760px){.desktop-parity-fields>.field,.desktop-rpc-grid .field{grid-template-columns:1fr}.desktop-rpc-grid{grid-template-columns:1fr}}';
    document.head.appendChild(style); setTimeout(arrange, 40);
})();
""";
		const string desktopRpcLayoutFixScript = """
(function(){
 document.addEventListener('input',e=>{if(e.target?.dataset?.serverKey==='rpcSshPassword')e.target.dataset.passwordMasked='false'},true);
 const profileSaveQuery=()=>{const query=new URLSearchParams({id:document.getElementById('profiles')?.value||''});document.querySelectorAll('#serverBodies [data-server-key]').forEach(input=>{if(input.dataset.passwordMasked==='true'||((input.type==='password'||input.dataset.serverKey==='apiKey'||input.dataset.serverKey==='rpcSshPassword')&&!input.value))return;query.set(input.dataset.serverKey,input.type==='checkbox'?String(input.checked):input.value)});return query};
 window.saveWebProfileValues=async()=>{const response=await fetch('/api/profile/save?'+profileSaveQuery());if(!response.ok)throw new Error(await response.text());return response};
 const previousR68Build=window.buildServerTabs;
 if(previousR68Build&&!window._r68NglWrapped){window._r68NglWrapped=true;window.buildServerTabs=function(p){const result=previousR68Build(p);setTimeout(function(){try{if(window.nglAutoAllForTensor)window.nglAutoAllForTensor(null);if(window.enhanceNglControls)window.enhanceNglControls();}catch(e){}},100);return result};}
 let autosaveTimer=0;
 document.addEventListener('change',e=>{const target=e.target;if(!target?.matches?.('#serverBodies [data-server-key]'))return;clearTimeout(autosaveTimer);autosaveTimer=setTimeout(async()=>{try{await window.saveWebProfileValues();const message=document.getElementById('message');if(message)message.textContent=t('Параметр сохранён автоматически.')}catch(error){const message=document.getElementById('message');if(message)message.textContent=t('Ошибка автосохранения: ')+error.message}},120)},true);
 const switchStyle=document.createElement('style');
 switchStyle.textContent=[
  'html body input[type=checkbox]{appearance:none!important;-webkit-appearance:none!important;width:34px!important;min-width:34px!important;height:20px!important;margin:0!important;padding:0!important;border:1px solid #64748b!important;border-radius:999px!important;background:#334155!important;position:relative!important;display:inline-block!important;cursor:pointer!important;accent-color:transparent!important}',
  'html body input[type=checkbox]::after{content:"";position:absolute!important;top:2px!important;left:2px!important;width:14px!important;height:14px!important;border-radius:50%!important;background:#f8fafc!important;transition:.16s!important}',
  'html body input[type=checkbox]:checked{background:#4f8bc4!important;border-color:#60a5fa!important}',
  'html body input[type=checkbox]:checked::after{transform:translateX(14px)!important}',
  'html body input[type=checkbox]:disabled{opacity:.55;cursor:not-allowed}',
  'html body input[type=checkbox]+span::after{content:" Off";color:#9ca3af;font-size:12px;margin-left:5px}',
  'html body input[type=checkbox]:checked+span::after{content:" On";color:#bfdbfe}',
  'html body .desktop-options-grid .desktop-option input[type=checkbox]{width:34px!important;min-width:34px!important;height:20px!important}',
  'html body .desktop-options-grid .desktop-option input[type=checkbox]::after{width:14px!important;height:14px!important;top:2px!important;left:2px!important}',
  'html body label.check,html body label.server-check,html body label.desktop-option{border:0!important;background:transparent!important;padding:3px 0!important;min-height:auto!important}',
  'html body label.gpu-choice{border:0!important;background:transparent!important;padding:3px 0!important;min-height:auto!important}',
  '.server-monitor-title span{display:none!important}',
  '[data-server-body="gpu"] .server-gpu-section:nth-of-type(2)>.server-formgrid{display:none!important}',
  '.desktop-gpu-label{display:block;color:#93a4bf;font-size:12px;margin:12px 0 6px}.desktop-gpu-list,.desktop-gpu-main{display:grid!important;gap:7px;justify-items:start}.desktop-gpu-main{margin-bottom:10px}.desktop-gpu-list label.gpu-choice,.desktop-gpu-main label.gpu-choice{display:flex!important;justify-content:flex-start!important;align-items:center!important;width:auto!important;min-width:0!important;text-align:left!important}.desktop-gpu-main input[type=radio]{appearance:auto!important;-webkit-appearance:auto!important;width:18px!important;min-width:18px!important;height:18px!important;margin:0!important;padding:0!important;position:static!important;background:initial!important;border:initial!important}.desktop-gpu-main label.gpu-choice span{display:inline!important;flex:none!important}',
  '.hf-file input[type=checkbox]+span::after,.gpu-choice input[type=checkbox]+span::after{content:""!important;margin-left:0}',
  '.check,.server-check,.desktop-option,.gpu-choice,.hf-file{align-items:center;gap:9px}'
 ].join('');
 document.head.appendChild(switchStyle);setTimeout(()=>document.head.appendChild(switchStyle),0);
 function normalizeServerBooleanControls(){
  const body=document.querySelector('[data-server-body="server"]'); if(!body)return;
  ['numa','cachePrompt','contBatching','contextShift','verboseLogging','enableSlots','enableWebUI','enableMetrics','reasoning','embeddingMode','priorityHigh'].forEach(key=>{
   const items=[...body.querySelectorAll('[data-server-key="'+key+'"]')].map(input=>input.closest('label')).filter(Boolean);
   if(items.length<2)return;
   const preferred=items.find(item=>item.closest('.desktop-parity-card'))||items[0];
   items.forEach(item=>{if(item!==preferred)item.remove()});
  });
 }
 function normalizeGpuCopy(){
  const body=document.querySelector('[data-server-body="gpu"]');if(!body)return;
  body.querySelectorAll('.diag').forEach(item=>item.remove());
  body.querySelectorAll('.server-gpu-section').forEach(section=>{if(section.querySelector('h3')?.textContent.trim()===t('Мониторинг GPU'))section.remove()});
 }
 async function renderDesktopGpuChooser(){
  const body=document.querySelector('[data-server-body="gpu"]');if(!body)return;
  let host=document.getElementById('serverGpuChooser');
  if(!host){const multiSection=[...body.querySelectorAll('.server-gpu-section')].find(section=>section.querySelector('h3')?.textContent.trim()===t('Работа с multi-GPU'));if(!multiSection)return;host=document.createElement('div');host.id='serverGpuChooser';host.className='server-gpu-chooser';multiSection.appendChild(host)}
  let profile={};try{const profileResponse=await fetch('/api/profile?id='+encodeURIComponent(document.getElementById('profiles')?.value||''));profile=profileResponse.ok?await profileResponse.json():{}}catch{}
  const getProfile=(key)=>{const name=Object.keys(profile).find(item=>item.toLowerCase()===key.toLowerCase());return name?profile[name]:undefined};
  const esc=value=>String(value??'').replace(/[&<>"']/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[char]));
  try{
   let response=await fetch('/api/gpu'),list=response.ok?await response.json():[];
   if(!Array.isArray(list)||!list.length){response=await fetch('/api/monitoring?id='+encodeURIComponent(document.getElementById('profiles')?.value||''));const monitoring=await response.json();list=Array.isArray(monitoring.gpus)?monitoring.gpus:[]}
   const selected=String(getProfile('selectedGpuIndices')||'').split(',').map(item=>item.trim()).filter(Boolean),main=String(getProfile('mainGpu')??'0');
   const savedMode=String(getProfile('gpuSplitMode')||'tensor').toLowerCase(),splitMode=savedMode==='tensor'?'tensor':'layer';
   body.querySelectorAll('[data-server-key="gpuSplitMode"],[data-server-key="mmqEnabled"]').forEach(input=>input.closest('label')?.remove());
   body.querySelector('[data-server-key="selectedGpuIndices"]')?.closest('label')?.remove();
   const mainInput=body.querySelector('[data-server-key="mainGpu"]');if(mainInput)mainInput.closest('label').style.display='none';
   const tensorInput=body.querySelector('[data-server-key="tensorSplit"]');if(tensorInput)tensorInput.closest('label').style.display='none';
   const name=g=>g.displayName||g.name||('GPU '+g.index),chosen=selected.length?selected:list.filter(g=>g.isSelected).map(g=>String(g.index)),cards=list.filter(g=>chosen.includes(String(g.index)));
   const splitValue=()=>{let n=Number(String(tensorInput?.value||'').split(',')[0]);return Number.isFinite(n)?Math.max(1,Math.min(99,n)):50};
   const first=splitValue(),second=100-first;
   const modeDots='<div class="desktop-gpu-label">Распределение модели</div><div class="desktop-gpu-main"><label class="gpu-choice"><input type="radio" name="desktop-web-split-mode" value="tensor" '+(splitMode==='tensor'?'checked':'')+'><span>Тензоры</span></label><label class="gpu-choice"><input type="radio" name="desktop-web-split-mode" value="layer" '+(splitMode==='layer'?'checked':'')+'><span>Слои</span></label></div>';
   const tensorControls=splitMode==='tensor'&&cards.length===2?'<div class="web-gpu-distribution"><span class="desktop-gpu-label">Распределение модели</span><input id="webGpuSplitSlider" type="range" min="1" max="99" value="'+first+'"><div class="web-gpu-ruler"><span>0%</span><span>10%</span><span>20%</span><span>30%</span><span>40%</span><span>50%</span><span>60%</span><span>70%</span><span>80%</span><span>90%</span><span>100%</span></div><div class="web-gpu-adjust"><button type="button" class="secondary" data-gpu-split-delta="-5">−5%</button><button type="button" class="secondary" data-gpu-split-delta="-1">−1%</button><button type="button" class="secondary" data-gpu-split-delta="1">+1%</button><button type="button" class="secondary" data-gpu-split-delta="5">+5%</button></div><div id="webGpuSplitText" class="web-gpu-split-text">'+esc(name(cards[0]))+': '+first+'%   '+esc(name(cards[1]))+': '+second+'%</div></div>':(splitMode==='tensor'&&cards.length>2?'<div class="muted">Для трёх и более GPU используйте автоматическое распределение по памяти карт.</div>':'');
   host.innerHTML='<div class="desktop-gpu-label">Используемые GPU</div><div class="desktop-gpu-list">'+list.map(g=>'<label class="gpu-choice"><input type="checkbox" data-gpu-index="'+esc(g.index)+'" '+(chosen.includes(String(g.index))?'checked':'')+'><span>'+esc(name(g))+'</span></label>').join('')+'</div>'+modeDots+'<div class="desktop-gpu-label">Основной GPU</div><div class="desktop-gpu-main">'+list.filter(g=>!g.isRemote).map(g=>'<label class="gpu-choice"><input type="radio" name="desktop-web-main-gpu" value="'+esc(g.index)+'" '+(String(g.index)===main?'checked':'')+' '+(splitMode==='tensor'?'disabled':'')+'><span>'+esc(name(g))+'</span></label>').join('')+'</div>'+tensorControls+'<button type="button" class="secondary web-gpu-auto">Автоматическое распределение</button><input type="hidden" data-server-key="selectedGpuIndices" value="'+esc(chosen.join(','))+'"><input type="hidden" data-server-key="gpuSplitMode" value="'+splitMode+'">';
   const persist=async()=>{const query=new URLSearchParams({id:document.getElementById('profiles')?.value||''});document.querySelectorAll('#serverBodies [data-server-key]').forEach(input=>{if(input.dataset.passwordMasked==='true'||((input.type==='password'||input.dataset.serverKey==='apiKey'||input.dataset.serverKey==='rpcSshPassword')&&!input.value))return;query.set(input.dataset.serverKey,input.type==='checkbox'?String(input.checked):input.value)});const saveResponse=await fetch('/api/profile/save?'+query);if(!saveResponse.ok)throw new Error(await saveResponse.text())};
   host.querySelectorAll('[data-gpu-index]').forEach(input=>input.addEventListener('change',async()=>{const values=[...host.querySelectorAll('[data-gpu-index]:checked')].map(item=>item.dataset.gpuIndex);host.querySelector('[data-server-key="selectedGpuIndices"]').value=values.join(',');await persist()}));
   host.querySelectorAll('[name="desktop-web-main-gpu"]').forEach(input=>input.addEventListener('change',async()=>{if(mainInput)mainInput.value=input.value;await persist()}));
   host.querySelectorAll('[name="desktop-web-split-mode"]').forEach(input=>input.addEventListener('change',async()=>{if(!input.checked)return;host.querySelector('[data-server-key="gpuSplitMode"]').value=input.value;if(input.value==='tensor')window.nglAutoAllForTensor&&window.nglAutoAllForTensor('Для режима Tensor слои GPU установлены автоматически: All.');if(tensorInput){if(input.value==='tensor'&&!tensorInput.value&&cards.length===2)tensorInput.value='50,50';if(input.value==='layer')tensorInput.value=''}await persist();await renderDesktopGpuChooser()}));
   const setTensorSplit=async value=>{if(!tensorInput)return;const n=Math.max(1,Math.min(99,Number(value)||50));tensorInput.value=n+','+(100-n);const label=host.querySelector('#webGpuSplitText');if(label&&cards.length===2)label.textContent=name(cards[0])+': '+n+'%   '+name(cards[1])+': '+(100-n)+'%';await persist()};
   host.querySelector('#webGpuSplitSlider')?.addEventListener('change',async event=>{await setTensorSplit(event.target.value)});
   host.querySelectorAll('[data-gpu-split-delta]').forEach(button=>button.addEventListener('click',async()=>{const slider=host.querySelector('#webGpuSplitSlider');if(!slider)return;slider.value=Math.max(1,Math.min(99,Number(slider.value)+Number(button.dataset.gpuSplitDelta)));await setTensorSplit(slider.value)}));
   host.querySelector('.web-gpu-auto')?.addEventListener('click',async()=>{if(tensorInput)tensorInput.value='';await persist();await renderDesktopGpuChooser()});
  }catch(error){host.innerHTML='<div class="muted">Список GPU недоступен: '+esc(error.message)+'</div>'}
 }
 window.normalizeServerGpuTab=renderDesktopGpuChooser;
 function fixRpcTab(){
  const body=document.querySelector('[data-server-body="rpc"]');
  if(!body||body.dataset.webRpcLayoutFixed==='1')return;
  window.ensureEmbeddedRpcControls?.();
  const endpoint=body.querySelector('[data-server-key="rpcEndpoint"]');
  const value=endpoint?.value||'',split=value.lastIndexOf(':'),remoteHost=split>0?value.slice(0,split):value,remotePort=split>0?value.slice(split+1):'50052';
  const makeCard=title=>{const item=document.createElement('section');item.className='desktop-parity-card';item.innerHTML='<h3>'+title+'</h3>';return item};
  const take=(host,key)=>{const item=body.querySelector('[data-server-key="'+key+'"]').closest('label');if(item)host.appendChild(item);return item};
  const ssh=makeCard(t('Подключение SSH'));
  ssh.insertAdjacentHTML('beforeend','<p class="muted">На удалённом компьютере должна быть включена служба OpenSSH Server.</p>');
  const sshFields=document.createElement('div');sshFields.className='desktop-rpc-fields';
  sshFields.innerHTML='<label class="field rpc-remote-host"><span class="label">Удалённый компьютер</span><input id="rpcRemoteComputer"></label>';
  sshFields.querySelector('#rpcRemoteComputer').value=remoteHost;
  take(sshFields,'rpcSshUsername');take(sshFields,'rpcSshPassword');
  ssh.appendChild(sshFields);
  const sshOptions=document.createElement('div');sshOptions.className='desktop-rpc-options';
  const showPassword=document.createElement('label');showPassword.className='server-check rpc-show-password';showPassword.innerHTML='<input id="rpcShowPassword" type="checkbox"><span>Показать пароль</span>';
  sshOptions.appendChild(showPassword);take(sshOptions,'rpcSavePassword');
  const passwordInput=sshFields.querySelector('[data-server-key="rpcSshPassword"]');
  showPassword.querySelector('input').addEventListener('change',()=>{if(passwordInput)passwordInput.type=showPassword.querySelector('input').checked?'text':'password'});
  ssh.appendChild(sshOptions);
  const sshActions=document.createElement('div');sshActions.className='desktop-launch-actions rpc-ssh-actions';sshActions.innerHTML='<button class="secondary" onclick="checkEmbeddedRpcSsh()">Подключить SSH</button><span id="serverRpcStatus" class="muted">Подключение ещё не проверялось.</span>';ssh.appendChild(sshActions);
  const manager=makeCard(t('Управление RPC-сервером'));
  manager.insertAdjacentHTML('beforeend','<p class="muted">Укажите полный путь к ggml-rpc-server.exe на удалённом компьютере. Желательно, чтобы версии llama.cpp на обоих компьютерах совпадали.</p>');
  const managerFields=document.createElement('div');managerFields.className='desktop-rpc-manager-fields';
  const pathField=take(managerFields,'rpcServerPath');if(pathField)pathField.classList.add('rpc-server-path');
  const portField=document.createElement('label');portField.className='field rpc-port-field';portField.innerHTML='<span class="label">Порт RPC</span><input id="rpcRemotePort" type="number">';portField.querySelector('#rpcRemotePort').value=remotePort;managerFields.appendChild(portField);
  const managerOptions=document.createElement('div');managerOptions.className='desktop-rpc-manager-options';take(managerOptions,'rpcEnabled');take(managerOptions,'rpcUseCache');managerFields.appendChild(managerOptions);manager.appendChild(managerFields);
  document.getElementById('serverRpcActions')?.remove();
  const managerActions=document.createElement('div');managerActions.className='desktop-launch-actions';managerActions.innerHTML='<button class="green" onclick="startEmbeddedRpc()">Запустить удалённый сервер</button><button class="stop" onclick="stopEmbeddedRpc()">Остановить удалённый сервер</button><span id="serverRpcMessage" class="server-message"></span>';manager.appendChild(managerActions);
  const syncEndpoint=()=>{if(endpoint)endpoint.value=(document.getElementById('rpcRemoteComputer')?.value||'').trim()+':'+(document.getElementById('rpcRemotePort')?.value||'50052').trim()};
  sshFields.querySelector('#rpcRemoteComputer')?.addEventListener('input',syncEndpoint);managerFields.querySelector('#rpcRemotePort')?.addEventListener('input',syncEndpoint);
  const hiddenEndpoint=endpoint?.closest('label');if(hiddenEndpoint)hiddenEndpoint.style.display='none';
  body.replaceChildren(ssh,manager);if(hiddenEndpoint)body.appendChild(hiddenEndpoint);body.dataset.webRpcLayoutFixed='1';
 }
 const oldBuild=window.buildServerTabs;
 if(oldBuild&&!window._dflashOptionFixWrapped){window._dflashOptionFixWrapped=true;window.buildServerTabs=function(p){const result=oldBuild(p);document.querySelectorAll('select[data-key="SpecType"]').forEach(s=>{if(![...s.options].some(o=>o.value==='draft-dflash')){const o=document.createElement('option');o.value='draft-dflash';o.textContent='draft-dflash';s.appendChild(o)}if(p&&p.SpecType)s.value=p.SpecType});return result}}
 const rpcOldBuild=window.buildServerTabs;
 if(rpcOldBuild&&!window._webRpcLayoutFixWrapped){window._webRpcLayoutFixWrapped=true;window.buildServerTabs=function(p){const result=rpcOldBuild(p);setTimeout(fixRpcTab,60);return result}}
 const style=document.createElement('style');style.textContent='.desktop-rpc-fields{display:grid;grid-template-columns:1fr 1fr;gap:10px}.desktop-rpc-fields>.rpc-remote-host{grid-column:1/-1}.desktop-rpc-fields>.field{min-width:0}.desktop-rpc-options{display:flex;gap:14px;align-items:center;flex-wrap:wrap;margin-top:10px}.desktop-rpc-options .server-check,.desktop-rpc-manager-options .server-check{display:flex;align-items:center;gap:8px;padding:3px 0;border:0;background:transparent}.desktop-rpc-manager-fields{display:grid;grid-template-columns:minmax(0,1fr) 180px;gap:10px 14px}.desktop-rpc-manager-fields>.rpc-server-path{grid-column:1/-1}.desktop-rpc-manager-fields>.rpc-port-field{grid-column:2}.desktop-rpc-manager-options{grid-column:1/-1;display:flex;gap:18px;align-items:center;flex-wrap:wrap}.rpc-ssh-actions{align-items:center}.rpc-ssh-actions #serverRpcStatus{font-weight:600}.desktop-rpc-manager-fields input{min-width:0}@media(max-width:760px){.desktop-rpc-fields{grid-template-columns:1fr}.desktop-rpc-fields>.rpc-remote-host,.desktop-rpc-manager-fields>.rpc-server-path,.desktop-rpc-manager-fields>.rpc-port-field{grid-column:1}.desktop-rpc-manager-fields{grid-template-columns:1fr}}';document.head.appendChild(style);setTimeout(fixRpcTab,120);
 async function renderAdvancedMultiGpu(){const host=document.getElementById('serverGpuChooser');if(!host||host.dataset.advancedGpuBusy==='1')return;host.dataset.advancedGpuBusy='1';try{let p={};try{const r=await fetch('/api/profile?id='+encodeURIComponent(document.getElementById('profiles')?.value||''));p=r.ok?await r.json():{}}catch{};const read=k=>{const q=Object.keys(p).find(x=>x.toLowerCase()===k.toLowerCase());return q?p[q]:undefined};let r=await fetch('/api/gpu'),all=r.ok?await r.json():[];if(!Array.isArray(all)||all.length===0)return;const selected=String(read('selectedGpuIndices')||'').split(',').filter(Boolean),chosen=selected.length?all.filter(g=>selected.includes(String(g.index))):all.filter(g=>g.isSelected);if(chosen.length<2)return;const esc2=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));const name=g=>g.displayName||g.name||('GPU '+g.index);const equal=n=>Array.from({length:n},(_,i)=>Math.floor(100/n)+(i<100%n?1:0));const parse=()=>{let a=String(read('tensorSplit')||'').split(',').map(Number);return a.length===chosen.length&&a.every(Number.isFinite)&&a.reduce((x,y)=>x+y,0)===100?a:equal(chosen.length)};let split=parse(),mode=String(read('gpuSplitMode')||'layer').toLowerCase()==='tensor'?'tensor':'layer';const draw=()=>{host.innerHTML='<div class="desktop-gpu-label">Используемые GPU</div><div class="desktop-gpu-list">'+all.map(g=>'<label class="gpu-choice"><input type="checkbox" data-advanced-gpu="'+esc2(g.index)+'" '+(chosen.some(x=>String(x.index)===String(g.index))?'checked':'')+'><span>'+esc2(name(g))+'</span></label>').join('')+'</div><div class="desktop-gpu-label">Режим распределения</div><div class="desktop-gpu-main"><label class="gpu-choice"><input type="radio" name="advanced-gpu-mode" value="tensor" '+(mode==='tensor'?'checked':'')+'><span>Тензоры</span></label><label class="gpu-choice"><input type="radio" name="advanced-gpu-mode" value="layer" '+(mode==='layer'?'checked':'')+'><span>Слои</span></label></div>' +(mode==='tensor'?'<div class="desktop-gpu-label">Распределение модели</div><div class="advanced-split-rows">'+chosen.map((g,i)=>'<div class="advanced-split-row"><span>'+esc2(name(g))+(g.isRemote?' <em>RPC</em>':'')+'</span><b>'+split[i]+'%</b><button class="secondary" data-advanced-delta="-5" data-advanced-pos="'+i+'">−5</button><button class="secondary" data-advanced-delta="-1" data-advanced-pos="'+i+'">−1</button><button class="secondary" data-advanced-delta="1" data-advanced-pos="'+i+'">+1</button><button class="secondary" data-advanced-delta="5" data-advanced-pos="'+i+'">+5</button></div>').join('')+'<div class="web-gpu-adjust"><button class="secondary" data-advanced-preset="vram">По VRAM</button><button class="secondary" data-advanced-preset="equal">Поровну</button><button class="secondary" data-advanced-preset="auto">Автоматически</button></div></div>':'')};const save=async(values)=>{const q=new URLSearchParams({id:document.getElementById('profiles')?.value||''});Object.entries(values).forEach(([k,v])=>q.set(k,String(v)));const x=await fetch('/api/profile/save?'+q);if(!x.ok)throw new Error(await x.text())};const adjust=(pos,delta)=>{let next=split.slice(),target=Math.max(0,Math.min(100,next[pos]+delta)),diff=target-next[pos],other=100-next[pos];if(!diff||other<=0)return;next[pos]=target;let total=target;next.forEach((v,i)=>{if(i===pos)return;next[i]=Math.max(0,Math.round(v-diff*v/other));total+=next[i]});let biggest=next.map((v,i)=>i===pos?-1:v).indexOf(Math.max(...next.map((v,i)=>i===pos?-1:v)));next[biggest]+=100-total;split=next};draw();host.querySelectorAll('[data-advanced-delta]').forEach(b=>b.onclick=async()=>{adjust(Number(b.dataset.advancedPos),Number(b.dataset.advancedDelta));await save({gpuSplitMode:'tensor',tensorSplit:split.join(',')});draw()});host.querySelectorAll('[name="advanced-gpu-mode"]').forEach(b=>b.onchange=async()=>{if(!b.checked)return;mode=b.value;if(mode==='tensor'&&!split.length)split=equal(chosen.length);let r68extra={};if(mode==='tensor'){let hv=document.querySelector('#serverBodies [data-server-key="gpuLayers"]');if(hv&&String(hv.value||'').trim()===''){hv.value='all';r68extra.gpuLayers='all';}}await save(Object.assign({gpuSplitMode:mode,tensorSplit:mode==='tensor'?split.join(','):''},r68extra));draw()});host.querySelectorAll('[data-advanced-gpu]').forEach(b=>b.onchange=async()=>{const ids=[...host.querySelectorAll('[data-advanced-gpu]:checked')].map(x=>x.dataset.advancedGpu);if(!ids.length){b.checked=true;return}await save({selectedGpuIndices:ids.join(',')});host.dataset.advancedGpuBusy='';await renderAdvancedMultiGpu()});host.querySelectorAll('[data-advanced-preset]').forEach(b=>b.onclick=async()=>{if(b.dataset.advancedPreset==='auto'){await save({tensorSplit:''});return}if(b.dataset.advancedPreset==='equal')split=equal(chosen.length);else{let sum=chosen.reduce((s,g)=>s+Math.max(1,(Number(g.memoryTotalGb)||1)-2),0);split=chosen.map(g=>Math.round(Math.max(1,(Number(g.memoryTotalGb)||1)-2)/sum*100));split[split.length-1]+=100-split.reduce((a,v)=>a+v,0)}await save({gpuSplitMode:'tensor',tensorSplit:split.join(',')});draw()})}catch(e){console.warn('Advanced multi-GPU UI:',e)}finally{host.dataset.advancedGpuBusy=''}}
 let normalizeRuns=0; const normalizeInterval=setInterval(()=>{normalizeServerBooleanControls();normalizeGpuCopy();renderDesktopGpuChooser();if(++normalizeRuns>20)clearInterval(normalizeInterval)},500);setTimeout(renderAdvancedMultiGpu,700);setInterval(renderAdvancedMultiGpu,1500);
})();
""";
		return HtmlV2.Replace(oldReleaseMapping, fixedReleaseMapping).Replace("<script>", "<script>" + webI18nScript).Replace("</script>", injectedScript + serverCommandScript + desktopServerScriptFinal + desktopModelScript + desktopModelTrimScript + webConsistencyScript + rpcServerIntegrationScript + desktopLayoutParityScript + desktopRpcLayoutFixScript + "</script>").Replace(((char)0x2605).ToString() + "+", "\\*+").Replace(@"/\s*?+$/g", @"/\s*\*+$/g").Replace("?+", "\\*+");
	}

	private static readonly string HtmlV2 = """
<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Llama Studio</title>
<style>
:root{--bg:#111827;--side:#0b1220;--panel:#1f2937;--field:#101827;--line:#374151;--blue:#2563eb;--green:#059669;--orange:#d97706;--red:#dc2626;--text:#e5e7eb;--muted:#9ca3af}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:14px system-ui,-apple-system,Segoe UI,sans-serif}.app{display:flex;min-height:100vh}.side{width:220px;flex:none;background:var(--side);padding:20px 12px}.brand{font-size:20px;font-weight:750;color:#93c5fd;margin:0 8px 24px}.nav{display:block;width:100%;text-align:left;background:transparent;color:var(--text);border:1px solid transparent;border-radius:6px;margin:3px 0;padding:11px 12px;font:inherit;cursor:pointer}.nav:hover,.nav.active{background:#263b5c;border-color:#3b82f6}.content{flex:1;padding:28px;max-width:1600px}h1{font-size:30px;margin:0 0 20px}h2{font-size:20px;margin:0 0 16px;color:#dbeafe}section{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:20px;margin:14px 0}.page{display:none}.page.active{display:block}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:10px}.dashboard-cards{display:grid;grid-template-columns:repeat(5,minmax(170px,1fr));gap:8px}.dash-card{min-height:142px;padding:16px;border:1px solid var(--line);border-radius:9px;background:var(--field);display:flex;flex-direction:column;gap:7px}.dash-card b{font-size:18px}.server-card{border-color:#4f46e5;background:#201b58}.models-card{border-color:#059669;background:#064e3b}.profile-card{border-color:#b45309;background:#78350f}.llama-card{border-color:#2563eb;background:#1e3a65}.studio-card{border-color:#4338ca}.dash-card select{margin-top:auto}.dash-card button{align-self:flex-start}.wide-button{width:100%}.small-list{font-size:11px;line-height:1.5;color:#bfdbfe;max-height:38px;overflow:hidden}.active-profile-card{background:#21163d;border-color:#35237a}.active-title{font-weight:700;margin-bottom:14px}.status-dot{display:inline-block;width:7px;height:7px;border-radius:50%;background:#6b7280;margin-right:5px}.active-model-row{display:grid;grid-template-columns:2fr 2fr 1fr;gap:12px;background:#35207e;border-radius:6px;padding:12px}.active-stats{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-top:12px}.active-stats b{font-size:15px}.tags{display:flex;gap:6px;margin-top:12px;flex-wrap:wrap}.tag{border-radius:5px;padding:4px 8px;font-size:11px;background:#4338ca}.tag.green{background:#059669}.tag.orange{background:#d97706}.tag.cyan{background:#0891b2}.live{font-size:11px;margin-left:8px}.tile,.card{background:var(--field);border:1px solid var(--line);border-radius:8px;padding:14px}.tile b{display:block;font-size:18px;margin-top:8px}.tile.server{border-color:#4f46e5}.tile.models{border-color:#059669}.tile.profile{border-color:#b45309}.tile.release{border-color:#2563eb}.label{display:block;color:var(--muted);font-size:12px;margin-bottom:6px}.row{display:flex;gap:10px;align-items:center;flex-wrap:wrap}.field{display:flex;flex-direction:column;gap:5px;min-width:180px;flex:1}.field.wide{min-width:360px}.field input,.field select,.field textarea,input,select,textarea{width:100%;background:var(--field);color:var(--text);border:1px solid var(--line);border-radius:6px;padding:9px;font:inherit}.field input[type=checkbox],input[type=checkbox]{width:auto;accent-color:#60a5fa}.check{display:flex;gap:8px;align-items:center;padding:7px 0}.tabs{display:flex;gap:5px;flex-wrap:wrap;border-bottom:1px solid var(--line);margin:-4px -4px 16px}.tab{background:#172236;color:#cbd5e1;border:1px solid #26364f;border-radius:6px 6px 0 0;padding:9px 12px;cursor:pointer}.tab.active{background:#4338ca;color:#fff}.tabbody{display:none}.tabbody.active{display:block}.formgrid{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}.actions{margin-top:16px;display:flex;gap:8px;flex-wrap:wrap}button{background:var(--blue);color:#fff;border:0;border-radius:6px;padding:9px 14px;font:inherit;cursor:pointer}button:hover{filter:brightness(1.12)}button.green{background:var(--green)}button.orange{background:var(--orange)}button.stop{background:var(--red)}button.secondary{background:#4b5563}button:disabled{opacity:.65;cursor:not-allowed;filter:none!important}.notice{color:#34d399;min-height:22px;margin-top:8px}.bad{color:#f87171}.muted{color:var(--muted)}.metric{margin:10px 0}.metricline{display:flex;justify-content:space-between;margin-bottom:4px;color:#cbd5e1;font-size:12px}.bar{height:8px;border-radius:8px;background:#374151;overflow:hidden}.bar i{display:block;height:100%;background:#3b82f6}.bar.green i{background:#10b981}.bar.orange i{background:#f59e0b}.bar.red i{background:#ef4444}.gpu{margin:10px 0}.gpu h3{margin:0 0 10px;font-size:15px}.modelname{font-weight:700;color:#bfdbfe}.release{display:flex;justify-content:space-between;gap:12px;align-items:center}.log{font-family:ui-monospace,Consolas,monospace;font-size:12px;border-bottom:1px solid var(--line);padding:8px 0}.chatanswer{white-space:pre-wrap;min-height:100px;line-height:1.5}.link{color:#93c5fd}.diag{font-size:12px;color:var(--muted)}@media(max-width:1100px){.dashboard-cards{grid-template-columns:repeat(3,minmax(170px,1fr))}}@media(max-width:800px){.side{width:175px}.content{padding:16px}.field.wide{min-width:220px}.dashboard-cards{grid-template-columns:repeat(2,minmax(150px,1fr))}.active-model-row{grid-template-columns:1fr}.active-stats{grid-template-columns:1fr}}
</style></head><body><div class="app"><aside class="side"><div class="brand">Llama Studio</div><button class="nav active" data-page="home">Главная</button><button class="nav" data-page="chat">Чат</button><button class="nav" data-page="models">Модели</button><button class="nav" data-page="monitoring">Мониторинг</button><button class="nav" data-page="releases">Релизы llama.cpp</button><button class="nav" data-page="server">Сервер</button><button class="nav" data-page="rpc">Удалённый RPC</button><button class="nav" data-page="logs">Логи</button><button class="nav" data-page="settings">Настройки</button><button class="nav" data-page="support">Обсуждение и поддержка</button></aside><main class="content">
<div id="home" class="page active"><h1>Главная</h1><div class="dashboard-cards"><div class="dash-card server-card"><span class="label">Сервер</span><b id="homeServer">Загрузка…</b><button class="green wide-button" onclick="startServer()">Запустить</button></div><div class="dash-card models-card"><span class="label">Модели</span><b id="homeModels">—</b><span class="muted">Найдено</span><div id="homeModelNames" class="small-list"></div></div><div class="dash-card profile-card"><span class="label">Профили</span><b id="homeProfile">—</b><span id="homeProfileCount" class="muted"></span><select id="homeProfileSelect" onchange="selectProfile(this.value)"></select><button class="orange" onclick="makeDefaultProfile()">Сделать дефолтным</button></div><div class="dash-card llama-card"><span class="label">llama.cpp</span><b id="homeRelease">—</b><span id="homeReleaseInfo" class="muted"></span><button id="homeReleaseUpdate" onclick="loadHomeReleases()">Обновить</button><select id="homeReleaseSelect" onchange="saveHomeRelease(this.value)"></select></div><div class="dash-card studio-card"><span class="label">Llama Studio</span><b id="homeAppVersion">…</b><span class="ok" id="homeAppVersionBadge">…</span></div></div><section class="active-profile-card"><div class="active-title"><span class="status-dot"></span><span id="homeActiveTitle">Активный профиль</span></div><div class="active-model-row"><div><span class="label">Основная модель</span><a id="homeModelPath" class="link"></a></div><div><span class="label">Зрение (mmproj)</span><a id="homeMmprojPath" class="link"></a></div><div><span class="label">Черновик</span><span id="homeDraftPath">—</span></div></div><div class="active-stats"><div><span class="label">Контекст</span><b id="homeContext">—</b></div><div><span class="label">Температура</span><b id="homeTemperature">—</b></div><div><span class="label">Слои GPU</span><b id="homeGpuLayers">—</b></div></div><div id="homeTags" class="tags"></div></section><section><h2>Использование GPU <span class="ok live">● Live</span></h2><div id="homeGpu"></div></section></div>
<div id="server" class="page"><h1>Сервер</h1><section><h2>Управление llama-server</h2><div class="row"><div class="field"><span class="label">Профиль</span><select id="profiles" onchange="profileChanged()"></select></div><div class="actions"><button id="srvStart" class="green" onclick="startServer()">Запустить сервер</button><button id="srvStop" class="stop" onclick="stopServer()">Остановить</button><button id="srvRestart" class="orange" onclick="restartServer()">Перезапустить</button></div></div><div id="message" class="notice"></div></section><section><div id="serverTabs" class="tabs"></div><div id="serverBodies"></div></section></div>
<div id="monitoring" class="page"><h1>Мониторинг</h1><section><h2>Локальные и удалённые GPU</h2><div id="monitorGpu"></div></section><section><h2>Лимит мощности GPU</h2><div id="powerLimit" class="muted">Загрузка…</div></section></div>
<div id="chat" class="page"><h1>Чат</h1><section><div class="row"><div class="field"><span class="label">Профиль</span><select id="chatProfile" onchange="selectProfile(this.value)"></select></div></div><div class="field"><span class="label">Сообщение</span><textarea id="prompt" rows="7" placeholder=t("Введите сообщение")></textarea></div><div class="actions"><button class="green" onclick="sendChat()">Отправить</button><button class="secondary" onclick="document.getElementById('prompt').value=''">Очистить</button></div><div id="answer" class="card chatanswer"></div></section></div>
<div id="models" class="page"><h1>Модели</h1><section><div class="row"><h2 style="flex:1">GGUF-модели</h2><button onclick="loadModels()">Обновить список</button></div><div id="modelsList" class="grid"></div></section></div>
<div id="releases" class="page"><h1>Релизы llama.cpp</h1><section><div class="field wide"><span class="label">Папка с серверами llama.cpp</span><div class="row"><input id="llamaBaseDirectory" class="release-directory-input" onchange="setLlamaDirectory(this.value)"><button onclick="browseLlamaDirectory()">Обзор…</button></div></div><div id="releaseDirectoryMessage" class="notice"></div></section><div class="tabs release-tabs"><button id="officialReleaseTab" class="tab active" onclick="showReleaseTab('official')">Официальные</button><button id="experimentalReleaseTab" class="tab" onclick="showReleaseTab('experimental')">Экспериментальные</button></div><div id="releaseOfficial"><div class="release-columns"><section class="release-panel"><div class="row"><h2 style="flex:1">Доступные релизы</h2><button id="releaseCheckButton" onclick="checkLlamaReleases()">Проверить обновления</button><label class="check"><input id="releaseLatestOnly" type="checkbox" checked onchange="checkLlamaReleases()"> Только последняя версия</label><label class="check"><input id="releaseNccl" type="checkbox" checked> NCCL (multi-GPU)</label><span id="releaseStatus" class="muted"></span></div><div id="releaseProgress" class="release-progress" style="display:none"><div class="metricline"><span id="releaseProgressStatus">Подготовка установки…</span><span id="releaseProgressValue">0%</span></div><div class="bar"><i id="releaseProgressBar" style="width:0%"></i></div><button class="stop" onclick="cancelLlamaInstall()">Отмена</button></div><div id="releaseCards"></div></section><section class="release-panel"><div class="row"><h2 style="flex:1">Установленные версии</h2><button class="secondary" onclick="loadInstalledLlama()">Обновить</button></div><div id="installedLlamaTitle" class="muted"></div><div id="installedLlamaList"></div></section></div></div><div id="releaseExperimental" style="display:none"><section><div class="row"><h2 style="flex:1">Экспериментальные сборки</h2><span id="experimentalStatus" class="muted"></span></div><div class="row"><div class="field"><span class="label">Репозиторий</span><select id="experimentalRepo" onchange="loadExperimentalReleases()"><option value="pytraveler/llama-cpp-turboquant">pytraveler/llama-cpp-turboquant</option><option value="DiffusionGemma/llama.cpp-diffusion">DiffusionGemma/llama.cpp-diffusion</option></select></div><button onclick="loadExperimentalReleases()">Получить релизы</button></div><div class="row"><div class="field"><span class="label">Релиз</span><select id="experimentalRelease" onchange="renderExperimentalAssets()"></select></div><div class="field"><span class="label">Сборка</span><select id="experimentalAsset"></select></div><button class="green" onclick="installExperimentalWeb()">Установить</button></div><div id="experimentalCards"></div></section></div></div>
<div id="rpc" class="page"><h1>Удалённый RPC</h1><section><h2>Подключение и управление RPC-сервером</h2><p class="muted">SSH используется для управления удалённым ggml-rpc-server.exe и отдельного запроса телеметрии GPU.</p><div id="rpcForm" class="formgrid"></div><div class="actions"><button class="green" onclick="saveRpcForm()">Сохранить RPC</button><button onclick="connectRpc()">Подключить SSH</button><button class="green" onclick="startRpc()">Запустить удалённый сервер</button><button class="stop" onclick="stopRpc()">Остановить удалённый сервер</button></div><div id="rpcMessage" class="notice"></div></section></div>
<div id="logs" class="page"><h1>Логи</h1><section><div class="row"><h2 style="flex:1">Журнал событий</h2><button onclick="loadLogs()">Обновить</button></div><div id="logsList"></div></section></div>
<div id="settings" class="page"><h1>Настройки</h1><section><h2>Глобальные настройки Studio</h2><div id="settingsForm" class="formgrid"></div><div class="actions"><button class="green" onclick="saveSettings()">Сохранить настройки</button></div><div id="settingsMessage" class="notice"></div></section></div>
<div id="support" class="page"><h1>Обсуждение и поддержка</h1><section><h2>Помощь по Llama Studio</h2><p>Документация и обсуждение проекта доступны пользователю через указанные ссылки.</p><p><a class="link" href="https://github.com/satspace-cpu/llamastudio" target="_blank">GitHub проекта</a></p><p><a class="link" href="https://t.me/LlamaStudioApp" target="_blank">Официальный канал: Llama Studio official</a></p><p><a class="link" href="https://t.me/+6RrP3o2WCCVmMTky" target="_blank">Чат поддержки: Llama Studio | Чат поддержки</a></p><p><a class="link" href="https://t.me/LlamaStudioSupport_bot" target="_blank">Бот быстрой помощи: @LlamaStudioSupport_bot</a></p></section></div>
</main></div><script>
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));let currentId='';let profileCache={};
function ensureChatLinkUi(){let section=document.querySelector('#chat section');if(!section||document.getElementById('chatServerLink'))return;if(!document.getElementById('chatLinkStyle')){let style=document.createElement('style');style.id='chatLinkStyle';style.textContent='.chat-link-panel{background:#101827;border:1px solid #374151;border-radius:8px;padding:22px}.chat-link-panel h2{margin-top:0}.chat-link-panel p{color:#9ca3af;line-height:1.5}.chat-link-address{font-family:ui-monospace,Consolas,monospace;color:#93c5fd;margin:12px 0}.chat-link-button{display:inline-block;background:#2563eb;color:#fff;border-radius:6px;padding:10px 16px;text-decoration:none}.chat-link-button:hover{filter:brightness(1.12)}';document.head.appendChild(style)}section.innerHTML='<div class="chat-link-panel"><h2>Чат llama-server</h2><p>Чат работает во встроенном веб-интерфейсе llama-server. Откройте его в новой вкладке браузера.</p><div id="chatServerAddress" class="chat-link-address">Определение адреса сервера…</div><a id="chatServerLink" class="chat-link-button" href="#" target="_blank" rel="noopener">Открыть чат llama-server</a></div>'}
async function loadChatLink(){ensureChatLinkUi();let link=document.getElementById('chatServerLink');let address=document.getElementById('chatServerAddress');if(!link)return;try{let s=await json('/api/status');let url=s.llamaWebUiUrl||('http://'+(s.llamaHost==='0.0.0.0'||s.llamaHost==='::'?'127.0.0.1':(s.llamaHost||'127.0.0.1'))+':'+(s.llamaPort||8080)+'/');link.href=url;if(address)address.textContent=url}catch{if(address)address.textContent=t('Адрес сервера недоступен')}}
let modelsState={repo:'',dir:'',files:[],selected:new Set()};
function modelsUi(){let page=document.getElementById('models');if(!page||document.getElementById('modelsUi'))return;let style=document.createElement('style');style.id='modelsUiStyle';style.textContent='.hf-panel{background:#1f2937;border:1px solid #374151;border-radius:10px;padding:20px;margin-bottom:18px}.hf-title{font-size:20px;font-weight:700;color:#dbeafe;margin-bottom:14px}.hf-row{display:flex;gap:8px;align-items:center;margin:9px 0}.hf-row input{flex:1}.hf-actions{display:flex;gap:8px;flex-wrap:wrap;border-top:1px solid #374151;margin-top:16px;padding-top:16px}.hf-files{max-height:340px;overflow:auto;margin-top:12px}.hf-file{display:flex;gap:10px;align-items:center;padding:8px 10px;border-bottom:1px solid #26364f}.hf-file small{color:#9ca3af;margin-left:auto}.hf-search-results{position:relative;z-index:2}.hf-result{display:flex;justify-content:space-between;gap:10px;align-items:center;background:#101827;border:1px solid #374151;border-radius:7px;padding:9px;margin:5px 0}.model-card{background:#101827;border:1px solid #374151;border-radius:8px;padding:14px;margin:9px 0}.model-head{display:flex;gap:8px;align-items:center;flex-wrap:wrap}.model-head b{color:#bfdbfe}.badge{border-radius:5px;padding:3px 7px;background:#4338ca;font-size:11px}.badge.quant{background:#2563eb}.model-meta{color:#9ca3af;font-size:12px;margin:8px 0}.model-actions{display:flex;gap:8px;flex-wrap:wrap}.models-status{min-height:22px;color:#34d399;margin-top:8px}.models-error{color:#f87171}.path-row{display:flex;gap:8px;align-items:center}.path-row input{flex:1}';document.head.appendChild(style);page.innerHTML='<h1>Модели</h1><div id="modelsUi"><div class="hf-panel"><div class="hf-title">🤗 HuggingFace Download</div><div class="hf-row"><span>🔑</span><input id="hfToken" type="password" placeholder="Токен авторизации HF (для приватных репозиториев)"><button onclick="hfSaveToken()">Сохранить токен</button></div><div class="hf-row"><span>🔎</span><input id="hfSearch" placeholder="Введите название модели для поиска HuggingFace"><button onclick="hfSearchModels()">Поиск</button></div><div id="hfSearchResults" class="hf-search-results"></div><div class="hf-row"><span>🌐</span><input id="hfRepo" placeholder="Репозиторий, например TheBloke/model-GGUF"><button onclick="hfFetchRepo()">Получить файлы</button></div><div class="path-row"><span>📁</span><input id="hfDir" placeholder="Каталог загрузки и моделей"><button onclick="hfSaveDir()">Сохранить каталог</button></div><div id="hfStatus" class="models-status"></div><div id="hfFiles" class="hf-files"></div><div class="hf-actions"><button onclick="hfSelectAll()">Выбрать все</button><button class="secondary" onclick="hfDeselectAll()">Снять выбор</button><button class="green" onclick="hfDownloadSelected()">Скачать выбранные</button><button class="stop" onclick="hfCancel()">Отмена</button><button class="secondary" onclick="hfPause()">Пауза</button></div></div><section><div class="row"><h2 style="flex:1">Модели <span id="modelsCount" class="muted"></span></h2><button onclick="scanModelsWeb()">Сканировать модели</button></div><div class="path-row"><input id="modelsDir" placeholder="Каталог моделей"><button onclick="setModelsDirWeb()">Выбрать каталог</button></div><input id="modelsFilter" placeholder="Поиск моделей…" oninput="renderLocalModels()"><div id="localModels"></div></section></div>';loadModelSettings();scanModelsWeb()}
async function loadModelSettings(){try{let s=await json('/api/settings');let d=s.modelsDirectory||'';for(const id of ['hfDir','modelsDir']){let e=document.getElementById(id);if(e)e.value=d}addModelFolderButtons()}catch{}}
function addModelFolderButtons(){for(const id of ['hfDir','modelsDir']){let e=document.getElementById(id),row=e?.parentElement;if(!e||!row||row.querySelector('.native-browse'))continue;let b=document.createElement('button');b.className='secondary native-browse';b.textContent=t('Обзор…');b.onclick=()=>window.browseModelsDirectory(id);row.appendChild(b)}}
async function browseModelsDirectory(targetId){let input=document.getElementById(targetId),st=document.getElementById('hfStatus');try{if(st)st.textContent=t('Открывается выбор каталога…');let r=await json('/api/models/browse?path='+encodeURIComponent(input?.value||''));if(!r.ok)return;if(document.getElementById('hfDir'))document.getElementById('hfDir').value=r.directory;if(document.getElementById('modelsDir'))document.getElementById('modelsDir').value=r.directory;if(st)st.textContent=t('Каталог выбран: ')+r.directory;await scanModelsWeb()}catch(e){if(st)st.textContent=t('Не удалось выбрать каталог: ')+e.message}}
function modelQuant(name){let m=String(name).match(/(?:Q[0-9]+[_A-Z0-9]*|F16|F32|BF16)/i);return m?m[0].toUpperCase():''}
function renderSpeedMonitor(s){let page=document.getElementById('monitoring');if(!page)return;let box=document.getElementById('speedMonitor');if(!box){box=document.createElement('div');box.id='speedMonitor';page.appendChild(box)}let busy=!!s.isBenchmarking;let gen=Number(s.tokensPerSecond||0);let prompt=Number(s.promptTokensPerSecond||0);box.innerHTML='<section><div class="row"><h2 style="flex:1">Скорость генерации</h2><button class="green" onclick="measureSpeedWeb()"'+(busy?' disabled':'')+'>'+(busy?t('Измерение…'):t('Измерить скорость'))+'</button></div><div class="grid"><div class="tile"><span class="label">Токены генерации</span><b>'+gen.toFixed(1)+' токенов/с</b><div class="metricline"><span>Скорость</span><span>'+gen.toFixed(1)+' токенов/с</span></div><div class="bar green"><i style="width:'+Math.min(100,gen/300*100)+'%"></i></div></div><div class="tile"><span class="label">Обработка prompt</span><b>'+prompt.toFixed(1)+' токенов/с</b><div class="metricline"><span>Скорость</span><span>'+prompt.toFixed(1)+' токенов/с</span></div><div class="bar"><i style="width:'+Math.min(100,prompt/3000*100)+'%"></i></div></div></div>'+(s.result?'<div class="notice bad">'+esc(s.result)+'</div>':'')+'</section>'}
// Keep the web cards in the same order as the desktop monitor: prompt first, generation second.
function renderSpeedMonitor(s){let page=document.getElementById('monitoring');if(!page)return;let box=document.getElementById('speedMonitor');if(!box){box=document.createElement('div');box.id='speedMonitor';page.appendChild(box)}let busy=!!s.isBenchmarking;let gen=Number(s.tokensPerSecond||0),prompt=Number(s.promptTokensPerSecond||0);box.innerHTML='<section><div class="row"><h2 style="flex:1">Скорость генерации</h2><button class="green" onclick="measureSpeedWeb()"'+(busy?' disabled':'')+'>'+(busy?t('Измерение…'):t('Измерить скорость'))+'</button></div><div class="grid"><div class="tile"><span class="label">Обработка prompt</span><b>'+prompt.toFixed(1)+' токенов/с</b><div class="metricline"><span>Скорость</span><span>'+prompt.toFixed(1)+' токенов/с</span></div><div class="bar"><i style="width:'+Math.min(100,prompt/3000*100)+'%"></i></div></div><div class="tile"><span class="label">Токены генерации</span><b>'+gen.toFixed(1)+' токенов/с</b><div class="metricline"><span>Скорость</span><span>'+gen.toFixed(1)+' токенов/с</span></div><div class="bar green"><i style="width:'+Math.min(100,gen/300*100)+'%"></i></div></div></div>'+(s.result?'<div class="notice bad">'+esc(s.result)+'</div>':'')+'</section>'}
function placeSpeedMonitor(){let page=document.getElementById('monitoring'),box=document.getElementById('speedMonitor'),heading=page?.querySelector('h1');if(page&&box&&heading&&heading.nextSibling!==box)page.insertBefore(box,heading.nextSibling)}
async function speedInfoLoad(){try{let d=await json('/api/monitoring?id='+encodeURIComponent(currentId));renderSpeedMonitor(d.speed||{});placeSpeedMonitor()}catch(e){renderSpeedMonitor({result:t('Не удалось получить скорость: ')+e.message});placeSpeedMonitor()}}
async function measureSpeedWeb(){let box=document.getElementById('speedMonitor');if(box)box.scrollIntoView({block:'nearest'});renderSpeedMonitor({isBenchmarking:true});try{let r=await fetch('/api/monitoring/speed');let s=await r.json();renderSpeedMonitor(s)}catch(e){renderSpeedMonitor({result:t('Ошибка измерения скорости: ')+e.message})}}
function renderLocalModels(){let box=document.getElementById('localModels');if(!box)return;let q=(document.getElementById('modelsFilter')?.value||'').toLowerCase();let list=(modelsState.local||[]).filter(x=>String(x.fileName||x.name||'').toLowerCase().includes(q));let c=document.getElementById('modelsCount');if(c)c.textContent=list.length+t(' моделей');box.innerHTML=list.map(x=>{let name=x.fileName||x.name||'';let path=x.path||x.fullName||'';let isMm=String(name).toLowerCase().includes('mmproj');let quant=x.quantizationTag||modelQuant(name);let sizeGb=Number(x.sizeGb)||Number(x.sizeBytes||0)/1073741824;let meta=[x.architecture||'unknown',quant,x.layerCount?(x.layerCount+' layers'):'',x.estimatedVramGb?('~'+Number(x.estimatedVramGb).toFixed(1)+' GB VRAM'):''].filter(Boolean).join(' · ');let flags=(x.hasMtp?'<span class="badge">MTP</span>':'')+(x.isVision?'<span class="badge quant">Vision</span>':'');return '<div class="model-card"><div class="model-head"><b>'+esc(name)+'</b><span class="badge">GGUF</span>'+(quant?'<span class="badge quant">'+esc(quant)+'</span>':'')+flags+'</div><div class="model-meta">'+esc(sizeGb.toFixed(2))+' GB · '+esc(meta)+'</div><div class="model-meta">'+esc(path)+'</div><div class="model-actions"><button class="green" onclick="useModelWeb(\''+esc(path).replace(/'/g,'\\\'')+'\','+(!isMm)+')">'+(isMm?t('Использовать mmproj'):t('Модель сервера'))+'</button></div></div>'}).join('')||'<div class="muted">Модели не найдены</div>'}
function renderLocalModels(){let box=document.getElementById('localModels');if(!box)return;let q=(document.getElementById('modelsFilter')?.value||'').toLowerCase();let list=(modelsState.local||[]).filter(x=>String(x.name).toLowerCase().includes(q));let c=document.getElementById('modelsCount');if(c)c.textContent=list.length+t(' моделей');box.innerHTML=list.map(x=>{let quant=modelQuant(x.name);let isMm=String(x.name).toLowerCase().includes('mmproj');return '<div class="model-card"><div class="model-head"><b>'+esc(x.name)+'</b><span class="badge">GGUF</span>'+(quant?'<span class="badge quant">'+esc(quant)+'</span>':'')+'</div><div class="model-meta">'+esc(Number(x.sizeGb||0).toFixed(2))+' GB · '+esc(x.path)+'</div><div class="model-actions"><button class="green" onclick="useModelWeb(\''+esc(x.path).replace(/'/g,'\\\'')+'\','+(!isMm)+')">'+(isMm?t('Использовать mmproj'):t('Модель сервера'))+'</button></div></div>'}).join('')||'<div class="muted">Модели не найдены</div>'}
async function scanModelsWeb(){try{let dir=document.getElementById('modelsDir')?.value||'';let url=dir?'/api/models/scan?path='+encodeURIComponent(dir):'/api/models';let m=await json(url);modelsState.local=m;renderLocalModels();let st=document.getElementById('hfStatus');if(st)st.textContent=t('Сканирование завершено: ')+m.length+t(' моделей')}catch(e){let st=document.getElementById('hfStatus');if(st)st.textContent=t('Ошибка сканирования: ')+e.message}}
async function setModelsDirWeb(){let dir=document.getElementById('modelsDir')?.value.trim();if(!dir)return;let r=await fetch('/api/models/directory?path='+encodeURIComponent(dir));let st=document.getElementById('hfStatus');if(st)st.textContent=r.ok?t('Каталог моделей сохранён'):t('Каталог не найден');if(r.ok){document.getElementById('hfDir').value=dir;await scanModelsWeb()}}
async function useModelWeb(path,isModel){let q=new URLSearchParams({id:currentId});q.set(isModel?'modelPath':'mmprojPath',path);let r=await fetch('/api/profile/save?'+q);let st=document.getElementById('hfStatus');if(st)st.textContent=r.ok?(isModel?t('Модель сервера назначена'):t('mmproj назначен')):t('Не удалось сохранить профиль');if(r.ok)await profileForm()}
async function hfSaveToken(){let token=document.getElementById('hfToken')?.value||'';let r=await fetch('/api/hf/token/save?token='+encodeURIComponent(token));let st=document.getElementById('hfStatus');if(st)st.textContent=r.ok?t('Токен HuggingFace сохранён'):t('Не удалось сохранить токен')}
async function hfSearchModels(){let q=document.getElementById('hfSearch')?.value.trim();let box=document.getElementById('hfSearchResults');if(!q||!box)return;box.innerHTML='<div class="muted">Поиск…</div>';try{let r=await json('/api/hf/search?q='+encodeURIComponent(q));box.innerHTML=r.map(x=>'<div class="hf-result"><span><b>'+esc(x.id||x.name||'')+'</b><br><small class="muted">'+esc(x.description||'')+'</small></span><button onclick="selectHfRepo(\''+esc(x.id||'').replace(/'/g,'\\\'')+'\')">Выбрать</button></div>').join('')||'<div class="muted">Ничего не найдено</div>'}catch(e){box.innerHTML='<div class="models-error">Ошибка поиска: '+esc(e.message)+'</div>'}}
function selectHfRepo(repo){document.getElementById('hfRepo').value=repo;document.getElementById('hfSearchResults').innerHTML='';hfFetchRepo()}
async function hfFetchRepo(){let repo=document.getElementById('hfRepo')?.value.trim();let dir=document.getElementById('hfDir')?.value.trim();if(!repo)return;modelsState.repo=repo;modelsState.dir=dir||'';let st=document.getElementById('hfStatus');if(st)st.textContent=t('Получение списка файлов…');try{modelsState.files=await json('/api/hf/repo?repo='+encodeURIComponent(repo)+'&dir='+encodeURIComponent(modelsState.dir));modelsState.selected=new Set(modelsState.files.filter(x=>!x.isCompleted&&/\.gguf$/i.test(x.fileName||'')).map(x=>x.fileName));renderHfFiles();if(st)st.textContent=t('Найдено файлов: ')+modelsState.files.length}catch(e){if(st)st.textContent=t('Ошибка HuggingFace: ')+e.message}}
function renderHfFiles(){let box=document.getElementById('hfFiles');if(!box)return;box.innerHTML=modelsState.files.map(x=>'<label class="hf-file"><input type="checkbox" '+(modelsState.selected.has(x.fileName)?'checked':'')+' onchange="toggleHfFile(\''+esc(x.fileName).replace(/'/g,'\\\'')+'\',this.checked)"><span>'+esc(x.fileName)+'</span><small>'+esc(x.isCompleted?t('Уже скачан'):(x.size?((Number(x.size)/1073741824).toFixed(2)+' GB'):'—'))+'</small></label>').join('')||'<div class="muted">Файлы не найдены</div>'}
function toggleHfFile(name,on){if(on)modelsState.selected.add(name);else modelsState.selected.delete(name)}function hfSelectAll(){modelsState.files.forEach(x=>{if(/\.gguf$/i.test(x.fileName||'')&&!x.isCompleted)modelsState.selected.add(x.fileName)});renderHfFiles()}function hfDeselectAll(){modelsState.selected.clear();renderHfFiles()}
async function hfDownloadSelected(){let st=document.getElementById('hfStatus');if(!modelsState.repo||!modelsState.dir||!modelsState.selected.size){if(st)st.textContent=t('Укажите репозиторий, каталог и выберите файлы');return}if(st)st.textContent=t('Загрузка…');let r=await fetch('/api/hf/download?repo='+encodeURIComponent(modelsState.repo)+'&dir='+encodeURIComponent(modelsState.dir)+'&files='+encodeURIComponent([...modelsState.selected].join(',')));if(st)st.textContent=r.ok?t('Загрузка завершена'):t('Ошибка загрузки');if(r.ok)await scanModelsWeb()}
function hfCancel(){fetch('/api/hf/cancel').catch(()=>{});let st=document.getElementById('hfStatus');if(st)st.textContent=t('Загрузка отменена')}function hfPause(){fetch('/api/hf/pause').catch(()=>{});let st=document.getElementById('hfStatus');if(st)st.textContent=t('Пауза загрузки')}
async function syncDefaultButton(){let b=document.querySelector('button[onclick="makeDefaultProfile()"]');if(!b)return;try{let p=await json('/api/profiles');let d=p.find(x=>x.isDefault);let isDefault=!!d&&d.id===currentId;b.disabled=isDefault;b.className=isDefault?'secondary':'orange';b.textContent=isDefault?t('Дефолтный профиль'):t('Сделать дефолтным')}catch{}}
async function json(url){let r=await fetch(url);if(!r.ok)throw new Error(await r.text());return r.json()}
async function profiles(){let p=await json('/api/profiles');let html=p.map(x=>{let name=String(x.name||x.id).replace(/\s*★+$/g,'');return '<option value="'+esc(x.id)+'">'+esc(name)+(x.isDefault?' ★':'')+'</option>'}).join('');for(const id of ['profiles','homeProfileSelect','chatProfile']){let e=document.getElementById(id);if(e)e.innerHTML=html}if(!currentId&&p.length)currentId=(p.find(x=>x.isSelected)?.id||p.find(x=>x.isDefault)?.id||p[0].id);for(const id of ['profiles','homeProfileSelect','chatProfile']){let e=document.getElementById(id);if(e)e.value=currentId}let count=document.getElementById('homeProfileCount');if(count)count.textContent=p.length+t(' сохранено');return p}
function setServerBusy(busy,text){document.querySelectorAll('.server-card button,.actions button,.side-actions button').forEach(b=>b.disabled=busy);let e=document.getElementById('message');if(e)e.textContent=busy?(text||t('Выполняется…')):''}
async function selectProfile(id){let previousId=currentId;currentId=id;for(const k of ['profiles','homeProfileSelect','chatProfile']){let e=document.getElementById(k);if(e)e.value=id}if(previousId===id){await profileForm();await loadRpc();await refresh();return}let s=await json('/api/status');if(s.llamaServer==='Running'){setServerBusy(true,t('Перезапуск сервера с выбранным профилем…'));try{await fetch('/api/profile/select?id='+encodeURIComponent(id));await fetch('/api/server/restart?id='+encodeURIComponent(id))}finally{setServerBusy(false)}}else{await fetch('/api/profile/select?id='+encodeURIComponent(id));await profileForm();await loadRpc()}await profiles();await profileForm();await loadRpc();await refresh()}
async function action(path){let e=document.getElementById('message');let restart=path.includes('/restart');setServerBusy(true,restart?t('Перезапуск сервера…'):t('Выполняется…'));try{let r=await fetch(path+'?id='+encodeURIComponent(currentId));if(e)e.textContent=r.ok?(restart?t('Сервер перезапущен.'):t('Готово')):await r.text()}finally{setServerBusy(false);await profiles();await profileForm();await refresh()}}
function startServer(){return action('/api/server/start')}function stopServer(){return action('/api/server/stop')}function restartServer(){return action('/api/server/restart')}async function profileChanged(){if((await json('/api/status')).llamaServer==='Running')await restartServer();else await selectProfile(document.getElementById('profiles').value)}
const tabs={model:['ModelPath','MmprojPath','DraftModelPath','LlamaCppVersion','Alias'],gpu:['GpuLayers','GpuSplitMode','TensorSplit','MainGpu','SelectedGpuIndices','FlashAttention','Mmap','Mlock','MmqEnabled','KvOffloadEnabled'],context:['ContextSize','BatchSize','UbatchSize','Threads','ThreadsBatch','PredictCount','Slots'],sampling:['Temperature','TopK','TopP','MinP','TypicalP','RepeatPenalty','RepeatLastN','Mirostat','MirostatTau','MirostatEta','Seed'],kv:['CacheTypeK','CacheTypeV','CacheRam','CacheReuse','ContBatching','ContextShift','CachePrompt'],mtp:['DraftModelPath','SpecType','SpeculativeDecoding','DraftModelLocalOnly'],rope:['RopeScaling','RopeFreqBase','RopeFreqScale','YarnExtFactor','YarnAttnFactor','YarnBetaFast','YarnBetaSlow'],server:['Host','Port','EnableSlots','EnableMetrics','EnableWebUI','ApiKey','LogFilePath','VerboseLogging'],args:['CustomArguments'],rpc:['RpcEnabled','RpcEndpoint','RpcSshUsername','RpcSshPassword','RpcSavePassword','RpcUseCache','RpcRemoteTelemetry','RpcServerPath']};const labels={ModelPath:t('Основная модель'),MmprojPath:t('Файл mmproj'),DraftModelPath:t('Draft-модель'),DraftModelLocalOnly:t('Draft-модель только на локальную GPU'),LlamaCppVersion:t('Версия llama.cpp'),GpuLayers:t('Слои GPU'),GpuSplitMode:t('Режим распределения'),ContextSize:t('Контекст'),BatchSize:'Batch size',UbatchSize:'UBatch size',Temperature:t('Температура'),TopK:'Top K',TopP:'Top P',MinP:'Min P',RepeatPenalty:t('Штраф повторов'),RepeatLastN:t('Последние токены'),CacheRam:t('Кэш RAM'),RpcEndpoint:'RPC endpoint (--rpc)',RpcServerPath:t('Путь ggml-rpc-server.exe'),RpcSshUsername:t('Логин SSH'),RpcSshPassword:t('Пароль SSH'),RpcRemoteTelemetry:t('Передавать телеметрию')};
function control(k,v){let name=labels[k]||k;let bool=typeof v==='boolean';let type=bool?'checkbox':(typeof v==='number'?'number':'text');let checked=bool&&v?' checked':'';if(k==='GpuSplitMode'||k==='RopeScaling'||k==='SpecType'){return '<label class="field"><span class="label">'+name+'</span><select data-key="'+k+'"><option>'+esc(v||'Auto')+'</option><option>none</option><option>layer</option><option>row</option><option>tensor</option></select></label>'}return '<label class="field '+(k.includes('Path')||k==='CustomArguments'||k==='RpcEndpoint'||k==='RpcServerPath'?'wide':'')+'"><span class="label">'+name+'</span><input data-key="'+k+'" type="'+type+'" value="'+(bool?'':esc(v??''))+'"'+checked+(k.includes('Password')?' autocomplete="new-password"':'')+'></label>'}
function buildServerTabs(p){let host=document.getElementById('serverBodies');document.getElementById('serverTabs').innerHTML=Object.keys(tabs).map((k,i)=>'<button class="tab '+(i===0?'active':'')+'" data-tab="'+k+'">'+({model:t('Модель'),gpu:'GPU',context:t('Контекст'),sampling:t('Сэмплинг'),kv:t('KV Кэш'),mtp:t('MTP и Спекулятивное'),rope:'Rope/YARN',server:t('Сервер'),args:t('Пользовательские аргументы'),rpc:t('Удалённый RPC')}[k])+'</button>').join('');host.innerHTML=Object.entries(tabs).map(([tab,keys],i)=>'<div class="tabbody '+(i===0?'active':'')+'" data-body="'+tab+'"><div class="formgrid">'+keys.map(k=>control(k,p[k])).join('')+'</div></div>').join('');document.querySelectorAll('.tab').forEach(b=>b.onclick=()=>{document.querySelectorAll('.tab,.tabbody').forEach(x=>x.classList.remove('active'));b.classList.add('active');document.querySelector('[data-body="'+b.dataset.tab+'"]')?.classList.add('active')})}
async function profileForm(){if(!currentId)return;let p=await json('/api/profile?id='+encodeURIComponent(currentId));profileCache=p;buildServerTabs(p);if(window._arrangeParity)setTimeout(window._arrangeParity,50);let name=p.name||currentId;document.getElementById('homeProfile').textContent=name;document.getElementById('homeActiveTitle').textContent=t('Активный профиль: ')+name;document.getElementById('homeModelPath').textContent=p.modelPath||'—';document.getElementById('homeMmprojPath').textContent=p.mmprojPath||'—';document.getElementById('homeDraftPath').textContent=p.draftModelPath||'—';document.getElementById('homeContext').textContent=p.contextSize??'—';document.getElementById('homeTemperature').textContent=p.temperature??'—';document.getElementById('homeGpuLayers').textContent=p.gpuLayers||t('Авто');document.getElementById('homeTags').innerHTML=[p.speculativeDecoding||p.specType?'<span class="tag">MTP</span>':'',p.reasoning?'<span class="tag green">Рассуждение</span>':'',p.flashAttention?'<span class="tag">Flash Attention</span>':'',p.cachePrompt?'<span class="tag orange">Кэш промпта</span>':'',p.contBatching?'<span class="tag cyan">Непрерывный батч</span>':''].join('');let version=document.getElementById('homeRelease');if(version)version.textContent=p.selectedServerVersion||'llama.cpp';let sel=document.getElementById('homeReleaseSelect');if(sel){sel.innerHTML='<option value="'+esc(p.selectedServerVersion||'')+'">'+esc(p.selectedServerVersion||t('Не выбрано'))+'</option>';sel.value=p.selectedServerVersion||''}}
function values(){let q=new URLSearchParams({id:currentId});document.querySelectorAll('[data-key]').forEach(e=>q.set(e.dataset.key,e.type==='checkbox'?String(e.checked):e.value));return q}async function saveFullProfile(){let r=await fetch('/api/profile/save?'+values());document.getElementById('message').textContent=r.ok?t('Профиль сохранён. Перезапустите сервер для применения.'):await r.text();await profileForm()}
function rpcField(k,v){return control(k,v)}async function loadRpc(){let p=profileCache.rpcEnabled!==undefined?profileCache:await json('/api/profile?id='+encodeURIComponent(currentId));document.getElementById('rpcForm').innerHTML=['RpcEnabled','RpcEndpoint','RpcSshUsername','RpcSshPassword','RpcSavePassword','RpcUseCache','RpcRemoteTelemetry','RpcServerPath'].map(k=>rpcField(k,p[k])).join('')}
async function saveRpcForm(){let q=values();let r=await fetch('/api/profile/save?'+q);document.getElementById('rpcMessage').textContent=r.ok?t('Параметры RPC сохранены.'):await r.text();await loadRpc()}async function rpcAction(path){document.getElementById('rpcMessage').textContent=t('Выполняется…');let r=await fetch(path);let x=await r.json().catch(()=>({}));document.getElementById('rpcMessage').textContent=r.ok?(x.status|| (x.ok?t('Готово'):t('Отключено'))):await r.text()}function connectRpc(){return rpcAction('/api/rpc/check')}function startRpc(){return rpcAction('/api/rpc/start')}function stopRpc(){return rpcAction('/api/rpc/stop')}
function metric(label,value,max,kind=''){let n=typeof value==='number'?value:parseFloat(String(value).replace(',','.'));n=Number.isFinite(n)?n:0;let pct=max?Math.max(0,Math.min(100,n/max*100)):n;return '<div class="metric"><div class="metricline"><span>'+label+'</span><span>'+esc(value)+'</span></div><div class="bar '+kind+'"><i style="width:'+pct+'%"></i></div></div>'}function gpuSummaryCards(g){if(!g||!g.length)return '';let used=g.reduce((a,x)=>a+(Number(x.memoryUsedGb)||0),0),total=g.reduce((a,x)=>a+(Number(x.memoryTotalGb)||0),0),load=g.map(x=>'GPU'+(x.index??'')+' '+(Number(x.gpuUtilization)||0).toFixed(0)+'%').join('  '),draw=g.reduce((a,x)=>a+(Number(x.powerDrawWatts)||0),0),limit=g.reduce((a,x)=>a+((Number(x.powerLimitWatts)||Number(x.powerLimit)||0)),0);let h='<div class="card gpu"><h3>'+t('Все GPU')+'</h3>'+metric('VRAM',used.toFixed(1)+' / '+total.toFixed(1)+' GB',total)+'<div class="metricline"><span>'+t('Загрузка GPU')+'</span><span>'+load+'</span></div><div class="metricline"><span>'+t('Мощность')+'</span><span>'+draw.toFixed(0)+' W</span></div></div>';if(limit>0)h+='<div class="card gpu"><h3>'+t('Мощность всех GPU')+'</h3>'+metric(t('Мощность'),draw.toFixed(0)+' / '+limit.toFixed(0)+' W',limit,'orange')+'</div>';return h}function gpuCards(g){return gpuSummaryCards(g)+g.map(x=>{let draw=Number(x.powerDrawWatts)||0;let limit=Number(x.powerLimitWatts)||0;let powerText=limit>0?draw.toFixed(0)+' W / '+limit.toFixed(0)+' W':draw.toFixed(0)+' W / —';return '<div class="card gpu"><h3>'+esc(x.displayName)+(x.isRemote?' <span class="ok">RPC</span>':'')+'</h3>'+metric('VRAM',Number(x.memoryUsedGb||0).toFixed(1)+' / '+Number(x.memoryTotalGb||0).toFixed(1)+' GB',x.memoryTotalGb)+metric(t('Загрузка GPU'),Number(x.gpuUtilization||0).toFixed(0)+'%',100)+metric(t('Мощность GPU'),powerText,limit,'orange')+metric(t('Температура GPU'),Number(x.temperatureCelsius||0).toFixed(0)+' °C',100,'green')+(Number(x.memoryTemperatureCelsius||0)>0?metric(t('Темп. памяти GPU'),Number(x.memoryTemperatureCelsius).toFixed(0)+' °C',110,'red'):'')+metric(t('Вентилятор'),Number(x.fanSpeed||0).toFixed(0)+'%',100)+'</div>'}).join('')||'<div class="muted">Данные GPU недоступны</div>'}async function gpuInfo(){let d=await json('/api/monitoring?id='+encodeURIComponent(currentId));let h=gpuCards(d.gpus||[]);document.getElementById('homeGpu').innerHTML=h;document.getElementById('monitorGpu').innerHTML=h;let box=document.getElementById('monitorConfig');if(!box){box=document.createElement('div');box.id='monitorConfig';document.getElementById('monitorGpu').parentElement.appendChild(box)}let p=d.profile||{};box.innerHTML='<section><h2>Параметры мониторинга из профиля</h2><div class="grid"><div class="tile"><span class="label">Контекст</span><b>'+esc(p.contextSize??'—')+'</b></div><div class="tile"><span class="label">Слои GPU</span><b>'+esc(p.gpuLayers||t('Авто'))+'</b></div><div class="tile"><span class="label">Распределение</span><b>'+esc(p.tensorSplit||'—')+'</b></div><div class="tile"><span class="label">Выбранные GPU</span><b>'+esc(p.selectedGpuIndices||'—')+'</b></div><div class="tile"><span class="label">RPC-телеметрия</span><b>'+((p.rpcEnabled&&p.rpcRemoteTelemetry)?t('Включена'):t('Выключена'))+'</b></div></div></section>';let pl=document.getElementById('powerLimit');if(pl&&!pl.dataset.ready){renderPowerCard()}}
function ramCard(r){let used=Number(r.usedGb||0),committed=Number(r.committedGb||0),cache=Number(r.cacheLimitGb||0),total=Number(r.totalGb||0),value=total?used.toFixed(1)+' / '+total.toFixed(1)+' GB':'— / — GB';if(committed>0)value+=t(' · выделено: ')+committed.toFixed(1)+' GB';if(cache>0)value+=t(' · кэш: до ')+cache.toFixed(1)+' GB';return '<div class="card ram-card"><h3 style="color:#10b981">RAM</h3>'+metric(t('ОЗУ'),value,total,'green')+'</div>'}
async function ramInfoLoad(){try{let d=await json('/api/monitoring?id='+encodeURIComponent(currentId));let card=ramCard(d.ram||{});for(const id of ['homeGpu','monitorGpu']){let e=document.getElementById(id);if(e){e.querySelectorAll('.ram-card').forEach(x=>x.remove());e.insertAdjacentHTML('afterbegin',card)}}}catch{} }
async function gpuInfo(){let d=await json('/api/monitoring?id='+encodeURIComponent(currentId));let h=ramCard(d.ram||{})+gpuCards(d.gpus||[]);document.getElementById('homeGpu').innerHTML=h;document.getElementById('monitorGpu').innerHTML=h;let box=document.getElementById('monitorConfig');if(!box){box=document.createElement('div');box.id='monitorConfig';document.getElementById('monitorGpu').parentElement.appendChild(box)}let p=d.profile||{};box.innerHTML='<section><h2>Параметры мониторинга из профиля</h2><div class="grid"><div class="tile"><span class="label">Контекст</span><b>'+esc(p.contextSize??'—')+'</b></div><div class="tile"><span class="label">Слои GPU</span><b>'+esc(p.gpuLayers||t('Авто'))+'</b></div><div class="tile"><span class="label">Распределение</span><b>'+esc(p.tensorSplit||'—')+'</b></div><div class="tile"><span class="label">Выбранные GPU</span><b>'+esc(p.selectedGpuIndices||'—')+'</b></div><div class="tile"><span class="label">RPC-телеметрия</span><b>'+((p.rpcEnabled&&p.rpcRemoteTelemetry)?t('Включена'):t('Выключена'))+'</b></div></div></section>';let pl=document.getElementById('powerLimit');if(pl&&!pl.dataset.ready){renderPowerCard()}}
async function renderPowerCard(){let host=document.getElementById('powerLimit');if(!host||host.dataset.ready)return;host.dataset.ready='1';host.innerHTML='<div class="muted">Загрузка…</div>';try{let items=await json('/api/gpu/power');let profileLimits={};try{let pf=await json('/api/profile?id='+encodeURIComponent(currentId));let raw=String(pf.gpuPowerLimits||pf.GpuPowerLimits||'');raw.split(',').forEach(x=>{let i=x.split(':');if(i.length===2&&i[0].trim())profileLimits[i[0].trim()]=i[1].trim()})}catch(e){}let firstW=Object.values(profileLimits)[0]||'250';if(Array.isArray(items)&&items.length){host.innerHTML='<div class="card"><div id="powerRows">'+items.map(g=>{let id=String(g.index??'');let has=Object.prototype.hasOwnProperty.call(profileLimits,id);let range=Number(g.minWatts)>0?Number(g.minWatts).toFixed(0)+'–'+Number(g.maxWatts).toFixed(0)+' W':Number(g.maxWatts).toFixed(0)+' W max';return '<label class="check gpu-choice"><input type="checkbox" data-power-index="'+esc(id)+'" '+(has?'checked':'')+'><span>GPU '+id+' · '+esc(g.name||'')+' — лимит '+Number(g.limitWatts).toFixed(0)+' W · сейчас '+Number(g.currentWatts).toFixed(0)+' W · '+range+'</span></label>'}).join('')+'</div><div class="metricline" style="margin-top:10px;gap:8px;flex-wrap:wrap"><span class="muted">Ватты:</span><input id="powerWatts" type="number" value="'+esc(firstW)+'" style="width:90px"><button onclick="applyPowerLimit()">Применить к выбранным</button><span id="powerStatus" class="muted"></span></div></div>'}else{host.innerHTML='<div class="muted">GPU не найдены</div>'}}catch(e){host.innerHTML='<div class="muted">Не удалось получить данные nvidia-smi</div>';host.dataset.ready='0'}}
async function applyPowerLimit(){let wEl=document.getElementById('powerWatts');let watts=parseFloat(wEl?wEl.value:'');let st=document.getElementById('powerStatus');if(!isFinite(watts)||watts<=0){if(st)st.textContent='Введите число ватт, например 250';return}let sel=Array.from(document.querySelectorAll('#powerRows [data-power-index]:checked')).map(x=>x.dataset.powerIndex);if(!sel.length){if(st)st.textContent='Сначала отметьте GPU';return}if(st){st.textContent='Применяю...';st.style.color='#9CA3AF'}let msgs=[];let anyFail=false;for(let id of sel){try{let r=await json('/api/gpu/power/set?index='+id+'&watts='+watts);if(!r||!r.ok)anyFail=true;msgs.push((r&&r.message)||('GPU '+id+': ошибка'))}catch(e){anyFail=true;msgs.push('GPU '+id+': ошибка сети')}}if(st){st.textContent=msgs.join('  |  ');st.style.color=anyFail?'#EF4444':'#10B981'}try{let map=sel.map(id=>id+':'+watts).join(',');await json('/api/profile/save?id='+encodeURIComponent(currentId)+'&gpuPowerLimits='+encodeURIComponent(map))}catch(e){}setTimeout(()=>{let h=document.getElementById('powerLimit');if(h){h.dataset.ready='0';renderPowerCard()}},600)}

async function refresh(){try{let x=await json('/api/status');let run=x.llamaServer==='Running';document.getElementById('homeServer').textContent=run?'Running':'Stopped';await gpuInfo();await speedInfoLoad()}catch(e){document.getElementById('homeServer').textContent=t('Нет связи')}}
async function loadModels(){let m=await json('/api/models');document.getElementById('homeModels').textContent=m.length;document.getElementById('homeModelNames').innerHTML=m.slice(0,2).map(x=>'<div>• '+esc(x.name)+'</div>').join('');document.getElementById('modelsList').innerHTML=m.map(x=>'<div class="card"><div class="modelname">'+esc(x.name)+'</div><div>'+esc(Number(x.sizeGb||0).toFixed(2))+' GB</div><small class="muted">'+esc(x.path)+'</small></div>').join('')||'<div class="muted">Модели не найдены</div>'}async function loadReleases(){let r=await json('/api/llama/releases');document.getElementById('releasesList').innerHTML=r.map(x=>'<div class="card release"><div><b>'+esc(x.tag||x.name)+'</b><div class="muted">'+esc(x.publishedAt||'')+'</div></div><a class="link" href="'+esc(x.url||'#')+'" target="_blank">Открыть</a></div>').join('')||'<div class="muted">Релизы не найдены</div>'}async function loadHomeReleases(){let r=await json('/api/llama/releases');let sel=document.getElementById('homeReleaseSelect');let options=[];for(const x of r){for(const a of (x.assets||[])){if(a.name&&a.name.toLowerCase().includes('cuda'))options.push({value:(x.tag||x.name)+'|'+a.name.replace(/^llama-/i,'').replace(/\.zip$/i,''),name:(x.tag||x.name)+' '+a.name})}}if(sel&&options.length)sel.innerHTML=options.slice(0,30).map(x=>'<option value="'+esc(x.value)+'">'+esc(x.name)+'</option>').join('');document.getElementById('homeReleaseInfo').textContent=options.length?options.length+t(' доступных сборок'):t('Обновления не найдены')}async function saveHomeRelease(value){if(!value)return;let q=new URLSearchParams({id:currentId,selectedServerVersion:value});let r=await fetch('/api/profile/save?'+q);document.getElementById('homeReleaseInfo').textContent=r.ok?t('Версия сохранена'):t('Не удалось сохранить версию');await profileForm()}async function makeDefaultProfile(){let r=await fetch('/api/profile/default?id='+encodeURIComponent(currentId));document.getElementById('homeProfileCount').textContent=r.ok?t('Профиль назначен дефолтным'):t('Не удалось назначить');await profiles();await profileForm()}
async function loadLogs(){let l=await json('/api/logs');document.getElementById('logsList').innerHTML=l.map(x=>'<div class="log">'+esc(x.timestamp||'')+' <b>'+esc(x.category||'')+'</b> '+esc(x.message||x.text||'')+'</div>').join('')||'<div class="muted">Логи отсутствуют</div>'}async function loadSettings(){let s=await json('/api/settings');document.getElementById('settingsForm').innerHTML=[['webHost',t('Адрес веб-интерфейса'),s.webHost,'text'],['webPort',t('Порт веб-интерфейса'),s.webPort,'number'],['webEnabled',t('Веб-интерфейс включён'),s.webEnabled,'checkbox'],['loginRequired',t('Требовать логин и пароль'),s.loginRequired,'checkbox'],['username',t('Логин'),s.username,'text'],['password',t('Пароль'),'','password'],['defaultHost',t('Адрес llama-server'),s.defaultHost,'text'],['defaultPort',t('Порт llama-server'),s.defaultPort,'number'],['autoCheckUpdates',t('Автопроверка обновлений'),s.autoCheckUpdates,'checkbox'],['autoStartWithOs',t('Автозапуск с системой'),s.autoStartWithOs,'checkbox'],['keepServerOnExit',t('Не выгружать сервер при закрытии'),s.keepServerOnExit,'checkbox'],['autoStartServer',t('Запускать сервер при старте'),s.autoStartServer,'checkbox']].map(a=>'<label class="field"><span class="label">'+a[1]+'</span><input id="set_'+a[0]+'" type="'+a[3]+'" value="'+(a[3]==='checkbox'?'':esc(a[2]??''))+'"'+(a[3]==='checkbox'&&a[2]?' checked':'')+'></label>').join('')+'<label class="field"><span class="label">Язык / Language</span><select id="set_language"><option value="ru">Русский</option><option value="en">English</option></select></label>';let langSel=document.getElementById('set_language');if(langSel){langSel.value=s.language||'ru';langSel.onchange=function(){window.setWebLang&&window.setWebLang(this.value,true)}}}async function saveSettings(){let q=new URLSearchParams();for(const k of ['webHost','webPort','webEnabled','loginRequired','username','password','defaultHost','defaultPort','autoCheckUpdates','autoStartWithOs','keepServerOnExit','autoStartServer']){let e=document.getElementById('set_'+k);if(e)q.set(k,e.type==='checkbox'?String(e.checked):e.value)}let r=await fetch('/api/settings/save?'+q);document.getElementById('settingsMessage').textContent=r.ok?t('Настройки сохранены. Перезапустите Studio при изменении адреса или порта.'):await r.text()}
async function sendChat(){let msg=document.getElementById('prompt').value.trim();if(!msg)return;document.getElementById('answer').textContent=t('Генерация…');let r=await fetch('/api/chat?id='+encodeURIComponent(currentId)+'&message='+encodeURIComponent(msg));document.getElementById('answer').textContent=r.ok?(await r.json()).answer:await r.text()}
document.querySelectorAll('.nav').forEach(b=>b.onclick=async()=>{document.querySelectorAll('.nav,.page').forEach(x=>x.classList.remove('active'));b.classList.add('active');document.getElementById(b.dataset.page).classList.add('active');if(b.dataset.page==='models'){modelsUi();scanModelsWeb()}if(b.dataset.page==='releases')loadReleases();if(b.dataset.page==='logs')loadLogs();if(b.dataset.page==='settings')loadSettings();if(b.dataset.page==='rpc')loadRpc()});async function loadAppVersion(){try{let s=await json('/api/settings');let v=String(s.appVersion||'');let b=document.getElementById('homeAppVersion');if(b)b.textContent=v;let badge=document.getElementById('homeAppVersionBadge');if(badge)badge.textContent=v?'✓ '+t('Установлена'):''}catch{let b=document.getElementById('homeAppVersion');if(b)b.textContent='';let badge=document.getElementById('homeAppVersionBadge');if(badge)badge.textContent=''}}(async()=>{let p=await profiles();if(p.length){await profileForm();await loadRpc()}await loadModels();await refresh();await syncDefaultButton();loadAppVersion()})();setInterval(()=>{refresh()},4000);setInterval(syncDefaultButton,1000);
function ensureSideActions(){if(document.getElementById('sideServerAction'))return;let side=document.querySelector('.side');if(!side)return;let box=document.createElement('div');box.className='side-actions';box.innerHTML='<button id="sideServerAction" class="side-start" onclick="toggleServer()">Запустить сервер</button><button id="sideStopAction" class="side-stop" onclick="stopServer()">Остановить</button><button id="sideRestartAction" class="side-restart" onclick="restartServer()">Перезапустить</button>';side.appendChild(box);let style=document.createElement('style');style.textContent='.side-actions{border-top:1px solid #26364f;margin-top:24px;padding-top:14px}.side-actions button{display:block;width:100%;margin:5px 0}.side-start{background:#4f83b5!important}.side-running{background:#4b5563!important}.side-stop{background:#dc2626!important}.side-restart{background:#d97706!important}.side-stop:disabled{background:#4b5563!important}';document.head.appendChild(style)}
async function updateServerButtons(){ensureSideActions();try{let x=await json('/api/status');let running=x.llamaServer==='Running';let home=document.querySelector('.server-card button');let side=document.getElementById('sideServerAction');let stop=document.getElementById('sideStopAction');if(home){home.textContent=running?t('Остановить'):t('Запустить');home.className=running?'stop wide-button':'green wide-button';home.onclick=running?stopServer:startServer}if(side){side.textContent=running?t('✓ Запущен'):t('Запустить сервер');side.className=running?'side-running':'side-start';side.onclick=running?(()=>{}):startServer}if(stop){stop.disabled=!running;stop.className=running?'side-stop':'side-stop'}let srvStart=document.getElementById('srvStart');let srvStop=document.getElementById('srvStop');if(srvStart){srvStart.textContent=running?t('✓ Запущен'):t('Запустить сервер');srvStart.className=running?'secondary':'green';srvStart.onclick=running?(()=>{}):startServer;srvStart.disabled=false}if(srvStop){srvStop.disabled=!running;srvStop.textContent=running?t('Остановить'):t('Остановить')}}catch{}}
async function toggleServer(){let x=await json('/api/status');return x.llamaServer==='Running'?stopServer():startServer()}
async function loadHomeReleases(){let d=await json('/api/llama/installed');let sel=document.getElementById('homeReleaseSelect');let versions=d.versions||[];let items=versions.map(v=>{let key=String(v).replace(/-cuda12x$/i,'|Cuda12x').replace(/-cuda13x$/i,'|Cuda13x');return {key:key,label:v}});if(sel)sel.innerHTML=items.map(x=>'<option value="'+esc(x.key)+'">'+esc(x.label)+'</option>').join('');let active=profileCache.selectedServerVersion||d.active||'';if(sel&&active)sel.value=active;let info=document.getElementById('homeReleaseInfo'),button=document.getElementById('homeReleaseUpdate');try{let releases=await json('/api/llama/releases');let latest=releases[0];let latestTag=latest?.tag||'';if(latestTag&&latestTag!==String(active).split('|')[0]){if(info)info.textContent=t('↑ Доступно обновление, ')+latestTag;if(button)button.textContent=t('Обновить (')+latestTag+')'}else{if(info)info.textContent=versions.length?versions.length+t(' установлено'):t('Установленные сборки не найдены');if(button)button.textContent=t('Проверить обновления')}}catch{if(info)info.textContent=versions.length?versions.length+t(' установлено'):t('Установленные сборки не найдены')} }
async function updateHomeLlama(){let info=document.getElementById('homeReleaseInfo');if(info)info.textContent=t('Проверка обновлений…');let releases=await json('/api/llama/releases');let release=releases[0];let asset=(release?.assets||[]).find(a=>/cuda-?12|cuda12/i.test(a.name));if(!release||!asset){if(info)info.textContent=t('Подходящая CUDA-сборка не найдена');return}if(info)info.textContent=t('Загрузка ')+release.tag+'…';let r=await fetch('/api/llama/update?version='+encodeURIComponent(release.tag)+'&asset='+encodeURIComponent(asset.name));let x=await r.json().catch(()=>({}));if(info)info.textContent=r.ok?t('Установлено: ')+(x.installed||release.tag):t('Ошибка обновления: ')+(x.error||r.status);await loadHomeReleases()}
metric=function(label,value,max,kind=''){let n=Number.parseFloat(String(value))||0;let pct=max?Math.max(0,Math.min(100,n/max*100)):n;return '<div class="metric"><div class="metricline"><span>'+label+'</span><span>'+esc(value)+'</span></div><div class="bar '+kind+'"><i style="width:'+pct+'%"></i></div></div>'};ensureSideActions();let barsStyle=document.createElement('style');barsStyle.textContent='.bar i{display:block!important;height:100%!important;min-width:2px;background:#3b82f6!important}.bar.green i{background:#10b981!important}.bar.orange i{background:#f59e0b!important}.bar.red i{background:#ef4444!important}';document.head.appendChild(barsStyle);let updateButton=document.getElementById('homeReleaseUpdate');if(updateButton)updateButton.onclick=updateHomeLlama;updateServerButtons();loadHomeReleases();setTimeout(loadHomeReleases,1500);setInterval(updateServerButtons,2000);setInterval(pollLlamaGlobal,3000);
	let installedLlamaState={versions:[],active:''},experimentalReleases=[],cudaToolkitState={cuda12Installed:false,toolkitSupported:false,isInstalling:false,status:''};
function releaseDate(value){try{return new Date(value).toLocaleDateString()}catch{return value||''}}
function jsq(value){return String(value??'').replace(/\\/g,'\\\\').replace(/'/g,"\\'")}
function releaseBadge(asset){let type=String(asset.buildType||'').toLowerCase();let color=type.includes('cuda13')?'#a855f7':type.includes('cuda12')?'#f59e0b':type.includes('vulkan')?'#3b82f6':type.includes('openvino')?'#10b981':'#64748b';return '<span style="background:'+color+';color:white;border-radius:4px;padding:4px 7px;font-size:11px">'+esc(asset.description||asset.buildType||'CPU only')+'</span>'}
function assetInstalled(tag,asset){let name=String(asset.name||'').toLowerCase(),cuda=name.includes('cuda13')?'cuda13':(name.includes('cuda12')?'cuda12':'');let same=v=>String(v.version||'').toLowerCase()===String(tag||'').toLowerCase(),matches=v=>{let folder=String(v.folderName||'').toLowerCase();return same(v)&&(!cuda||folder.includes(cuda))};if(name.includes('-local-'))return installedLlamaState.versions.some(v=>matches(v)&&String(v.folderName||'').toLowerCase().includes('-local-sm'));if(name.includes('-universal-'))return installedLlamaState.versions.some(v=>matches(v)&&!String(v.folderName||'').toLowerCase().includes('-local-sm'));let type=String(asset.buildType||'').toLowerCase();return installedLlamaState.versions.some(v=>same(v)&&String(v.buildType||'').toLowerCase()===type)}
function renderReleaseCards(releases){let host=document.getElementById('releaseCards'),cards=[];for(const release of releases){for(const asset of (release.assets||[])){let installed=assetInstalled(release.tag,asset),assetName=String(asset.name||''),isLocalBuild=/^llama-studio-build-/i.test(assetName),cuda12=/cuda-?12|cuda12/i.test(assetName),needsToolkit=cuda12&&cudaToolkitState.toolkitSupported&&!cudaToolkitState.cuda12Installed&&!isLocalBuild,disabled=installed||llamaInstallPending||(needsToolkit&&cudaToolkitState.isInstalling),caption=installed?t('Установлено'):(needsToolkit?(cudaToolkitState.isInstalling?t('Установка Toolkit…'):t('Установить CUDA 12 Toolkit')):t('Установить')),action=needsToolkit?'installCuda12Toolkit()':'installLlamaBuild(\''+jsq(release.tag)+'\',\''+jsq(asset.name)+'\')';cards.push('<div class="card" style="margin:8px 0"><div class="row"><b>'+esc(release.tag)+'</b><span class="muted">'+releaseDate(release.publishedAt)+'</span>'+releaseBadge(asset)+'<span class="muted">'+esc(asset.sizeDisplay||((Number(asset.size||0)/1048576).toFixed(1)+' MB'))+'</span><button class="'+(installed?'secondary':'green')+'" data-tt="tt.install_build" '+(disabled?'disabled':'')+' onclick="'+action+'">'+caption+'</button></div><div class="muted" style="margin-top:8px;font-size:12px">'+esc(asset.name)+'</div>'+(needsToolkit?'<div class="muted" style="margin-top:7px;color:#fbbf24">Для этой сборки нужен CUDA Toolkit 12.x. Установщик добавит только Toolkit 12.8 и не меняет драйвер NVIDIA.</div>':'')+'</div>')}}host.innerHTML=cards.join('')||'<div class="muted">Релизы не найдены</div>'}
async function loadCudaToolkitState(){try{cudaToolkitState=await json('/api/cuda/toolkit/status')}catch{cudaToolkitState={cuda12Installed:false,toolkitSupported:false,isInstalling:false,status:t('Не удалось проверить CUDA Toolkit')}}return cudaToolkitState}
async function loadLlamaDirectory(){let data=await json('/api/llama/installed');installedLlamaState=data;await loadCudaToolkitState();let input=document.getElementById('llamaBaseDirectory');if(input)input.value=data.baseDirectory||'';return data}
async function loadInstalledLlama(){let data=await loadLlamaDirectory(),title=document.getElementById('installedLlamaTitle'),host=document.getElementById('installedLlamaList');if(title)title.textContent=data.active?t('Активна: ')+data.active:t('Активная версия не выбрана');let html=(data.versions||[]).map(v=>'<div class="card" style="margin:8px 0"><div class="row"><b>'+esc(v.version||v.folderName)+'</b>'+releaseBadge({description:v.buildType||'CPU only',buildType:v.buildType})+(v.isActive?'<span class="tag green">Активна</span>':'')+'</div><div class="muted" style="margin:7px 0">'+esc(v.directory||v.folderName||'')+'</div>'+(v.versionDetail?'<div class="muted" style="margin:-2px 0 7px;font-size:11px" title="'+jsq(v.versionDetail||'')+'">'+esc(v.versionDetail)+'</div>':'')+'<div class="actions">'+(v.isActive?'<button class="secondary" disabled>Активная</button>':'<button onclick="useLlamaVersion(\''+jsq(v.versionKey||'')+'\',\''+jsq(v.folderName||'')+'\')">Использовать</button><button class="stop" onclick="removeLlamaVersion(\''+jsq(v.folderName||'')+'\')">Удалить</button>')+'</div></div>').join('');if(host)host.innerHTML=html||'<div class="muted">Установленные версии не найдены</div>';return data}
async function checkLlamaReleases(){let status=document.getElementById('releaseStatus'),button=document.getElementById('releaseCheckButton');if(status)status.textContent=t('Проверка обновлений…');if(button)button.disabled=true;try{await loadInstalledLlama();let only=document.getElementById('releaseLatestOnly')?.checked?'true':'false';let releases=await json('/api/llama/releases?latestOnly='+only);renderReleaseCards(releases);if(status)status.textContent=releases.length?t('Последняя: ')+releases[0].tag:t('Релизы не найдены')}catch(error){if(status)status.textContent=t('Ошибка: ')+error.message}finally{if(button)button.disabled=false}}
async function loadReleasePage(){await loadInstalledLlama();await checkLlamaReleases()}
async function loadReleases(){return loadReleasePage()}
function showReleaseTab(tab){let official=tab==='official';document.getElementById('releaseOfficial').style.display=official?'block':'none';document.getElementById('releaseExperimental').style.display=official?'none':'block';document.getElementById('officialReleaseTab').classList.toggle('active',official);document.getElementById('experimentalReleaseTab').classList.toggle('active',!official);if(!official&&experimentalReleases.length===0)loadExperimentalReleases()}
async function browseLlamaDirectory(){let data=await json('/api/llama/browse');if(data.path){let input=document.getElementById('llamaBaseDirectory');if(input)input.value=data.path;await setLlamaDirectory(data.path)}}
async function setLlamaDirectory(value){let message=document.getElementById('releaseDirectoryMessage'),response=await fetch('/api/llama/set-directory?path='+encodeURIComponent(value));if(message)message.textContent=response.ok?t('Папка сохранена'):t('Ошибка: ')+await response.text();if(response.ok)await loadReleasePage()}
let releasePollTimer=null,llamaInstallStartedAt=0,llamaInstallPending=false,llamaDesktopActive=false;
function llamaElapsed(){let seconds=Math.max(0,Math.floor((Date.now()-llamaInstallStartedAt)/1000));return Math.floor(seconds/60)+':'+String(seconds%60).padStart(2,'0')}
async function pollLlamaInstall(){let data=await json('/api/llama/status'),active=!!data.isDownloading,box=document.getElementById('releaseProgress'),bar=document.getElementById('releaseProgressBar'),value=document.getElementById('releaseProgressValue'),progressStatus=document.getElementById('releaseProgressStatus');if(box)box.style.display=active?'block':'none';if(bar)bar.style.width=active?'100%':Math.max(0,Math.min(100,Number(data.progress)||0))+'%';if(value)value.textContent=active?t('Выполняется · ')+llamaElapsed():(Number(data.progress)||0).toFixed(0)+'%';if(progressStatus)progressStatus.textContent=(data.status||t('Подготовка'))+(active?t(' · прошло ')+llamaElapsed():'');if(active){releasePollTimer=setTimeout(pollLlamaInstall,700)}else{llamaInstallPending=false;let finalStatus=data.status||t('Установка завершена'),failed=!!data.isError;await loadInstalledLlama();await checkLlamaReleases();if(failed&&box){box.style.display='block';if(bar)bar.style.width='100%';if(bar)bar.style.background='#dc2626';if(value)value.textContent=t('Не выполнено');if(progressStatus)progressStatus.textContent=finalStatus}let status=document.getElementById('releaseStatus');if(status){status.textContent=finalStatus;status.style.color=failed?'#fca5a5':''}}}
async function pollLlamaGlobal(){if(llamaInstallPending)return;let data;try{data=await json('/api/llama/status')}catch(e){return}let active=!!data.isDownloading,box=document.getElementById('releaseProgress'),bar=document.getElementById('releaseProgressBar'),value=document.getElementById('releaseProgressValue'),progressStatus=document.getElementById('releaseProgressStatus'),statusEl=document.getElementById('releaseStatus');let text=data.status||data.detailStatus||'';if(active){if(!llamaDesktopActive){llamaDesktopActive=true;llamaInstallStartedAt=Date.now()}if(box)box.style.display='block';if(bar){bar.style.background='';bar.style.width='100%'}if(value)value.textContent=t('Выполняется · ')+llamaElapsed();if(progressStatus)progressStatus.textContent=(text||t('Подготовка'))+t(' · прошло ')+llamaElapsed();let info=document.getElementById('homeReleaseInfo');if(info)info.textContent=t('Установка: ')+(text||'')+' '+(Number(data.progress)||0).toFixed(0)+'%'}else if(data.isError){if(box){box.style.display='block';if(bar){bar.style.width='100%';bar.style.background='#dc2626'}if(value)value.textContent=t('Не выполнено');if(progressStatus)progressStatus.textContent=text||t('Ошибка установки')}}else if(llamaDesktopActive){llamaDesktopActive=false;let failed=!!data.isError;try{await loadInstalledLlama();await checkLlamaReleases()}catch(e){}if(box){if(failed){box.style.display='block';if(bar){bar.style.width='100%';bar.style.background='#dc2626'}if(value)value.textContent=t('Не выполнено');if(progressStatus)progressStatus.textContent=text||t('Ошибка установки')}else{box.style.display='none'}}if(statusEl){statusEl.textContent=text||t('Установка завершена');statusEl.style.color=failed?'#fca5a5':''}try{loadHomeReleases()}catch(e){}}}
async function installLlamaBuild(version,asset,repo=''){if(llamaInstallPending)return;llamaInstallPending=true;llamaInstallStartedAt=Date.now();document.querySelectorAll('#releaseCards button').forEach(b=>b.disabled=true);let status=document.getElementById('releaseStatus');if(status)status.textContent=t('Запуск установки ')+version+'…';let nccl=document.getElementById('releaseNccl')?.checked?'true':'false';try{let response=await fetch('/api/llama/install?version='+encodeURIComponent(version)+'&asset='+encodeURIComponent(asset)+(repo?'&repo='+encodeURIComponent(repo):'')+'&nccl='+nccl);if(!response.ok){llamaInstallPending=false;if(status)status.textContent=t('Ошибка запуска: ')+await response.text();await checkLlamaReleases();return}await pollLlamaInstall()}catch(error){llamaInstallPending=false;if(status)status.textContent=t('Ошибка запуска: ')+error.message;await checkLlamaReleases()}}
async function installCuda12Toolkit(){if(!confirm(t('Установить официальный NVIDIA CUDA Toolkit 12.8? Будет загружено около 5 ГБ. Драйвер NVIDIA не будет заменён.')))return;let status=document.getElementById('releaseStatus');if(status)status.textContent=t('Запуск установщика CUDA Toolkit 12.8…');let r=await fetch('/api/cuda/toolkit/install');if(!r.ok){if(status)status.textContent=t('Ошибка запуска Toolkit: ')+await r.text();return}let poll=async()=>{let s=await loadCudaToolkitState();if(status)status.textContent=s.status||t('Установка CUDA Toolkit…');if(s.isInstalling){setTimeout(poll,1000);return}await checkLlamaReleases()};await poll()}
async function cancelLlamaInstall(){await fetch('/api/llama/cancel');await pollLlamaInstall()}
async function useLlamaVersion(key,folder){let status=document.getElementById('releaseStatus');if(status)status.textContent=t('Переключение версии…');let response=await fetch('/api/llama/use?versionKey='+encodeURIComponent(key)+'&folder='+encodeURIComponent(folder)),data=await response.json().catch(()=>({}));if(status)status.textContent=response.ok?t('Версия активирована')+(data.restarted?t(' и сервер перезапущен'):''):t('Ошибка: ')+(data.error||response.status);await loadReleasePage()}
async function removeLlamaVersion(folder){if(!confirm(t('Удалить установленную версию llama.cpp?')))return;let response=await fetch('/api/llama/remove?folder='+encodeURIComponent(folder)),data=await response.json().catch(()=>({}));let status=document.getElementById('releaseStatus');if(status)status.textContent=response.ok?t('Версия удалена'):t('Ошибка удаления: ')+(data.error||response.status);await loadReleasePage()}
async function loadExperimentalReleases(){let repo=document.getElementById('experimentalRepo')?.value||'',status=document.getElementById('experimentalStatus');if(status)status.textContent=t('Загрузка…');try{experimentalReleases=await json('/api/llama/releases?repo='+encodeURIComponent(repo)+'&latestOnly=false');let select=document.getElementById('experimentalRelease');if(select)select.innerHTML=experimentalReleases.map((release,index)=>'<option value="'+index+'">'+esc(release.tag)+'</option>').join('');renderExperimentalAssets();if(status)status.textContent=experimentalReleases.length?t('Последняя: ')+experimentalReleases[0].tag:t('Релизы не найдены')}catch(error){if(status)status.textContent=t('Ошибка: ')+error.message}}
function renderExperimentalAssets(){let release=experimentalReleases[Number(document.getElementById('experimentalRelease')?.value||0)],select=document.getElementById('experimentalAsset');if(select)select.innerHTML=(release?.assets||[]).map(asset=>'<option value="'+esc(asset.name)+'">'+esc(asset.name)+' — '+esc(asset.sizeDisplay||'')+'</option>').join('')}
async function installExperimentalWeb(){let release=experimentalReleases[Number(document.getElementById('experimentalRelease')?.value||0)],asset=document.getElementById('experimentalAsset')?.value,repo=document.getElementById('experimentalRepo')?.value||'';if(release&&asset)await installLlamaBuild(release.tag,asset,repo)}
	setTimeout(loadChatLink,0);setInterval(loadChatLink,4000);</script></body></html>
""".Replace("</style>", ".release-columns{display:grid;grid-template-columns:minmax(0,1.35fr) minmax(300px,.9fr);gap:14px;align-items:start}@media(max-width:1100px){.release-columns{grid-template-columns:1fr}}</style>");

	private const string Html = """
<!doctype html><html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Llama Studio</title>
<style>body{font:16px system-ui;background:#111827;color:#e5e7eb;margin:0} .app{display:flex;min-height:100vh}.side{width:220px;background:#0b1220;padding:22px 14px;box-sizing:border-box}.side h1{font-size:20px;color:#93c5fd;margin:0 0 24px}.nav{display:block;width:100%;text-align:left;background:transparent;border:1px solid transparent;margin:3px 0}.nav.active,.nav:hover{background:#263b5c;border-color:#3b82f6}.content{flex:1;padding:28px;max-width:1100px}section{background:#1f2937;border:1px solid #374151;border-radius:10px;padding:22px;margin:16px 0}h2{margin-top:0;color:#dbeafe}pre{background:#111827;padding:16px;border-radius:8px;overflow:auto}.ok{color:#34d399}.bad{color:#f87171}button,select{font:inherit;padding:9px 14px;border:0;border-radius:6px;margin:4px;background:#2563eb;color:white;cursor:pointer}button.stop{background:#dc2626}.row{display:flex;gap:10px;align-items:center;flex-wrap:wrap}.card{background:#111827;border:1px solid #374151;border-radius:8px;padding:13px;margin:8px 0}.page{display:none}.page.active{display:block} [data-tip]{position:relative}[data-tip]:hover::after{content:attr(data-tip);position:absolute;left:0;bottom:calc(100% + 6px);background:#111827;color:#f9fafb;border:1px solid #4b5563;border-radius:6px;padding:8px 10px;font-size:12px;line-height:1.45;white-space:pre-line;width:max-content;max-width:340px;z-index:10000;pointer-events:none;box-shadow:0 8px 24px rgba(0,0,0,.45)}[data-tip]:hover::before{content:'';position:absolute;left:14px;bottom:calc(100% + 1px);border:5px solid transparent;border-top-color:#4b5563;z-index:10000;pointer-events:none} .ngl-wrap{display:flex;gap:8px;align-items:center}</style></head>
<body><div class="app"><aside class="side"><h1>Llama Studio</h1><button class="nav active" data-page="home">Главная</button><button class="nav" data-page="chat">Чат</button><button class="nav" data-page="models">Модели</button><button class="nav" data-page="monitoring">Мониторинг</button><button class="nav" data-page="server">Сервер</button><button class="nav" data-page="rpc">Удалённый RPC</button><button class="nav" data-page="logs">Логи</button><button class="nav" data-page="settings">Настройки</button></aside><main class="content"><div id="home" class="page active"><h1>Главная</h1><section><h2>Состояние Studio</h2><div id="status">Загрузка…</div><pre id="details"></pre></section><section><h2>GPU-мониторинг</h2><div id="gpu">Загрузка…</div></section></div><div id="server" class="page"><h1>Сервер</h1><section><h2>Управление llama-server</h2><div class="row"><select id="profiles" onchange="profileChanged()"></select><button onclick="startServer()">Запустить</button><button class="stop" onclick="stopServer()">Остановить</button><button onclick="restartServer()">Перезапустить</button></div><div id="message"></div></section><section><h2>Профиль</h2><pre id="profile">Загрузка…</pre></section></div><div id="monitoring" class="page"><h1>Мониторинг</h1><section><h2>Локальные и удалённые GPU</h2><div id="gpu2">Загрузка…</div></section></div><div id="chat" class="page"><h1>Чат</h1><section><textarea id="prompt" style="width:100%;min-height:120px;background:#111827;color:#e5e7eb;border:1px solid #374151;border-radius:8px;padding:12px" placeholder=t("Сообщение пока отправляется через desktop API…")></textarea><p>Чат подключается к llama-server выбранного профиля.</p></section></div><div id="models" class="page"><h1>Модели</h1><section><h2>Найденные GGUF-модели</h2><div id="modelsList">Загрузка…</div></section></div><div id="rpc" class="page"><h1>Удалённый RPC</h1><section><h2>Параметры выбранного профиля</h2><pre id="rpcInfo">Загрузка…</pre><p>SSH-подключение и управление RPC используют те же параметры профиля, что и desktop Studio.</p></section></div><div id="logs" class="page"><h1>Логи</h1><section><pre id="logsList">Загрузка…</pre></section></div><div id="settings" class="page"><h1>Настройки</h1><section><h2>Глобальные настройки</h2><pre id="settingsInfo">Загрузка…</pre></section></div></main></div><script>
async function profiles(){let r=await fetch('/api/profiles');let p=await r.json();document.getElementById('profiles').innerHTML=p.map(x=>'<option value="'+x.id+'">'+x.name+'</option>').join('')}
async function action(path){document.getElementById('message').textContent=t('Выполняется…');let r=await fetch(path+'?id='+encodeURIComponent(document.getElementById('profiles').value));document.getElementById('message').textContent=r.ok?t('Готово'):await r.text();refresh()}
function startServer(){action('/api/server/start')} function stopServer(){action('/api/server/stop')} function restartServer(){action('/api/server/restart')}
async function profileChanged(){let r=await fetch('/api/status');let x=await r.json();await profileInfo();await profileForm();if(x.llamaServer==='Running')await action('/api/server/start')}
async function profileInfo(){let r=await fetch('/api/profile?id='+encodeURIComponent(document.getElementById('profiles').value));document.getElementById('profile').textContent=JSON.stringify(await r.json(),null,2)}
function initControls(){let s=document.querySelector('#server section:nth-of-type(2)');if(s&&!document.getElementById('contextSize'))s.innerHTML='<h2>Параметры профиля</h2><div class="row"><label>Контекст <input id="contextSize" type="number"></label><label>Temperature <input id="temperature" type="number" step="0.01"></label><label>Top K <input id="topK" type="number"></label><label>Top P <input id="topP" type="number" step="0.01"></label><label>Min P <input id="minP" type="number" step="0.01"></label></div><button onclick="saveProfile()">Сохранить профиль</button><pre id="profile">Загрузка…</pre>';let c=document.querySelector('#chat section');if(c&&!document.getElementById('sendChatButton')){c.innerHTML='<textarea id="prompt" style="width:100%;min-height:120px;background:#111827;color:#e5e7eb;border:1px solid #374151;border-radius:8px;padding:12px" placeholder="Введите сообщение"></textarea><br><button id="sendChatButton" onclick="sendChat()">Отправить</button><pre id="answer"></pre>'}let r=document.querySelector('#rpc section');if(r&&!document.getElementById('rpcSave')){r.innerHTML='<h2>Удалённый RPC</h2><div class="row"><label>RPC endpoint <input id="rpcEndpoint" size="28"></label><label>Путь ggml-rpc-server.exe <input id="rpcServerPath" size="50"></label></div><label><input id="rpcEnabled" type="checkbox"> Использовать удалённый RPC</label><label><input id="rpcUseCache" type="checkbox"> Использовать кэш</label><label><input id="rpcRemoteTelemetry" type="checkbox" checked> Передавать телеметрию</label><br><button id="rpcSave" onclick="saveRpc()">Сохранить RPC</button><pre id="rpcInfo">Загрузка…</pre>'}}
async function profileForm(){let p=await (await fetch('/api/profile?id='+encodeURIComponent(document.getElementById('profiles').value))).json();for(let k of ['contextSize','temperature','topK','topP','minP']){let e=document.getElementById(k);if(e)e.value=p[k]??''}}
async function rpcForm(){let p=await (await fetch('/api/profile?id='+encodeURIComponent(profiles.value))).json();rpcEndpoint.value=p.rpcEndpoint||'';rpcServerPath.value=p.rpcServerPath||'';rpcEnabled.checked=!!p.rpcEnabled;rpcUseCache.checked=!!p.rpcUseCache;rpcRemoteTelemetry.checked=!!p.rpcRemoteTelemetry}
async function saveRpc(){let q=new URLSearchParams({id:profiles.value,rpcEndpoint:rpcEndpoint.value,rpcServerPath:rpcServerPath.value,rpcEnabled:rpcEnabled.checked,rpcUseCache:rpcUseCache.checked,rpcRemoteTelemetry:rpcRemoteTelemetry.checked});let r=await fetch('/api/profile/save?'+q);message.textContent=r.ok?t('RPC сохранён. Для применения перезапустите сервер.'):await r.text();loadExtra()}
async function saveWebSettings(){let q=new URLSearchParams({webEnabled:webEnabled.checked,webHost:webHost.value,webPort:webPort.value,loginRequired:loginRequired.checked,username:webUsername.value,password:webPassword.value});let r=await fetch('/api/settings/save?'+q);settingsInfo.textContent=r.ok?t('Настройки сохранены. Перезапустите Studio для смены адреса или порта.'):await r.text()}
async function saveProfile(){let q=new URLSearchParams({id:profiles.value,contextSize:contextSize.value,temperature:temperature.value,topK:topK.value,topP:topP.value,minP:minP.value});let r=await fetch('/api/profile/save?'+q);message.textContent=r.ok?t('Профиль сохранён. Для применения перезапустите сервер.'):await r.text();profileInfo()}
async function sendChat(){let msg=prompt.value.trim();if(!msg)return;answer.textContent=t('Генерация…');let r=await fetch('/api/chat?id='+encodeURIComponent(profiles.value)+'&message='+encodeURIComponent(msg));answer.textContent=r.ok?(await r.json()).answer:await r.text()}
async function loadExtra(){try{let id=encodeURIComponent(document.getElementById('profiles').value);let p=await (await fetch('/api/profile?id='+id)).json();document.getElementById('rpcInfo').textContent=JSON.stringify({rpcEnabled:p.rpcEnabled,rpcEndpoint:p.rpcEndpoint,rpcRemoteTelemetry:p.rpcRemoteTelemetry,rpcUseCache:p.rpcUseCache,rpcServerPath:p.rpcServerPath},null,2);rpcForm();let m=await (await fetch('/api/models')).json();document.getElementById('modelsList').innerHTML=m.map(x=>'<div class="card"><b>'+x.name+'</b><br>'+x.sizeGb+' GB<br><small>'+x.path+'</small></div>').join('')||t('Модели не найдены');let s=await (await fetch('/api/settings')).json();document.getElementById('settingsInfo').textContent=JSON.stringify(s,null,2);if(document.getElementById('webHost')){webHost.value=s.webHost;webPort.value=s.webPort;webEnabled.checked=!!s.webEnabled;loginRequired.checked=!!s.loginRequired}let l=await (await fetch('/api/logs')).json();document.getElementById('logsList').textContent=l.map(x=>JSON.stringify(x)).join('\n')||t('Логи отсутствуют')}catch(e){}}
async function gpuInfo(){let r=await fetch('/api/gpu');let g=await r.json();let html=g.map(x=>'<div class="card"><b>'+x.displayName+'</b><br>VRAM: '+x.memoryUsedGb.toFixed(1)+' / '+x.memoryTotalGb.toFixed(1)+' GB · GPU: '+x.gpuUtilization.toFixed(0)+t('% · Температура: ')+x.temperatureCelsius.toFixed(0)+t('°C')+(x.memoryTemperatureCelsius>0?t(' · Темп. памяти: ')+x.memoryTemperatureCelsius.toFixed(0)+t('°C'):'')+t(' · Мощность: ')+x.powerDrawWatts.toFixed(0)+' W</div>').join('')||t('Данные GPU недоступны');document.getElementById('gpu').innerHTML=html;document.getElementById('gpu2').innerHTML=html}
async function refresh(){try{let r=await fetch('/api/status');let x=await r.json();document.getElementById('status').innerHTML='<span class="'+(x.llamaServer==='Running'?'ok':'bad')+'">'+x.llamaServer+'</span>';document.getElementById('details').textContent=JSON.stringify(x,null,2);gpuInfo()}catch(e){document.getElementById('status').textContent=t('Не удалось получить состояние')}}
document.querySelectorAll('.nav').forEach(b=>b.onclick=()=>{document.querySelectorAll('.nav,.page').forEach(x=>x.classList.remove('active'));b.classList.add('active');document.getElementById(b.dataset.page).classList.add('active');loadExtra()});
initControls();profiles().then(()=>{profileInfo();profileForm();loadExtra()});refresh();setInterval(refresh,3000);
</script></body></html>
""";
}
