  using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
  using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

public partial class ModelsViewModel : ObservableObject
{
    readonly IModelScanner _scanner;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly IProfileManager _profileManager;
    readonly ILocalizationService _loc;
    readonly IHuggingFaceDownloader _hfDownloader;

    [ObservableProperty] ObservableCollection<GgufModelInfo> _models = new();
    [ObservableProperty] GgufModelInfo? _selectedModel;
    [ObservableProperty] string _modelsDirectory;
    [ObservableProperty] bool _isScanning;
    [ObservableProperty] string _scanStatus = "Ready";
    [ObservableProperty] string _searchFilter = "";
    [ObservableProperty] int _filteredCount;

    // Server model paths (from default profile)
    [ObservableProperty] string _serverModelPath = string.Empty;
    [ObservableProperty] string _serverMmprojPath = string.Empty;

    // --- HuggingFace section ---
    [ObservableProperty] string _hfUrl = "";
    [ObservableProperty] string _hfAuthToken = "";
    [ObservableProperty] string _hfDownloadDir = "";
    [ObservableProperty] bool _isHfLoading;
    [ObservableProperty] bool _isHfDownloading;
    [ObservableProperty] double _hfOverallProgress;
    [ObservableProperty] string _hfStatus = "";
    [ObservableProperty] ObservableCollection<HfFileInfo> _hfFiles = new();
    [ObservableProperty] int _hfFilesCount;

    // Translated strings
    public string Title => _loc.T("models.title");
    public string CountLabel => _loc.T("models.count");
    public string ScanBtn => _loc.T("models.scan");
    public string ScanningStatus => _loc.T("models.scanning");
    public string BrowseBtn => _loc.T("models.browse");
    public string SearchPlaceholder => _loc.T("models.search");
    public string NoModelsText => _loc.T("models.no_models");
    public string ToServerModel => _loc.T("models.to_server");
    public string ToMmproj => _loc.T("models.to_mmproj");
    public string Connected => _loc.T("models.connected");

    // HuggingFace translations
    public string HfTitle => _loc.T("hf.title");
    public string HfUrlPlaceholder => _loc.T("hf.url_placeholder");
    public string HfAuthTokenPlaceholder => _loc.T("hf.auth_token_placeholder");
    public string HfBrowseBtn => _loc.T("hf.browse");
    public string HfFetchBtn => _loc.T("hf.fetch");
    public string HfDownloadSelectedBtn => _loc.T("hf.download_selected");
    public string HfCancelBtn => _loc.T("hf.cancel");
    public string HfSelectAllBtn => _loc.T("hf.select_all");
    public string HfDeselectAllBtn => _loc.T("hf.deselect_all");
    public string HfLoginBtn => _loc.T("hf.login");
    public string HfSelectDirLabel => _loc.T("hf.select_dir");
    public string HfFilesFound => _loc.T("hf.files_found");

    public ModelsViewModel(IModelScanner scanner, ISettings settings, ILogService log, IDialogService dialog, IProfileManager profileManager, ILocalizationService loc, IHuggingFaceDownloader hfDownloader)
    {
        _scanner = scanner;
        _settings = settings;
        _log = log;
        _dialog = dialog;
        _profileManager = profileManager;
        _loc = loc;
        _hfDownloader = hfDownloader;
        ModelsDirectory = _settings.ModelsDirectory;
        HfDownloadDir = _settings.ModelsDirectory;
        _ = LoadHfUrlFromProfileAsync();

        // Subscribe to HF downloader events
        _hfDownloader.StatusMessage += (_, msg) => HfStatus = msg;
        _hfDownloader.DownloadProgress += (_, progress) => HfOverallProgress = progress;
        _hfDownloader.AllDownloadsCompleted += async (_, _) =>
        {
            IsHfDownloading = false;
            HfStatus = _loc.T("models.hf_status_download_complete");
            ModelsDirectory = HfDownloadDir;
            await ScanModels();
        };

        // Subscribe to profile changes — reload HfUrl and refresh server paths
        _profileManager.ProfileChanged += OnProfileChanged;
    }

   void OnProfileChanged(string? profileId)
    {
        _ = LoadHfUrlFromProfileAsync();
        _ = RefreshServerPathsAsync();
    }

    async Task LoadHfUrlFromProfileAsync()
    {
        try
        {
            // Load from currently selected profile, fallback to default
            ServerProfile? profile = null;
            if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            {
                profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
            }
            profile ??= await _profileManager.GetDefaultProfileAsync();

            if (!string.IsNullOrEmpty(profile?.HfUrl))
                HfUrl = profile.HfUrl;
            else
                HfUrl = string.Empty;
        }
        catch { }
    }

    // --- HuggingFace commands ---

    [RelayCommand]
    async Task HfLogin()
    {
        await _hfDownloader.LoginViaBrowserAsync();
        HfStatus = _loc.T("models.hf_status_token_instructions");
    }

    [RelayCommand]
    async Task HfSaveToken()
    {
        _hfDownloader.SetAuthToken(string.IsNullOrWhiteSpace(HfAuthToken) ? null : HfAuthToken);
        if (string.IsNullOrWhiteSpace(HfAuthToken))
          HfStatus = _loc.T("models.hf_status_token_cleared");
            else
            HfStatus = _loc.T("models.hf_status_token_saved");
    }

     [RelayCommand]
    async Task HfBrowseDirectory()
    {
        var path = await _dialog.SelectFolderAsync(
            "Select Download Directory",
            HfDownloadDir);

        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            HfDownloadDir = path;
            _settings.ModelsDirectory = path;
            ModelsDirectory = path;
            await _settings.SaveAsync();
        }
    }

    [RelayCommand]
    async Task HfFetchRepo()
    {
        var url = HfUrl.Trim();

        if (string.IsNullOrWhiteSpace(url))
        {
            HfStatus = _loc.T("models.hf_status_enter_repo");
            return;
        }

        IsHfLoading = true;
        HfStatus = _loc.T("models.hf_status_fetching");
        HfFiles.Clear();
        HfFilesCount = 0;

        try
        {
            var files = await _hfDownloader.ListRepoFilesAsync(url);
            var downloadDir = HfDownloadDir.Trim();

            foreach (var file in files)
            {
                // Quick local file check
                if (!string.IsNullOrEmpty(downloadDir) && Directory.Exists(downloadDir))
                {
                    var localPath = Path.Combine(downloadDir, file.FileName);
                    if (File.Exists(localPath))
                    {
                        var localSize = new FileInfo(localPath).Length;
                        if (localSize > 0)
                        {
                            file.Status = _loc.T("models.hf_status_already_downloaded");
                            file.IsCompleted = true;
                            file.DownloadProgress = 100;
                            file.LocalPath = localPath;
                        }
                    }
                }
                HfFiles.Add(file);
            }

            HfFilesCount = HfFiles.Count;
            int alreadyDownloaded = HfFiles.Count(f => f.IsCompleted);
            if (HfFilesCount > 0)
            {
                HfStatus = alreadyDownloaded > 0
                    ? $"{HfFilesCount} files found ({alreadyDownloaded} already downloaded)"
                    : $"Found {HfFilesCount} model files";
            }
            else
            {
                HfStatus = _loc.T("models.hf_status_no_gguf");
            }
        }
        catch (Exception ex)
        {
            HfStatus = string.Format(_loc.T("models.hf_status_error"), ex.Message);
            _log.Error(ex, "HF fetch failed", "Models");
        }
        finally
        {
            IsHfLoading = false;
        }
    }

    [RelayCommand]
    void HfSelectAll()
    {
        foreach (var f in HfFiles)
            f.IsSelected = true;
        HfFilesCount = HfFiles.Count;
    }

    [RelayCommand]
    void HfDeselectAll()
    {
        foreach (var f in HfFiles)
            f.IsSelected = false;
        HfFilesCount = 0;
    }

    [RelayCommand]
    async Task HfDownloadSelected()
    {
        var selected = HfFiles.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0)
        {
            HfStatus = _loc.T("models.hf_status_no_files_selected");
            return;
        }

        if (string.IsNullOrWhiteSpace(HfDownloadDir) || !Directory.Exists(HfDownloadDir))
        {
            HfStatus = _loc.T("models.hf_status_select_dir_first");
            return;
        }

        IsHfDownloading = true;
        HfOverallProgress = 0;
        HfStatus = string.Format(_loc.T("models.hf_status_downloading"), selected.Count);

        try
        {
            await _hfDownloader.DownloadSelectedAsync(selected, HfDownloadDir);
        }
        catch (Exception ex)
        {
            HfStatus = string.Format(_loc.T("models.hf_status_error"), ex.Message);
            _log.Error(ex, "HF download failed", "Models");
            IsHfDownloading = false;
        }
    }

    [RelayCommand]
    void HfCancelDownloads()
    {
        _hfDownloader.CancelDownloads();
        IsHfDownloading = false;
        HfStatus = _loc.T("models.hf_status_cancelled");
    }

    [RelayCommand]
    async Task ShowHfFileDetails(HfFileInfo file)
    {
        // Toggle visibility
        if (file.ShowDetails && file.Metadata.Count > 0)
        {
            file.ShowDetails = false;
            return;
        }

        file.ShowDetails = true;

        // Load metadata if not already loaded
        if (file.Metadata.Count == 0)
        {
            await _hfDownloader.LoadFileMetadataAsync(file);
        }
    }

    // --- Local models ---

    [RelayCommand]
    internal async Task ScanModels()
    {
        if (string.IsNullOrWhiteSpace(ModelsDirectory) || !Directory.Exists(ModelsDirectory))
        {
            ScanStatus = _loc.T("models.scan_status_invalid_dir");
            return;
        }

        IsScanning = true;
          ScanStatus = _loc.T("models.scan_status_scanning");
        Models.Clear();

        try
        {
            var results = await _scanner.ScanDirectoryAsync(ModelsDirectory);

            foreach (var model in results)
                Models.Add(model);

            FilteredCount = Models.Count;
            ScanStatus = string.Format(_loc.T("models.scan_status_found"), Models.Count);
            _log.Information($"Scan complete: {Models.Count} models", "Models");
            await RefreshServerPathsAsync();
        }
        catch (Exception ex)
        {
            ScanStatus = string.Format(_loc.T("models.hf_status_error"), ex.Message);
            _log.Error(ex, "Scan failed", "Models");
        }
        finally
        {
            IsScanning = false;
        }
    }

    partial void OnHfUrlChanged(string value)
    {
        _ = SaveHfUrlToProfileAsync(value);
    }

    async Task SaveHfUrlToProfileAsync(string url)
    {
        try
        {
            var profile = await GetActiveProfileAsync();
            if (profile != null)
            {
                profile.HfUrl = url;
                await _profileManager.SaveProfileAsync(profile);
            }
        }
        catch { }
    }

    partial void OnSearchFilterChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredCount = Models.Count;
            return;
        }

        var filter = value.ToLower();
        FilteredCount = Models.Count(m =>
            m.FileName.ToLower().Contains(filter) ||
            m.Architecture.ToLower().Contains(filter) ||
            m.QuantizationTag.ToLower().Contains(filter));
    }

    [RelayCommand]
    async Task SelectDirectory()
    {
        var path = await _dialog.SelectFolderAsync(
            "Select Models Directory",
            ModelsDirectory);

        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            ModelsDirectory = path;
            HfDownloadDir = path;
            _settings.ModelsDirectory = path;
            await _settings.SaveAsync();
            ScanStatus = string.Format(_loc.T("models.scan_status_directory"), path);
            await ScanModels();
        }
    }

    [RelayCommand]
    async Task UseInServer(GgufModelInfo? model)
    {
        if (model == null) return;
        var profile = await GetActiveProfileAsync();
        if (profile == null)
        {
            _log.Warning("No profile selected. Create a profile first.", "Models");
            return;
        }
        profile.ModelPath = model.Path;
        await _profileManager.SaveProfileAsync(profile);
        ServerModelPath = model.Path;
        await RefreshServerPathsAsync();
          ScanStatus = string.Format(_loc.T("models.scan_status_set_main"), model.FileName);
    }

    [RelayCommand]
    async Task UseAsMmproj(GgufModelInfo? model)
    {
        if (model == null) return;
        var profile = await GetActiveProfileAsync();
        if (profile == null)
        {
            _log.Warning("No profile selected. Create a profile first.", "Models");
            return;
        }
        profile.MmprojPath = model.Path;
        await _profileManager.SaveProfileAsync(profile);
        ServerMmprojPath = model.Path;
        await RefreshServerPathsAsync();
          ScanStatus = string.Format(_loc.T("models.scan_status_set_mmproj"), model.FileName);
    }

    async Task<ServerProfile?> GetActiveProfileAsync()
    {
        ServerProfile? profile = null;
        if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
            profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
        profile ??= await _profileManager.GetDefaultProfileAsync();
        return profile;
    }

    async Task RefreshServerPathsAsync()
    {
        try
        {
            // Use active profile (LastSelectedProfileId), fallback to default
            ServerProfile? profile = null;
            if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
                profile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
            profile ??= await _profileManager.GetDefaultProfileAsync();

            var serverModelPath = profile?.ModelPath ?? string.Empty;
            var serverMmprojPath = profile?.MmprojPath ?? string.Empty;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ServerModelPath = serverModelPath;
                ServerMmprojPath = serverMmprojPath;

                foreach (var model in Models)
                {
                    model.IsServerModelConnected = model.Path == ServerModelPath;
                    model.IsServerMmprojConnected = model.Path == ServerMmprojPath;
                }

                // Force UI refresh — ListBox needs collection change notification
                var temp = Models.ToList();
                Models.Clear();
                foreach (var m in temp)
                    Models.Add(m);
            });
        }
        catch
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ServerModelPath = string.Empty;
                ServerMmprojPath = string.Empty;
                foreach (var model in Models)
                {
                    model.IsServerModelConnected = false;
                    model.IsServerMmprojConnected = false;
                }
            });
        }
    }
}
