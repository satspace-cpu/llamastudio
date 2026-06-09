# Llama Studio — AGENTS.md

## Язык общения
- **Все общение с пользователем — только на русском языке.**
- Ответы, пояснения, сводки изменений — всегда на русском.
- Код, комментарии в коде и имена переменных остаются на английском (стандарт).

---

## Полная карта проекта Llama Studio

### Что это
Десктопное Avalonia .NET 8 приложение для управления локальными AI-моделями через llama.cpp сервер.
Поддерживает: запуск/остановку сервера, сканирование моделей, профили запуска, мониторинг сервера, просмотр логов, тестирование API, настройки приложения, мультиязычность (EN/RU).

### Архитектура — 3 слоя (Clean Architecture)

```
Llama Studio (UI)                  → Avalonia Views + ViewModels + App Shell
  └── Llama Studio.Core            → Domain: Models, Interfaces, Enums, Services (Localization)
        └── Llama Studio.Infrastructure → Infrastructure: Llama, Profiles, Updater, Logging, IO
```

**Направления зависимостей:**
```
UI → Core ← Infrastructure
```
- UI зависит от Core (интерфейсы + модели)
- Infrastructure зависит от Core (реализует интерфейсы)
- Core не зависит от UI и Infrastructure

### Директории

```
src/
├── Llama Studio/                    # UI слой (Avalonia Desktop)
│   ├── Program.cs              # Host.CreateDefaultBuilder → DI регистрация → Build/AutoRun
│   │                           # Регистрирует: ISettings, ILogService, ILocalizationService,
│   │                           #   IServerManager, IModelScanner, IProfileManager, IUpdateManager,
│   │                           #   все ViewModels (Singleton), PageViewModel (Transient)
│   ├── App.axaml(.cs)          # Styles: AppStyles.xaml + MainWindow (x:FieldModifier="public")
│   ├── AppStyles.xaml          # Global styles: Window, Button, TextBox, ComboBox, ToggleSwitch,
│   │                           #   Expander, ScrollViewer, TreeView, ProgressBar, TextBlock, Border, Panel
│   ├── Views/
│   │   ├── MainWindow.axaml(.cs)  # Grid 3 колонки (72* Auto Auto), Sidebar + NavigationView
│   │   │                         # Sidebar: логотип + меню (Dashboard, Models, Server, Profiles,
│   │   │   │                     #   Logs, ApiTest, Settings) + статус сервера/модели снизу
│   │   │                         # DataTemplates: DashboardViewModel→DashboardPage и т.д.
│   │   └── Pages/
│   │       ├── DashboardPage.axaml(.cs)   # 6 карточек: Server, Models, Profiles, llama.cpp,
│   │       │                             #   VRAM Usage, GPU Memory + Quick Actions (3 кнопки)
│   │       ├── ModelsPage.axaml(.cs)      # Search + Scan + Browse + Grid карточек моделей
│   │       │                             # ContextMenu: Use in Default, →Server, →mmproj, →Draft
│   │       ├── ServerPage.axaml(.cs)      # TabControl 4 таба: Model, GPU, Context, Advanced
│   │       │                             # Model: путь сервера, 3 модели (main/mmproj/draft),
│   │       │   │                     #   HuggingFace recommended, quick selection
│   │       │                             # GPU: layers, threads, batch, main_gpu, флаги
│   │       │                             # Context: context, batch, ubatch, temp, top_p/top_k/min_p,
│   │       │   │                     #   repeat_penalty, mirostat, seed
│   │       │                             # Advanced: MTP, speculative draft, YARN/Rope, custom args,
│   │       │                               NUMA, cache_prompt, web_ui, metrics, reasoning
│   │       ├── ProfilesPage.axaml(.cs)    # Left: список профилей + New/Import
│   │       │                             # Right: редактор профиля (name, desc, model, GPU, sampling)
│   │       │                               кнопки: SetDefault, SaveSettings, StartServer
│   │       ├── LogsPage.axaml(.cs)        # Search + MinLevel filter + Export/Clear + ItemsRepeater
│   │       ├── ApiTestPage.axaml(.cs)     # Host/Port + 3 кнопки (Health, Models, Chat) + Output
│   │       └── SettingsPage.axaml(.cs)    # Language, llama.cpp dir, models dir, auto-check,
│   │                                     #   portable mode, theme, update channel, server defaults,
│   │                                     #   llama.cpp releases list с Download & Install
│   ├── Converters/
│   │   ├── BoolToOpacityConverter      # true→1.0, false→0.3
│   │   ├── InvertedBoolConverter       # логическое НЕ
│   │   ├── ServerStateToBrushConverter # Running=Green, Starting=Amber, Stopped=Gray, Error=Red
│   │   ├── ServerStateToVisibilityConverter # Running/Starting→Visible, else Collapsed
│   │   └── LogSeverityToBrushConverter # Debug=Gray, Info=Blue, Warning=Amber, Error=Red, Fatal=White
│   └── ViewModels/
│       ├── MainViewModel               # INotifyPropertyChanged + _pages (Dictionary<Type, IPageViewModel>)
│       │                               # CurrentPage: getter маппит тип → IPageViewModel, setter переключает
│       │                               # AutoStartDefaultProfile(): если сервер не запущен → default profile → Start
│       │                               # OnLanguageChanged(): обновляет DisplayName у всех IPageViewModel
│       │                               # NavigateTo<T>()
│       ├── DashboardViewModel : IPageViewModel  # DisplayName="main.dashboard"
│       │                               # ServerState (ServerStatus), ModelsCount, ProfilesCount,
│       │                               # LlamaCppVersion, IsActive, VramUsedGb, GpuTotalGb,
│       │                               # ActiveModelName, UptimeText
│       │                               # Commands: ScanModelsAsync, CreateProfileAsync, CheckUpdatesAsync
│       │                               # Подписка: _serverManager.StatusChanged → UpdateServerStatusAsync
│       │                               # UpdateDashboardAsync: агрегирует данные из всех сервисов
│       ├── ModelsViewModel : IPageViewModel     # DisplayName="main.models"
│       │                               # Models (ObservableCollection<GgufModelInfo>),
│       │                               # SearchText, ModelsDirectory, IsScanning
│       │                               # Commands: ScanModelsAsync, BrowseDirectoryAsync,
│       │                               #   SelectModelAsync, SetAsDefaultModelAsync,
│       │                               #   SetAsServerModelAsync, SetAsServerMmprojAsync,
│       │                               #   SetAsServerDraftAsync
│       ├── ServerViewModel : IPageViewModel     # DisplayName="main.server"
│       │                               # ServerStatus (ServerStatus), IsRunning, Port, Host,
│       │                               # LlamaCppDirectory, ServerExePath, ExecutableFound,
│       │                               # ModelPath, MmprojPath, DraftModelPath, EnableSpeculative,
│       │                               # GpuLayers, Threads, ThreadsBatch, MainGpu, FlashAttention,
│       │                               # MemoryMap, Mlock, NoMmq, NoKvOffload,
│       │                               # ContextSize, BatchSize, UbatchSize, MaxTokens,
│       │                               # Temperature, TopP, TopK, MinP, TypicalP,
│       │                               # RepeatPenalty, RepeatLastN, PresencePenalty,
│       │                               # FrequencyPenalty, Seed, MirostatSampling, MirostatTau,
│       │                               # MirostatEta, PredictCount, EnableMtp,
│       │                               # DraftGpuLayers, DraftNMax, DraftNMin, DraftPSplit,
│       │                               # DraftPMin, RopeFreqBase, RopeFreqScale, YarnOrigCtx,
│       │                               # Num a, CachePrompt, ContBatching, VerboseLogging,
│       │                               # WebUi, Metrics, Reasoning, EmbeddingMode,
│       │                               # CustomArgs, ProcessPriority,
│       │                               # ProfileId, Profiles (ObservableCollection<ServerProfile>),
│       │                               # SelectedProfile, HfRepo, OfflineMode,
│       │                               # RecommendedModels (HuggingFace рекомendации),
│       │                               # ScannedModels (ObservableCollection<GgufModelInfo>),
│       │                               # IsScanning, ModelsDirectory, Timeout, Slots
│       │                               # Commands: StartServerAsync, StopServerAsync, HealthCheckAsync,
│       │                               #   SaveProfileAsync, LoadProfileAsync, CreateProfileAsync,
│       │                               #   DeleteProfileAsync, RenameProfileAsync, ClearProfileSelection,
│       │                               #   ExportProfileAsync, ImportProfileAsync, RefreshProfilesAsync,
│       │                               #   ExportBatAsync, CopyCmdlineAsync, SelectLlamaDirectoryAsync,
│       │                               #   CheckVersionAsync, BrowseModelAsync, BrowseMmprojAsync,
│       │                               #   BrowseDraftAsync, QuickScanModelsAsync,
│       │                               #   QuickSelectModelAsync, QuickSelectMmprojAsync,
│       │                               #   QuickSelectDraftAsync, SelectHfModelAsync
│       │                               # LoadProfileSettings(): заполняет VM из профиля
│       │                               # BuildProfileFromSettings(): строит ServerProfile из VM
│       │                               # UpdateServerStatus(): обновляет UI из ServerStatus
│       │                               # Подписка: _serverManager.StatusChanged + LogReceived
│       ├── ProfilesViewModel : IPageViewModel   # DisplayName="main.profiles"
│       │                               # Profiles (ObservableCollection<ServerProfile>),
│       │                               # SelectedProfile, NewProfileName, NewProfileDesc,
│       │                               # AvailableModels (ObservableCollection<GgufModelInfo>),
│       │                               # SelectedModel
│       │                               # Commands: CreateProfileAsync, ImportProfileAsync,
│       │                               #   SetDefaultProfileAsync, EditProfileAsync,
│       │                               #   DuplicateProfileAsync, ExportProfileAsync,
│       │                               #   DeleteProfileAsync, SaveProfileSettingsAsync,
│       │                               #   StartServerWithProfileAsync, LoadModelsAsync
│       ├── LogsViewModel : IPageViewModel       # DisplayName="main.logs"
│       │                               # Logs (ObservableCollection<LogEntry>),
│       │                               # FilterText, MinLevel (LogLevel?)
│       │                               # Commands: ExportLogsAsync, ClearLogsAsync
│       │                               # ApplyFilters(): фильтрует по уровню и тексту
│       ├── ApiTestViewModel : IPageViewModel    # DisplayName="main.apitest"
│       │                               # Host, Port, Output (ObservableCollection<string>),
│       │                               # IsTesting
│       │                               # Commands: TestHealthAsync, TestModelsAsync, TestChatAsync
│       ├── SettingsViewModel : IPageViewModel   # DisplayName="main.settings"
│       │                               # Language, LlamaCppDirectory, ModelsDirectory,
│       │                               # AutoCheckUpdates, PortableMode, Theme (string),
│       │                               # UpdateChannel (string), ActiveVersion,
│       │                               # DefaultHost, DefaultPort, DefaultGpuLayers,
│       │                               # DefaultFlashAttention, BuildType (string),
│       │                               # Releases (ObservableCollection<LlamaCppRelease>),
│       │                               # IsCheckingUpdates
│       │                               # Commands: BrowseLlamaDirectoryAsync, BrowseModelsDirectoryAsync,
│       │                               #   CheckForUpdatesAsync, DownloadAndInstallAsync,
│       │                               #   InstallReleaseAsync, SaveSettingsAsync
│       │                               # LoadSettingsAsync / SaveSettingsAsync
│       └── IPageViewModel                     # DisplayName (string), IsSelected (bool)

├── Llama Studio.Core/               # Core слой (Domain + Interfaces)
│   ├── Models/
│   │   ├── GgufModelInfo.cs       # FileName, Name, Path, Size, SizeStr, Layers, TotalLayers,
│   │   │                         # RamGb, VramGb, ContextSize, FileType, QuantPkg,
│   │   │                         # IsVisionModel, IsEmbeddingModel, IsLoraModel, IsPrefixModel,
│   │   │                         # IsSparsityModel, IsTokenClassifierModel, IsUnknownModel
│   │   ├── ServerProfile.cs       # Id (Guid), Name, Description, IsDefault, CreatedAt, ModifiedAt,
│   │   │                         # ModelPath, MmprojPath, DraftModelPath, EnableSpeculative,
│   │   │                         # Host, Port, GpuLayers, Threads, ThreadsBatch, MainGpu,
│   │   │                         # FlashAttention, MemoryMap, Mlock, NoMmq, NoKvOffload,
│   │   │                         # ContextSize, BatchSize, UbatchSize, MaxTokens,
│   │   │                         # Temperature, TopP, TopK, MinP, TypicalP,
│   │   │                         # RepeatPenalty, RepeatLastN, PresencePenalty,
│   │   │                         # FrequencyPenalty, Seed, MirostatSampling, MirostatTau,
│   │   │                         # MirostatEta, PredictCount, EnableMtp,
│   │   │                         # DraftGpuLayers, DraftNMax, DraftNMin, DraftPSplit,
│   │   │                         # DraftPMin, RopeFreqBase, RopeFreqScale, YarnOrigCtx,
│   │   │                         # Num a, CachePrompt, ContBatching, VerboseLogging,
│   │   │                         # WebUi, Metrics, Reasoning, EmbeddingMode,
│   │   │                         # CustomArgs, ProcessPriority
│   │   │                         # BuildFullArgsString() → строит аргументы для llama-server.exe
│   │   │                         # ToJson() / FromJson() → сериализация
│   │   ├── ServerStatus.cs        # State (ServerState), Port, Host, ModelName, ContextSize,
│   │   │                         # Threads, GpuLayers, VramUsedGb, RamUsedGb,
│   │   │                         # TokensPerSecond, QueueSize, ActiveSlots,
│   │   │                         # TotalTokensProcessed, Uptime, StartedAt,
│   │   │                         # ErrorMessage, ProcessId
│   │   ├── LogEntry.cs            # Timestamp, Level (LogLevel), Message, Source, IsStderr
│   │   ├── LlamaCppRelease.cs     # TagName, Name, PublishedAt, Assets (List<ReleaseAsset>),
│   │   │                           # DownloadUrl, DownloadUrlFiltered, DownloadedSize,
│   │   │                           # InstallAction, CanInstall, CanDownload, IsInstalling,
│   │   │                           # InstallDirectory, IsValid
│   │   └── ReleaseAsset.cs        # Name, Size, BrowserDownloadUrl
│   ├── Interfaces/
│   │   ├── IServerManager         # StartAsync(ServerProfile), StopAsync, GetStatusAsync,
│   │   │                         # HealthCheckAsync(host, port)
│   │   │                         # Events: StatusChanged(EventHandler<ServerStatus>),
│   │   │                           LogReceived(EventHandler<LogEntry>)
│   │   ├── IModelScanner          # ScanDirectoryAsync(directory), AnalyzeModelAsync(modelPath),
│   │   │                           # FormatSize(bytes), EstimateVramUsage(model, gpuLayers)
│   │   ├── IProfileManager        # GetAllProfilesAsync, GetProfileAsync(id), CreateProfile(name),
│   │   │                           # SaveProfileAsync(profile), DeleteProfileAsync(id),
│   │   │                           # DuplicateProfileAsync(id), ImportProfileAsync(json),
│   │   │                           # ExportProfile(profile), SetDefaultProfileAsync(id),
│   │   │                           # GetDefaultProfileAsync
│   │   │                           # Event: ProfileChanged(Action<string>)
│   │   ├── IUpdateManager         # CheckForUpdatesAsync(prerelease),
│   │   │                           # DownloadAndInstallAsync(release, targetDir),
│   │   │                           # FilterReleasesByBuildType(releases, buildType)
│   │   ├── ISettings              # INotifyPropertyChanged
│   │   │                           # LlamaCppDirectory, ModelsDirectory, AutoCheckUpdates,
│   │   │                           # PortableMode, Theme, Language, UpdateChannel,
│   │   │                           # DefaultHost, DefaultPort, DefaultGpuLayers,
│   │   │                           # DefaultFlashAttention, ActiveVersion, DataDirectory,
│   │   │                           # ConfigDirectory, BackupDirectory, LogDirectory,
│   │   │                           # GetSettingsPath(), GetProfilesDirectory(),
│   │   │                           # GetBackupDirectory(), GetLogDirectory()
│   │   ├── ILogService            # Debug/Information/Warning/Error(message, source)
│   │   ├── ILocalizationService   # Language, T(key), ChangeLanguage(language)
│   │   │                           # Event: OnLanguageChanged(EventHandler<string>)
│   │   └── IPageViewModel         # DisplayName, IsSelected (для навигации)
│   ├── Enums/
│   │   ├── ServerState.cs         # Stopped, Starting, Running, Stopping, Error
│   │   └── LogLevel.cs            # Debug, Information, Warning, Error, Fatal
│   └── Services/
│       └── LocalizationService    # Реализация ILocalizationService
│                                   # Встроенный словарь 320+ ключей (EN/RU)
│                                   # Формат: ["key"] = new() { ["en"] = "...", ["ru"] = "..." }
│                                   # Fallback: ru → en → [key]

└── Llama Studio.Infrastructure/       # Infrastructure слой (реализации интерфейсов)
    ├── Llama/
    │   ├── ServerManager.cs       # Реализация IServerManager
    │   │                         # Process-based запуск llama-server.exe
    │   │                         # BuildServerArgs() → profile.BuildFullArgsString()
    │   │                         # HealthCheckAsync → GET /health → парсинг JSON
    │   │                         # Background health polling каждые 2 сек
    │   │                         # Process priority support
    │   │                         # Output/Error redirection → LogReceived events
    │   │                         # IDisposable (Kill + Dispose)
    │   └── ModelScanner.cs        # Реализация IModelScanner
    │                               # ScanDirectoryAsync → поиск .gguf файлов
    │                               # AnalyzeModelAsync → GGUF header parsing (layers, RAM, VRAM)
    │                               # FormatSize → human-readable размер
    │                               # EstimateVramUsage → оценка по layers + model size
    ├── Profiles/
    │   └── ProfileManager.cs      # Реализация IProfileManager
    │                               # JSON файл: %config%/profiles.json
    │                               # CRUD + Import/Export + Duplicate + SetDefault
    ├── Updater/
    │   └── UpdateManager.cs       # Реализация IUpdateManager
    │                               # GitHub API: /repos/ggerganov/llama.cpp/releases
    │                               # Filter by build type (CUDA 12.x, CUDA 13.x, Vulkan, CPU)
    │                               # Download + extract (SharpCompress)
    │                               # Auto-detect llama-server.exe
    ├── Logging/
    │   └── FileLogService.cs      # Реализация ILogService
    │                               # File logging: %log%/app.log
    │                               # In-memory buffer (последние N записей)
    ├── IO/
    │   └── (file operations)      # Утилиты для работы с файлами
    ├── Models/
    │   └── (infrastructure models)
    └── Api/
        └── (empty — пока нет dedicated HTTP сервиса, используется HttpClient напрямую)
```

### Resources
```
Resources/
└── (embedded resources — иконки, шрифты и т.д.)
```

### NuGet пакеты
- **Avalonia 11.3.6** — cross-platform UI framework
- **Avalonia.Themes.Fluent** — Fluent Design тема
- **CommunityToolkit.Mvvm** — MVVM helpers (ObservableProperty, RelayCommand) — через Llama Studio.Core
- **Microsoft.Extensions.DependencyInjection** — DI контейнер
- **Microsoft.Extensions.Hosting** — Host builder
- **SharpCompress** — извлечение архивов (для llama.cpp обновлений)

### Ключевые паттерны

1. **Navigation**: `MainViewModel.CurrentPage` маппит `Type → IPageViewModel` через Dictionary.
   - Setter CurrentPage вызывает `AutoStartDefaultProfile()` при первом запуске.
   - Sidebar в MainWindow использует привязки к `IsSelected` каждого IPageViewModel.

2. **Dependency Injection**: `Program.cs` регистрирует всё через `Host.CreateDefaultBuilder()`:
   - Singleton: ISettings, ILogService, ILocalizationService, IServerManager, IModelScanner, IProfileManager, IUpdateManager
   - Singleton: все ViewModels (*ViewModel)
   - Transient: IPageViewModel (для PageViewModel регистрации)

3. **Settings**: `ISettings` — в-memory кэш с `INotifyPropertyChanged`.
   - Путь: `%config%/settings.json` (ConfigDirectory из ISettings)
   - Portable mode: данные в папке приложения вместо AppData

4. **Localization**: `ILocalizationService.T("key")` с fallback ru → en → `[key]`.
   - Событие `OnLanguageChanged` для обновления UI при смене языка.
   - Ключи в формате `section.key` (например `main.dashboard`, `server.start_btn`).
   - 320+ ключей, полностью покрывают все страницы.

5. **Server Management**: `IServerManager` (ServerManager) запускает `llama-server.exe` как Process.
   - Аргументы строятся из `ServerProfile.BuildFullArgsString()`.
   - Health check через `GET /health` каждые 2 секунды.
   - Логирование stdout/stderr сервера в реальном времени через `LogReceived` события.
   - Поддержка process priority.

6. **Profiles**: `ServerProfile` — полная модель параметров сервера с JSON сериализацией.
   - Поддержка 3 моделей: main, mmproj (vision), draft (speculative decoding).
   - Параметры: GPU, sampling, context, MTP, YARN/Rope, custom args и др.
   - Хранение: JSON файл `profiles.json` в ConfigDirectory.

7. **Models**: `IModelScanner` сканирует директорию на `.gguf` файлы.
   - Парсинг GGUF заголовков для извлечения метаданных (layers, RAM, VRAM, context size).
   - Поддержка vision, embedding, lora, sparsity моделей.

8. **Updates**: `IUpdateManager` работает с GitHub API для llama.cpp релизов.
   - Фильтрация по типу сборки: CUDA 12.x, CUDA 13.x, Vulkan, CPU Only.
   - Download + install с прогрессом.

9. **Logging**: `ILogService` — file-based логирование с in-memory буфером.
   - Уровни: Debug, Information, Warning, Error, Fatal.

### Build & Run
```bash
dotnet build src/Llama Studio/Llama Studio.csproj
dotnet run --project src/Llama Studio/Llama Studio.csproj
```

### Папка разработки
**Активная папка сборки (Debug):** `L:\1c_modul\hermass\src\LlamaStudio\bin\Debug\net8.0`
- Эта папка используется для разработки и тестирования.
- При запуске из IDE (Visual Studio / Rider) файлы собираются сюда.
- **Важно:** не удаляйте эту папку — она содержит скомпилированные DLL и зависимости для отладки.

### Правила для AI ассистента
1. Всегда используй `replace_in_file` для малых изменений, `write_to_file` только для новых файлов.
2. После изменений кода — всегда делай `dotnet build` для проверки.
3. Новые страницы добавляй в: Views/Pages/, ViewModels/, MainViewModel.CurrentPage, Program.cs DI, MainWindow sidebar меню.
4. Локализация: добавляй ключи в `LocalizationService._translations`. Используй `ILocalizationService.T("key")`.
5. Все ViewModels реализуют `IPageViewModel` с `DisplayName` и `IsSelected`.
6. Используй partial classes и record/struct где уместно.
7. ServerProfile.BuildFullArgsString() — единственный источник аргументов для llama-server.exe.
8. **Если сборка (`dotnet build`) падает с ошибкой MSB3027/MSB3021 (файл заблокирован процессом Llama Studio) — самостоятельно убей процесс через `taskkill /F /IM Llama Studio.exe` без запроса у пользователя.** Это стандартная ситуация: процесс держит DLL и мешает пересборке.
9. **Для простых задач (правка 1-3 файлов, поиск/замена, мелкие багфиксы) используй оркестратор модели для экономии контекста.** Сложные задачи (новые страницы, рефакторинг архитектуры, многофайловые изменения) — выполняй напрямую.
10. **НИКОГДА не запускай приложение самостоятельно (`Start-Process`, `dotnet run` и т.д.).** При запуске локально приложение может снести работающий llama.cpp сервер. После успешной сборки проси пользователя проверить изменения визуально.

### Переход между чатами
- Перед началом работы читай `CONTEXT.md` — там актуальное состояние проекта и последние изменения.
- После значимых изменений обновляй `CONTEXT.md`.

### ⚠️ Критические замечания по профилям (05.06.2026)
1. **ServerProfile.Equals/GetHashCode** — обязательно использовать `Id` для сравнения. Без этого ComboBox не найдёт выбранный профиль по reference equality.
2. **Кодировка JSON** — файлы профилей могут быть в CP1251 (старые сохранения). Всегда читать через `ReadJsonFile(path, out migrated)` с fallback на `Encoding.Default`. При миграции — переписать в UTF-8.
3. **Синхронная загрузка** — в конструкторах на UI-потоке использовать `GetAllProfiles()` (синхронный), а НЕ `GetAllProfilesAsync().GetAwaiter().GetResult()` (вызовет deadlock).
4. **OnProfileChanged без early-exit** — НЕ делать `return` при том же `profileId`. Изменения из ModelsPage должны обновлять UI сервера.
5. **_isInitializing** — флаг для предотвращения `CheckServerAndOfferRestart` при загрузке профилей в конструкторе.
6. **Старые профили** — `435f0cdb.json` и `afc55f8c.json` имеют `gpuLayers` как число вместо строки. При загрузке выдаётся ошибка, не влияет на работу. Удалить или переписать вручную.
