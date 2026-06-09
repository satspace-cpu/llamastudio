using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    readonly IProfileManager _profileManager;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly ILocalizationService _loc;
    readonly ISettings _settings;
    readonly ServerViewModel _serverViewModel;

    [ObservableProperty] ObservableCollection<ServerProfile> _profiles = new();
    [ObservableProperty] ServerProfile? _selectedProfile;
    [ObservableProperty] string _profileName = string.Empty;
    [ObservableProperty] string _profileDescription = string.Empty;
    [ObservableProperty] string _selectedModelPath = string.Empty;
    [ObservableProperty] bool _isCreating;

    // Translated strings
    public string Title => _loc.T("profiles.title");
    public string NewProfileBtn => _loc.T("profiles.new");
    public string ImportJsonBtn => _loc.T("profiles.import");
    public string NamePlaceholder => _loc.T("profiles.name_placeholder");
    public string DescPlaceholder => _loc.T("profiles.desc_placeholder");
    public string SelectModelPlaceholder => _loc.T("profiles.select_model");
    public string DefaultBadge => _loc.T("profiles.default_badge");
    public string SetDefaultBtn => _loc.T("profiles.set_default");
    public string EditBtn => _loc.T("profiles.edit");
    public string DuplicateBtn => _loc.T("profiles.duplicate");
    public string ExportBtn => _loc.T("profiles.export");
    public string DeleteBtn => _loc.T("profiles.delete");
    public string SaveSettingsBtn => _loc.T("profiles.save_settings");
    public string StartServerBtn => _loc.T("profiles.start_server");

    public ProfilesViewModel(IProfileManager profileManager, ILogService log, IDialogService dialog, ILocalizationService loc, ISettings settings, ServerViewModel serverViewModel)
    {
        _profileManager = profileManager;
        _log = log;
        _dialog = dialog;
        _loc = loc;
        _settings = settings;
        _serverViewModel = serverViewModel;
    }

    public async Task LoadProfilesAsync()
    {
        Profiles.Clear();
        var list = await _profileManager.GetAllProfilesAsync();
        foreach (var p in list)
            Profiles.Add(p);
    }

    partial void OnSelectedProfileChanged(ServerProfile? value)
    {
        if (value != null)
        {
            ProfileName = value.Name;
            ProfileDescription = value.Description;
            SelectedModelPath = value.ModelPath ?? string.Empty;
            _settings.LastSelectedProfileId = value.Id;
        }
    }

    [RelayCommand]
    async Task CreateProfile()
    {
        var name = await _dialog.ShowInputAsync(_loc.T("dialog.create_profile_title"), _loc.T("dialog.profile_name_prompt"), "");
        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim();

        if (Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
            return;
        }

        var profile = new ServerProfile
        {
            Name = name,
            Description = string.Empty,
            ModelPath = null,
            MmprojPath = null,
            DraftModelPath = null,
        };

        await _profileManager.SaveProfileAsync(profile);
        _settings.LastSelectedProfileId = profile.Id;
        _log.Information($"Created profile: {profile.Name}", "Profiles");
        await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.profile_created"), profile.Name));
        await LoadProfilesAsync();
        ProfileName = string.Empty;
        ProfileDescription = string.Empty;
        SelectedModelPath = string.Empty;
    }

    [RelayCommand]
    async Task EditProfile(ServerProfile profile)
    {
        var name = string.IsNullOrWhiteSpace(ProfileName) ? profile.Name : ProfileName.Trim();

        // Validate: check for duplicates (excluding current profile)
        if (Profiles.Any(p => p.Id != profile.Id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
            return;
        }

        // Validate: model file must exist if specified
        if (!string.IsNullOrEmpty(SelectedModelPath) && !File.Exists(SelectedModelPath))
        {
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.model_not_found"), SelectedModelPath));
            return;
        }

        profile.Name = name;
        profile.Description = ProfileDescription;
        profile.ModelPath = SelectedModelPath;

        await _profileManager.SaveProfileAsync(profile);
        _log.Information($"Edited profile: {profile.Name}", "Profiles");
        await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.profile_updated"), profile.Name));
        await LoadProfilesAsync();
    }

    [RelayCommand]
    async Task DeleteProfile(ServerProfile profile)
    {
        var confirmed = await _dialog.ShowConfirmationAsync(
            string.Format(_loc.T("dialog.confirm_delete_profile"), profile.Name),
            _loc.T("dialog.delete_profile_title"));

        if (!confirmed) return;

        await _profileManager.DeleteProfileAsync(profile.Id);
        _log.Information($"Deleted profile: {profile.Name}", "Profiles");
        await LoadProfilesAsync();
    }

    [RelayCommand]
    async Task DuplicateProfile(ServerProfile profile)
    {
        var copy = await _profileManager.DuplicateProfileAsync(profile.Id);
        _log.Information($"Duplicated profile: {copy.Name}", "Profiles");
        await LoadProfilesAsync();
    }

    [RelayCommand]
    async Task SetDefault(ServerProfile profile)
    {
        await _profileManager.SetDefaultProfileAsync(profile.Id);
        _log.Information($"Set default: {profile.Name}", "Profiles");
        await LoadProfilesAsync();
    }

    [RelayCommand]
    async Task ExportProfile(ServerProfile profile)
    {
        var path = await _dialog.SaveFileAsync(
            "Export Profile",
            $"{profile.Name.Replace(" ", "_")}.json",
            "JSON Files|*.json|All Files|*.*");

        if (!string.IsNullOrEmpty(path))
        {
            var json = _profileManager.ExportProfile(profile);
            await File.WriteAllTextAsync(path, json);
            _log.Information($"Exported: {profile.Name} → {path}", "Profiles");
        }
    }

    [RelayCommand]
    async Task ImportProfile()
    {
        var path = await _dialog.SelectFileAsync(
            "Import Profile JSON",
            null,
            "JSON Files|*.json|All Files|*.*");

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var profile = await _profileManager.ImportProfileAsync(json);
                _log.Information($"Imported profile: {profile.Name}", "Profiles");
                await LoadProfilesAsync();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to import profile", "Profiles");
            }
        }
    }

    [RelayCommand]
    async Task SelectModel()
    {
        var path = await _dialog.SelectFileAsync(
            "Select Model File",
            null,
            "GGUF Files|*.gguf|All Files|*.*");

        if (!string.IsNullOrEmpty(path))
        {
            SelectedModelPath = path;
            _log.Information($"Selected model: {Path.GetFileName(path)}", "Profiles");
        }
    }

    [RelayCommand]
    async Task SaveSettingsToProfile(ServerProfile profile)
    {
        _serverViewModel.SelectedProfile = profile;
        _serverViewModel.SyncSettingsToProfile();
        await _profileManager.SaveProfileAsync(profile);
        _log.Information($"Settings saved to profile: {profile.Name}", "Profiles");
        await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.settings_saved"), profile.Name));
    }

    [RelayCommand]
    async Task StartServerWithProfile(ServerProfile profile)
    {
        _serverViewModel.SelectedProfile = profile;
        _serverViewModel.SyncSettingsToProfile();
        await _profileManager.SaveProfileAsync(profile);
        await _serverViewModel.StartServerAsync();
    }
}