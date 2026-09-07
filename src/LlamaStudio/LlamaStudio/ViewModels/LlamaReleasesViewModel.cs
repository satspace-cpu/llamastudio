using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;

namespace LlamaStudio.ViewModels;

public class LlamaReleasesViewModel : ObservableObject
{
	public event EventHandler<string>? InstallFailed;
	private static string LlamaServerExecutableName => OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server";
	private readonly ILlamaUpdater _updater;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly IDialogService _dialog;

	private readonly ILocalizationService _loc;

	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly IVersionCache _versionCache;

	[ObservableProperty]
	private ObservableCollection<ReleaseBuildCard> _releaseBuildCards = new ObservableCollection<ReleaseBuildCard>();

	[ObservableProperty]
	private ObservableCollection<InstalledVersionItem> _installedVersionItems = new ObservableCollection<InstalledVersionItem>();

	[ObservableProperty]
	private bool _noServersFound;

	[ObservableProperty]
	private bool _isCheckingUpdates;

	[ObservableProperty]
	 private bool _showOnlyLatest = true;



	[ObservableProperty]
	private string _updateStatus = "Not checked";

	[ObservableProperty]
	private double _downloadProgress;

	[ObservableProperty]
	private string _downloadStatus = string.Empty;

	[ObservableProperty]
	private string _activeVersion = "";

	[ObservableProperty]
	private bool _isDownloading;

	private CancellationTokenSource? _downloadCts;

	[ObservableProperty]
	private bool _isExperimentalTab;

	[ObservableProperty]
	private bool _isFetchingExperimental;

	[ObservableProperty]
	private string _selectedExperimentalRepo = "DiffusionGemma/llama.cpp-diffusion";

	[ObservableProperty]
	private string _experimentalStatus = "";

	[ObservableProperty]
	private ObservableCollection<ReleaseBuildCard> _experimentalBuildCards = new ObservableCollection<ReleaseBuildCard>();

	[ObservableProperty]
	private ObservableCollection<LlamaCppRelease> _experimentalReleases = new ObservableCollection<LlamaCppRelease>();

	[ObservableProperty]
	private LlamaCppRelease? _selectedExperimentalRelease;

	[ObservableProperty]
	private ReleaseAsset? _selectedExperimentalAsset;

	[ObservableProperty]
	private bool _isDownloadingExperimental;

	[ObservableProperty]
	private ObservableCollection<ReleaseAsset> _availableExperimentalAssets = new ObservableCollection<ReleaseAsset>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? fetchExperimentalReleasesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? installExperimentalBuildCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? checkForUpdatesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ReleaseBuildCard>? installBuildCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelDownloadCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? refreshInstalledVersionsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<InstalledVersionItem>? switchVersionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<InstalledVersionItem>? uninstallVersionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectLlamaCppDirectoryCommand;

	public string CheckForUpdatesBtnDisplay
	{
		get
		{
			if (!IsCheckingUpdates)
			{
				return CheckForUpdatesBtn;
			}
			return CheckingStatus;
		}
	}

	public string ActiveVersionDisplay
	{
		get
		{
			if (string.IsNullOrWhiteSpace(ActiveVersion))
			{
				return string.Empty;
			}
			string[] versionParts = ActiveVersion.Split('|');
			string display;
			if (versionParts.Length == 2 && !string.IsNullOrEmpty(versionParts[1]))
			{
				display = versionParts[0] + " (" + versionParts[1] + ")";
			}
			else
			{
				display = versionParts[0];
			}
			// r71: show the real commit/build of the active binary next to the version.
			try
			{
				VersionInfo? info = _versionCache.GetByVersionKey(ActiveVersion) ?? _versionCache.GetByVersion(versionParts[0]);
				if (info != null && !string.IsNullOrEmpty(info.VersionDetail))
				{
					display += " · " + info.VersionDetail;
				}
			}
			catch
			{
			}
			return display;
		}
	}

	public bool CanCancelDownload
	{
		get
		{
			if (_downloadCts != null)
			{
				return !_downloadCts.IsCancellationRequested;
			}
			return false;
		}
	}

	public bool CanPauseDownload
	{
		get
		{
			if (_downloadCts != null)
			{
				return !_downloadCts.IsCancellationRequested;
			}
			return false;
		}
	}

	public string LlamaCppBaseDirectoryDisplay
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory))
			{
				return _settings.LlamaCppBaseDirectory;
			}
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
		}
	}

	public bool CanInstallExperimental
	{
		get
		{
			if (SelectedExperimentalAsset != null && !IsFetchingExperimental)
			{
				return !IsDownloadingExperimental;
			}
			return false;
		}
	}

	public IEnumerable<string> ExperimentalReposList { get; } = new string[1] { "pytraveler/llama-cpp-turboquant" };

	public string Title => _loc.T("llama_releases.title");

	public string ReleasesSection => _loc.T("llama_releases.releases_section");

	public string CheckForUpdatesBtn => _loc.T("llama_releases.check_updates");

	public string ShowOnlyLatestLabel => _loc.T("llama_releases.show_only_latest");

	public string CheckingStatus => _loc.T("llama_releases.checking");

	public string InstalledVersionsTitle => _loc.T("llama_releases.installed_versions");

	public string RefreshBtn => _loc.T("llama_releases.refresh");

	public string InstallBtn => _loc.T("llama_releases.install");

	public string InstallingBtn => _loc.T("llama_releases.installing");

	public string InstalledBtn => _loc.T("llama_releases.installed");

	public string UseBtn => _loc.T("llama_releases.use");

	public string RemoveBtn => _loc.T("llama_releases.remove");

	public string ActiveBadge => _loc.T("llama_releases.active");

	public string TtInstallBuild => _loc.T("tt.install_build");

	public string TabOfficial => _loc.T("llama_releases.tab_official");

	public string TabExperimental => _loc.T("llama_releases.tab_experimental");

	public string SelectRepoLabel => _loc.T("llama_releases.select_repo");

	public string SelectReleaseLabel => _loc.T("llama_releases.select_release");

	public string FetchingRepoStatus => _loc.T("llama_releases.fetching_repo");

	public string NoExperimentalRepos => _loc.T("llama_releases.no_experimental_repos");

	public string NoAssets => _loc.T("llama_releases.no_assets");

	public string ExperimentalBadge => _loc.T("releases.experimental");

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ReleaseBuildCard> ReleaseBuildCards
	{
		get
		{
			return _releaseBuildCards;
		}
		[MemberNotNull("_releaseBuildCards")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ReleaseBuildCard>>.Default.Equals(_releaseBuildCards, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ReleaseBuildCards);
				_releaseBuildCards = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ReleaseBuildCards);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<InstalledVersionItem> InstalledVersionItems
	{
		get
		{
			return _installedVersionItems;
		}
		[MemberNotNull("_installedVersionItems")]
		set
		{
			if (!EqualityComparer<ObservableCollection<InstalledVersionItem>>.Default.Equals(_installedVersionItems, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.InstalledVersionItems);
				_installedVersionItems = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.InstalledVersionItems);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool NoServersFound
	{
		get
		{
			return _noServersFound;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_noServersFound, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.NoServersFound);
				_noServersFound = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.NoServersFound);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsCheckingUpdates
	{
		get
		{
			return _isCheckingUpdates;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCheckingUpdates, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCheckingUpdates);
				_isCheckingUpdates = value;
				OnIsCheckingUpdatesChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCheckingUpdates);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowOnlyLatest
	{
		get
		{
			return _showOnlyLatest;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showOnlyLatest, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowOnlyLatest);
				_showOnlyLatest = value;
				OnShowOnlyLatestChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowOnlyLatest);
			}
		}
	}

	// A "stable" llama.cpp release uses a semantic version tag (v0.3.0, v0.2.0...).
	// Rolling nightly/CI builds use b#### tags and are the experimental ones.
	// Kept for display/labeling only — no list filtering by it (r45).
	private static readonly Regex StableTagRegex = new Regex(@"^v\d+(\.\d+)+$", RegexOptions.Compiled);
	private static bool IsStableTag(string? tag) => tag != null && StableTagRegex.IsMatch(tag);

	private bool _ncclEnabled = true;

	/// <summary>
	/// Build the CUDA server with -DGGML_CUDA_NCCL=ON (multi-GPU collectives
	/// through NVIDIA NCCL). Default on for local GPU builds; the flag only
	/// takes effect when NCCL itself is found on the build host.
	/// </summary>
	public bool NcclEnabled
	{
		get
		{
			return _ncclEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_ncclEnabled, value))
			{
				OnPropertyChanging("NcclEnabled");
				_ncclEnabled = value;
				OnPropertyChanged("NcclEnabled");
				// The install button state depends on the NCCL variant:
				// re-evaluate every card against the folders that actually exist.
				_ = RefreshReleaseCardsAsync();
			}
		}
	}

	public string NcclEnabledLabel => "NVIDIA NCCL (GGML_CUDA_NCCL)";

	// cafe-llama.cpp (Flash-Next MTP): a separate, always-on build source that
	// is independent of ggml-org/llama.cpp.  The card is a fixed sm80 CUDA 12 +
	// NCCL build that installs to its own folder, so NCCL is forced on for it
	// regardless of the page's NCCL checkbox.
	private const string CafeCardTitle = "cafe-llama.cpp (Flash-Next MTP)";
	private const string CafeAssetName = "llama-studio-build-cafe-llama-cuda12-rpc-local-flashnext";
	private const string CafeVersion = "cafe-main";

	private static bool IsCafeCard(ReleaseBuildCard? card) => card != null && card.AssetName == CafeAssetName;
	private bool EffectiveNccl(ReleaseBuildCard card) => IsCafeCard(card) ? true : NcclEnabled;

	private ReleaseBuildCard CreateCafeBuildCard()
	{
		ReleaseAsset cafeAsset = new ReleaseAsset
		{
			Name = CafeAssetName,
			BuildType = BuildType.Cuda12x,
			CudaVersion = "CUDA 12",
			DescriptionOverride = "cafe-llama.cpp fork · Flash-Next + MTP · CUDA 12 + NCCL · sm80 (CMP 170HX)",
			SizeDisplayOverride = "local build"
		};
		bool installed = IsAssetInstalled(GetBaseLlamaDir(), CafeVersion, CafeAssetName, true);
		return new ReleaseBuildCard
		{
			ReleaseTag = CafeVersion,
			ReleaseDate = DateTime.MinValue,
			IsPrerelease = true,
			Asset = cafeAsset,
			BuildType = BuildType.Cuda12x,
			BuildDescription = CafeAssetName,
			SizeDisplay = "local build",
			AssetName = CafeAssetName,
			InstallBtnText = (installed ? InstalledBtn : InstallBtn),
			IsInstalled = installed
		};
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UpdateStatus
	{
		get
		{
			return _updateStatus;
		}
		[MemberNotNull("_updateStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_updateStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.UpdateStatus);
				_updateStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.UpdateStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double DownloadProgress
	{
		get
		{
			return _downloadProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_downloadProgress, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DownloadProgress);
				_downloadProgress = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DownloadProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string DownloadStatus
	{
		get
		{
			return _downloadStatus;
		}
		[MemberNotNull("_downloadStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_downloadStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.DownloadStatus);
				_downloadStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.DownloadStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ActiveVersion
	{
		get
		{
			return _activeVersion;
		}
		[MemberNotNull("_activeVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_activeVersion, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ActiveVersion);
				_activeVersion = value;
				OnActiveVersionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ActiveVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDownloading
	{
		get
		{
			return _isDownloading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDownloading, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsDownloading);
				_isDownloading = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsDownloading);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsExperimentalTab
	{
		get
		{
			return _isExperimentalTab;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isExperimentalTab, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsExperimentalTab);
				_isExperimentalTab = value;
				OnIsExperimentalTabChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsExperimentalTab);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsFetchingExperimental
	{
		get
		{
			return _isFetchingExperimental;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isFetchingExperimental, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsFetchingExperimental);
				_isFetchingExperimental = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsFetchingExperimental);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedExperimentalRepo
	{
		get
		{
			return _selectedExperimentalRepo;
		}
		[MemberNotNull("_selectedExperimentalRepo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedExperimentalRepo, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedExperimentalRepo);
				_selectedExperimentalRepo = value;
				OnSelectedExperimentalRepoChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedExperimentalRepo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ExperimentalStatus
	{
		get
		{
			return _experimentalStatus;
		}
		[MemberNotNull("_experimentalStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_experimentalStatus, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ExperimentalStatus);
				_experimentalStatus = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ExperimentalStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ReleaseBuildCard> ExperimentalBuildCards
	{
		get
		{
			return _experimentalBuildCards;
		}
		[MemberNotNull("_experimentalBuildCards")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ReleaseBuildCard>>.Default.Equals(_experimentalBuildCards, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ExperimentalBuildCards);
				_experimentalBuildCards = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ExperimentalBuildCards);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<LlamaCppRelease> ExperimentalReleases
	{
		get
		{
			return _experimentalReleases;
		}
		[MemberNotNull("_experimentalReleases")]
		set
		{
			if (!EqualityComparer<ObservableCollection<LlamaCppRelease>>.Default.Equals(_experimentalReleases, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ExperimentalReleases);
				_experimentalReleases = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ExperimentalReleases);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public LlamaCppRelease? SelectedExperimentalRelease
	{
		get
		{
			return _selectedExperimentalRelease;
		}
		set
		{
			if (!EqualityComparer<LlamaCppRelease>.Default.Equals(_selectedExperimentalRelease, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedExperimentalRelease);
				_selectedExperimentalRelease = value;
				OnSelectedExperimentalReleaseChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedExperimentalRelease);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ReleaseAsset? SelectedExperimentalAsset
	{
		get
		{
			return _selectedExperimentalAsset;
		}
		set
		{
			if (!EqualityComparer<ReleaseAsset>.Default.Equals(_selectedExperimentalAsset, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedExperimentalAsset);
				_selectedExperimentalAsset = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedExperimentalAsset);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDownloadingExperimental
	{
		get
		{
			return _isDownloadingExperimental;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDownloadingExperimental, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsDownloadingExperimental);
				_isDownloadingExperimental = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsDownloadingExperimental);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ReleaseAsset> AvailableExperimentalAssets
	{
		get
		{
			return _availableExperimentalAssets;
		}
		[MemberNotNull("_availableExperimentalAssets")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ReleaseAsset>>.Default.Equals(_availableExperimentalAssets, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AvailableExperimentalAssets);
				_availableExperimentalAssets = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AvailableExperimentalAssets);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand FetchExperimentalReleasesCommand => fetchExperimentalReleasesCommand ?? (fetchExperimentalReleasesCommand = new AsyncRelayCommand(FetchExperimentalReleases));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand InstallExperimentalBuildCommand => installExperimentalBuildCommand ?? (installExperimentalBuildCommand = new AsyncRelayCommand(InstallExperimentalBuild));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckForUpdatesCommand => checkForUpdatesCommand ?? (checkForUpdatesCommand = new AsyncRelayCommand(CheckForUpdates));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ReleaseBuildCard> InstallBuildCommand => installBuildCommand ?? (installBuildCommand = new AsyncRelayCommand<ReleaseBuildCard>(InstallBuild));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelDownloadCommand => cancelDownloadCommand ?? (cancelDownloadCommand = new RelayCommand(CancelDownload));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RefreshInstalledVersionsCommand => refreshInstalledVersionsCommand ?? (refreshInstalledVersionsCommand = new AsyncRelayCommand(RefreshInstalledVersions));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<InstalledVersionItem> SwitchVersionCommand => switchVersionCommand ?? (switchVersionCommand = new AsyncRelayCommand<InstalledVersionItem>(SwitchVersion));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<InstalledVersionItem> UninstallVersionCommand => uninstallVersionCommand ?? (uninstallVersionCommand = new AsyncRelayCommand<InstalledVersionItem>(UninstallVersion));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectLlamaCppDirectoryCommand => selectLlamaCppDirectoryCommand ?? (selectLlamaCppDirectoryCommand = new AsyncRelayCommand(SelectLlamaCppDirectory));

	public LlamaReleasesViewModel(ILlamaUpdater updater, ISettings settings, ILogService log, IDialogService dialog, ILocalizationService loc, IServerManager serverManager, IProfileManager profileManager, IVersionCache versionCache)
	{
		_updater = updater;
		_settings = settings;
		_log = log;
		_dialog = dialog;
		_loc = loc;
		_serverManager = serverManager;
		_profileManager = profileManager;
		_versionCache = versionCache;
		_updater.DownloadProgress += delegate(object? _, double progress)
		{
			Dispatcher.UIThread.Post(delegate
			{
				DownloadProgress = progress;
			});
		};
		_updater.StatusMessage += delegate(object? _, string msg)
		{
			Dispatcher.UIThread.Post(delegate
			{
				DownloadStatus = msg;
			});
		};
		ActiveVersion = _settings.ActiveLlamaCppVersion;
		_settings.SettingsChanged += OnSettingsChanged;
		RefreshInstalledVersions();
		CheckForUpdates();
		_loc.OnLanguageChanged += delegate
		{
			string[] localizedPropertyNames = new string[23]
			{
				"Title", "ReleasesSection", "CheckForUpdatesBtn", "CheckForUpdatesBtnDisplay", "ShowOnlyLatestLabel", "CheckingStatus", "InstalledVersionsTitle", "RefreshBtn", "InstallBtn", "InstallingBtn",
				"InstalledBtn", "UseBtn", "RemoveBtn", "ActiveBadge", "TtInstallBuild", "TabOfficial", "TabExperimental", "SelectRepoLabel", "SelectReleaseLabel", "FetchingRepoStatus",
				"NoExperimentalRepos", "NoAssets", "ExperimentalBadge"
			};
			foreach (string propertyName in localizedPropertyNames)
			{
				OnPropertyChanged(propertyName);
			}
		};
	}

	[RelayCommand]
	private async Task FetchExperimentalReleases()
	{
		if (string.IsNullOrWhiteSpace(SelectedExperimentalRepo))
		{
			return;
		}
		IsFetchingExperimental = true;
		ExperimentalStatus = FetchingRepoStatus;
		ExperimentalBuildCards.Clear();
		ExperimentalReleases.Clear();
		SelectedExperimentalRelease = null;
		SelectedExperimentalAsset = null;
		try
		{
			foreach (LlamaCppRelease release in await _updater.FetchReleasesFromRepoAsync(SelectedExperimentalRepo))
			{
				ExperimentalReleases.Add(release);
			}
			if (ExperimentalReleases.Count > 0)
			{
				SelectedExperimentalRelease = ExperimentalReleases[0];
				ExperimentalStatus = _loc.T("llama_releases.latest") + ": " + ExperimentalReleases[0].TagName;
			}
			else
			{
				ExperimentalStatus = NoAssets;
			}
		}
		catch (Exception ex)
		{
			ExperimentalStatus = "Error: " + ex.Message;
			_log.Error(ex, "Failed to fetch experimental releases", "LlamaReleases");
		}
		finally
		{
			IsFetchingExperimental = false;
		}
	}

	[RelayCommand]
	private async Task InstallExperimentalBuild()
	{
		if (SelectedExperimentalAsset == null || SelectedExperimentalRelease == null)
		{
			return;
		}
		IsDownloadingExperimental = true;
		OnPropertyChanged("CanInstallExperimental");
		ReleaseBuildCard card = new ReleaseBuildCard
		{
			ReleaseTag = SelectedExperimentalRelease.TagName,
			ReleaseDate = SelectedExperimentalRelease.PublishedAt,
			IsPrerelease = true,
			Asset = SelectedExperimentalAsset,
			BuildType = SelectedExperimentalAsset.BuildType,
			BuildDescription = SelectedExperimentalAsset.Description,
			SizeDisplay = SelectedExperimentalAsset.SizeDisplay,
			AssetName = SelectedExperimentalAsset.Name,
			InstallBtnText = InstallingBtn,
			IsInstalled = false
		};
		DownloadStatus = $"Downloading {card.ReleaseTag} ({card.BuildDescription})...";
		DownloadProgress = 0.0;
		// Changing process priority requires extra privileges on Linux and may throw
		// before the download's error handling begins.  The desktop Windows behavior
		// is retained, while Linux keeps its scheduler-assigned priority.
		if (OperatingSystem.IsWindows())
		{
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
		}
		try
		{
			string baseDir = GetBaseLlamaDir();
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			string installPath = await _updater.DownloadAndExtractAsync(card.ReleaseTag, baseDir, card.Asset, default, EffectiveNccl(card));
			string activeLlamaCppVersion = (ActiveVersion = Path.GetFileName(installPath.TrimEnd(Path.DirectorySeparatorChar)));
			_settings.LlamaCppDirectory = installPath;
			_settings.ActiveLlamaCppVersion = activeLlamaCppVersion;
			await _settings.SaveAsync();
			DownloadStatus = $"Installed: {card.ReleaseTag} ({card.BuildDescription})";
			DownloadProgress = 100.0;
			card.InstallBtnText = InstalledBtn;
			card.IsInstalled = true;
			_log.Information("Installed experimental: " + card.ReleaseTag + " -> " + installPath, "LlamaReleases");
			await _versionCache.ScanAsync(baseDir);
			await RefreshInstalledVersions();
			foreach (ReleaseBuildCard releaseCard in ReleaseBuildCards.ToList())
			{
				bool isInstalled = (releaseCard.IsInstalled = IsAssetInstalled(baseDir, releaseCard.ReleaseTag, releaseCard.AssetName, NcclEnabled));
				releaseCard.InstallBtnText = (isInstalled ? InstalledBtn : InstallBtn);
			}
		}
		catch (Exception ex)
		{
			DownloadStatus = "Error: " + ex.Message;
			_log.Error(ex, "Experimental download failed", "LlamaReleases");
			InstallFailed?.Invoke(this, ex.Message);
		}
		finally
		{
			IsDownloadingExperimental = false;
			OnPropertyChanged("CanInstallExperimental");
			if (OperatingSystem.IsWindows())
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
			}
		}
	}

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		IsCheckingUpdates = true;
		UpdateStatus = _loc.T("llama_releases.checking");
		ReleaseBuildCards.Clear();
		// r84: the cafe-llama.cpp local build card is no longer offered in the
		// release list — official llama.cpp releases only. People who need the
		// custom build compile it themselves (folder detection for already
		// installed cafe builds is kept in IsAssetInstalled).
		try
		{
		// r42: show recent builds (prerelease b####) too — llama.cpp now ships
		// almost everything as prerelease, so a stable-only list is empty.
		// Prerelease cards are already marked on the page (IsPrerelease badge).
		List<LlamaCppRelease> releases = await _updater.FetchReleasesAsync(true);
		if (ShowOnlyLatest && releases.Count > 1)
		{
			string latestTag = releases[0].TagName;
			releases = releases.Where((LlamaCppRelease release) => release.TagName == latestTag).ToList();
		}
		// Evaluate all cards off the UI thread: IsAssetInstalled performs a
		// recursive Directory.GetFiles + spawns llama-server --version per card.
		// With 100+ cards on the UI thread this freezes the app (r44 bug).
		string baseLlamaDir = GetBaseLlamaDir();
		bool ncclSnapshot = NcclEnabled;
		var newCards = await Task.Run(() =>
		{
			var cards = new List<ReleaseBuildCard>();
			foreach (LlamaCppRelease release in releases)
			{
				foreach (ReleaseAsset asset in release.Assets.Where(IsSupportedReleaseAsset))
				{
					bool isInstalled = IsAssetInstalled(baseLlamaDir, release.TagName, asset.Name, ncclSnapshot);
					cards.Add(new ReleaseBuildCard
					{
						ReleaseTag = release.TagName,
						ReleaseDate = release.PublishedAt,
						IsPrerelease = release.IsPrerelease,
						Asset = asset,
						BuildType = asset.BuildType,
						BuildDescription = asset.Description,
						SizeDisplay = asset.SizeDisplay,
						AssetName = asset.Name,
						InstallBtnText = (isInstalled ? InstalledBtn : InstallBtn),
						IsInstalled = isInstalled
					});
				}
			}
			return cards;
		});
		foreach (ReleaseBuildCard card in newCards)
		{
			ReleaseBuildCards.Add(card);
		}
		UpdateStatus = ((releases.Count > 0) ? (_loc.T("llama_releases.latest") + ": " + releases[0].TagName) : _loc.T("llama_releases.no_releases"));
		}
		catch (Exception ex)
		{
			UpdateStatus = "Error: " + ex.Message;
			_log.Error(ex, "Update check failed", "LlamaReleases");
		}
		finally
		{
			IsCheckingUpdates = false;
		}
	}

	private static bool IsAssetInstalled(string baseDir, string version, string assetName, bool ncclEnabled)
	{
	if (!Directory.Exists(baseDir))
	{
	return false;
	}
	// The cafe-llama card installs to a single fixed, dedicated folder
	// (never a versioned tag), so it is matched by folder name only — the
	// general CUDA/NCCL variant rules below do not apply to it.
	if (assetName.StartsWith("llama-studio-build-cafe-llama-", StringComparison.OrdinalIgnoreCase))
	{
	string cafeServer = Path.Combine(baseDir, "cafe-llama-cuda12x-sm80-nccl", OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server");
	return File.Exists(cafeServer);
	}
	// The Linux CUDA + RPC card is produced locally by LlamaStudio.  Its
	// package name does not contain the usual "cuda-12" marker used by the
	// GitHub archives, so without this flag it was mistakenly tested as CPU.
		bool isLinuxCudaRpcBuild = OperatingSystem.IsLinux()
			&& (assetName.StartsWith("llama-studio-build-ubuntu-cuda-rpc-", StringComparison.OrdinalIgnoreCase)
				|| assetName.StartsWith("llama-studio-build-linux-cuda-rpc-local-", StringComparison.OrdinalIgnoreCase)
				|| assetName.StartsWith("llama-studio-build-linux-cuda-rpc-universal-", StringComparison.OrdinalIgnoreCase)
				|| assetName.StartsWith("llama-studio-build-linux-cuda12-rpc-", StringComparison.OrdinalIgnoreCase)
				|| assetName.StartsWith("llama-studio-build-linux-cuda13-rpc-", StringComparison.OrdinalIgnoreCase));
		bool isLocalLinuxCudaBuild = assetName.Contains("-rpc-local-", StringComparison.OrdinalIgnoreCase);
		bool isUniversalLinuxCudaBuild = assetName.Contains("-rpc-universal-", StringComparison.OrdinalIgnoreCase);
		string requestedCuda = assetName.Contains("cuda13", StringComparison.OrdinalIgnoreCase) ? "13" : (assetName.Contains("cuda12", StringComparison.OrdinalIgnoreCase) ? "12" : string.Empty);
		foreach (string versionDir in Directory.EnumerateDirectories(baseDir))
		{
			string serverExePath = Path.Combine(versionDir, OperatingSystem.IsWindows() ? "llama-server.exe" : "llama-server");
			if (!File.Exists(serverExePath))
			{
				continue;
			}
			IEnumerable<string> fileNames = Directory.GetFiles(versionDir, OperatingSystem.IsWindows() ? "*.dll" : "*", SearchOption.AllDirectories)
				.Select(Path.GetFileName)
				.Where((string? fileName) => !string.IsNullOrWhiteSpace(fileName))
				.Select((string fileName) => fileName.ToLowerInvariant());
			string folderName = Path.GetFileName(versionDir).ToLowerInvariant();
			// libggml-cuda exists in both CUDA 12 and CUDA 13 builds.  Do not
			// use it to decide that two locally-built CUDA variants are the same
			// installation: their folder identity explicitly contains cuda12/cuda13.
			if (isLinuxCudaRpcBuild && requestedCuda.Length > 0
				&& !folderName.Contains("cuda" + requestedCuda, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			bool isCuda13 = folderName.Contains("cuda13") || fileNames.Any((string fileName) => fileName.Contains("cudart64_13"));
			bool isCuda12 = folderName.Contains("cuda12") || fileNames.Any((string fileName) => fileName.Contains("cudart64_12") || fileName.Contains("libggml-cuda") || fileName.Contains("ggml-cuda"));
			if (isLocalLinuxCudaBuild && !folderName.Contains("-local-sm"))
			{
				continue;
			}
			if (isUniversalLinuxCudaBuild && folderName.Contains("-local-sm"))
			{
				continue;
			}
			// NCCL variant: local/universal CUDA install folders carry a trailing
			// "-nccl" when they were built with -DGGML_CUDA_NCCL=ON.  The card's
			// NCCL checkbox selects the variant, so a folder only satisfies the
			// card when its NCCL suffix matches the checkbox.  This keeps all four
			// variants (CUDA12/13 x with/without NCCL) installable side by side.
			if (isLocalLinuxCudaBuild || isUniversalLinuxCudaBuild)
			{
				bool folderHasNccl = folderName.EndsWith("-nccl", StringComparison.OrdinalIgnoreCase);
				if (folderHasNccl != ncclEnabled)
				{
					continue;
				}
			}
			if (requestedCuda == "13" && !isCuda13)
			{
				continue;
			}
			if (requestedCuda == "12" && !isCuda12)
			{
				continue;
			}
			bool isVulkan = fileNames.Any((string fileName) => fileName.Contains("ggml-vulkan") || fileName.Contains("vulkan-1"));
			if (assetName.Contains("cuda-13") || assetName.Contains("cuda13"))
			{
				if (!isCuda13)
				{
					continue;
				}
			}
			else if (isLinuxCudaRpcBuild)
			{
				// CUDA identity was verified above; this only rejects non-CUDA folders.
				if (!isCuda13 && !isCuda12)
				{
					continue;
				}
			}
			else if (assetName.Contains("cuda-12") || assetName.Contains("cuda12"))
			{
				if (!isCuda12)
				{
					continue;
				}
			}
			else if (assetName.Contains("vulkan"))
			{
				if (!isVulkan)
				{
					continue;
				}
			}
			else if (!assetName.Contains("cpu") || fileNames.Any((string fileName) => fileName.Contains("cudart") || fileName.Contains("cublas") || fileName.Contains("vulkan")))
			{
				continue;
			}
			if (TryGetVersionFromExe(serverExePath, out string foundVersion))
			{
				if (foundVersion.Equals(version, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			if (Path.GetFileName(versionDir).Contains(version, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryGetVersionFromExe(string exePath, out string version)
	{
		version = string.Empty;
		try
		{
			string exeDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
			string systemPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
			string combinedPath = exeDirectory + Path.PathSeparator + systemPath;
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				FileName = OperatingSystem.IsWindows() ? "cmd.exe" : exePath,
				Arguments = OperatingSystem.IsWindows() ? "/c \"" + exePath + "\" --version" : "--version",
				WorkingDirectory = exeDirectory,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
			{
				processStartInfo.Environment[(string)environmentVariable.Key] = (string)environmentVariable.Value;
			}
			processStartInfo.Environment["PATH"] = combinedPath;
			using Process process = Process.Start(processStartInfo);
			if (process == null)
			{
				return false;
			}
			process.WaitForExit(10000);
			Match match = Regex.Match(process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd(), "version:\\s*(\\d+)");
			if (match.Success)
			{
				version = "b" + match.Groups[1].Value;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	[RelayCommand]
	private async Task InstallBuild(ReleaseBuildCard card)
	{
		if (card.IsInstalled || IsDownloading)
		{
			return;
		}
		_downloadCts = new CancellationTokenSource();
		IsDownloading = true;
		OnPropertyChanged("CanCancelDownload");
		OnPropertyChanged("CanPauseDownload");
		card.InstallBtnText = InstallingBtn;
		DownloadStatus = $"Downloading {card.ReleaseTag} ({card.BuildDescription})...";
		DownloadProgress = 0.0;
		if (OperatingSystem.IsWindows())
		{
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
		}
		try
		{
			string baseDir = GetBaseLlamaDir();
			if (!Directory.Exists(baseDir))
			{
				Directory.CreateDirectory(baseDir);
			}
			string installPath = await _updater.DownloadAndExtractAsync(card.ReleaseTag, baseDir, card.Asset, _downloadCts.Token, EffectiveNccl(card));
			if (string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory))
			{
				_settings.LlamaCppBaseDirectory = baseDir;
				await _settings.SaveAsync();
				OnPropertyChanged("LlamaCppBaseDirectoryDisplay");
			}
			DownloadStatus = $"Installed: {card.ReleaseTag} ({card.BuildDescription})";
			DownloadProgress = 100.0;
			card.InstallBtnText = InstalledBtn;
			card.IsInstalled = true;
			_log.Information("Installed: " + card.ReleaseTag + " -> " + installPath, "LlamaReleases");
			await _versionCache.ScanAsync(baseDir);
			await RefreshInstalledVersions();
			await RefreshReleaseCardsAsync();
		}
		catch (OperationCanceledException)
		{
			DownloadStatus = "Download cancelled";
			card.InstallBtnText = InstallBtn;
			_log.Information("Download cancelled", "LlamaReleases");
		}
		catch (Exception ex2)
		{
			DownloadStatus = "Error: " + ex2.Message;
			card.InstallBtnText = InstallBtn;
			_log.Error(ex2, "Download failed", "LlamaReleases");
			InstallFailed?.Invoke(this, ex2.Message);
		}
		finally
		{
			IsDownloading = false;
			_downloadCts = null;
			DownloadProgress = 0.0;
			DownloadStatus = string.Empty;
			OnPropertyChanged("CanCancelDownload");
			OnPropertyChanged("CanPauseDownload");
			if (OperatingSystem.IsWindows())
			{
				Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
			}
		}
	}

	[RelayCommand]
	private void CancelDownload()
	{
		if (_downloadCts != null && !_downloadCts.IsCancellationRequested)
		{
			_downloadCts.Cancel();
			DownloadStatus = "Cancelling download...";
		}
	}

	[RelayCommand]
	private async Task RefreshInstalledVersions()
	{
		string baseLlamaDir = GetBaseLlamaDir();
		_log.Information("RefreshInstalledVersions scanning: " + baseLlamaDir, "LlamaReleases");
		if (!Directory.Exists(baseLlamaDir))
		{
			_log.Warning("Base dir not found: " + baseLlamaDir, "LlamaReleases");
			NoServersFound = true;
			return;
		}
		try
		{
			await _versionCache.ScanAsync(baseLlamaDir);
			IReadOnlyList<VersionInfo> all = _versionCache.GetAll();
			_log.Information($"Found {all.Count} versions: [{string.Join(", ", all.Select((VersionInfo versionInfo) => versionInfo.Display))}]", "LlamaReleases");

			// Resolve the active directory from the profile if settings is empty
			// (e.g. after a fresh install where the profile was created before
			// LlamaCppDirectory was explicitly set in settings).
			string activeDir = _settings.LlamaCppDirectory ?? string.Empty;
			if (string.IsNullOrWhiteSpace(activeDir))
			{
				ServerProfile activeProfile = await GetActiveProfileAsync();
				string? profileVersionKey = activeProfile?.SelectedServerVersion;
				if (!string.IsNullOrWhiteSpace(profileVersionKey))
				{
					string? folder = _versionCache.GetFolderByVersionKey(profileVersionKey)
						?? _versionCache.GetFolderByVersion(profileVersionKey.Split('|')[0]);
					if (folder != null)
					{
						activeDir = Path.Combine(baseLlamaDir, folder);
						_log.Information("RefreshInstalledVersions: resolved active dir from profile: " + activeDir, "LlamaReleases");
					}
				}
			}

			InstalledVersionItems.Clear();
			string normActiveDir = (activeDir ?? string.Empty).TrimEnd('/', '\\');
			string activeVersionKey = _settings.ActiveLlamaCppVersion ?? string.Empty;
			string activeVersionPart = activeVersionKey.Contains('|') ? activeVersionKey.Split('|')[0] : string.Empty;
			string activeBuildPart = activeVersionKey.Contains('|') ? activeVersionKey.Split('|', 2)[1] : string.Empty;
			foreach (VersionInfo versionInfo in all)
			{
				string fileName = Path.GetFileName(versionInfo.Directory);
				string normItemDir = (versionInfo.Directory ?? string.Empty).TrimEnd('/', '\\');
				bool pathMatch = normActiveDir.Length > 0 && normItemDir.Equals(normActiveDir, StringComparison.OrdinalIgnoreCase);
				bool keyMatch = activeVersionKey.Contains('|') && string.Equals(versionInfo.Version, activeVersionPart, StringComparison.OrdinalIgnoreCase) && string.Equals(versionInfo.BuildType, activeBuildPart, StringComparison.OrdinalIgnoreCase);
				bool isActive = pathMatch || keyMatch;
				// r73: make Unsloth MTP builds visually distinct from official llama.cpp.
				string unslothDesc = versionInfo.Version.Contains("unsloth-mtp", StringComparison.OrdinalIgnoreCase) ? versionInfo.BuildType + " · Unsloth MTP" : versionInfo.BuildType;
				InstalledVersionItems.Add(new InstalledVersionItem(versionInfo.Version, fileName, isActive, Enum.TryParse<BuildType>(versionInfo.BuildType, ignoreCase: true, out var parsedBuildType) ? parsedBuildType : BuildType.Unknown, unslothDesc, versionInfo.Directory, versionInfo.VersionDetail));
			}
			NoServersFound = all.Count == 0;
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to list installed versions", "LlamaReleases");
		}
	}

	private async Task RefreshReleaseCardsAsync()
	{
		string baseLlamaDir = GetBaseLlamaDir();
		foreach (ReleaseBuildCard card in ReleaseBuildCards.ToList())
		{
			bool isInstalled = (card.IsInstalled = IsAssetInstalled(baseLlamaDir, card.ReleaseTag, card.AssetName, NcclEnabled));
			card.InstallBtnText = (isInstalled ? InstalledBtn : InstallBtn);
			OnPropertyChanged("ReleaseBuildCards");
		}
	}

	[RelayCommand]
	private async Task SwitchVersion(InstalledVersionItem versionItem)
	{
		if (versionItem == null)
		{
			return;
		}
		string baseLlamaDir = GetBaseLlamaDir();
		string newPath = Path.Combine(baseLlamaDir, versionItem.FolderName);
		if (!Directory.Exists(newPath) || !File.Exists(Path.Combine(newPath, LlamaServerExecutableName)))
		{
			_log.Warning("Version " + versionItem.Version + " not found or incomplete", "LlamaReleases");
			return;
		}
		ServerStatus serverStatus = await _serverManager.GetStatusAsync();
		bool wasRunning = serverStatus.State == ServerState.Running || serverStatus.State == ServerState.Starting;
		IDisposable loadingWin = null;
		if (wasRunning)
		{
			loadingWin = _dialog.ShowLoading(string.Format(_loc.T("llama_releases.restart_server"), versionItem.Description), _loc.T("common.loading"));
		}
		try
		{
			if (wasRunning)
			{
				_log.Information("Stopping server before switching to " + versionItem.Version, "LlamaReleases");
				await _serverManager.StopAsync();
				for (int i = 0; i < 30; i++)
				{
					await Task.Delay(500);
					if ((await _serverManager.GetStatusAsync()).State == ServerState.Stopped)
					{
						break;
					}
				}
			}
			VersionInfo byPath = _versionCache.GetByPath(newPath);
			if (byPath?.Version == null)
			{
				_ = versionItem.FolderName;
			}
			string versionKey = (ActiveVersion = byPath?.VersionKey ?? versionItem.FolderName);
			_settings.LlamaCppDirectory = newPath;
			_settings.ActiveLlamaCppVersion = versionKey;
			await _settings.SaveAsync();
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile != null)
			{
				serverProfile.SelectedServerVersion = versionKey;
				await _profileManager.SaveProfileAsync(serverProfile);
			}
			_log.Information("Switched to version " + versionKey + " -> " + newPath, "LlamaReleases");
			if (versionItem.BuildType == BuildType.Cuda12x || versionItem.BuildType == BuildType.Cuda13x)
			{
				string[] files = Directory.GetFiles(newPath, "*.dll", SearchOption.TopDirectoryOnly);
				_log.Information($"DLLs in {newPath}: [{string.Join(", ", files.Select((string filePath) => Path.GetFileName(filePath)))}]", "LlamaReleases");
				if (!files.Where((string filePath) => filePath.Contains("cudart64")).ToList().Any())
				{
					string ggmlCudaFile = files.FirstOrDefault((string filePath) => filePath.Contains("ggml-cuda"));
					string ggmlCudaSize = ((ggmlCudaFile != null) ? $"{Math.Round((double)new FileInfo(ggmlCudaFile).Length / 1024.0 / 1024.0, 1)} MB" : "N/A");
					_log.Warning($"CUDA runtime DLL (cudart64_*.dll) NOT found in {newPath}! ggml-cuda.dll size: {ggmlCudaSize}. CUDA may not work — install with internet for automatic cudart download.", "LlamaReleases");
				}
			}
			await RefreshInstalledVersions();
			if (wasRunning)
			{
				ServerProfile serverProfile2 = await GetActiveProfileAsync();
				if (serverProfile2 != null && !string.IsNullOrWhiteSpace(serverProfile2.ModelPath))
				{
					_log.Information("Restarting server with profile: " + serverProfile2.Name, "LlamaReleases");
					await _serverManager.StartAsync(serverProfile2);
					_log.Information("Server restarted with version " + versionItem.Version, "LlamaReleases");
				}
			}
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to switch version to " + versionItem.Version + ": " + ex.Message, "LlamaReleases");
		}
		finally
		{
			loadingWin?.Dispose();
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

	[RelayCommand]
	private async Task UninstallVersion(InstalledVersionItem versionItem)
	{
		if (versionItem.IsActive)
		{
			await _dialog.ShowErrorAsync(_loc.T("llama_releases.cannot_delete_active"));
			return;
		}
		string baseLlamaDir = GetBaseLlamaDir();
		string path = Path.Combine(baseLlamaDir, versionItem.FolderName);
		_log.Information("Uninstalling " + versionItem.Version + " from " + path, "LlamaReleases");
		try
		{
			await _updater.UninstallVersionAsync(baseLlamaDir, versionItem.FolderName);
			if (Directory.Exists(path))
			{
				_log.Error("Directory still exists after uninstall: " + path, "LlamaReleases");
				await _dialog.ShowErrorAsync(string.Format(_loc.T("llama_releases.delete_failed_folder"), versionItem.Version));
				return;
			}
			await _versionCache.ScanAsync(baseLlamaDir);
			InstalledVersionItems.Remove(versionItem);
			_log.Information("Successfully uninstalled " + versionItem.Version, "LlamaReleases");
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to uninstall " + versionItem.Version, "LlamaReleases");
			await _dialog.ShowErrorAsync(string.Format(_loc.T("llama_releases.delete_failed_error"), versionItem.Version, ex.Message));
		}
	}

	[RelayCommand]
	private async Task SelectLlamaCppDirectory()
	{
		string path = await _dialog.SelectFolderAsync("Select llama.cpp Base Directory", _settings.LlamaCppBaseDirectory);
		if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
		{
			string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string rootPath = (Path.GetPathRoot(fullPath) ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase))
			{
				await _dialog.ShowErrorAsync("Выберите папку с версиями llama.cpp, а не корень диска. Например: C:\\LlamaServers\\ или C:\\Users\\info\\AppData\\Roaming\\LlamaStudio\\.");
				return;
			}
			_settings.LlamaCppBaseDirectory = path;
			await _settings.SaveAsync();
			_versionCache.Clear();
			await _versionCache.ScanAsync(path);
			ServerProfile serverProfile = await GetActiveProfileAsync();
			if (serverProfile != null)
			{
				serverProfile.LlamaCppBaseDirectory = path;
				await _profileManager.SaveProfileAsync(serverProfile);
			}
			OnPropertyChanged("LlamaCppBaseDirectoryDisplay");
			_log.Information("llama.cpp base directory set: " + path, "LlamaReleases");
			await RefreshInstalledVersions();
		}
	}

	private string GetBaseLlamaDir()
	{
		if (!string.IsNullOrWhiteSpace(_settings.LlamaCppBaseDirectory) && Directory.Exists(_settings.LlamaCppBaseDirectory))
		{
			_log.Information("GetBaseLlamaDir: using base dir " + _settings.LlamaCppBaseDirectory, "LlamaReleases");
			return _settings.LlamaCppBaseDirectory;
		}
		string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LlamaStudio");
		if (Directory.Exists(defaultDir))
		{
			_log.Information("GetBaseLlamaDir: using default dir " + defaultDir, "LlamaReleases");
			return defaultDir;
		}
		if (!string.IsNullOrWhiteSpace(_settings.LlamaCppDirectory))
		{
			string settingsDir = _settings.LlamaCppDirectory.TrimEnd(Path.DirectorySeparatorChar);
			string fileName = Path.GetFileName(settingsDir);
			if (File.Exists(Path.Combine(settingsDir, LlamaServerExecutableName)))
			{
				string serverParentDir = Path.GetDirectoryName(settingsDir) ?? settingsDir;
				_log.Information("GetBaseLlamaDir: using parent of server dir " + serverParentDir, "LlamaReleases");
				return serverParentDir;
			}
			if ((fileName.StartsWith("b", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("v", StringComparison.OrdinalIgnoreCase)) && Directory.Exists(settingsDir))
			{
				string versionedParentDir = Path.GetDirectoryName(settingsDir) ?? settingsDir;
				_log.Information("GetBaseLlamaDir: using parent of versioned dir " + versionedParentDir, "LlamaReleases");
				return versionedParentDir;
			}
			_log.Information("GetBaseLlamaDir: using settings dir " + settingsDir, "LlamaReleases");
			return settingsDir;
		}
		_log.Information("GetBaseLlamaDir: using default dir " + defaultDir, "LlamaReleases");
		return defaultDir;
	}

	private static bool IsSupportedReleaseAsset(ReleaseAsset asset)
	{
		string name = asset.Name ?? string.Empty;
		if (OperatingSystem.IsWindows())
		{
			return name.Contains("-win-", StringComparison.OrdinalIgnoreCase);
		}
		if (OperatingSystem.IsLinux())
		{
			return name.Contains("-ubuntu-", StringComparison.OrdinalIgnoreCase) || name.Contains("-linux-", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private void OnSettingsChanged()
	{
		Dispatcher.UIThread.InvokeAsync(delegate
		{
			if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion) && _settings.ActiveLlamaCppVersion != ActiveVersion)
			{
				ActiveVersion = _settings.ActiveLlamaCppVersion;
				RefreshInstalledVersions();
			}
		});
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsCheckingUpdatesChanged(bool value)
	{
		OnPropertyChanged("CheckForUpdatesBtnDisplay");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnShowOnlyLatestChanged(bool value)
	{
		CheckForUpdates();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnActiveVersionChanged(string value)
	{
		OnPropertyChanged("ActiveVersionDisplay");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnIsExperimentalTabChanged(bool value)
	{
		if (value && ExperimentalBuildCards.Count == 0)
		{
			FetchExperimentalReleases();
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedExperimentalRepoChanged(string value)
	{
		FetchExperimentalReleases();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedExperimentalReleaseChanged(LlamaCppRelease? value)
	{
		AvailableExperimentalAssets.Clear();
		SelectedExperimentalAsset = null;
		if (value == null)
		{
			return;
		}
		foreach (ReleaseAsset asset in value.Assets.Where(IsSupportedReleaseAsset))
		{
			AvailableExperimentalAssets.Add(asset);
		}
		if (AvailableExperimentalAssets.Count > 0)
		{
			SelectedExperimentalAsset = AvailableExperimentalAssets[0];
		}
	}
}
