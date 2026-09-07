using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class ModelsViewModel : ObservableObject
{
	private readonly IModelScanner _scanner;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly IProfileManager _profileManager;

	private readonly ILocalizationService _loc;

	private readonly IHuggingFaceDownloader _hfDownloader;

	[ObservableProperty]
	private ObservableCollection<GgufModelInfo> _models = new ObservableCollection<GgufModelInfo>();

	[ObservableProperty]
	private GgufModelInfo? _selectedModel;

	[ObservableProperty]
	private string _modelsDirectory;

	// r75: true while the constructor applies the initial values, so the
	// ModelsDirectory persistence in the setter does not fire on load.
	private bool _modelsDirectoryInit;

	[ObservableProperty]
	private bool _isScanning;

	[ObservableProperty]
	private string _scanStatus = "Ready";

	[ObservableProperty]
	private string _searchFilter = "";

	[ObservableProperty]
	private int _filteredCount;

	[ObservableProperty]
	private string _serverModelPath = string.Empty;

	[ObservableProperty]
	private string _serverMmprojPath = string.Empty;

	[ObservableProperty]
	private string _hfUrl = "";

	[ObservableProperty]
	private string _hfAuthToken = "";

	[ObservableProperty]
	private string _hfDownloadDir = "";

	[ObservableProperty]
	private bool _isHfLoading;

	[ObservableProperty]
	private bool _isHfDownloading;

	[ObservableProperty]
	private double _hfOverallProgress;

	[ObservableProperty]
	private string _hfStatus = "";

	[ObservableProperty]
	private ObservableCollection<HfFileInfo> _hfFiles = new ObservableCollection<HfFileInfo>();

	[ObservableProperty]
	private int _hfFilesCount;

	[ObservableProperty]
	private string _hfSearchQuery = "";

	[ObservableProperty]
	private ObservableCollection<HfSearchResult> _hfSearchResults = new ObservableCollection<HfSearchResult>();

	[ObservableProperty]
	private bool _hfSearchVisible;

	[ObservableProperty]
	private bool _hfSearchIsSearching;

	private readonly Dictionary<string, List<HfSearchResult>> _searchCache = new Dictionary<string, List<HfSearchResult>>();

	private CancellationTokenSource? _searchCts;

	private DispatcherTimer? _searchDebounceTimer;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? hfLoginCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? hfSaveTokenCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? hfBrowseDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? hfFetchRepoCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? hfSelectAllCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? hfDeselectAllCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? hfDownloadSelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? hfCancelDownloadsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? hfPauseDownloadsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<HfFileInfo>? showHfFileDetailsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<HfSearchResult>? selectHfModelCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? hideHfSearchCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? scanModelsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectDirectoryCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<GgufModelInfo?>? useInServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<GgufModelInfo?>? useAsMmprojCommand;

	public string Title => _loc.T("models.title");

	public string CountLabel => _loc.T("models.count");

	public string ScanBtn => _loc.T("models.scan");

	public string ScanningStatus => _loc.T("models.scanning");

	public string BrowseBtn => _loc.T("models.browse");

	public string SearchPlaceholder => _loc.T("models.search");

	// r75: watermark for the models directory path field on the Models page.
	public string ModelsPathHint => _loc.T("models.select_models_dir");

	// r76: localized Scan button label (localized "Scanning..." while a scan runs).
	public string ScanButtonLabel => IsScanning ? _loc.T("models.scanning") : _loc.T("models.scan");

	public string NoModelsText => _loc.T("models.no_models");

	public string ToServerModel => _loc.T("models.to_server");

	public string ToMmproj => _loc.T("models.to_mmproj");

	public string Connected => _loc.T("models.connected");

	public string HfTitle => _loc.T("hf.title");

	public string HfUrlPlaceholder => _loc.T("hf.url_placeholder");

	public string HfAuthTokenPlaceholder => _loc.T("hf.auth_token_placeholder");

	public string HfBrowseBtn => _loc.T("hf.browse");

	public string HfFetchBtn => _loc.T("hf.fetch");

	public string HfDownloadSelectedBtn => _loc.T("hf.download_selected");

	public string HfCancelBtn => _loc.T("hf.cancel");

	public string HfPauseBtn => "Пауза";

	public string HfSelectAllBtn => _loc.T("hf.select_all");

	public string HfDeselectAllBtn => _loc.T("hf.deselect_all");

	public string HfLoginBtn => _loc.T("hf.login");

	public string HfSelectDirLabel => _loc.T("hf.select_dir");

	public string HfFilesFound => _loc.T("hf.files_found");

	public string HfSearchPlaceholder => _loc.T("hf.search_placeholder");

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<GgufModelInfo> Models
	{
		get
		{
			return _models;
		}
		[MemberNotNull("_models")]
		set
		{
			if (!EqualityComparer<ObservableCollection<GgufModelInfo>>.Default.Equals(_models, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Models);
				_models = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Models);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public GgufModelInfo? SelectedModel
	{
		get
		{
			return _selectedModel;
		}
		set
		{
			if (!EqualityComparer<GgufModelInfo>.Default.Equals(_selectedModel, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedModel);
				_selectedModel = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedModel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelsDirectory
	{
		get
		{
			return _modelsDirectory;
		}
		[MemberNotNull("_modelsDirectory")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelsDirectory, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelsDirectory);
				_modelsDirectory = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelsDirectory);
				// r76: keep the HF download directory in sync — the page now has a
				// single models path field, downloads go into the same folder.
				HfDownloadDir = value;
				// r75: the models path is editable on the page — persist it as soon
				// as it changes (picker, typed text + Enter, HF download flow).
				if (!_modelsDirectoryInit && !string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
				{
					_settings.ModelsDirectory = value;
					_ = _settings.SaveAsync();
				}
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsScanning
	{
		get
		{
			return _isScanning;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isScanning, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsScanning);
				_isScanning = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsScanning);
				OnPropertyChanged("ScanButtonLabel");
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ScanStatus
	{
		get
		{
			return _scanStatus;
		}
		[MemberNotNull("_scanStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_scanStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ScanStatus);
				_scanStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ScanStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SearchFilter
	{
		get
		{
			return _searchFilter;
		}
		[MemberNotNull("_searchFilter")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_searchFilter, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SearchFilter);
				_searchFilter = value;
				OnSearchFilterChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SearchFilter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int FilteredCount
	{
		get
		{
			return _filteredCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_filteredCount, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FilteredCount);
				_filteredCount = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FilteredCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ServerModelPath
	{
		get
		{
			return _serverModelPath;
		}
		[MemberNotNull("_serverModelPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serverModelPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ServerModelPath);
				_serverModelPath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ServerModelPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ServerMmprojPath
	{
		get
		{
			return _serverMmprojPath;
		}
		[MemberNotNull("_serverMmprojPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serverMmprojPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ServerMmprojPath);
				_serverMmprojPath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ServerMmprojPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfUrl
	{
		get
		{
			return _hfUrl;
		}
		[MemberNotNull("_hfUrl")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfUrl, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfUrl);
				_hfUrl = value;
				OnHfUrlChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfUrl);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfAuthToken
	{
		get
		{
			return _hfAuthToken;
		}
		[MemberNotNull("_hfAuthToken")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfAuthToken, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfAuthToken);
				_hfAuthToken = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfAuthToken);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfDownloadDir
	{
		get
		{
			return _hfDownloadDir;
		}
		[MemberNotNull("_hfDownloadDir")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfDownloadDir, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfDownloadDir);
				_hfDownloadDir = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfDownloadDir);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsHfLoading
	{
		get
		{
			return _isHfLoading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isHfLoading, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsHfLoading);
				_isHfLoading = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsHfLoading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsHfDownloading
	{
		get
		{
			return _isHfDownloading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isHfDownloading, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsHfDownloading);
				_isHfDownloading = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsHfDownloading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double HfOverallProgress
	{
		get
		{
			return _hfOverallProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_hfOverallProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfOverallProgress);
				_hfOverallProgress = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfOverallProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfStatus
	{
		get
		{
			return _hfStatus;
		}
		[MemberNotNull("_hfStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfStatus);
				_hfStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<HfFileInfo> HfFiles
	{
		get
		{
			return _hfFiles;
		}
		[MemberNotNull("_hfFiles")]
		set
		{
			if (!EqualityComparer<ObservableCollection<HfFileInfo>>.Default.Equals(_hfFiles, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfFiles);
				_hfFiles = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfFiles);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int HfFilesCount
	{
		get
		{
			return _hfFilesCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_hfFilesCount, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfFilesCount);
				_hfFilesCount = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfFilesCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HfSearchQuery
	{
		get
		{
			return _hfSearchQuery;
		}
		[MemberNotNull("_hfSearchQuery")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hfSearchQuery, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfSearchQuery);
				_hfSearchQuery = value;
				OnHfSearchQueryChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfSearchQuery);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<HfSearchResult> HfSearchResults
	{
		get
		{
			return _hfSearchResults;
		}
		[MemberNotNull("_hfSearchResults")]
		set
		{
			if (!EqualityComparer<ObservableCollection<HfSearchResult>>.Default.Equals(_hfSearchResults, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfSearchResults);
				_hfSearchResults = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfSearchResults);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HfSearchVisible
	{
		get
		{
			return _hfSearchVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hfSearchVisible, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfSearchVisible);
				_hfSearchVisible = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfSearchVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HfSearchIsSearching
	{
		get
		{
			return _hfSearchIsSearching;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hfSearchIsSearching, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HfSearchIsSearching);
				_hfSearchIsSearching = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HfSearchIsSearching);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HfLoginCommand => hfLoginCommand ?? (hfLoginCommand = new AsyncRelayCommand(HfLogin));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HfSaveTokenCommand => hfSaveTokenCommand ?? (hfSaveTokenCommand = new AsyncRelayCommand(HfSaveToken));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HfBrowseDirectoryCommand => hfBrowseDirectoryCommand ?? (hfBrowseDirectoryCommand = new AsyncRelayCommand(HfBrowseDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HfFetchRepoCommand => hfFetchRepoCommand ?? (hfFetchRepoCommand = new AsyncRelayCommand(HfFetchRepo));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand HfSelectAllCommand => hfSelectAllCommand ?? (hfSelectAllCommand = new RelayCommand(HfSelectAll));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand HfDeselectAllCommand => hfDeselectAllCommand ?? (hfDeselectAllCommand = new RelayCommand(HfDeselectAll));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HfDownloadSelectedCommand => hfDownloadSelectedCommand ?? (hfDownloadSelectedCommand = new AsyncRelayCommand(HfDownloadSelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand HfCancelDownloadsCommand => hfCancelDownloadsCommand ?? (hfCancelDownloadsCommand = new RelayCommand(HfCancelDownloads));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand HfPauseDownloadsCommand => hfPauseDownloadsCommand ?? (hfPauseDownloadsCommand = new RelayCommand(HfPauseDownloads));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<HfFileInfo> ShowHfFileDetailsCommand => showHfFileDetailsCommand ?? (showHfFileDetailsCommand = new AsyncRelayCommand<HfFileInfo>(ShowHfFileDetails));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<HfSearchResult> SelectHfModelCommand => selectHfModelCommand ?? (selectHfModelCommand = new AsyncRelayCommand<HfSearchResult>(SelectHfModel));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand HideHfSearchCommand => hideHfSearchCommand ?? (hideHfSearchCommand = new RelayCommand(HideHfSearch));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ScanModelsCommand => scanModelsCommand ?? (scanModelsCommand = new AsyncRelayCommand(ScanModels));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectDirectoryCommand => selectDirectoryCommand ?? (selectDirectoryCommand = new AsyncRelayCommand(SelectDirectory));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<GgufModelInfo?> UseInServerCommand => useInServerCommand ?? (useInServerCommand = new AsyncRelayCommand<GgufModelInfo>(UseInServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<GgufModelInfo?> UseAsMmprojCommand => useAsMmprojCommand ?? (useAsMmprojCommand = new AsyncRelayCommand<GgufModelInfo>(UseAsMmproj));

	public ModelsViewModel(IModelScanner scanner, ISettings settings, ILogService log, IDialogService dialog, IProfileManager profileManager, ILocalizationService loc, IHuggingFaceDownloader hfDownloader)
	{
		_scanner = scanner;
		_settings = settings;
		_log = log;
		_dialog = dialog;
		_profileManager = profileManager;
		_loc = loc;
		_hfDownloader = hfDownloader;
		_modelsDirectoryInit = true;
		ModelsDirectory = _settings.ModelsDirectory;
		HfDownloadDir = _settings.ModelsDirectory;
		_modelsDirectoryInit = false;
		LoadHfUrlFromProfileAsync();
		_hfDownloader.StatusMessage += delegate(object? _, string msg)
		{
			HfStatus = msg;
		};
		_hfDownloader.DownloadProgress += delegate(object? _, double progress)
		{
			HfOverallProgress = progress;
		};
		_hfDownloader.AllDownloadsCompleted += async delegate
		{
			IsHfDownloading = false;
			HfStatus = _loc.T("models.hf_status_download_complete");
			ModelsDirectory = HfDownloadDir;
			await ScanModels();
		};
		_profileManager.ProfileChanged += OnProfileChanged;
		_loc.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		string[] propertyNames = new string[26]
		{
			"Title", "CountLabel", "ScanBtn", "ScanningStatus", "ScanButtonLabel", "BrowseBtn", "SearchPlaceholder", "ModelsPathHint", "NoModelsText", "ToServerModel", "ToMmproj", "Connected",
			"HfTitle", "HfUrlPlaceholder", "HfAuthTokenPlaceholder", "HfBrowseBtn", "HfFetchBtn", "HfDownloadSelectedBtn", "HfCancelBtn", "HfPauseBtn", "HfSelectAllBtn", "HfDeselectAllBtn", "HfLoginBtn",
			"HfSelectDirLabel", "HfFilesFound", "HfSearchPlaceholder"
		};
		foreach (string propertyName in propertyNames)
		{
			OnPropertyChanged(propertyName);
		}
	}

	private void OnProfileChanged(string? profileId)
	{
		LoadHfUrlFromProfileAsync();
		RefreshServerPathsAsync();
	}

	private async Task LoadHfUrlFromProfileAsync()
	{
		try
		{
			ServerProfile serverProfile = null;
			if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
			{
				serverProfile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
			}
			if (serverProfile == null)
			{
				serverProfile = await _profileManager.GetDefaultProfileAsync();
			}
			if (!string.IsNullOrEmpty(serverProfile?.HfUrl))
			{
				HfUrl = serverProfile.HfUrl;
			}
			else
			{
				HfUrl = string.Empty;
			}
		}
		catch
		{
		}
	}

	[RelayCommand]
	private async Task HfLogin()
	{
		await _hfDownloader.LoginViaBrowserAsync();
		HfStatus = _loc.T("models.hf_status_token_instructions");
	}

	[RelayCommand]
	private async Task HfSaveToken()
	{
		_hfDownloader.SetAuthToken(string.IsNullOrWhiteSpace(HfAuthToken) ? null : HfAuthToken);
		if (string.IsNullOrWhiteSpace(HfAuthToken))
		{
			HfStatus = _loc.T("models.hf_status_token_cleared");
		}
		else
		{
			HfStatus = _loc.T("models.hf_status_token_saved");
		}
	}

	[RelayCommand]
	private async Task HfBrowseDirectory()
	{
		string selectedFolder = await _dialog.SelectFolderAsync(_loc.T("models.select_dir"), HfDownloadDir);
		if (!string.IsNullOrEmpty(selectedFolder) && Directory.Exists(selectedFolder))
		{
			HfDownloadDir = selectedFolder;
			_settings.ModelsDirectory = selectedFolder;
			ModelsDirectory = selectedFolder;
			await _settings.SaveAsync();
		}
	}

	[RelayCommand]
	private async Task HfFetchRepo()
	{
		string repoUrl = HfUrl.Trim();
		if (string.IsNullOrWhiteSpace(repoUrl))
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
			List<HfFileInfo> repoFiles = await _hfDownloader.ListRepoFilesAsync(repoUrl);
			string downloadDir = HfDownloadDir.Trim();
			foreach (HfFileInfo fileInfo in repoFiles)
			{
				if (!string.IsNullOrEmpty(downloadDir) && Directory.Exists(downloadDir))
				{
					string localPath = Path.Combine(downloadDir, fileInfo.FileName);
					if (File.Exists(localPath) && new FileInfo(localPath).Length > 0)
					{
						fileInfo.Status = _loc.T("models.hf_status_already_downloaded");
						fileInfo.IsCompleted = true;
						fileInfo.DownloadProgress = 100.0;
						fileInfo.LocalPath = localPath;
					}
				}
				HfFiles.Add(fileInfo);
			}
			HfFilesCount = HfFiles.Count;
			int completedCount = HfFiles.Count((HfFileInfo f) => f.IsCompleted);
			if (HfFilesCount > 0)
			{
				HfStatus = ((completedCount > 0) ? string.Format(_loc.T("models.hf_status_files_found_dl"), HfFilesCount, completedCount) : string.Format(_loc.T("models.hf_status_files_found"), HfFilesCount));
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
	private void HfSelectAll()
	{
		foreach (HfFileInfo hfFile in HfFiles)
		{
			hfFile.IsSelected = true;
		}
		HfFilesCount = HfFiles.Count;
	}

	[RelayCommand]
	private void HfDeselectAll()
	{
		foreach (HfFileInfo hfFile in HfFiles)
		{
			hfFile.IsSelected = false;
		}
		HfFilesCount = 0;
	}

	[RelayCommand]
	private async Task HfDownloadSelected()
	{
		List<HfFileInfo> selectedFiles = HfFiles.Where((HfFileInfo f) => f.IsSelected).ToList();
		if (selectedFiles.Count == 0)
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
		HfOverallProgress = 0.0;
		HfStatus = string.Format(_loc.T("models.hf_status_downloading"), selectedFiles.Count);
		try
		{
			await _hfDownloader.DownloadSelectedAsync(selectedFiles, HfDownloadDir);
		}
		catch (Exception ex)
		{
			HfStatus = string.Format(_loc.T("models.hf_status_error"), ex.Message);
			_log.Error(ex, "HF download failed", "Models");
			IsHfDownloading = false;
		}
	}

	[RelayCommand]
	private void HfCancelDownloads()
	{
		_hfDownloader.CancelDownloads();
		IsHfDownloading = false;
		HfStatus = _loc.T("models.hf_status_cancelled");
	}

	private void HfPauseDownloads()
	{
		_hfDownloader.PauseDownloads();
		IsHfDownloading = false;
		HfStatus = "Загрузка приостановлена. Нажмите «Скачать выбранные», чтобы продолжить.";
	}

	[RelayCommand]
	private async Task ShowHfFileDetails(HfFileInfo file)
	{
		if (file.ShowDetails && file.Metadata.Count > 0)
		{
			file.ShowDetails = false;
			return;
		}
		file.ShowDetails = true;
		if (file.Metadata.Count == 0)
		{
			await _hfDownloader.LoadFileMetadataAsync(file);
		}
	}

	private async Task SearchHfModelsAsync(string query, CancellationToken ct)
	{
		HfSearchIsSearching = true;
		try
		{
			List<HfSearchResult> results = await _hfDownloader.SearchModelsAsync(query, ct);
			if (ct.IsCancellationRequested)
			{
				return;
			}
			_searchCache[query] = results;
			Dispatcher.UIThread.InvokeAsync(delegate
			{
				HfSearchResults = new ObservableCollection<HfSearchResult>(results);
				HfSearchVisible = results.Count > 0;
			});
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
		}
		finally
		{
			if (!ct.IsCancellationRequested)
			{
				HfSearchIsSearching = false;
			}
		}
	}

	[RelayCommand]
	private async Task SelectHfModel(HfSearchResult result)
	{
		if (result == null || string.IsNullOrWhiteSpace(result.Id))
		{
			return;
		}
		HfSearchVisible = false;
		HfSearchQuery = string.Empty;
		HfUrl = result.Id;
		await HfFetchRepo();
	}

	[RelayCommand]
	private void HideHfSearch()
	{
		HfSearchVisible = false;
	}

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
			foreach (GgufModelInfo model in await _scanner.ScanDirectoryAsync(ModelsDirectory))
			{
				Models.Add(model);
			}
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

	private async Task SaveHfUrlToProfileAsync(string url)
	{
		try
		{
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile != null)
			{
				serverProfile.HfUrl = url;
				await _profileManager.SaveProfileAsync(serverProfile);
			}
		}
		catch
		{
		}
	}

	[RelayCommand]
	private async Task SelectDirectory()
	{
		string path = await _dialog.SelectFolderAsync(_loc.T("models.select_models_dir"), ModelsDirectory);
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
	private async Task UseInServer(GgufModelInfo? model)
	{
		if (model != null)
		{
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile == null)
			{
				_log.Warning("No profile selected. Create a profile first.", "Models");
				return;
			}
			serverProfile.ModelPath = model.Path;
			await _profileManager.SaveProfileAsync(serverProfile);
			ServerModelPath = model.Path;
			await RefreshServerPathsAsync();
			ScanStatus = string.Format(_loc.T("models.scan_status_set_main"), model.FileName);
		}
	}

	[RelayCommand]
	private async Task UseAsMmproj(GgufModelInfo? model)
	{
		if (model != null)
		{
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile == null)
			{
				_log.Warning("No profile selected. Create a profile first.", "Models");
				return;
			}
			serverProfile.MmprojPath = model.Path;
			await _profileManager.SaveProfileAsync(serverProfile);
			ServerMmprojPath = model.Path;
			await RefreshServerPathsAsync();
			ScanStatus = string.Format(_loc.T("models.scan_status_set_mmproj"), model.FileName);
		}
	}

	private async Task<ServerProfile?> GetActiveProfileAsync()
	{
		ServerProfile serverProfile = null;
		if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
		{
			serverProfile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
		}
		if (serverProfile == null)
		{
			serverProfile = await _profileManager.GetDefaultProfileAsync();
		}
		return serverProfile;
	}

	private async Task RefreshServerPathsAsync()
	{
		try
		{
			ServerProfile serverProfile = null;
			if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
			{
				serverProfile = await _profileManager.GetProfileAsync(_settings.LastSelectedProfileId);
			}
			if (serverProfile == null)
			{
				serverProfile = await _profileManager.GetDefaultProfileAsync();
			}
			string serverModelPath = serverProfile?.ModelPath ?? string.Empty;
			string serverMmprojPath = serverProfile?.MmprojPath ?? string.Empty;
			Dispatcher.UIThread.InvokeAsync(delegate
			{
				ServerModelPath = serverModelPath;
				ServerMmprojPath = serverMmprojPath;
				foreach (GgufModelInfo model in Models)
				{
					model.IsServerModelConnected = model.Path == ServerModelPath;
					model.IsServerMmprojConnected = model.Path == ServerMmprojPath;
				}
				List<GgufModelInfo> modelsCopy = Models.ToList();
				Models.Clear();
				foreach (GgufModelInfo model in modelsCopy)
				{
					Models.Add(model);
				}
			});
		}
		catch
		{
			Dispatcher.UIThread.InvokeAsync(delegate
			{
				ServerModelPath = string.Empty;
				ServerMmprojPath = string.Empty;
				foreach (GgufModelInfo model in Models)
				{
					model.IsServerModelConnected = false;
					model.IsServerMmprojConnected = false;
				}
			});
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSearchFilterChanged(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			FilteredCount = Models.Count;
			return;
		}
		string filter = value.ToLower();
		FilteredCount = Models.Count((GgufModelInfo m) => m.FileName.ToLower().Contains(filter) || m.Architecture.ToLower().Contains(filter) || m.QuantizationTag.ToLower().Contains(filter));
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHfUrlChanged(string value)
	{
		SaveHfUrlToProfileAsync(value);
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHfSearchQueryChanged(string value)
	{
		_searchCts?.Cancel();
		_searchCts = new CancellationTokenSource();
		if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
		{
			HfSearchResults.Clear();
			HfSearchVisible = false;
			return;
		}
		_searchDebounceTimer?.Stop();
		_searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
		_searchDebounceTimer.Tick += async (_, _) =>
		{
			_searchDebounceTimer?.Stop();
			string searchQuery = value.Trim().ToLowerInvariant();
			CancellationToken ct = _searchCts?.Token ?? CancellationToken.None;
			if (_searchCache.TryGetValue(searchQuery, out List<HfSearchResult> cached))
			{
				HfSearchResults = new ObservableCollection<HfSearchResult>(cached);
				HfSearchVisible = true;
			}
			else
			{
				await SearchHfModelsAsync(searchQuery, ct);
			}
		};
		_searchDebounceTimer.Start();
	}
}
