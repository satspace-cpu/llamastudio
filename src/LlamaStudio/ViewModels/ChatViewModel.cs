     using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;

using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using LlamaStudio.Core.Enums;
using LlamaStudio.Infrastructure.Chat;
using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Timers;

namespace LlamaStudio.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    readonly IChatService _chatService;
    readonly IMcpToolsService _mcpToolsService;
    readonly ChatSessionStore _sessionStore;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly ILocalizationService _loc;
    readonly IServerManager _serverManager;
    readonly IFilePickerService _filePicker;
    readonly IDialogService _dialog;

    [ObservableProperty] ObservableCollection<ChatSession> _sessions = new();
    [ObservableProperty] ChatSession? _selectedSession;
    [ObservableProperty] ObservableCollection<ChatMessage> _messages = new();
    [ObservableProperty] string _inputText = "";
    [ObservableProperty] bool _isGenerating = false;
    [ObservableProperty] bool _showSettings = false;
    [ObservableProperty] bool _mcpToolsEnabled = false;

    partial void OnMcpToolsEnabledChanged(bool value)
    {
        _log.Information($"[CHAT] McpToolsEnabled changed to {value}", "ChatVM");
        if (SelectedSession != null)
            _ = SaveCurrentSessionAsync();
    }
    [ObservableProperty] bool _showMcpServers = false;

    // Reasoning visibility
    [ObservableProperty] bool _hideReasoning = true; // Hidden by default

    public bool ShowReasoning => !HideReasoning;

    partial void OnHideReasoningChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowReasoning));
    }

    // MCP server management
    [ObservableProperty] ObservableCollection<McpServerConfig> _mcpServers = new();
    [ObservableProperty] string _newServerName = "";
    [ObservableProperty] string _newServerCommand = "";
    [ObservableProperty] string _newServerArgs = "";
    [ObservableProperty] string _newServerUrl = "http://127.0.0.1:8080";
    [ObservableProperty] McpTransportType _selectedTransportType = McpTransportType.Stdio;
    public List<McpTransportType> TransportTypes { get; } = Enum.GetValues<McpTransportType>().ToList();

    // Attached files for current message
    [ObservableProperty] List<string> _attachedFilesPreview = new();
    [ObservableProperty] ObservableCollection<string> _attachedImageBase64 = new();

    // Session rename inline editing
    private ChatSession? _renamingSession;
    public ChatSession? RenamingSession
    {
        get => _renamingSession;
        set
        {
            if (_renamingSession != value)
            {
                _renamingSession = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRenaming));
            }
        }
    }

    [ObservableProperty] string _renameText = "";

    public bool IsRenaming => RenamingSession != null;

    // Server connection status
    [ObservableProperty] ServerState _serverState = ServerState.Stopped;
    [ObservableProperty] string _serverModelName = "";

 

    // Generation parameters
    [ObservableProperty] float _temperature = 0.8f;
    [ObservableProperty] float _topP = 0.95f;
    [ObservableProperty] int _topK = 40;
    [ObservableProperty] float _minP = 0.05f;
    [ObservableProperty] float _repeatPenalty = 1.0f;
    [ObservableProperty] float _presencePenalty = 0.0f;
    [ObservableProperty] float _frequencyPenalty = 0.0f;
    [ObservableProperty] int _seed = -1;
    [ObservableProperty] int _maxTokens = -1;
    [ObservableProperty] string _stopSequences = "";
    [ObservableProperty] string _systemPrompt = "";

    // Connection
    [ObservableProperty] string _host = "localhost";
    [ObservableProperty] int _port = 8080;

    // Context tracking
    [ObservableProperty] int _contextSize = 8192;
    [ObservableProperty] int _contextUsedTokens = 0;
    [ObservableProperty] double _contextUsedPercent = 0;
    [ObservableProperty] bool _isCompressing = false;

    public string ContextUsageText => $"{ContextUsedTokens}/{ContextSize} tokens";

      CancellationTokenSource? _cancellationTokenSource;
    System.Timers.Timer? _streamTimer;
    System.Timers.Timer? _healthCheckTimer;
    System.Timers.Timer? _generatingAnimTimer;

    // Generating indicator animation
    [ObservableProperty] string _generatingText = "Модель генерирует ответ...";
    [ObservableProperty] double _dot1Opacity = 0.3;
    [ObservableProperty] double _dot2Opacity = 0.6;
    [ObservableProperty] double _dot3Opacity = 1.0;

    static readonly string[] GeneratingPhrases = new[]
    {
        "Модель генерирует ответ",
        "Модель генерирует ответ.",
        "Модель генерирует ответ..",
        "Модель генерирует ответ..."
    };
    int _generatingPhraseIdx = 0;
    static readonly (double d1, double d2, double d3)[] DotFrames = new[]
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
        (0.8, 1.0, 0.8),
    };
    int _dotFrameIdx = 0;

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

    public ChatViewModel(
        IChatService chatService,
        IMcpToolsService mcpToolsService,
        ChatSessionStore sessionStore,
        ISettings settings,
        ILogService log,
        ILocalizationService loc,
        IServerManager serverManager,
        IFilePickerService filePicker,
        IDialogService dialog)
    {
        _chatService = chatService;
        _mcpToolsService = mcpToolsService;
        _sessionStore = sessionStore;
        _settings = settings;
        _log = log;
        _loc = loc;
        _serverManager = serverManager;
        _filePicker = filePicker;
        _dialog = dialog;

        var host = settings.DefaultHost ?? "localhost";
        // Normalize wildcard bind addresses to localhost for client connections
        if (host == "0.0.0.0" || host == "::")
            host = "127.0.0.1";
        Host = host;
        Port = settings.DefaultPort != 0 ? settings.DefaultPort : 8080;

        _loc.OnLanguageChanged += (_, _) => UpdateLocalizedProperties();
        _serverManager.StatusChanged += OnServerStatusChanged;
        _ = LoadSessionsAsync();
        _ = LoadMcpServersAsync().ContinueWith(_ => SetupDefaultMcpServersAsync());
        StartHealthCheck();
    }

    void OnServerStatusChanged(object? sender, ServerStatus e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ServerState = e.State;
            ServerModelName = e.ModelName ?? "";
            OnPropertyChanged(nameof(ServerConnected));
            OnPropertyChanged(nameof(ServerDisconnected));
        });
    }

    void StartHealthCheck()
    {
        _healthCheckTimer = new System.Timers.Timer(3000) { AutoReset = true };
        _healthCheckTimer.Elapsed += async (_, _) =>
        {
            try
            {
                var status = await _serverManager.HealthCheckAsync(Host, Port);
                Dispatcher.UIThread.Post(() =>
                {
                    if (status != null)
                    {
                        ServerState = ServerState.Running;
                        ServerModelName = status.ModelName ?? "";
                        if (status.ContextSize > 0)
                            ContextSize = status.ContextSize;
                    }
                    else
                    {
                        ServerState = ServerState.Stopped;
                        ServerModelName = "";
                    }
                    OnPropertyChanged(nameof(ServerConnected));
                    OnPropertyChanged(nameof(ServerDisconnected));
                });
            }
            catch
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ServerState = ServerState.Stopped;
                    OnPropertyChanged(nameof(ServerConnected));
                    OnPropertyChanged(nameof(ServerDisconnected));
                });
            }
        };
        _healthCheckTimer.Start();
    }

    async Task LoadSessionsAsync()
    {
        try
        {
            var sessions = await _sessionStore.LoadAllAsync();

            // Auto-rename sessions that still have "New Chat" name
            foreach (var session in sessions)
            {
                if ((session.Name == "New Chat" || string.IsNullOrEmpty(session.Name)) && session.Messages.Count > 0)
                {
                    var firstUserMsg = session.Messages.FirstOrDefault(m => m.Role == ChatRole.User);
                    if (firstUserMsg != null && !string.IsNullOrWhiteSpace(firstUserMsg.Content))
                    {
                        session.Name = firstUserMsg.Content.Length > 40
                            ? firstUserMsg.Content[..40] + "..."
                            : firstUserMsg.Content;
                        await _sessionStore.SaveSessionAsync(session);
                    }
                }
            }

            Sessions = new ObservableCollection<ChatSession>(sessions);

            // Auto-select last session
            if (sessions.Count > 0 && SelectedSession == null)
            {
                SelectedSession = sessions.Last();
                _log.Information($"[CHAT] Auto-selected last session: {SelectedSession.Name}", "ChatVM");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load chat sessions", "ChatViewModel");
        }
    }

    void UpdateLocalizedProperties()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(NewSessionBtn));
        OnPropertyChanged(nameof(DeleteSessionBtn));
        OnPropertyChanged(nameof(RenameSessionBtn));
        OnPropertyChanged(nameof(DuplicateSessionBtn));
        OnPropertyChanged(nameof(ExportSessionBtn));
        OnPropertyChanged(nameof(ClearSessionBtn));
        OnPropertyChanged(nameof(DeleteSessionCtxBtn));
        OnPropertyChanged(nameof(SendBtn));
        OnPropertyChanged(nameof(StopBtn));
        OnPropertyChanged(nameof(InputWatermark));
        OnPropertyChanged(nameof(SettingsLabel));
        OnPropertyChanged(nameof(McpToolsLabel));
        OnPropertyChanged(nameof(SystemPromptLabel));
        OnPropertyChanged(nameof(SystemPromptWatermark));
        OnPropertyChanged(nameof(ParamsLabel));
        OnPropertyChanged(nameof(SessionsLabel));
     OnPropertyChanged(nameof(NoSessionLabel));
            OnPropertyChanged(nameof(NoServerLabel));
            OnPropertyChanged(nameof(CopyBtn));
            OnPropertyChanged(nameof(ServerConnected));
            OnPropertyChanged(nameof(ServerDisconnected));
            OnPropertyChanged(nameof(HideReasoningLabel));
            OnPropertyChanged(nameof(McpAddServer));
            OnPropertyChanged(nameof(McpName));
            OnPropertyChanged(nameof(McpType));
            OnPropertyChanged(nameof(McpCommand));
            OnPropertyChanged(nameof(McpArgs));
            OnPropertyChanged(nameof(McpUrl));
            OnPropertyChanged(nameof(McpAdd));
            OnPropertyChanged(nameof(McpToolsCount));
            OnPropertyChanged(nameof(ChatWelcome));
            OnPropertyChanged(nameof(ChatWelcomeSub));
            OnPropertyChanged(nameof(ChatStreaming));
            OnPropertyChanged(nameof(ChatInputWatermark));
            OnPropertyChanged(nameof(ChatCut));
            OnPropertyChanged(nameof(ChatCopy));
            OnPropertyChanged(nameof(ChatPaste));
            OnPropertyChanged(nameof(ChatSelectAll));
            OnPropertyChanged(nameof(SetupFilesystemBtn));
            OnPropertyChanged(nameof(SelectFsFolderBtn));
            OnPropertyChanged(nameof(McpGuideBtn));
            OnPropertyChanged(nameof(ContextUsageText));
            OnPropertyChanged(nameof(UndoMessageBtn));
    }

    partial void OnSelectedSessionChanged(ChatSession? value)
    {
        if (value == null)
        {
            Messages = new ObservableCollection<ChatMessage>();
            SystemPrompt = "";
            // Keep MCP enabled by default
            McpToolsEnabled = true;
            Temperature = 0.8f;
            TopP = 0.95f;
            TopK = 40;
            MinP = 0.05f;
            RepeatPenalty = 1.0f;
            PresencePenalty = 0.0f;
            FrequencyPenalty = 0.0f;
            Seed = -1;
            MaxTokens = -1;
            StopSequences = "";
            return;
        }

        Messages = new ObservableCollection<ChatMessage>(value.Messages);
        SystemPrompt = value.SystemPrompt;
        // Auto-enable MCP if servers are available, regardless of session setting
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

        // Recalculate context usage
        ContextUsedTokens = EstimateTokenCount(value.Messages);
        ContextUsedPercent = Math.Min(100.0, (ContextUsedTokens / (double)ContextSize) * 100.0);
        OnPropertyChanged(nameof(ContextUsageText));
    }

    [RelayCommand]
    void SelectSession(ChatSession session)
    {
        SelectedSession = session;
    }

    [RelayCommand]
    async Task NewSession()
    {
        var session = new ChatSession
        {
            Name = "New Chat",
            SystemPrompt = SystemPrompt,
            McpToolsEnabled = true, // Always enable MCP for new sessions
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
    async Task DeleteSession(ChatSession? session = null)
    {
        session ??= SelectedSession;
        if (session == null)
            return;

        if (session == SelectedSession)
            SelectedSession = null;

        await _sessionStore.DeleteSessionAsync(session.Id);
        await LoadSessionsAsync();
    }

    [RelayCommand]
    async Task DuplicateSession(ChatSession session)
    {
        var dup = new ChatSession
        {
            Name = $"{session.Name} (copy)",
            SystemPrompt = session.SystemPrompt,
            McpToolsEnabled = true, // Always enable MCP for duplicates
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

        foreach (var msg in session.Messages)
            dup.AddMessage(new ChatMessage
            {
                Role = msg.Role,
                Content = msg.Content
            });

        await _sessionStore.SaveSessionAsync(dup);
        await LoadSessionsAsync();
        SelectedSession = dup;
    }

    [RelayCommand]
    async Task ClearSessionMessages(ChatSession session)
    {
        session.Messages.Clear();
        if (session == SelectedSession)
            Messages = new ObservableCollection<ChatMessage>();
        await _sessionStore.SaveSessionAsync(session);
    }

    [RelayCommand]
    async Task ExportSessionCtx(ChatSession session)
    {
        var prev = SelectedSession;
        SelectedSession = session;

        var text = $"# {session.Name}\n\n";
        foreach (var msg in session.Messages)
        {
            text += $"**{msg.RoleName}:**\n{msg.Content}\n\n";
        }

        var suggestedName = $"chat_{session.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        var path = await _filePicker.SaveFileAsync(suggestedName, ".md");
        if (path != null)
        {
            await File.WriteAllTextAsync(path, text);
        }

        SelectedSession = prev;
    }

    [RelayCommand]
    void StartRenameSession(ChatSession? session)
    {
        if (session == null)
            session = SelectedSession;
        if (session == null)
            return;

        RenamingSession = session;
        RenameText = session.Name;
    }

    [RelayCommand]
    async Task CommitRenameSession()
    {
        if (RenamingSession == null)
            return;

        var newName = RenameText.Trim();
        if (!string.IsNullOrWhiteSpace(newName))
        {
            RenamingSession.Name = newName;
            await _sessionStore.SaveSessionAsync(RenamingSession);
        }

        RenamingSession = null;
        RenameText = "";
    }

    [RelayCommand]
    async Task ReorderSessions()
    {
        await _sessionStore.ReorderSessionsAsync(Sessions.ToList());
    }

    [RelayCommand]
    void CancelRenameSession()
    {
        RenamingSession = null;
        RenameText = "";
    }

    [RelayCommand]
    void ToggleSettings()
    {
        ShowSettings = !ShowSettings;
    }

    [RelayCommand]
    void ToggleMcpTools()
    {
        McpToolsEnabled = !McpToolsEnabled;
        if (SelectedSession != null)
            _ = SaveCurrentSessionAsync();
    }

 [RelayCommand]
    void ToggleMcpServers()
    {
        ShowMcpServers = !ShowMcpServers;
    }

    [RelayCommand]
    void ShowMcpGuide()
    {
        _dialog.ShowInfoAsync(McpGuideText, _loc.T("chat.mcp_guide_title"), true);
    }

    public string McpGuideText => @"
# MCP — Руководство по настройке

MCP (Model Context Protocol) позволяет подключать внешние инструменты к чату: управление файлами, поиск в интернете, доступ к API и многое другое.

---

## 🔧 Быстрый старт

При первом запуске Llama Studio автоматически создаёт 2 MCP сервера для поиска в интернете. Для управления файлами используйте кнопку **«Настроить Filesystem»** в панели MCP.

---

## 📁 Filesystem — управление файлами

**Быстрая настройка (рекомендуется):**
1. В панели MCP нажмите кнопку **«Выберите папку для управления файлами»**
2. Выберите папку — всё настроится автоматически

**Ручная настройка:**
1. Установите Node.js: https://nodejs.org/
2. В панели MCP заполните форму:
   - **Имя:** Filesystem
   - **Тип:** Stdio
   - **Команда:** npx
   - **Аргументы:** -y @modelcontextprotocol/server-filesystem C:\Ваша\Папка
3. Нажмите **«Добавить»**

---

## 🌐 Поиск в интернете

**Встроенный поиск (без настройки):**
Llama Studio имеет встроенный инструмент `web_search`, работающий через DuckDuckGo и Bing. Включите MCP-инструменты кнопкой «MCP» внизу чата.

**Jina Web Search (MCP):**
1. В панели MCP → Добавить сервер:
   - **Имя:** Web Search
   - **Тип:** SSE
   - **URL:** https://chat.mcp.so/http/app.mcp.so
2. Нажмите **«Добавить»**

**Brave Search (MCP):**
1. Получите API ключ: https://brave.com/search/api/
2. В панели MCP → Добавить сервер:
   - **Имя:** Brave Search
   - **Тип:** Stdio
   - **Команда:** npx
   - **Аргументы:** -y @modelcontextprotocol/server-brave-search
3. Добавьте переменную среды BRAVE_API_KEY

---

## 📋 Другие популярные MCP серверы

| Сервер | Команда | Примечание |
|--------|---------|------------|
| GitHub | npx -y @modelcontextprotocol/server-github | Нужен GITHUB_TOKEN |
| Puppeteer (браузер) | npx -y @modelcontextprotocol/server-puppeteer | Автоматизация браузера |
| Google Maps | npx -y @modelcontextprotocol/server-google-maps | Нужен API ключ |
| PostgreSQL | npx -y @modelcontextprotocol/server-postgres | Укажите DSN |
| Fetch (страницы) | npx -y @modelcontextprotocol/server-fetch | Загрузка веб-страниц |

---

## ⚙️ Управление MCP серверами

**Добавление нового сервера:**
1. Откройте панель MCP (кнопка «MCP» вверху чата)
2. Заполните форму:
   - **Имя** — любое удобное
   - **Тип:** Stdio (локальная программа) или SSE (удалённый сервер)
   - **Команда/URL** — путь к программе или адрес сервера
   - **Аргументы** — параметры запуска (для Stdio)
3. Нажмите **«Добавить»**

**Управление серверами:**
- ⚙️ **Зелёная шестерёнка** — отключить сервер
- ⚙️ **Серая шестерёнка** — включить сервер
- ✕ **Красный крестик** — удалить сервер

**Глобальное включение:**
Кнопка «MCP» внизу чата (синяя) — включает/выключает все MCP-инструменты

---

## 💡 Полезные советы

• **Встроенные инструменты** (web_search, web_fetch, time, calculator) работают без MCP — просто включите кнопку «MCP»
• **MCP-инструменты** добавляются автоматически при подключении сервера
• **Серверы сохраняются** между запусками в mcp_servers.json
• **Ошибка «npx не найдена»** — установите Node.js с https://nodejs.org/
• **Сервер не подключается** — проверьте путь к программе и аргументы запуска";

    partial void OnSystemPromptChanged(string value)
    {
        _ = SaveCurrentSessionAsync();
    }

    [RelayCommand]
    async Task SendMessageAsync()
    {
        _log.Information($"[CHAT] SendMessageAsync START, IsGenerating={IsGenerating}, InputText='{InputText?.Trim()}'", "Chat");

        if ((string.IsNullOrWhiteSpace(InputText) && AttachedImageBase64.Count == 0) || IsGenerating)
        {
            _log.Information($"[CHAT] ABORT: InputText empty or IsGenerating", "Chat");
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

        // Estimate context usage
        int estimatedTokens = EstimateTokenCount(SelectedSession.Messages);
        ContextUsedTokens = estimatedTokens;
        ContextUsedPercent = Math.Min(100.0, (estimatedTokens / (double)ContextSize) * 100.0);
        OnPropertyChanged(nameof(ContextUsageText));

        // Auto-compress if context is nearing limit (>80%)
        if (ContextUsedPercent > 80 && SelectedSession.Messages.Count > 4)
        {
            _log.Information($"[CHAT] Context at {ContextUsedPercent:F1}%, triggering auto-compression", "Chat");
            await CompressChatHistoryAsync();
        }

        // Check server status before sending
        if (ServerState != ServerState.Running)
        {
            _log.Warning("[CHAT] Server not running, aborting send", "Chat");
            var serverNotReadyMsg = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = "⚠️ Сервер не запущен или модель ещё загружается. Подождите и попробуйте снова."
            };
            SelectedSession.AddMessage(serverNotReadyMsg);
            Messages.Add(serverNotReadyMsg);
            await _sessionStore.SaveSessionAsync(SelectedSession);
            return;
        }

        // Start generation UI
        IsGenerating = true;
        StartGeneratingAnimation();

        var userMessage = InputText.Trim();
        var imageAttachments = AttachedImageBase64.ToList();
        var attachedFiles = AttachedFilesPreview.ToList();
        InputText = "";
        AttachedImageBase64.Clear();
        AttachedFilesPreview = new List<string>();

        var userMsg = new ChatMessage
        {
            Role = ChatRole.User,
            Content = userMessage,
            ImageAttachments = imageAttachments,
            AttachedFiles = attachedFiles
        };

        SelectedSession.AddMessage(userMsg);
        Messages.Add(userMsg);

        // Auto-rename session from first user message
        if (SelectedSession.Name == "New Chat" || string.IsNullOrEmpty(SelectedSession.Name))
        {
            SelectedSession.Name = userMessage.Length > 40 ? userMessage[..40] + "..." : userMessage;
        }

        await _sessionStore.SaveSessionAsync(SelectedSession);

        var assistantContent = new StringBuilder();
        var reasoningBuffer = new StringBuilder();
        var contentLock = new object();

        var assistantMsg = new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = "",
            IsStreaming = true
        };
        Messages.Add(assistantMsg);

        _cancellationTokenSource = new CancellationTokenSource();

        _streamTimer = new System.Timers.Timer(80) { AutoReset = true };
        _streamTimer.Elapsed += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                string contentSnapshot, reasoningSnapshot;
                lock (contentLock)
                {
                    contentSnapshot = assistantContent.ToString();
                    reasoningSnapshot = reasoningBuffer.ToString();
                }
                
                // If we have explicit reasoning from SSE, use it directly
                string finalReasoning;
                if (!string.IsNullOrEmpty(reasoningSnapshot))
                {
                    finalReasoning = reasoningSnapshot;
                }
                else
                {
                    // Fallback: extract <think> tags from content
                    var (tagReasoning, cleanContent) = ExtractReasoning(contentSnapshot);
                    assistantMsg.Content = cleanContent;
                    finalReasoning = tagReasoning;
                }
                
                if (!string.IsNullOrEmpty(reasoningSnapshot))
                {
                    // When using SSE reasoning_content, content is already clean
                    assistantMsg.Content = contentSnapshot;
                }
                
                if (assistantMsg.Content != contentSnapshot || finalReasoning != assistantMsg.Reasoning)
                {
                    assistantMsg.Reasoning = string.IsNullOrEmpty(finalReasoning) ? null : finalReasoning;
                    // ChatMessage implements INotifyPropertyChanged, Content/Reasoning changes will propagate to UI
                }
            });
        };
        _streamTimer.Start();

        try
        {
            _log.Information($"[CHAT] Calling server: http://{Host}:{Port}, MCP={McpToolsEnabled}", "Chat");

            if (McpToolsEnabled)
            {
                _log.Information("[CHAT] Using SendChatWithToolsAsync", "Chat");
                await _chatService.SendChatWithToolsAsync(
                    Host, Port,
                    SelectedSession.SystemPrompt,
                    SelectedSession.Messages.ToList(),
                    userMessage,
                    imageAttachments,
                    _mcpToolsService,
                    Temperature, TopP, TopK, MinP,
                    RepeatPenalty, PresencePenalty, FrequencyPenalty,
                    Seed, MaxTokens, ParseStopSequences(),
                    onToken: (token) =>
                    {
                        lock (contentLock)
                        {
                            assistantContent.Append(token);
                        }
                    },
                    onMessage: (msg) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            Messages.Add(msg);
                        });
                    },
                    _cancellationTokenSource.Token);
            }
            else
            {
                _log.Information("[CHAT] Using SendChatAsync", "Chat");
                await _chatService.SendChatAsync(
                    Host, Port,
                    SelectedSession.SystemPrompt,
                    SelectedSession.Messages.ToList(),
                    userMessage,
                    imageAttachments,
                    false, null,
                    Temperature, TopP, TopK, MinP,
                    RepeatPenalty, PresencePenalty, FrequencyPenalty,
                    Seed, MaxTokens, ParseStopSequences(),
                    onToken: (token) =>
                    {
                        lock (contentLock)
                        {
                            assistantContent.Append(token);
                        }
                    },
                    onToolCall: _ => { },
                    onReasoning: (reasoningToken) =>
                    {
                        lock (contentLock)
                        {
                            reasoningBuffer.Append(reasoningToken);
                        }
                    },
                    _cancellationTokenSource.Token);
            }

            string finalRawContent, finalReasoningSse;
            lock (contentLock)
            {
                finalRawContent = assistantContent.ToString();
                finalReasoningSse = reasoningBuffer.ToString();
            }
            _log.Information($"[CHAT] Response received, content length={finalRawContent.Length}, reasoning length={reasoningBuffer.Length}", "Chat");

            // Extract reasoning: prefer SSE reasoning_content, fallback to <think> tags
            string? finalReasoning;
            string cleanContent;
            if (!string.IsNullOrEmpty(finalReasoningSse))
            {
                finalReasoning = finalReasoningSse;
                cleanContent = finalRawContent;
            }
            else
            {
                var (tagReasoning, extractedContent) = ExtractReasoning(finalRawContent);
                finalReasoning = tagReasoning;
                cleanContent = extractedContent;
            }
            assistantMsg.Content = cleanContent;
            assistantMsg.Reasoning = string.IsNullOrEmpty(finalReasoning) ? null : finalReasoning;
            assistantMsg.IsStreaming = false;

            // Force final UI update without replacing collection
            OnPropertyChanged(nameof(Messages));

            if (!string.IsNullOrWhiteSpace(cleanContent) || !string.IsNullOrWhiteSpace(finalReasoning))
            {
                SelectedSession.AddMessage(new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    Content = cleanContent,
                    Reasoning = string.IsNullOrEmpty(finalReasoning) ? null : finalReasoning
                });
            }

            await _sessionStore.SaveSessionAsync(SelectedSession);

            // Update context usage
            ContextUsedTokens = EstimateTokenCount(SelectedSession.Messages);
            ContextUsedPercent = Math.Min(100.0, (ContextUsedTokens / (double)ContextSize) * 100.0);
            OnPropertyChanged(nameof(ContextUsageText));
        }
        catch (OperationCanceledException)
        {
            _log.Information("[CHAT] Generation canceled", "Chat");
            string canceledContent;
            lock (contentLock)
            {
                canceledContent = assistantContent.ToString();
            }
            assistantMsg.Content = canceledContent;
            assistantMsg.IsStreaming = false;
            OnPropertyChanged(nameof(Messages));

            if (!string.IsNullOrWhiteSpace(canceledContent))
            {
                SelectedSession.AddMessage(new ChatMessage
                {
                    Role = ChatRole.Assistant,
                    Content = canceledContent
                });
                await _sessionStore.SaveSessionAsync(SelectedSession);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"[CHAT] ERROR: {ex.Message}", "Chat");
            assistantMsg.IsStreaming = false;
            Messages.Remove(assistantMsg);

            // Friendly error message
            string errorMessage = ex.Message.Contains("503") || ex.Message.Contains("loading")
                ? "⚠️ Модель ещё загружается на видеокарту. Подождите минуту и попробуйте снова."
                : $"⚠️ Ошибка: {ex.Message}";

            Messages.Add(new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = errorMessage
            });
            OnPropertyChanged(nameof(Messages));
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

    /// <summary>Estimate token count for messages (rough: 1 token ≈ 4 chars)</summary>
    int EstimateTokenCount(List<ChatMessage> messages)
    {
        int total = 0;
        foreach (var msg in messages)
        {
            total += msg.Content?.Length / 4 ?? 0;
            if (!string.IsNullOrEmpty(msg.Reasoning))
                total += msg.Reasoning.Length / 4;
        }
        return Math.Max(total, 0);
    }

    /// <summary>Compress chat history by summarizing older messages</summary>
    async Task CompressChatHistoryAsync()
    {
        if (IsCompressing || SelectedSession == null || SelectedSession.Messages.Count < 4)
            return;

        IsCompressing = true;
        _log.Information("[CHAT] Starting chat compression...", "Chat");

        // Add a visible compression message
        var compressMsg = new ChatMessage
        {
            Role = ChatRole.System,
            Content = "⏳ Сжатие истории чата для экономии контекста...",
            IsStreaming = false
        };
        Messages.Add(compressMsg);
        SelectedSession.AddMessage(compressMsg);

        try
        {
            // Keep last 2 messages, summarize the rest
            int keepCount = Math.Max(2, SelectedSession.Messages.Count - SelectedSession.Messages.Count / 2);
            var messagesToSummarize = SelectedSession.Messages.Take(keepCount - 2).ToList();

            // Build summary prompt
            var summaryParts = new List<string>();
            foreach (var msg in messagesToSummarize)
            {
                summaryParts.Add($"{msg.Role}: {msg.Content}");
            }

            var summaryPrompt = $@"Сожми этот диалог в краткое резюме (3-5 предложений):
{string.Join("\n---\n", summaryParts)}
---
Резюме:";

            // Send summary request to server
            var summaryMessages = new List<ChatMessage>
            {
                new() { Role = ChatRole.User, Content = summaryPrompt }
            };

            string summary = "";
            try
            {
                await _chatService.SendChatAsync(
                    Host, Port, "", summaryMessages, summaryPrompt, new List<string>(),
                    false, null,
                    0.3f, 0.9f, 40, 0.05f, 1.0f, 0f, 0f, -1, 256, new List<string>(),
                    onToken: (token) => { summary += token; },
                    onToolCall: _ => { },
                    onReasoning: _ => { },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _log.Warning($"[CHAT] Compression failed: {ex.Message}, keeping history as-is", "Chat");
                summary = "[Сжатие не удалось, история сохранена полностью]";
            }

            // Remove compression message
            Messages.Remove(compressMsg);
            var idx = SelectedSession.Messages.IndexOf(compressMsg);
            if (idx >= 0) SelectedSession.Messages.RemoveAt(idx);

            // Replace old messages with summary
            var messagesToRemove = SelectedSession.Messages.Take(keepCount - 2).ToList();
            foreach (var m in messagesToRemove)
                SelectedSession.Messages.Remove(m);

            // Insert summary as system message
            var summaryMsg = new ChatMessage
            {
                Role = ChatRole.System,
                Content = $"📋 Сжато {messagesToRemove.Count} сообщений. Резюме: {summary}",
                IsStreaming = false
            };
            SelectedSession.Messages.Insert(0, summaryMsg);

            // Rebuild Messages collection
            Messages = new ObservableCollection<ChatMessage>(SelectedSession.Messages);
            await _sessionStore.SaveSessionAsync(SelectedSession);

            // Recalculate context
            ContextUsedTokens = EstimateTokenCount(SelectedSession.Messages);
            ContextUsedPercent = Math.Min(100.0, (ContextUsedTokens / (double)ContextSize) * 100.0);
            OnPropertyChanged(nameof(ContextUsageText));

            _log.Information($"[CHAT] Compression done. Tokens: {ContextUsedTokens}/{ContextSize} ({ContextUsedPercent:F1}%)", "Chat");
        }
        finally
        {
            IsCompressing = false;
        }
    }

    public void NotifyImagesChanged()
    {
        OnPropertyChanged(nameof(AttachedImageBase64));
    }

    void StartGeneratingAnimation()
    {
        _generatingPhraseIdx = 0;
        _dotFrameIdx = 0;
        GeneratingText = GeneratingPhrases[0];
        var frame = DotFrames[0];
        Dot1Opacity = frame.d1;
        Dot2Opacity = frame.d2;
        Dot3Opacity = frame.d3;
        _generatingAnimTimer?.Stop();
        _generatingAnimTimer?.Dispose();
        _generatingAnimTimer = new System.Timers.Timer(240) { AutoReset = true };
        _generatingAnimTimer.Elapsed += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _generatingPhraseIdx = (_generatingPhraseIdx + 1) % GeneratingPhrases.Length;
                GeneratingText = GeneratingPhrases[_generatingPhraseIdx];

                _dotFrameIdx = (_dotFrameIdx + 1) % DotFrames.Length;
                var f = DotFrames[_dotFrameIdx];
                Dot1Opacity = f.d1;
                Dot2Opacity = f.d2;
                Dot3Opacity = f.d3;
            });
        };
        _generatingAnimTimer.Start();
    }

    void StopGeneratingAnimation()
    {
        _generatingAnimTimer?.Stop();
        _generatingAnimTimer?.Dispose();
        _generatingAnimTimer = null;
        GeneratingText = "Модель генерирует ответ...";
    }

    [RelayCommand]
    void RemoveImage(string base64)
    {
        if (AttachedImageBase64.Remove(base64))
            OnPropertyChanged(nameof(AttachedImageBase64));
    }

    /// <summary>Extracts reasoning from <think> tags, returns (reasoning, cleanContent)</summary>
    static (string? Reasoning, string Content) ExtractReasoning(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return (null, string.Empty);

        const string openTag = "<think>";
        const string closeTag = "</think>";

        var firstOpen = raw.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
        if (firstOpen < 0)
            return (null, raw); // No reasoning tags found

        var firstClose = raw.IndexOf(closeTag, firstOpen + openTag.Length, StringComparison.OrdinalIgnoreCase);
        if (firstClose < 0)
        {
            // Tag not closed yet (still streaming) — treat everything after open tag as reasoning
            var prefixPart = raw[..firstOpen].Trim();
            var reasoningPart = raw[(firstOpen + openTag.Length)..].Trim();
            return (string.IsNullOrEmpty(reasoningPart) ? null : reasoningPart, prefixPart);
        }

        // Extract reasoning content between tags
        var reasoningText = raw[(firstOpen + openTag.Length)..firstClose].Trim();
        // Combine content before and after the reasoning tags
        var prefixContent = raw[..firstOpen].Trim();
        var suffixContent = raw[(firstClose + closeTag.Length)..].Trim();
        var cleanContent = string.Join("\n", new[] { prefixContent, suffixContent }.Where(s => !string.IsNullOrEmpty(s))).Trim();

        return (string.IsNullOrEmpty(reasoningText) ? null : reasoningText, cleanContent);
    }

    [RelayCommand]
    async Task CopyMessage(ChatMessage message)
    {
        try
        {
            var top = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var window = top?.MainWindow;
            if (window != null)
            {
                var clipboard = window.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(message.Content);
            }
        }
        catch { }
    }

    [RelayCommand]
    void ToggleBookmark(ChatMessage message)
    {
        message.IsBookmarked = !message.IsBookmarked;
        _log.Information($"[CHAT] Bookmark {(message.IsBookmarked ? "added" : "removed")} for message {message.Id}", "Chat");
    }

    [RelayCommand]
    void DeleteMessage(ChatMessage message)
    {
        if (SelectedSession == null) return;
        SelectedSession.Messages.Remove(message);
        Messages.Remove(message);
        _ = _sessionStore.SaveSessionAsync(SelectedSession);
        _log.Information($"[CHAT] Deleted message {message.Id}", "Chat");
    }

    [RelayCommand]
    void UndoMessage(ChatMessage message)
    {
        if (message.Role != ChatRole.User || SelectedSession == null) return;

        // Find the index of this message
        var idx = SelectedSession.Messages.IndexOf(message);
        if (idx < 0) return;

        // Put the user message text back in the input field
        InputText = message.Content;

        // Remove this message and everything after it from session
        for (int i = SelectedSession.Messages.Count - 1; i >= idx; i--)
            SelectedSession.Messages.RemoveAt(i);

        // Also remove from Messages display collection
        while (Messages.Count > SelectedSession.Messages.Count)
            Messages.RemoveAt(Messages.Count - 1);

        _ = _sessionStore.SaveSessionAsync(SelectedSession);
        _log.Information($"[CHAT] Undid message {message.Id}, text restored to input", "Chat");
    }

    [RelayCommand]
    async Task RegenerateMessage(ChatMessage message)
    {
        if (SelectedSession == null || ServerState != Core.Enums.ServerState.Running) return;

        _log.Information($"[CHAT] RegenerateMessage: role={message.Role}, contentLen={message.Content?.Length ?? 0}", "ChatVM");

        // Messages collection has different object instances than SelectedSession.Messages
        // Strategy 1: Find by Id
        var idx = SelectedSession.Messages.FindIndex(m => m.Id == message.Id);

        // Strategy 2: Find by content match (last 50 chars)
        if (idx < 0 && !string.IsNullOrEmpty(message.Content))
        {
            var contentKey = message.Content.Length > 50 ? message.Content[^50..] : message.Content;
            idx = SelectedSession.Messages.FindIndex(m =>
                m.Role == ChatRole.Assistant &&
                (m.Content.Length > 50 ? m.Content[^50..] : m.Content) == contentKey);
        }

        // Strategy 3: Find by index in Messages collection
        if (idx < 0)
        {
            var msgIdx = Messages.IndexOf(message);
            if (msgIdx >= 0 && msgIdx <= SelectedSession.Messages.Count)
                idx = msgIdx;
        }

        _log.Information($"[CHAT] RegenerateMessage: idx={idx}, sessionMsgCount={SelectedSession.Messages.Count}", "ChatVM");

        if (idx < 0 || idx == 0) return;

        // Find the user message before this assistant response
        var userMsg = SelectedSession.Messages[idx - 1];
        if (userMsg.Role != ChatRole.User) return;

        // Remove this assistant message and everything after it from session
        for (int i = SelectedSession.Messages.Count - 1; i >= idx; i--)
            SelectedSession.Messages.RemoveAt(i);

        // Also remove from Messages display collection
        while (Messages.Count > SelectedSession.Messages.Count)
            Messages.RemoveAt(Messages.Count - 1);

        await _sessionStore.SaveSessionAsync(SelectedSession);

        // Re-send the user message to trigger regeneration
        InputText = "";
        AttachedImageBase64.Clear();
        foreach (var img in userMsg.ImageAttachments)
            AttachedImageBase64.Add(img);
        await SendMessageAsync(userMsg.Content, userMsg.ImageAttachments);
    }

    async Task SendMessageAsync(string text, List<string> images)
    {
        InputText = "";
        AttachedImageBase64.Clear();
        foreach (var img in images)
            AttachedImageBase64.Add(img);
        await SendMessageAsync();
    }

    [RelayCommand]
    async Task EditMessage(ChatMessage message)
    {
        if (message.Role != Core.Models.ChatRole.User || SelectedSession == null) return;

        string? newText = await _dialog.ShowInputAsync(
            _loc.T("chat.edit_message"),
            _loc.T("chat.input_watermark"),
            message.Content);

        if (!string.IsNullOrWhiteSpace(newText) && newText != message.Content)
        {
            var idx = SelectedSession.Messages.IndexOf(message);
            // Remove this user message and everything after it (assistant reply, etc.)
            for (int i = SelectedSession.Messages.Count - 1; i >= idx; i--)
                SelectedSession.Messages.RemoveAt(i);

            // Also remove from Messages display collection
            while (Messages.Count > SelectedSession.Messages.Count)
                Messages.RemoveAt(Messages.Count - 1);

            _ = _sessionStore.SaveSessionAsync(SelectedSession);

            // Re-send with new text
            InputText = newText;
            AttachedImageBase64.Clear();
            foreach (var img in message.ImageAttachments)
                AttachedImageBase64.Add(img);
            await SendMessageAsync();
        }
    }

    [RelayCommand]
    void StopGeneration()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    void OpenWebUi()
    {
        var host = Host == "0.0.0.0" ? "127.0.0.1" : Host;
        var url = $"http://{host}:{Port}/";
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            _log.Information($"[CHAT] Opening Web UI: {url}", "ChatVM");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to open Web UI: {url}", "ChatVM");
        }
    }

    [RelayCommand]
    async Task ClearSession()
    {
        if (SelectedSession == null)
            return;

        SelectedSession.Messages.Clear();
        Messages = new ObservableCollection<ChatMessage>();
        // Save session to preserve MCP and settings
        SelectedSession.McpToolsEnabled = McpToolsEnabled;
        SelectedSession.SystemPrompt = SystemPrompt;
        await _sessionStore.SaveSessionAsync(SelectedSession);
    }

    [RelayCommand]
    async Task ExportSession()
    {
        if (SelectedSession == null)
            return;

        var text = $"# {SelectedSession.Name}\n\n";
        foreach (var msg in SelectedSession.Messages)
        {
            text += $"**{msg.RoleName}:**\n{msg.Content}\n\n";
        }

        var suggestedName = $"chat_{SelectedSession.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        var path = await _filePicker.SaveFileAsync(suggestedName, ".md");
        if (path != null)
        {
            await File.WriteAllTextAsync(path, text);
        }
    }

    async Task SaveCurrentSessionAsync()
    {
        if (SelectedSession == null)
            return;

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
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save session", "ChatViewModel");
        }
    }

    List<string> ParseStopSequences()
    {
        if (string.IsNullOrWhiteSpace(StopSequences))
            return new List<string>();

        return StopSequences.Split(',', ';')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    async Task LoadMcpServersAsync()
    {
        try
        {
            var servers = _mcpToolsService.GetMcpServers();
            McpServers = new ObservableCollection<McpServerConfig>(servers);

            // Auto-enable MCP if servers are available
            if (servers.Count > 0 && !McpToolsEnabled)
            {
                McpToolsEnabled = true;
                _log.Information("[CHAT] Auto-enabled MCP tools (servers available)", "ChatVM");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load MCP servers", "ChatViewModel");
        }
    }

    [RelayCommand]
    async Task AddMcpServer()
    {
        var name = NewServerName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var config = new McpServerConfig
        {
            Name = name,
            TransportType = SelectedTransportType
        };

        if (SelectedTransportType == McpTransportType.Stdio)
        {
            config.Command = NewServerCommand.Trim();
            if (string.IsNullOrWhiteSpace(config.Command))
                return;
            var args = NewServerArgs.Trim();
            if (!string.IsNullOrWhiteSpace(args))
                config.Args = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        else
        {
            config.Url = NewServerUrl.Trim();
            if (string.IsNullOrWhiteSpace(config.Url))
                return;
        }

        await _mcpToolsService.AddMcpServerAsync(config);
        await LoadMcpServersAsync();

        NewServerName = "";
        NewServerCommand = "";
        NewServerArgs = "";
        NewServerUrl = "http://127.0.0.1:8080";
    }

    [RelayCommand]
    async Task RemoveMcpServer(string id)
    {
        await _mcpToolsService.RemoveMcpServerAsync(id);
        await LoadMcpServersAsync();
    }

    [RelayCommand]
    async Task ToggleMcpServer(string id)
    {
        var servers = _mcpToolsService.GetMcpServers();
        var server = servers.FirstOrDefault(s => s.Id == id);
        if (server != null)
        {
            await _mcpToolsService.ToggleMcpServerAsync(id, !server.Enabled);
            await LoadMcpServersAsync();
        }
    }

    [RelayCommand]
    async Task EnableMcpServer(string id)
    {
        await _mcpToolsService.ToggleMcpServerAsync(id, true);
        await LoadMcpServersAsync();
    }

    [RelayCommand]
    async Task DisableMcpServer(string id)
    {
        await _mcpToolsService.ToggleMcpServerAsync(id, false);
        await LoadMcpServersAsync();
    }

    // Filesystem MCP folder
    [ObservableProperty] string _filesystemMcpFolder = "";

    public string SetupFilesystemBtn => _loc.T("chat.setup_filesystem");
    public string SelectFsFolderBtn => _loc.T("chat.select_fs_folder");
    public string McpGuideBtn => _loc.T("chat.mcp_guide_btn");

    [RelayCommand]
    async Task SelectFilesystemFolderAsync()
    {
        var folder = await _dialog.SelectFolderAsync(
            _loc.T("chat.select_fs_folder"),
            FilesystemMcpFolder);

        if (!string.IsNullOrWhiteSpace(folder))
        {
            FilesystemMcpFolder = folder;
            await SetupFilesystemMcpAsync(folder);
        }
    }

    async Task SetupFilesystemMcpAsync(string folder)
    {
        try
        {
            var servers = _mcpToolsService.GetMcpServers();
            var existing = servers.FirstOrDefault(s => s.Name == "Filesystem");

            var config = existing ?? new McpServerConfig
            {
                Name = "Filesystem",
                Description = _loc.T("chat.fs_description"),
            };

            config.TransportType = McpTransportType.Stdio;
            config.Command = "npx";
            config.Args = new List<string> { "-y", "@modelcontextprotocol/server-filesystem", folder };
            config.Enabled = true;

            if (existing != null)
            {
                await _mcpToolsService.RemoveMcpServerAsync(existing.Id);
            }

            await _mcpToolsService.AddMcpServerAsync(config);
            await LoadMcpServersAsync();
            _log.Information($"[CHAT] Filesystem MCP configured for: {folder}", "ChatVM");
            await _dialog.ShowSuccessAsync(_loc.T("chat.fs_configured"), _loc.T("chat.success"));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to setup filesystem MCP", "ChatVM");
            await _dialog.ShowErrorAsync(ex.Message);
        }
    }

    // Setup default MCP servers on first launch
    async Task SetupDefaultMcpServersAsync()
    {
        try
        {
            var servers = _mcpToolsService.GetMcpServers();
            if (servers.Count > 0)
                return; // Already configured

            _log.Information("[CHAT] Setting up default MCP servers", "ChatVM");

            // 1. Filesystem MCP (placeholder — user needs to select folder)
            // We'll add it when user clicks the button

            // 2. Jina Web Search (SSE)
            var webSearch = new McpServerConfig
            {
                Name = "Web Search",
                Description = "Internet search via MCP",
                TransportType = McpTransportType.Sse,
                Url = "https://chat.mcp.so/http/app.mcp.so",
                Enabled = true
            };
            await _mcpToolsService.AddMcpServerAsync(webSearch);

            // 3. Brave Search (SSE alternative)
            var braveSearch = new McpServerConfig
            {
                Name = "Brave Search",
                Description = "Brave web search",
                TransportType = McpTransportType.Sse,
                Url = "https://search.brave.com/mcp",
                Enabled = true
            };
            await _mcpToolsService.AddMcpServerAsync(braveSearch);

            await LoadMcpServersAsync();
            _log.Information("[CHAT] Default MCP servers configured", "ChatVM");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to setup default MCP servers", "ChatVM");
        }
    }

    // Code block actions
    public string CopyCodeBtn => _loc.T("chat.copy_code");
    public string ExpandCodeBtn => _loc.T("chat.expand_code");

    [RelayCommand]
    void CopyCode(string code)
    {
        // Clipboard access is handled in View code-behind
        _log.Information($"[CHAT] Copy code requested ({code.Length} chars)", "ChatVM");
    }

    [RelayCommand]
    async Task ExpandCode(ContentPart part)
    {
        // Open code in a dialog
        try
        {
            await _dialog.ShowMessageAsync(
                part.Language.ToUpper(),
                part.Content);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to expand code", "ChatVM");
        }
    }
}
