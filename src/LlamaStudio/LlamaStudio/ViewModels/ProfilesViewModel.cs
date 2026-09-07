using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class ProfilesViewModel : ObservableObject
{
	private readonly IProfileManager _profileManager;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly ILocalizationService _loc;

	private readonly ISettings _settings;

	private readonly ServerViewModel _serverViewModel;

	[ObservableProperty]
	private ObservableCollection<ServerProfile> _profiles = new ObservableCollection<ServerProfile>();

	[ObservableProperty]
	private ServerProfile? _selectedProfile;

	[ObservableProperty]
	private string _profileName = string.Empty;

	[ObservableProperty]
	private string _profileDescription = string.Empty;

	[ObservableProperty]
	private string _selectedModelPath = string.Empty;

	[ObservableProperty]
	private bool _isCreating;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? createProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? editProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? deleteProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? duplicateProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? setDefaultCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? exportProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? importProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? saveSettingsToProfileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ServerProfile>? startServerWithProfileCommand;

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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ServerProfile> Profiles
	{
		get
		{
			return _profiles;
		}
		[MemberNotNull("_profiles")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ServerProfile>>.Default.Equals(_profiles, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Profiles);
				_profiles = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Profiles);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerProfile? SelectedProfile
	{
		get
		{
			return _selectedProfile;
		}
		set
		{
			if (!EqualityComparer<ServerProfile>.Default.Equals(_selectedProfile, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedProfile);
				_selectedProfile = value;
				OnSelectedProfileChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedProfile);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProfileName
	{
		get
		{
			return _profileName;
		}
		[MemberNotNull("_profileName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_profileName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ProfileName);
				_profileName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ProfileName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProfileDescription
	{
		get
		{
			return _profileDescription;
		}
		[MemberNotNull("_profileDescription")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_profileDescription, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ProfileDescription);
				_profileDescription = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ProfileDescription);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedModelPath
	{
		get
		{
			return _selectedModelPath;
		}
		[MemberNotNull("_selectedModelPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedModelPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedModelPath);
				_selectedModelPath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedModelPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsCreating
	{
		get
		{
			return _isCreating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCreating, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCreating);
				_isCreating = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCreating);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CreateProfileCommand => createProfileCommand ?? (createProfileCommand = new AsyncRelayCommand(CreateProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> EditProfileCommand => editProfileCommand ?? (editProfileCommand = new AsyncRelayCommand<ServerProfile>(EditProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> DeleteProfileCommand => deleteProfileCommand ?? (deleteProfileCommand = new AsyncRelayCommand<ServerProfile>(DeleteProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> DuplicateProfileCommand => duplicateProfileCommand ?? (duplicateProfileCommand = new AsyncRelayCommand<ServerProfile>(DuplicateProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> SetDefaultCommand => setDefaultCommand ?? (setDefaultCommand = new AsyncRelayCommand<ServerProfile>(SetDefault));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> ExportProfileCommand => exportProfileCommand ?? (exportProfileCommand = new AsyncRelayCommand<ServerProfile>(ExportProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ImportProfileCommand => importProfileCommand ?? (importProfileCommand = new AsyncRelayCommand(ImportProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectModelCommand => selectModelCommand ?? (selectModelCommand = new AsyncRelayCommand(SelectModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> SaveSettingsToProfileCommand => saveSettingsToProfileCommand ?? (saveSettingsToProfileCommand = new AsyncRelayCommand<ServerProfile>(SaveSettingsToProfile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ServerProfile> StartServerWithProfileCommand => startServerWithProfileCommand ?? (startServerWithProfileCommand = new AsyncRelayCommand<ServerProfile>(StartServerWithProfile));

	public ProfilesViewModel(IProfileManager profileManager, ILogService log, IDialogService dialog, ILocalizationService loc, ISettings settings, ServerViewModel serverViewModel)
	{
		_profileManager = profileManager;
		_log = log;
		_dialog = dialog;
		_loc = loc;
		_settings = settings;
		_serverViewModel = serverViewModel;
		_loc.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] array = new string[14]
		{
			"Title", "NewProfileBtn", "ImportJsonBtn", "NamePlaceholder", "DescPlaceholder", "SelectModelPlaceholder", "DefaultBadge", "SetDefaultBtn", "EditBtn", "DuplicateBtn",
			"ExportBtn", "DeleteBtn", "SaveSettingsBtn", "StartServerBtn"
		};
		foreach (string propertyName in array)
		{
			OnPropertyChanged(propertyName);
		}
	}

	public async Task LoadProfilesAsync()
	{
		Profiles.Clear();
		foreach (ServerProfile item in await _profileManager.GetAllProfilesAsync())
		{
			Profiles.Add(item);
		}
	}

	[RelayCommand]
	private async Task CreateProfile()
	{
		string name = await _dialog.ShowInputAsync(_loc.T("dialog.create_profile_title"), _loc.T("dialog.profile_name_prompt"));
		if (!string.IsNullOrWhiteSpace(name))
		{
			name = name.Trim();
			if (Profiles.Any((ServerProfile p) => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
			{
				await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
				return;
			}
			ServerProfile profile = new ServerProfile
			{
				Name = name,
				Description = string.Empty,
				ModelPath = null,
				MmprojPath = null,
				DraftModelPath = null
			};
			await _profileManager.SaveProfileAsync(profile);
			_settings.LastSelectedProfileId = profile.Id;
			_log.Information("Created profile: " + profile.Name, "Profiles");
			await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.profile_created"), profile.Name));
			await LoadProfilesAsync();
			ProfileName = string.Empty;
			ProfileDescription = string.Empty;
			SelectedModelPath = string.Empty;
		}
	}

	[RelayCommand]
	private async Task EditProfile(ServerProfile profile)
	{
		string name = (string.IsNullOrWhiteSpace(ProfileName) ? profile.Name : ProfileName.Trim());
		if (Profiles.Any((ServerProfile p) => p.Id != profile.Id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
		{
			await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
			return;
		}
		if (!string.IsNullOrEmpty(SelectedModelPath) && !File.Exists(SelectedModelPath))
		{
			await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.model_not_found"), SelectedModelPath));
			return;
		}
		profile.Name = name;
		profile.Description = ProfileDescription;
		profile.ModelPath = SelectedModelPath;
		await _profileManager.SaveProfileAsync(profile);
		_log.Information("Edited profile: " + profile.Name, "Profiles");
		await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.profile_updated"), profile.Name));
		await LoadProfilesAsync();
	}

	[RelayCommand]
	private async Task DeleteProfile(ServerProfile profile)
	{
		if (await _dialog.ShowConfirmationAsync(string.Format(_loc.T("dialog.confirm_delete_profile"), profile.Name), _loc.T("dialog.delete_profile_title")))
		{
			await _profileManager.DeleteProfileAsync(profile.Id);
			_log.Information("Deleted profile: " + profile.Name, "Profiles");
			await LoadProfilesAsync();
		}
	}

	[RelayCommand]
	private async Task DuplicateProfile(ServerProfile profile)
	{
		ServerProfile serverProfile = await _profileManager.DuplicateProfileAsync(profile.Id);
		_log.Information("Duplicated profile: " + serverProfile.Name, "Profiles");
		await LoadProfilesAsync();
	}

	[RelayCommand]
	public async Task SetDefault(ServerProfile profile)
	{
		await _profileManager.SetDefaultProfileAsync(profile.Id);
		_log.Information("Set default: " + profile.Name, "Profiles");
		await LoadProfilesAsync();
	}

	[RelayCommand]
	private async Task ExportProfile(ServerProfile profile)
	{
		string path = await _dialog.SaveFileAsync("Export Profile", profile.Name.Replace(" ", "_") + ".json", "JSON Files|*.json|All Files|*.*");
		if (!string.IsNullOrEmpty(path))
		{
			string contents = _profileManager.ExportProfile(profile);
			await File.WriteAllTextAsync(path, contents);
			_log.Information("Exported: " + profile.Name + " → " + path, "Profiles");
		}
	}

	[RelayCommand]
	private async Task ImportProfile()
	{
		string text = await _dialog.SelectFileAsync("Import Profile JSON", null, "JSON Files|*.json|All Files|*.*");
		if (!string.IsNullOrEmpty(text))
		{
			try
			{
				string json = await File.ReadAllTextAsync(text);
				ServerProfile serverProfile = await _profileManager.ImportProfileAsync(json);
				_log.Information("Imported profile: " + serverProfile.Name, "Profiles");
				await LoadProfilesAsync();
			}
			catch (Exception exception)
			{
				_log.Error(exception, "Failed to import profile", "Profiles");
			}
		}
	}

	[RelayCommand]
	private async Task SelectModel()
	{
		string text = await _dialog.SelectFileAsync("Select Model File", null, "GGUF Files|*.gguf|All Files|*.*");
		if (!string.IsNullOrEmpty(text))
		{
			SelectedModelPath = text;
			_log.Information("Selected model: " + Path.GetFileName(text), "Profiles");
		}
	}

	[RelayCommand]
	private async Task SaveSettingsToProfile(ServerProfile profile)
	{
		_serverViewModel.SelectedProfile = profile;
		_serverViewModel.SyncSettingsToProfile();
		await _profileManager.SaveProfileAsync(profile);
		_log.Information("Settings saved to profile: " + profile.Name, "Profiles");
		await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.settings_saved"), profile.Name));
	}

	[RelayCommand]
	private async Task StartServerWithProfile(ServerProfile profile)
	{
		_serverViewModel.SelectedProfile = profile;
		_serverViewModel.SyncSettingsToProfile();
		await _profileManager.SaveProfileAsync(profile);
		await _serverViewModel.StartServerAsync();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedProfileChanged(ServerProfile? value)
	{
		if (value != null)
		{
			ProfileName = value.Name;
			ProfileDescription = value.Description;
			SelectedModelPath = value.ModelPath ?? string.Empty;
			_settings.LastSelectedProfileId = value.Id;
		}
	}
}
