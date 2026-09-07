using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Infrastructure.Chat;

namespace LlamaStudio.ViewModels;

public class ChatViewModel : ObservableObject
{
	private readonly IChatService _chatService;

	private readonly IMcpToolsService _mcpToolsService;

	private readonly ChatSessionStore _sessionStore;

	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly ILocalizationService _loc;

	private readonly IServerManager _serverManager;

	private readonly IProfileManager _profileManager;

	private readonly IFilePickerService _filePicker;

	private readonly IDialogService _dialog;

	[ObservableProperty]
	private ObservableCollection<ChatSession> _sessions = new ObservableCollection<ChatSession>();

	[ObservableProperty]
	private ChatSession? _selectedSession;

	[ObservableProperty]
	private ObservableCollection<ChatMessage> _messages = new ObservableCollection<ChatMessage>();

	[ObservableProperty]
	private string _inputText = "";

	[ObservableProperty]
	private bool _isGenerating;

	[ObservableProperty]
	private bool _showSettings;

	[ObservableProperty]
	private bool _mcpToolsEnabled;

	[ObservableProperty]
	private bool _showMcpServers;

	[ObservableProperty]
	private bool _hideReasoning = true;

	[ObservableProperty]
	private ObservableCollection<McpServerConfig> _mcpServers = new ObservableCollection<McpServerConfig>();

	[ObservableProperty]
	private string _newServerName = "";

	[ObservableProperty]
	private string _newServerCommand = "";

	[ObservableProperty]
	private string _newServerArgs = "";

	[ObservableProperty]
	private string _newServerUrl = "http://127.0.0.1:8080";

	[ObservableProperty]
	private McpTransportType _selectedTransportType;

	[ObservableProperty]
	private List<string> _attachedFilesPreview = new List<string>();

	[ObservableProperty]
	private ObservableCollection<string> _attachedImageBase64 = new ObservableCollection<string>();

	private ChatSession? _renamingSession;

	[ObservableProperty]
	private string _renameText = "";

	[ObservableProperty]
	private ServerState _serverState;

	[ObservableProperty]
	private string _serverModelName = "";

	[ObservableProperty]
	private float _temperature = 0.8f;

	[ObservableProperty]
	private float _topP = 0.95f;

	[ObservableProperty]
	private int _topK = 40;

	[ObservableProperty]
	private float _minP = 0.05f;

	[ObservableProperty]
	private float _repeatPenalty = 1f;

	[ObservableProperty]
	private float _presencePenalty;

	[ObservableProperty]
	private float _frequencyPenalty;

	[ObservableProperty]
	private int _seed = -1;

	[ObservableProperty]
	private int _maxTokens = -1;

	[ObservableProperty]
	private string _stopSequences = "";

	[ObservableProperty]
	private string _systemPrompt = "";

	[ObservableProperty]
	private string _host = "localhost";

	[ObservableProperty]
	private int _port = 8080;

	[ObservableProperty]
	private int _contextSize = 8192;

	[ObservableProperty]
	private int _contextUsedTokens;

	[ObservableProperty]
	private double _contextUsedPercent;

	[ObservableProperty]
	private bool _isCompressing;

	private CancellationTokenSource? _cancellationTokenSource;

	private System.Timers.Timer? _streamTimer;

	private System.Timers.Timer? _healthCheckTimer;

	private System.Timers.Timer? _generatingAnimTimer;

	[ObservableProperty]
	private string _generatingText = "";

	[ObservableProperty]
	private double _dot1Opacity = 0.3;

	[ObservableProperty]
	private double _dot2Opacity = 0.6;

	[ObservableProperty]
	private double _dot3Opacity = 1.0;

	private static readonly int[] GeneratingPhraseDots = new int[4] { 0, 1, 2, 3 };

	private int _generatingPhraseIdx;

	private static readonly (double d1, double d2, double d3)[] DotFrames = new(double, double, double)[10]
	{
		(0.2, 0.4, 0.6),
		(0.4, 0.6, 0.8),
		(0.6, 0.8, 1.0),
		(0.8, 1.0, 0.8),
		(0.6, 0.8, 0.6),
		(0.4, 0.6, 0.4),
		(0.2, 0.4, 0.2),
		(0.4, 0.6, 0.4),
		(0.6, 0.8, 0.6),
		(0.8, 1.0, 0.8)
	};

	private int _dotFrameIdx;

	[ObservableProperty]
	private string _filesystemMcpFolder = "";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<ChatSession>? selectSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? newSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatSession?>? deleteSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatSession>? duplicateSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatSession>? clearSessionMessagesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatSession>? exportSessionCtxCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<ChatSession?>? startRenameSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? commitRenameSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? reorderSessionsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? cancelRenameSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleSettingsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleMcpToolsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleMcpServersCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? showMcpGuideCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? sendMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<string>? removeImageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatMessage>? copyMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<ChatMessage>? toggleBookmarkCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<ChatMessage>? deleteMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<ChatMessage>? undoMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatMessage>? regenerateMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ChatMessage>? editMessageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? stopGenerationCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? openWebUiCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? clearSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? exportSessionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? addMcpServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? removeMcpServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? toggleMcpServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? enableMcpServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<string>? disableMcpServerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? selectFilesystemFolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand<string>? copyCodeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand<ContentPart>? expandCodeCommand;

	public bool ShowReasoning => !HideReasoning;

	public List<McpTransportType> TransportTypes { get; } = Enum.GetValues<McpTransportType>().ToList();

	public ChatSession? RenamingSession
	{
		get
		{
			return _renamingSession;
		}
		set
		{
			if (_renamingSession != value)
			{
				_renamingSession = value;
				OnPropertyChanged("RenamingSession");
				OnPropertyChanged("IsRenaming");
			}
		}
	}

	public bool IsRenaming => RenamingSession != null;

	public string ContextUsageText => $"{ContextUsedTokens}/{ContextSize} tokens";

	public string Title => _loc.T("chat.title");

	public string NewSessionBtn => _loc.T("chat.new_session");

	public string DeleteSessionBtn => _loc.T("chat.delete_session");

	public string RenameSessionBtn => _loc.T("chat.rename_session");

	public string DuplicateSessionBtn => _loc.T("chat.duplicate_session");

	public string ExportSessionBtn => _loc.T("chat.export_session");

	public string ClearSessionBtn => _loc.T("chat.clear_session");

	public string DeleteSessionCtxBtn => _loc.T("chat.delete_session_ctx");

	public string SendBtn => _loc.T("chat.send");

	public string StopBtn => _loc.T("chat.stop");

	public string InputWatermark => _loc.T("chat.input_watermark");

	public string SettingsLabel => _loc.T("chat.settings");

	public string McpToolsLabel => _loc.T("chat.mcp_tools");

	public string McpServersLabel => _loc.T("chat.mcp_servers");

	public string McpServersTitle => _loc.T("chat.mcp_servers_title");

	public string McpAddServer => _loc.T("chat.add_mcp_server");

	public string McpName => _loc.T("chat.mcp_name");

	public string McpType => _loc.T("chat.mcp_type");

	public string McpCommand => _loc.T("chat.mcp_command");

	public string McpArgs => _loc.T("chat.mcp_args");

	public string McpUrl => _loc.T("chat.mcp_url");

	public string McpAdd => _loc.T("chat.mcp_add");

	public string McpToolsCount => _loc.T("chat.tools_count");

	public string ChatWelcome => _loc.T("chat.welcome");

	public string ChatWelcomeSub => _loc.T("chat.welcome_sub");

	public string ExperimentalWarning => _loc.T("chat.experimental");

	public string ChatStreaming => _loc.T("chat.streaming");

	public string ChatInputWatermark => _loc.T("chat.input_watermark");

	public string ChatCut => _loc.T("chat.cut");

	public string ChatCopy => _loc.T("chat.copy");

	public string ChatPaste => _loc.T("chat.paste");

	public string ChatSelectAll => _loc.T("chat.select_all");

	public string SystemPromptLabel => _loc.T("chat.system_prompt");

	public string SystemPromptWatermark => _loc.T("chat.system_prompt_watermark");

	public string ParamsLabel => _loc.T("chat.params");

	public string SessionsLabel => _loc.T("chat.sessions");

	public string NoSessionLabel => _loc.T("chat.no_session");

	public string CopyBtn => _loc.T("chat.copy");

	public string EditMessageBtn => _loc.T("chat.edit_message");

	public string RegenerateBtn => _loc.T("chat.regenerate");

	public string BookmarkBtn => _loc.T("chat.bookmark");

	public string UnbookmarkBtn => _loc.T("chat.unbookmark");

	public string DeleteMessageBtn => _loc.T("chat.delete_message");

	public string UndoMessageBtn => _loc.T("chat.undo_message");

	public string ReasoningHeader => _loc.T("chat.reasoning");

	public string HideReasoningLabel => _loc.T("chat.hide_reasoning");

	public string NoServerLabel => _loc.T("chat.no_server");

	public string ServerConnected => _loc.T("chat.server_connected");

	public string ServerDisconnected => _loc.T("chat.server_disconnected");

	public string McpGuideText => _loc.T("chat.mcp_guide");

	public string SetupFilesystemBtn => _loc.T("chat.setup_filesystem");

	public string SelectFsFolderBtn => _loc.T("chat.select_fs_folder");

	public string McpGuideBtn => _loc.T("chat.mcp_guide_btn");

	public string CopyCodeBtn => _loc.T("chat.copy_code");

	public string ExpandCodeBtn => _loc.T("chat.expand_code");

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ChatSession> Sessions
	{
		get
		{
			return _sessions;
		}
		[MemberNotNull("_sessions")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ChatSession>>.Default.Equals(_sessions, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Sessions);
				_sessions = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Sessions);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ChatSession? SelectedSession
	{
		get
		{
			return _selectedSession;
		}
		set
		{
			if (!EqualityComparer<ChatSession>.Default.Equals(_selectedSession, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedSession);
				_selectedSession = value;
				OnSelectedSessionChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedSession);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<ChatMessage> Messages
	{
		get
		{
			return _messages;
		}
		[MemberNotNull("_messages")]
		set
		{
			if (!EqualityComparer<ObservableCollection<ChatMessage>>.Default.Equals(_messages, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Messages);
				_messages = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Messages);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string InputText
	{
		get
		{
			return _inputText;
		}
		[MemberNotNull("_inputText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_inputText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.InputText);
				_inputText = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.InputText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsGenerating
	{
		get
		{
			return _isGenerating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isGenerating, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsGenerating);
				_isGenerating = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsGenerating);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowSettings
	{
		get
		{
			return _showSettings;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showSettings, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowSettings);
				_showSettings = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowSettings);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool McpToolsEnabled
	{
		get
		{
			return _mcpToolsEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mcpToolsEnabled, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.McpToolsEnabled);
				_mcpToolsEnabled = value;
				OnMcpToolsEnabledChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.McpToolsEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowMcpServers
	{
		get
		{
			return _showMcpServers;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showMcpServers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ShowMcpServers);
				_showMcpServers = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ShowMcpServers);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HideReasoning
	{
		get
		{
			return _hideReasoning;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hideReasoning, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HideReasoning);
				_hideReasoning = value;
				OnHideReasoningChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HideReasoning);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<McpServerConfig> McpServers
	{
		get
		{
			return _mcpServers;
		}
		[MemberNotNull("_mcpServers")]
		set
		{
			if (!EqualityComparer<ObservableCollection<McpServerConfig>>.Default.Equals(_mcpServers, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.McpServers);
				_mcpServers = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.McpServers);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewServerName
	{
		get
		{
			return _newServerName;
		}
		[MemberNotNull("_newServerName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_newServerName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.NewServerName);
				_newServerName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.NewServerName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewServerCommand
	{
		get
		{
			return _newServerCommand;
		}
		[MemberNotNull("_newServerCommand")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_newServerCommand, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.NewServerCommand);
				_newServerCommand = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.NewServerCommand);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewServerArgs
	{
		get
		{
			return _newServerArgs;
		}
		[MemberNotNull("_newServerArgs")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_newServerArgs, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.NewServerArgs);
				_newServerArgs = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.NewServerArgs);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewServerUrl
	{
		get
		{
			return _newServerUrl;
		}
		[MemberNotNull("_newServerUrl")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_newServerUrl, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.NewServerUrl);
				_newServerUrl = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.NewServerUrl);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public McpTransportType SelectedTransportType
	{
		get
		{
			return _selectedTransportType;
		}
		set
		{
			if (!EqualityComparer<McpTransportType>.Default.Equals(_selectedTransportType, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SelectedTransportType);
				_selectedTransportType = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SelectedTransportType);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public List<string> AttachedFilesPreview
	{
		get
		{
			return _attachedFilesPreview;
		}
		[MemberNotNull("_attachedFilesPreview")]
		set
		{
			if (!EqualityComparer<List<string>>.Default.Equals(_attachedFilesPreview, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AttachedFilesPreview);
				_attachedFilesPreview = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AttachedFilesPreview);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<string> AttachedImageBase64
	{
		get
		{
			return _attachedImageBase64;
		}
		[MemberNotNull("_attachedImageBase64")]
		set
		{
			if (!EqualityComparer<ObservableCollection<string>>.Default.Equals(_attachedImageBase64, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.AttachedImageBase64);
				_attachedImageBase64 = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.AttachedImageBase64);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string RenameText
	{
		get
		{
			return _renameText;
		}
		[MemberNotNull("_renameText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_renameText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RenameText);
				_renameText = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RenameText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ServerState ServerState
	{
		get
		{
			return _serverState;
		}
		set
		{
			if (!EqualityComparer<ServerState>.Default.Equals(_serverState, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ServerState);
				_serverState = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ServerState);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ServerModelName
	{
		get
		{
			return _serverModelName;
		}
		[MemberNotNull("_serverModelName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serverModelName, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ServerModelName);
				_serverModelName = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ServerModelName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float Temperature
	{
		get
		{
			return _temperature;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_temperature, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Temperature);
				_temperature = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Temperature);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float TopP
	{
		get
		{
			return _topP;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_topP, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TopP);
				_topP = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TopP);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TopK
	{
		get
		{
			return _topK;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_topK, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TopK);
				_topK = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TopK);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float MinP
	{
		get
		{
			return _minP;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_minP, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MinP);
				_minP = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MinP);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float RepeatPenalty
	{
		get
		{
			return _repeatPenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_repeatPenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.RepeatPenalty);
				_repeatPenalty = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.RepeatPenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float PresencePenalty
	{
		get
		{
			return _presencePenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_presencePenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.PresencePenalty);
				_presencePenalty = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.PresencePenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public float FrequencyPenalty
	{
		get
		{
			return _frequencyPenalty;
		}
		set
		{
			if (!EqualityComparer<float>.Default.Equals(_frequencyPenalty, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FrequencyPenalty);
				_frequencyPenalty = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FrequencyPenalty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Seed
	{
		get
		{
			return _seed;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_seed, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Seed);
				_seed = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Seed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int MaxTokens
	{
		get
		{
			return _maxTokens;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_maxTokens, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.MaxTokens);
				_maxTokens = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.MaxTokens);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string StopSequences
	{
		get
		{
			return _stopSequences;
		}
		[MemberNotNull("_stopSequences")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_stopSequences, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.StopSequences);
				_stopSequences = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.StopSequences);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string SystemPrompt
	{
		get
		{
			return _systemPrompt;
		}
		[MemberNotNull("_systemPrompt")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_systemPrompt, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.SystemPrompt);
				_systemPrompt = value;
				OnSystemPromptChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.SystemPrompt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return _host;
		}
		[MemberNotNull("_host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_host, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Host);
				_host = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_port, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Port);
				_port = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ContextSize
	{
		get
		{
			return _contextSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_contextSize, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContextSize);
				_contextSize = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContextSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int ContextUsedTokens
	{
		get
		{
			return _contextUsedTokens;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_contextUsedTokens, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContextUsedTokens);
				_contextUsedTokens = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContextUsedTokens);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double ContextUsedPercent
	{
		get
		{
			return _contextUsedPercent;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_contextUsedPercent, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ContextUsedPercent);
				_contextUsedPercent = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ContextUsedPercent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsCompressing
	{
		get
		{
			return _isCompressing;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isCompressing, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsCompressing);
				_isCompressing = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsCompressing);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string GeneratingText
	{
		get
		{
			return _generatingText;
		}
		[MemberNotNull("_generatingText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_generatingText, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.GeneratingText);
				_generatingText = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.GeneratingText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double Dot1Opacity
	{
		get
		{
			return _dot1Opacity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_dot1Opacity, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Dot1Opacity);
				_dot1Opacity = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Dot1Opacity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double Dot2Opacity
	{
		get
		{
			return _dot2Opacity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_dot2Opacity, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Dot2Opacity);
				_dot2Opacity = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Dot2Opacity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public double Dot3Opacity
	{
		get
		{
			return _dot3Opacity;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_dot3Opacity, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Dot3Opacity);
				_dot3Opacity = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Dot3Opacity);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string FilesystemMcpFolder
	{
		get
		{
			return _filesystemMcpFolder;
		}
		[MemberNotNull("_filesystemMcpFolder")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_filesystemMcpFolder, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.FilesystemMcpFolder);
				_filesystemMcpFolder = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.FilesystemMcpFolder);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChatSession> SelectSessionCommand => selectSessionCommand ?? (selectSessionCommand = new RelayCommand<ChatSession>(SelectSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand NewSessionCommand => newSessionCommand ?? (newSessionCommand = new AsyncRelayCommand(NewSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatSession?> DeleteSessionCommand => deleteSessionCommand ?? (deleteSessionCommand = new AsyncRelayCommand<ChatSession>(DeleteSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatSession> DuplicateSessionCommand => duplicateSessionCommand ?? (duplicateSessionCommand = new AsyncRelayCommand<ChatSession>(DuplicateSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatSession> ClearSessionMessagesCommand => clearSessionMessagesCommand ?? (clearSessionMessagesCommand = new AsyncRelayCommand<ChatSession>(ClearSessionMessages));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatSession> ExportSessionCtxCommand => exportSessionCtxCommand ?? (exportSessionCtxCommand = new AsyncRelayCommand<ChatSession>(ExportSessionCtx));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChatSession?> StartRenameSessionCommand => startRenameSessionCommand ?? (startRenameSessionCommand = new RelayCommand<ChatSession>(StartRenameSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CommitRenameSessionCommand => commitRenameSessionCommand ?? (commitRenameSessionCommand = new AsyncRelayCommand(CommitRenameSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ReorderSessionsCommand => reorderSessionsCommand ?? (reorderSessionsCommand = new AsyncRelayCommand(ReorderSessions));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelRenameSessionCommand => cancelRenameSessionCommand ?? (cancelRenameSessionCommand = new RelayCommand(CancelRenameSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleSettingsCommand => toggleSettingsCommand ?? (toggleSettingsCommand = new RelayCommand(ToggleSettings));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleMcpToolsCommand => toggleMcpToolsCommand ?? (toggleMcpToolsCommand = new RelayCommand(ToggleMcpTools));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleMcpServersCommand => toggleMcpServersCommand ?? (toggleMcpServersCommand = new RelayCommand(ToggleMcpServers));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ShowMcpGuideCommand => showMcpGuideCommand ?? (showMcpGuideCommand = new RelayCommand(ShowMcpGuide));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SendMessageCommand => sendMessageCommand ?? (sendMessageCommand = new AsyncRelayCommand(SendMessageAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> RemoveImageCommand => removeImageCommand ?? (removeImageCommand = new RelayCommand<string>(RemoveImage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatMessage> CopyMessageCommand => copyMessageCommand ?? (copyMessageCommand = new AsyncRelayCommand<ChatMessage>(CopyMessage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChatMessage> ToggleBookmarkCommand => toggleBookmarkCommand ?? (toggleBookmarkCommand = new RelayCommand<ChatMessage>(ToggleBookmark));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChatMessage> DeleteMessageCommand => deleteMessageCommand ?? (deleteMessageCommand = new RelayCommand<ChatMessage>(DeleteMessage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ChatMessage> UndoMessageCommand => undoMessageCommand ?? (undoMessageCommand = new RelayCommand<ChatMessage>(UndoMessage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatMessage> RegenerateMessageCommand => regenerateMessageCommand ?? (regenerateMessageCommand = new AsyncRelayCommand<ChatMessage>(RegenerateMessage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ChatMessage> EditMessageCommand => editMessageCommand ?? (editMessageCommand = new AsyncRelayCommand<ChatMessage>(EditMessage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand StopGenerationCommand => stopGenerationCommand ?? (stopGenerationCommand = new RelayCommand(StopGeneration));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenWebUiCommand => openWebUiCommand ?? (openWebUiCommand = new RelayCommand(OpenWebUi));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ClearSessionCommand => clearSessionCommand ?? (clearSessionCommand = new AsyncRelayCommand(ClearSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportSessionCommand => exportSessionCommand ?? (exportSessionCommand = new AsyncRelayCommand(ExportSession));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AddMcpServerCommand => addMcpServerCommand ?? (addMcpServerCommand = new AsyncRelayCommand(AddMcpServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> RemoveMcpServerCommand => removeMcpServerCommand ?? (removeMcpServerCommand = new AsyncRelayCommand<string>(RemoveMcpServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> ToggleMcpServerCommand => toggleMcpServerCommand ?? (toggleMcpServerCommand = new AsyncRelayCommand<string>(ToggleMcpServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> EnableMcpServerCommand => enableMcpServerCommand ?? (enableMcpServerCommand = new AsyncRelayCommand<string>(EnableMcpServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> DisableMcpServerCommand => disableMcpServerCommand ?? (disableMcpServerCommand = new AsyncRelayCommand<string>(DisableMcpServer));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SelectFilesystemFolderCommand => selectFilesystemFolderCommand ?? (selectFilesystemFolderCommand = new AsyncRelayCommand(SelectFilesystemFolderAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> CopyCodeCommand => copyCodeCommand ?? (copyCodeCommand = new RelayCommand<string>(CopyCode));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<ContentPart> ExpandCodeCommand => expandCodeCommand ?? (expandCodeCommand = new AsyncRelayCommand<ContentPart>(ExpandCode));

	public ChatViewModel(IChatService chatService, IMcpToolsService mcpToolsService, ChatSessionStore sessionStore, ISettings settings, ILogService log, ILocalizationService loc, IServerManager serverManager, IProfileManager profileManager, IFilePickerService filePicker, IDialogService dialog)
	{
		_chatService = chatService;
		_mcpToolsService = mcpToolsService;
		_sessionStore = sessionStore;
		_settings = settings;
		_log = log;
		_loc = loc;
		_serverManager = serverManager;
		_profileManager = profileManager;
		_filePicker = filePicker;
		_dialog = dialog;
		string defaultHost = settings.DefaultHost ?? "localhost";
		if (defaultHost == "0.0.0.0" || defaultHost == "::")
		{
			defaultHost = "127.0.0.1";
		}
		Host = defaultHost;
		ResolvePortAsync();
		GeneratingText = _loc.T("chat.generating");
		_loc.OnLanguageChanged += delegate
		{
			UpdateLocalizedProperties();
		};
		_serverManager.StatusChanged += OnServerStatusChanged;
		_profileManager.ProfileChanged += OnProfileChanged;
		LoadSessionsAsync();
		LoadMcpServersAsync().ContinueWith((Task _) => SetupDefaultMcpServersAsync());
		StartHealthCheck();
	}

	private void OnServerStatusChanged(object? sender, ServerStatus status)
	{
		Dispatcher.UIThread.Post(delegate
		{
			ServerState = status.State;
			ServerModelName = status.ModelName ?? "";
			OnPropertyChanged("ServerConnected");
			OnPropertyChanged("ServerDisconnected");
		});
	}

	private async Task ResolvePortAsync()
	{
		ServerProfile serverProfile = await GetActiveProfileAsync();
		int port = ((serverProfile != null && serverProfile.Port > 0) ? serverProfile.Port : ((_settings.DefaultPort > 0) ? _settings.DefaultPort : 8080));
		Dispatcher.UIThread.Post(delegate
		{
			Port = port;
		});
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

	private async void OnProfileChanged(string profileId)
	{
		await ResolvePortAsync();
	}

	private void StartHealthCheck()
	{
		_healthCheckTimer = new System.Timers.Timer(3000.0)
		{
			AutoReset = true
		};
		_healthCheckTimer.Elapsed += async delegate
		{
			try
			{
				ServerStatus status = await _serverManager.HealthCheckAsync(Host, Port);
				Dispatcher.UIThread.Post(delegate
				{
					if (status != null)
					{
						ServerState = ServerState.Running;
						ServerModelName = status.ModelName ?? "";
						if (status.ContextSize > 0)
						{
							ContextSize = status.ContextSize;
						}
					}
					else
					{
						ServerState = ServerState.Stopped;
						ServerModelName = "";
					}
					OnPropertyChanged("ServerConnected");
					OnPropertyChanged("ServerDisconnected");
				});
			}
			catch
			{
				Dispatcher.UIThread.Post(delegate
				{
					ServerState = ServerState.Stopped;
					OnPropertyChanged("ServerConnected");
					OnPropertyChanged("ServerDisconnected");
				});
			}
		};
		_healthCheckTimer.Start();
	}

	private async Task LoadSessionsAsync()
	{
		try
		{
			List<ChatSession> sessions = await _sessionStore.LoadAllAsync();
			foreach (ChatSession session in sessions)
			{
				if ((session.Name == "New Chat" || string.IsNullOrEmpty(session.Name)) && session.Messages.Count > 0)
				{
					ChatMessage chatMessage = session.Messages.FirstOrDefault((ChatMessage m) => m.Role == ChatRole.User);
					if (chatMessage != null && !string.IsNullOrWhiteSpace(chatMessage.Content))
					{
						session.Name = ((chatMessage.Content.Length > 40) ? (chatMessage.Content.Substring(0, 40) + "...") : chatMessage.Content);
						await _sessionStore.SaveSessionAsync(session);
					}
				}
			}
			Sessions = new ObservableCollection<ChatSession>(sessions);
			if (sessions.Count > 0 && SelectedSession == null)
			{
				SelectedSession = sessions.Last();
				_log.Information("[CHAT] Auto-selected last session: " + SelectedSession.Name, "ChatVM");
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to load chat sessions", "ChatViewModel");
		}
	}

	private void UpdateLocalizedProperties()
	{
		OnPropertyChanged("Title");
		OnPropertyChanged("NewSessionBtn");
		OnPropertyChanged("DeleteSessionBtn");
		OnPropertyChanged("RenameSessionBtn");
		OnPropertyChanged("DuplicateSessionBtn");
		OnPropertyChanged("ExportSessionBtn");
		OnPropertyChanged("ClearSessionBtn");
		OnPropertyChanged("DeleteSessionCtxBtn");
		OnPropertyChanged("SendBtn");
		OnPropertyChanged("StopBtn");
		OnPropertyChanged("InputWatermark");
		OnPropertyChanged("SettingsLabel");
		OnPropertyChanged("McpToolsLabel");
		OnPropertyChanged("SystemPromptLabel");
		OnPropertyChanged("SystemPromptWatermark");
		OnPropertyChanged("ParamsLabel");
		OnPropertyChanged("SessionsLabel");
		OnPropertyChanged("NoSessionLabel");
		OnPropertyChanged("NoServerLabel");
		OnPropertyChanged("CopyBtn");
		OnPropertyChanged("ServerConnected");
		OnPropertyChanged("ServerDisconnected");
		OnPropertyChanged("HideReasoningLabel");
		OnPropertyChanged("McpAddServer");
		OnPropertyChanged("McpName");
		OnPropertyChanged("McpType");
		OnPropertyChanged("McpCommand");
		OnPropertyChanged("McpArgs");
		OnPropertyChanged("McpUrl");
		OnPropertyChanged("McpAdd");
		OnPropertyChanged("McpToolsCount");
		OnPropertyChanged("ChatWelcome");
		OnPropertyChanged("ChatWelcomeSub");
		OnPropertyChanged("ExperimentalWarning");
		OnPropertyChanged("ChatStreaming");
		OnPropertyChanged("ChatInputWatermark");
		OnPropertyChanged("ChatCut");
		OnPropertyChanged("ChatCopy");
		OnPropertyChanged("ChatPaste");
		OnPropertyChanged("ChatSelectAll");
		OnPropertyChanged("SetupFilesystemBtn");
		OnPropertyChanged("SelectFsFolderBtn");
		OnPropertyChanged("McpGuideBtn");
		OnPropertyChanged("ContextUsageText");
		OnPropertyChanged("UndoMessageBtn");
	}

	[RelayCommand]
	private void SelectSession(ChatSession session)
	{
		SelectedSession = session;
	}

	[RelayCommand]
	private async Task NewSession()
	{
		ChatSession session = new ChatSession
		{
			Name = "New Chat",
			SystemPrompt = SystemPrompt,
			McpToolsEnabled = true,
			Temperature = Temperature,
			TopP = TopP,
			TopK = TopK,
			MinP = MinP,
			RepeatPenalty = RepeatPenalty,
			PresencePenalty = PresencePenalty,
			FrequencyPenalty = FrequencyPenalty,
			Seed = Seed,
			MaxTokens = MaxTokens,
			StopSequences = ParseStopSequences()
		};
		await _sessionStore.SaveSessionAsync(session);
		await LoadSessionsAsync();
		SelectedSession = session;
	}

	[RelayCommand]
	private async Task DeleteSession(ChatSession? session = null)
	{
		if (session == null)
		{
			session = SelectedSession;
		}
		if (session != null)
		{
			if (session == SelectedSession)
			{
				SelectedSession = null;
			}
			await _sessionStore.DeleteSessionAsync(session.Id);
			await LoadSessionsAsync();
		}
	}

	[RelayCommand]
	private async Task DuplicateSession(ChatSession session)
	{
		ChatSession dup = new ChatSession
		{
			Name = session.Name + " (copy)",
			SystemPrompt = session.SystemPrompt,
			McpToolsEnabled = true,
			Temperature = session.Temperature,
			TopP = session.TopP,
			TopK = session.TopK,
			MinP = session.MinP,
			RepeatPenalty = session.RepeatPenalty,
			PresencePenalty = session.PresencePenalty,
			FrequencyPenalty = session.FrequencyPenalty,
			Seed = session.Seed,
			MaxTokens = session.MaxTokens,
			StopSequences = session.StopSequences
		};
		foreach (ChatMessage message in session.Messages)
		{
			dup.AddMessage(new ChatMessage
			{
				Role = message.Role,
				Content = message.Content
			});
		}
		await _sessionStore.SaveSessionAsync(dup);
		await LoadSessionsAsync();
		SelectedSession = dup;
	}

	[RelayCommand]
	private async Task ClearSessionMessages(ChatSession session)
	{
		session.Messages.Clear();
		if (session == SelectedSession)
		{
			Messages = new ObservableCollection<ChatMessage>();
		}
		await _sessionStore.SaveSessionAsync(session);
	}

	[RelayCommand]
	private async Task ExportSessionCtx(ChatSession session)
	{
		ChatSession prev = SelectedSession;
		SelectedSession = session;
		string markdownContent = "# " + session.Name + "\n\n";
		foreach (ChatMessage message in session.Messages)
		{
			markdownContent += $"**{message.RoleName}:**\n{message.Content}\n\n";
		}
		string suggestedName = $"chat_{session.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
		string savePath = await _filePicker.SaveFileAsync(suggestedName, ".md");
		if (savePath != null)
		{
			await File.WriteAllTextAsync(savePath, markdownContent);
		}
		SelectedSession = prev;
	}

	[RelayCommand]
	private void StartRenameSession(ChatSession? session)
	{
		if (session == null)
		{
			session = SelectedSession;
		}
		if (session != null)
		{
			RenamingSession = session;
			RenameText = session.Name;
		}
	}

	[RelayCommand]
	private async Task CommitRenameSession()
	{
		if (RenamingSession != null)
		{
			string newSessionName = RenameText.Trim();
			if (!string.IsNullOrWhiteSpace(newSessionName))
			{
				RenamingSession.Name = newSessionName;
				await _sessionStore.SaveSessionAsync(RenamingSession);
			}
			RenamingSession = null;
			RenameText = "";
		}
	}

	[RelayCommand]
	private async Task ReorderSessions()
	{
		await _sessionStore.ReorderSessionsAsync(Sessions.ToList());
	}

	[RelayCommand]
	private void CancelRenameSession()
	{
		RenamingSession = null;
		RenameText = "";
	}

	[RelayCommand]
	private void ToggleSettings()
	{
		ShowSettings = !ShowSettings;
	}

	[RelayCommand]
	private void ToggleMcpTools()
	{
		McpToolsEnabled = !McpToolsEnabled;
		if (SelectedSession != null)
		{
			SaveCurrentSessionAsync();
		}
	}

	[RelayCommand]
	private void ToggleMcpServers()
	{
		ShowMcpServers = !ShowMcpServers;
	}

	[RelayCommand]
	private void ShowMcpGuide()
	{
		_dialog.ShowInfoAsync(McpGuideText, _loc.T("chat.mcp_guide_title"), isLongText: true);
	}

	[RelayCommand]
	private async Task SendMessageAsync()
	{
		_log.Information($"[CHAT] SendMessageAsync START, IsGenerating={IsGenerating}, InputLength={InputText?.Length ?? 0}", "Chat");
		if ((string.IsNullOrWhiteSpace(InputText) && AttachedImageBase64.Count == 0) || IsGenerating)
		{
			_log.Information("[CHAT] ABORT: InputText empty or IsGenerating", "Chat");
			return;
		}
		if (SelectedSession == null)
		{
			_log.Information("[CHAT] No session, creating new", "Chat");
			await NewSession();
			if (SelectedSession == null)
			{
				_log.Warning("[CHAT] Failed to create session", "Chat");
				return;
			}
		}
		int tokenCount = (ContextUsedTokens = EstimateTokenCount(SelectedSession.Messages));
		ContextUsedPercent = Math.Min(100.0, (double)tokenCount / (double)ContextSize * 100.0);
		OnPropertyChanged("ContextUsageText");
		if (ContextUsedPercent > 80.0 && SelectedSession.Messages.Count > 4)
		{
			_log.Information($"[CHAT] Context at {ContextUsedPercent:F1}%, triggering auto-compression", "Chat");
			await CompressChatHistoryAsync();
		}
		if (ServerState != ServerState.Running)
		{
			_log.Warning("[CHAT] Server not running, aborting send", "Chat");
			ChatMessage chatMessage = new ChatMessage
			{
				Role = ChatRole.Assistant,
				Content = _loc.T("chat.server_not_ready")
			};
			SelectedSession.AddMessage(chatMessage);
			Messages.Add(chatMessage);
			await _sessionStore.SaveSessionAsync(SelectedSession);
			return;
		}
		IsGenerating = true;
		StartGeneratingAnimation();
		string userMessage = InputText.Trim();
		List<string> imageAttachments = AttachedImageBase64.ToList();
		List<string> attachedFiles = AttachedFilesPreview.ToList();
		InputText = "";
		AttachedImageBase64.Clear();
		AttachedFilesPreview = new List<string>();
		ChatMessage userMsg = new ChatMessage
		{
			Role = ChatRole.User,
			Content = userMessage,
			ImageAttachments = imageAttachments,
			AttachedFiles = attachedFiles
		};
		SelectedSession.AddMessage(userMsg);
		Messages.Add(userMsg);
		if (SelectedSession.Name == "New Chat" || string.IsNullOrEmpty(SelectedSession.Name))
		{
			SelectedSession.Name = ((userMessage.Length > 40) ? (userMessage.Substring(0, 40) + "...") : userMessage);
		}
		await _sessionStore.SaveSessionAsync(SelectedSession);
		StringBuilder assistantContent = new StringBuilder();
		StringBuilder reasoningBuffer = new StringBuilder();
		object contentLock = new object();
		ChatMessage assistantMsg = new ChatMessage
		{
			Role = ChatRole.Assistant,
			Content = "",
			IsStreaming = true
		};
		Messages.Add(assistantMsg);
		_cancellationTokenSource = new CancellationTokenSource();
		_streamTimer = new System.Timers.Timer(80.0)
		{
			AutoReset = true
		};
		_streamTimer.Elapsed += delegate
		{
			Dispatcher.UIThread.Post(delegate
			{
				string content;
				string reasoning;
				lock (contentLock)
				{
					content = assistantContent.ToString();
					reasoning = reasoningBuffer.ToString();
				}
				string reasoningText;
				if (!string.IsNullOrEmpty(reasoning))
				{
					reasoningText = reasoning;
				}
				else
				{
					var (extractedReasoning, extractedContent) = ExtractReasoning(content);
					assistantMsg.Content = extractedContent;
					reasoningText = extractedReasoning;
				}
				if (!string.IsNullOrEmpty(reasoning))
				{
					assistantMsg.Content = content;
				}
				if (assistantMsg.Content != content || reasoningText != assistantMsg.Reasoning)
				{
					assistantMsg.Reasoning = (string.IsNullOrEmpty(reasoningText) ? null : reasoningText);
				}
			});
		};
		_streamTimer.Start();
		try
		{
			int canceledFlag = 0;
			try
			{
				_log.Information($"[CHAT] Calling server: http://{Host}:{Port}, MCP={McpToolsEnabled}", "Chat");
				if (McpToolsEnabled)
				{
					_log.Information("[CHAT] Using SendChatWithToolsAsync", "Chat");
					await _chatService.SendChatWithToolsAsync(Host, Port, SelectedSession.SystemPrompt, SelectedSession.Messages.ToList(), userMessage, imageAttachments, _mcpToolsService, Temperature, TopP, TopK, MinP, RepeatPenalty, PresencePenalty, FrequencyPenalty, Seed, MaxTokens, ParseStopSequences(), delegate(string token)
					{
						lock (contentLock)
						{
							assistantContent.Append(token);
						}
					}, delegate(ChatMessage msg)
					{
						Dispatcher.UIThread.Post(delegate
						{
							Messages.Add(msg);
						});
					}, _cancellationTokenSource.Token);
				}
				else
				{
					_log.Information("[CHAT] Using SendChatAsync", "Chat");
					await _chatService.SendChatAsync(Host, Port, SelectedSession.SystemPrompt, SelectedSession.Messages.ToList(), userMessage, imageAttachments, mcpToolsEnabled: false, null, Temperature, TopP, TopK, MinP, RepeatPenalty, PresencePenalty, FrequencyPenalty, Seed, MaxTokens, ParseStopSequences(), delegate(string token)
					{
						lock (contentLock)
						{
							assistantContent.Append(token);
						}
					}, delegate
					{
					}, delegate(string reasoningToken)
					{
						lock (contentLock)
						{
							reasoningBuffer.Append(reasoningToken);
						}
					}, _cancellationTokenSource.Token);
				}
				string assistantText;
				string reasoningText;
				lock (contentLock)
				{
					assistantText = assistantContent.ToString();
					reasoningText = reasoningBuffer.ToString();
				}
				_log.Information($"[CHAT] Response received, content length={assistantText.Length}, reasoning length={reasoningBuffer.Length}", "Chat");
				string finalReasoning;
				string finalContent;
				if (!string.IsNullOrEmpty(reasoningText))
				{
					finalReasoning = reasoningText;
					finalContent = assistantText;
				}
				else
				{
					(string? Reasoning, string Content) result = ExtractReasoning(assistantText);
					string extractedReasoning = result.Reasoning;
					string extractedContent = result.Content;
					finalReasoning = extractedReasoning;
					finalContent = extractedContent;
				}
				assistantMsg.Content = finalContent;
				assistantMsg.Reasoning = (string.IsNullOrEmpty(finalReasoning) ? null : finalReasoning);
				assistantMsg.IsStreaming = false;
				OnPropertyChanged("Messages");
				if (!string.IsNullOrWhiteSpace(finalContent) || !string.IsNullOrWhiteSpace(finalReasoning))
				{
					SelectedSession.AddMessage(new ChatMessage
					{
						Role = ChatRole.Assistant,
						Content = finalContent,
						Reasoning = (string.IsNullOrEmpty(finalReasoning) ? null : finalReasoning)
					});
				}
				await _sessionStore.SaveSessionAsync(SelectedSession);
				ContextUsedTokens = EstimateTokenCount(SelectedSession.Messages);
				ContextUsedPercent = Math.Min(100.0, (double)ContextUsedTokens / (double)ContextSize * 100.0);
				OnPropertyChanged("ContextUsageText");
			}
			catch (OperationCanceledException)
			{
				canceledFlag = 1;
			}
			catch (Exception ex)
			{
				_log.Error(ex, "[CHAT] ERROR: " + ex.Message, "Chat");
				assistantMsg.IsStreaming = false;
				Messages.Remove(assistantMsg);
				string content = ((ex.Message.Contains("503") || ex.Message.Contains("loading")) ? _loc.T("chat.model_loading") : (_loc.T("chat.error_prefix") + ex.Message));
				Messages.Add(new ChatMessage
				{
					Role = ChatRole.Assistant,
					Content = content
				});
				OnPropertyChanged("Messages");
			}
			if (canceledFlag == 1)
			{
				_log.Information("[CHAT] Generation canceled", "Chat");
				string partialContent;
				lock (contentLock)
				{
					partialContent = assistantContent.ToString();
				}
				assistantMsg.Content = partialContent;
				assistantMsg.IsStreaming = false;
				OnPropertyChanged("Messages");
				if (!string.IsNullOrWhiteSpace(partialContent))
				{
					SelectedSession.AddMessage(new ChatMessage
					{
						Role = ChatRole.Assistant,
						Content = partialContent
					});
					await _sessionStore.SaveSessionAsync(SelectedSession);
				}
			}
		}
		finally
		{
			IsGenerating = false;
			StopGeneratingAnimation();
			_cancellationTokenSource = null;
			_streamTimer?.Stop();
			_streamTimer?.Dispose();
			_streamTimer = null;
			_log.Information("[CHAT] SendMessageAsync FINISHED", "Chat");
		}
	}

	private int EstimateTokenCount(List<ChatMessage> messages)
	{
		int totalTokens = 0;
		foreach (ChatMessage message in messages)
		{
			int prevTokens = totalTokens;
			string content = message.Content;
			totalTokens = prevTokens + ((content != null) ? (content.Length / 4) : 0);
			if (!string.IsNullOrEmpty(message.Reasoning))
			{
				totalTokens += message.Reasoning.Length / 4;
			}
		}
		return Math.Max(totalTokens, 0);
	}

	private async Task CompressChatHistoryAsync()
	{
		if (IsCompressing || SelectedSession == null || SelectedSession.Messages.Count < 4)
		{
			return;
		}
		IsCompressing = true;
		_log.Information("[CHAT] Starting chat compression...", "Chat");
		ChatMessage compressMsg = new ChatMessage
		{
			Role = ChatRole.System,
			Content = _loc.T("chat.compressing"),
			IsStreaming = false
		};
		Messages.Add(compressMsg);
		SelectedSession.AddMessage(compressMsg);
		try
		{
			int keepCount = Math.Max(2, SelectedSession.Messages.Count - SelectedSession.Messages.Count / 2);
			List<ChatMessage> oldMessages = SelectedSession.Messages.Take(keepCount - 2).ToList();
			List<string> messageLines = new List<string>();
			foreach (ChatMessage message in oldMessages)
			{
				messageLines.Add($"{message.Role}: {message.Content}");
			}
			string summaryPrompt = string.Format(_loc.T("chat.summary_prompt"), string.Join("\n---\n", messageLines));
			List<ChatMessage> history = new List<ChatMessage>
			{
				new ChatMessage
				{
					Role = ChatRole.User,
					Content = summaryPrompt
				}
			};
			string summary = "";
			try
			{
				await _chatService.SendChatAsync(Host, Port, "", history, summaryPrompt, new List<string>(), mcpToolsEnabled: false, null, 0.3f, 0.9f, 40, 0.05f, 1f, 0f, 0f, -1, 256, new List<string>(), delegate(string token)
				{
					summary += token;
				}, delegate
				{
				}, delegate
				{
				}, CancellationToken.None);
			}
			catch (Exception ex)
			{
				_log.Warning("[CHAT] Compression failed: " + ex.Message + ", keeping history as-is", "Chat");
				summary = _loc.T("chat.compress_failed");
			}
			Messages.Remove(compressMsg);
			int compressMsgIndex = SelectedSession.Messages.IndexOf(compressMsg);
			if (compressMsgIndex >= 0)
			{
				SelectedSession.Messages.RemoveAt(compressMsgIndex);
			}
			List<ChatMessage> messagesToRemove = SelectedSession.Messages.Take(keepCount - 2).ToList();
			foreach (ChatMessage message in messagesToRemove)
			{
				SelectedSession.Messages.Remove(message);
			}
			ChatMessage compressedMsg = new ChatMessage
			{
				Role = ChatRole.System,
				Content = string.Format(_loc.T("chat.compressed_count"), messagesToRemove.Count, summary),
				IsStreaming = false
			};
			SelectedSession.Messages.Insert(0, compressedMsg);
			Messages = new ObservableCollection<ChatMessage>(SelectedSession.Messages);
			await _sessionStore.SaveSessionAsync(SelectedSession);
			ContextUsedTokens = EstimateTokenCount(SelectedSession.Messages);
			ContextUsedPercent = Math.Min(100.0, (double)ContextUsedTokens / (double)ContextSize * 100.0);
			OnPropertyChanged("ContextUsageText");
			_log.Information($"[CHAT] Compression done. Tokens: {ContextUsedTokens}/{ContextSize} ({ContextUsedPercent:F1}%)", "Chat");
		}
		finally
		{
			IsCompressing = false;
		}
	}

	public void NotifyImagesChanged()
	{
		OnPropertyChanged("AttachedImageBase64");
	}

	private void StartGeneratingAnimation()
	{
		_generatingPhraseIdx = 0;
		_dotFrameIdx = 0;
		string baseText = _loc.T("chat.generating_base");
		GeneratingText = baseText + new string('.', GeneratingPhraseDots[0]);
		(Dot1Opacity, Dot2Opacity, Dot3Opacity) = DotFrames[0];
		_generatingAnimTimer?.Stop();
		_generatingAnimTimer?.Dispose();
		_generatingAnimTimer = new System.Timers.Timer(240.0)
		{
			AutoReset = true
		};
		_generatingAnimTimer.Elapsed += delegate
		{
			Dispatcher.UIThread.Post(delegate
			{
				_generatingPhraseIdx = (_generatingPhraseIdx + 1) % GeneratingPhraseDots.Length;
				string newBaseText = _loc.T("chat.generating_base");
				GeneratingText = newBaseText + new string('.', GeneratingPhraseDots[_generatingPhraseIdx]);
				_dotFrameIdx = (_dotFrameIdx + 1) % DotFrames.Length;
				(Dot1Opacity, Dot2Opacity, Dot3Opacity) = DotFrames[_dotFrameIdx];
			});
		};
		_generatingAnimTimer.Start();
	}

	private void StopGeneratingAnimation()
	{
		_generatingAnimTimer?.Stop();
		_generatingAnimTimer?.Dispose();
		_generatingAnimTimer = null;
		GeneratingText = _loc.T("chat.generating");
	}

	[RelayCommand]
	private void RemoveImage(string base64)
	{
		if (AttachedImageBase64.Remove(base64))
		{
			OnPropertyChanged("AttachedImageBase64");
		}
	}

	private static (string? Reasoning, string Content) ExtractReasoning(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return (Reasoning: null, Content: string.Empty);
		}
		int thinkStart = raw.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
		if (thinkStart < 0)
		{
			return (Reasoning: null, Content: raw);
		}
		int thinkEnd = raw.IndexOf("</think>", thinkStart + "<think>".Length, StringComparison.OrdinalIgnoreCase);
		string rawText;
		int contentStart;
		if (thinkEnd < 0)
		{
			string beforeThink = raw.Substring(0, thinkStart).Trim();
			rawText = raw;
			contentStart = thinkStart + "<think>".Length;
			string tailReasoning = rawText.Substring(contentStart, rawText.Length - contentStart).Trim();
			return (Reasoning: string.IsNullOrEmpty(tailReasoning) ? null : tailReasoning, Content: beforeThink);
		}
		contentStart = thinkStart + "<think>".Length;
		string reasoning = raw.Substring(contentStart, thinkEnd - contentStart).Trim();
		string contentBefore = raw.Substring(0, thinkStart).Trim();
		rawText = raw;
		contentStart = thinkEnd + "</think>".Length;
		string contentAfter = rawText.Substring(contentStart, rawText.Length - contentStart).Trim();
		string content = string.Join("\n", new string[2] { contentBefore, contentAfter }.Where((string s) => !string.IsNullOrEmpty(s))).Trim();
		return (Reasoning: string.IsNullOrEmpty(reasoning) ? null : reasoning, Content: content);
	}

	[RelayCommand]
	private async Task CopyMessage(ChatMessage message)
	{
		try
		{
			Window window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
			if (window != null)
			{
				IClipboard clipboard = window.Clipboard;
				if (clipboard != null)
				{
					await clipboard.SetTextAsync(message.Content);
				}
			}
		}
		catch
		{
		}
	}

	[RelayCommand]
	private void ToggleBookmark(ChatMessage message)
	{
		message.IsBookmarked = !message.IsBookmarked;
		_log.Information("[CHAT] Bookmark " + (message.IsBookmarked ? "added" : "removed") + " for message " + message.Id, "Chat");
	}

	[RelayCommand]
	private void DeleteMessage(ChatMessage message)
	{
		if (SelectedSession != null)
		{
			SelectedSession.Messages.Remove(message);
			Messages.Remove(message);
			_sessionStore.SaveSessionAsync(SelectedSession);
			_log.Information("[CHAT] Deleted message " + message.Id, "Chat");
		}
	}

	[RelayCommand]
	private void UndoMessage(ChatMessage message)
	{
		if (message.Role != ChatRole.User || SelectedSession == null)
		{
			return;
		}
		int messageIndex = SelectedSession.Messages.IndexOf(message);
		if (messageIndex >= 0)
		{
			InputText = message.Content;
			for (int removeIndex = SelectedSession.Messages.Count - 1; removeIndex >= messageIndex; removeIndex--)
			{
				SelectedSession.Messages.RemoveAt(removeIndex);
			}
			while (Messages.Count > SelectedSession.Messages.Count)
			{
				Messages.RemoveAt(Messages.Count - 1);
			}
			_sessionStore.SaveSessionAsync(SelectedSession);
			_log.Information("[CHAT] Undid message " + message.Id + ", text restored to input", "Chat");
		}
	}

	[RelayCommand]
	private async Task RegenerateMessage(ChatMessage message)
	{
		if (SelectedSession == null || ServerState != ServerState.Running)
		{
			return;
		}
		_log.Information($"[CHAT] RegenerateMessage: role={message.Role}, contentLen={message.Content?.Length ?? 0}", "ChatVM");
		int assistantIndex = SelectedSession.Messages.FindIndex((ChatMessage m) => m.Id == message.Id);
		if (assistantIndex < 0 && !string.IsNullOrEmpty(message.Content))
		{
			string userContentTail;
			if (message.Content.Length <= 50)
			{
				userContentTail = message.Content;
			}
			else
			{
				string content = message.Content;
				int length = content.Length;
				int userTailStart = length - 50;
				userContentTail = content.Substring(userTailStart, length - userTailStart);
			}
			string contentKey = userContentTail;
			assistantIndex = SelectedSession.Messages.FindIndex(delegate(ChatMessage m)
			{
				if (m.Role == ChatRole.Assistant)
				{
					string assistantContentTail;
					if (m.Content.Length <= 50)
					{
						assistantContentTail = m.Content;
					}
					else
					{
						string content2 = m.Content;
						int length2 = content2.Length;
						int assistantTailStart = length2 - 50;
						assistantContentTail = content2.Substring(assistantTailStart, length2 - assistantTailStart);
					}
					return assistantContentTail == contentKey;
				}
				return false;
			});
		}
		if (assistantIndex < 0)
		{
			int messageIndex = Messages.IndexOf(message);
			if (messageIndex >= 0 && messageIndex <= SelectedSession.Messages.Count)
			{
				assistantIndex = messageIndex;
			}
		}
		_log.Information($"[CHAT] RegenerateMessage: idx={assistantIndex}, sessionMsgCount={SelectedSession.Messages.Count}", "ChatVM");
		if (assistantIndex < 0 || assistantIndex == 0)
		{
			return;
		}
		ChatMessage userMsg = SelectedSession.Messages[assistantIndex - 1];
		if (userMsg.Role != ChatRole.User)
		{
			return;
		}
		for (int removeIndex = SelectedSession.Messages.Count - 1; removeIndex >= assistantIndex; removeIndex--)
		{
			SelectedSession.Messages.RemoveAt(removeIndex);
		}
		while (Messages.Count > SelectedSession.Messages.Count)
		{
			Messages.RemoveAt(Messages.Count - 1);
		}
		await _sessionStore.SaveSessionAsync(SelectedSession);
		InputText = "";
		AttachedImageBase64.Clear();
		foreach (string imageAttachment in userMsg.ImageAttachments)
		{
			AttachedImageBase64.Add(imageAttachment);
		}
		await SendMessageAsync(userMsg.Content, userMsg.ImageAttachments);
	}

	private async Task SendMessageAsync(string userMessage, List<string> images)
	{
		InputText = "";
		AttachedImageBase64.Clear();
		foreach (string image in images)
		{
			AttachedImageBase64.Add(image);
		}
		await SendMessageAsync();
	}

	[RelayCommand]
	private async Task EditMessage(ChatMessage message)
	{
		if (message.Role != ChatRole.User || SelectedSession == null)
		{
			return;
		}
		string editedContent = await _dialog.ShowInputAsync(_loc.T("chat.edit_message"), _loc.T("chat.input_watermark"), message.Content);
		if (string.IsNullOrWhiteSpace(editedContent) || !(editedContent != message.Content))
		{
			return;
		}
		int messageIndex = SelectedSession.Messages.IndexOf(message);
		for (int removeIndex = SelectedSession.Messages.Count - 1; removeIndex >= messageIndex; removeIndex--)
		{
			SelectedSession.Messages.RemoveAt(removeIndex);
		}
		while (Messages.Count > SelectedSession.Messages.Count)
		{
			Messages.RemoveAt(Messages.Count - 1);
		}
		_sessionStore.SaveSessionAsync(SelectedSession);
		InputText = editedContent;
		AttachedImageBase64.Clear();
		foreach (string imageAttachment in message.ImageAttachments)
		{
			AttachedImageBase64.Add(imageAttachment);
		}
		await SendMessageAsync();
	}

	[RelayCommand]
	private void StopGeneration()
	{
		_cancellationTokenSource?.Cancel();
	}

	[RelayCommand]
	private void OpenWebUi()
	{
		string webHost = ((Host == "0.0.0.0") ? "127.0.0.1" : Host);
		string webUrl = $"http://{webHost}:{Port}/";
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = webUrl,
				UseShellExecute = true
			});
			_log.Information("[CHAT] Opening Web UI: " + webUrl, "ChatVM");
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to open Web UI: " + webUrl, "ChatVM");
		}
	}

	[RelayCommand]
	private async Task ClearSession()
	{
		if (SelectedSession != null)
		{
			SelectedSession.Messages.Clear();
			Messages = new ObservableCollection<ChatMessage>();
			SelectedSession.McpToolsEnabled = McpToolsEnabled;
			SelectedSession.SystemPrompt = SystemPrompt;
			await _sessionStore.SaveSessionAsync(SelectedSession);
		}
	}

	[RelayCommand]
	private async Task ExportSession()
	{
		if (SelectedSession == null)
		{
			return;
		}
		string markdownContent = "# " + SelectedSession.Name + "\n\n";
		foreach (ChatMessage message in SelectedSession.Messages)
		{
			markdownContent += $"**{message.RoleName}:**\n{message.Content}\n\n";
		}
		string suggestedName = $"chat_{SelectedSession.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
		string savePath = await _filePicker.SaveFileAsync(suggestedName, ".md");
		if (savePath != null)
		{
			await File.WriteAllTextAsync(savePath, markdownContent);
		}
	}

	private async Task SaveCurrentSessionAsync()
	{
		if (SelectedSession == null)
		{
			return;
		}
		SelectedSession.SystemPrompt = SystemPrompt;
		SelectedSession.McpToolsEnabled = McpToolsEnabled;
		SelectedSession.HideReasoning = HideReasoning;
		SelectedSession.Temperature = Temperature;
		SelectedSession.TopP = TopP;
		SelectedSession.TopK = TopK;
		SelectedSession.MinP = MinP;
		SelectedSession.RepeatPenalty = RepeatPenalty;
		SelectedSession.PresencePenalty = PresencePenalty;
		SelectedSession.FrequencyPenalty = FrequencyPenalty;
		SelectedSession.Seed = Seed;
		SelectedSession.MaxTokens = MaxTokens;
		SelectedSession.StopSequences = ParseStopSequences();
		try
		{
			await _sessionStore.SaveSessionAsync(SelectedSession);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to save session", "ChatViewModel");
		}
	}

	private List<string> ParseStopSequences()
	{
		if (string.IsNullOrWhiteSpace(StopSequences))
		{
			return new List<string>();
		}
		return (from s in StopSequences.Split(',', ';')
			select s.Trim() into s
			where !string.IsNullOrEmpty(s)
			select s).ToList();
	}

	private async Task LoadMcpServersAsync()
	{
		try
		{
			List<McpServerConfig> mcpServers = _mcpToolsService.GetMcpServers();
			McpServers = new ObservableCollection<McpServerConfig>(mcpServers);
			if (mcpServers.Count > 0 && !McpToolsEnabled)
			{
				McpToolsEnabled = true;
				_log.Information("[CHAT] Auto-enabled MCP tools (servers available)", "ChatVM");
			}
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to load MCP servers", "ChatViewModel");
		}
	}

	[RelayCommand]
	private async Task AddMcpServer()
	{
		string serverName = NewServerName.Trim();
		if (string.IsNullOrWhiteSpace(serverName))
		{
			return;
		}
		McpServerConfig mcpServerConfig = new McpServerConfig
		{
			Name = serverName,
			TransportType = SelectedTransportType
		};
		if (SelectedTransportType == McpTransportType.Stdio)
		{
			mcpServerConfig.Command = NewServerCommand.Trim();
			if (string.IsNullOrWhiteSpace(mcpServerConfig.Command))
			{
				return;
			}
			string serverArgs = NewServerArgs.Trim();
			if (!string.IsNullOrWhiteSpace(serverArgs))
			{
				mcpServerConfig.Args = serverArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			}
		}
		else
		{
			mcpServerConfig.Url = NewServerUrl.Trim();
			if (string.IsNullOrWhiteSpace(mcpServerConfig.Url))
			{
				return;
			}
		}
		await _mcpToolsService.AddMcpServerAsync(mcpServerConfig);
		await LoadMcpServersAsync();
		NewServerName = "";
		NewServerCommand = "";
		NewServerArgs = "";
		NewServerUrl = "http://127.0.0.1:8080";
	}

	[RelayCommand]
	private async Task RemoveMcpServer(string id)
	{
		await _mcpToolsService.RemoveMcpServerAsync(id);
		await LoadMcpServersAsync();
	}

	[RelayCommand]
	private async Task ToggleMcpServer(string id)
	{
		McpServerConfig mcpServerConfig = _mcpToolsService.GetMcpServers().FirstOrDefault((McpServerConfig s) => s.Id == id);
		if (mcpServerConfig != null)
		{
			await _mcpToolsService.ToggleMcpServerAsync(id, !mcpServerConfig.Enabled);
			await LoadMcpServersAsync();
		}
	}

	[RelayCommand]
	private async Task EnableMcpServer(string id)
	{
		await _mcpToolsService.ToggleMcpServerAsync(id, enabled: true);
		await LoadMcpServersAsync();
	}

	[RelayCommand]
	private async Task DisableMcpServer(string id)
	{
		await _mcpToolsService.ToggleMcpServerAsync(id, enabled: false);
		await LoadMcpServersAsync();
	}

	[RelayCommand]
	private async Task SelectFilesystemFolderAsync()
	{
		string folderPath = await _dialog.SelectFolderAsync(_loc.T("chat.select_fs_folder"), FilesystemMcpFolder);
		if (!string.IsNullOrWhiteSpace(folderPath))
		{
			FilesystemMcpFolder = folderPath;
			await SetupFilesystemMcpAsync(folderPath);
		}
	}

	private async Task SetupFilesystemMcpAsync(string folder)
	{
		try
		{
			McpServerConfig mcpServerConfig = _mcpToolsService.GetMcpServers().FirstOrDefault((McpServerConfig s) => s.Name == "Filesystem");
			McpServerConfig config = mcpServerConfig ?? new McpServerConfig
			{
				Name = "Filesystem",
				Description = _loc.T("chat.fs_description")
			};
			config.TransportType = McpTransportType.Stdio;
			config.Command = "npx";
			config.Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", folder };
			config.Enabled = true;
			if (mcpServerConfig != null)
			{
				await _mcpToolsService.RemoveMcpServerAsync(mcpServerConfig.Id);
			}
			await _mcpToolsService.AddMcpServerAsync(config);
			await LoadMcpServersAsync();
			_log.Information("[CHAT] Filesystem MCP configured for: " + folder, "ChatVM");
			await _dialog.ShowSuccessAsync(_loc.T("chat.fs_configured"), _loc.T("chat.success"));
		}
		catch (Exception ex)
		{
			_log.Error(ex, "Failed to setup filesystem MCP", "ChatVM");
			await _dialog.ShowErrorAsync(ex.Message);
		}
	}

	private async Task SetupDefaultMcpServersAsync()
	{
	}

	[RelayCommand]
	private void CopyCode(string code)
	{
		_log.Information($"[CHAT] Copy code requested ({code.Length} chars)", "ChatVM");
	}

	[RelayCommand]
	private async Task ExpandCode(ContentPart part)
	{
		try
		{
			await _dialog.ShowMessageAsync(part.Language.ToUpper(), part.Content);
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Failed to expand code", "ChatVM");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedSessionChanged(ChatSession? value)
	{
		if (value == null)
		{
			Messages = new ObservableCollection<ChatMessage>();
			SystemPrompt = "";
			McpToolsEnabled = true;
			Temperature = 0.8f;
			TopP = 0.95f;
			TopK = 40;
			MinP = 0.05f;
			RepeatPenalty = 1f;
			PresencePenalty = 0f;
			FrequencyPenalty = 0f;
			Seed = -1;
			MaxTokens = -1;
			StopSequences = "";
		}
		else
		{
			Messages = new ObservableCollection<ChatMessage>(value.Messages);
			SystemPrompt = value.SystemPrompt;
			McpToolsEnabled = McpServers.Count > 0 || value.McpToolsEnabled;
			HideReasoning = value.HideReasoning;
			Temperature = value.Temperature;
			TopP = value.TopP;
			TopK = value.TopK;
			MinP = value.MinP;
			RepeatPenalty = value.RepeatPenalty;
			PresencePenalty = value.PresencePenalty;
			FrequencyPenalty = value.FrequencyPenalty;
			Seed = value.Seed;
			MaxTokens = value.MaxTokens;
			StopSequences = string.Join(", ", value.StopSequences);
			ContextUsedTokens = EstimateTokenCount(value.Messages);
			ContextUsedPercent = Math.Min(100.0, (double)ContextUsedTokens / (double)ContextSize * 100.0);
			OnPropertyChanged("ContextUsageText");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnMcpToolsEnabledChanged(bool value)
	{
		_log.Information($"[CHAT] McpToolsEnabled changed to {value}", "ChatVM");
		if (SelectedSession != null)
		{
			SaveCurrentSessionAsync();
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHideReasoningChanged(bool value)
	{
		OnPropertyChanged("ShowReasoning");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSystemPromptChanged(string value)
	{
		SaveCurrentSessionAsync();
	}
}
