using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Enums;
using LlamaStudio.Core.Interfaces;
using LlamaStudio.Core.Models;
using System.Collections.ObjectModel;

namespace LlamaStudio.ViewModels;

public partial class ServerViewModel : ObservableObject
{
    readonly IServerManager _serverManager;
    readonly IProfileManager _profileManager;
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly IDialogService _dialog;
    readonly IModelScanner _scanner;
    readonly ILlamaUpdater _updater;
    readonly ILocalizationService _loc;
    readonly IHelpParser _helpParser;
    readonly ICliValidator _cliValidator;
    readonly IHuggingFaceDownloader _hfDownloader;
    readonly IGpuMonitor _gpuMonitor;
    readonly DispatcherTimer _gpuTimer;
    int _tickCounter;
    string? _previousSelectedProfileId;

    // Translated strings
    public string Title => _loc.T("server.title");
    public string StartBtn => _loc.T("server.start_btn");
    public string StopBtn => _loc.T("server.stop_btn");
    public string HealthCheckBtn => _loc.T("server.health_check_btn");
    public string SaveProfileBtn => _loc.T("server.save_profile_btn");
    public string LoadProfileBtn => _loc.T("server.load_profile_btn");
    public string CopyFromProfileBtn => _loc.T("server.copy_from_profile_btn");
    public string RenameProfileBtn => _loc.T("server.rename_profile_btn");
    public string DeleteProfileBtn => _loc.T("server.delete_profile_btn");
    public string CreateProfileBtn => _loc.T("server.create_profile_btn");
    public string ExportBatBtn => _loc.T("server.export_bat_btn");
    public string CopyCmdLineBtn => _loc.T("server.copy_cmdline_btn");
    public string NoProfileSelected => _loc.T("server.no_profile_selected");
    public string NoModelSelected => _loc.T("server.no_model_selected");
    public string BrowseModelBtn => _loc.T("server.browse_model_btn");
    public string BrowseMmprojBtn => _loc.T("server.browse_mmproj_btn");
    public string BrowseDraftBtn => _loc.T("server.browse_draft_btn");
    public string QuickScanBtn => _loc.T("server.quick_scan_btn");
    public string QuickSelectModelBtn => _loc.T("server.quick_select_model_btn");
    public string QuickSelectMmprojBtn => _loc.T("server.quick_select_mmproj_btn");
    public string QuickSelectDraftBtn => _loc.T("server.quick_select_draft_btn");
    public string CheckVersionBtn => _loc.T("server.check_version_btn");
    public string ValidateCliBtn => _loc.T("server.validate_cli_btn");
    public string SelectLlamaDirBtn => _loc.T("server.select_llama_dir_btn");

    // Computed display text
    public string ExecutableFoundText => ExecutableFound ? string.Format(_loc.T("server.exec_found"), ExecutablePath) : _loc.T("server.exec_not_found");

    // Actual server directory (with version subfolder if active version exists)
    public string ServerDirectory
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
            {
                var versionedPath = Path.Combine(LlamaCppDirectory, _settings.ActiveLlamaCppVersion);
                if (Directory.Exists(versionedPath))
                    return versionedPath;
            }
            return LlamaCppDirectory;
        }
    }

    // ServerPage labels
    public string StateLabel => _loc.T("server.state_label");
    public string TabModel => _loc.T("server.tab_model");
    public string TabGpu => _loc.T("server.tab_gpu");
    public string TabContextSampling => _loc.T("server.tab_context_sampling");
    public string TabAdvanced => _loc.T("server.tab_advanced");
    public string TabConnection => _loc.T("server.tab_connection");
    public string ServerPathTitle => _loc.T("server.server_path_title");
    public string ExecHint => _loc.T("server.exec_hint");
    public string MainModelLabel => $"{_loc.T("server.main_model")} (-m)";
    public string ModelWatermark => _loc.T("server.model_watermark");
    public string MmprojTitle => $"{_loc.T("server.mmproj_title")} (--mmproj)";
    public string OptionalBadge => _loc.T("server.optional");
    public string MmprojWatermark => _loc.T("server.mmproj_watermark");
    public string DraftTitle => $"{_loc.T("server.draft_title")} (-md)";
    public string AdvancedBadge => _loc.T("server.advanced_badge");
    public string EnableSpeculative => $"{_loc.T("server.enable_speculative")} (-md)";
    public string DraftWatermark => _loc.T("server.draft_watermark");
    public string QuickSelectTitle => _loc.T("server.quick_select_title");
    public string HfTitle => $"{_loc.T("server.hf_title")} (--hf)";
    public string DownloadToLabel => _loc.T("server.download_to_fmt");
    public string SelectThisModelBtn => _loc.T("server.select_this_model");
    public string DownloadModelBtn => _loc.T("server.download_model_btn");
    public string DownloadingStatus => _loc.T("server.downloading_status");
    public string OrManual => _loc.T("server.or_manual");
    public string RepoLabel => _loc.T("server.repo_label");
    public string OfflineMode => $"{_loc.T("server.offline_mode")} (--offline)";
    public string GpuAcceleration => _loc.T("server.gpu_acceleration");
    public string GpuLayersLabel => $"{_loc.T("server.gpu_layers_label")} (-ngl)";
    public string ThreadsLabel => $"{_loc.T("server.threads_label")} (-t)";
    public string ThreadsBatchLabel => $"{_loc.T("server.threads_batch_label")} (-tb)";
    public string MainGpuLabel => $"{_loc.T("server.main_gpu_label")} (-mg)";
    public string FlashAttentionCheck => $"{_loc.T("server.flash_attention")} (-fa)";
    public string MemoryMapCheck => $"{_loc.T("server.memory_map")} (--mmap)";
    public string MlockCheck => $"{_loc.T("server.mlock")} (--mlock)";
    public string MmqCheck => $"MMQ (--no-mmq)";
    public string KvOffloadCheck => $"KV Offload (-nkvo)";
    public string CacheTypeKLabel => $"{_loc.T("server.cache_type_k")} (-ctk)";
    public string CacheTypeVLabel => $"{_loc.T("server.cache_type_v")} (-ctv)";
    public string CacheTypeKTip => _loc.T("server.tip_cache_type_k");
    public string CacheTypeVTip => _loc.T("server.tip_cache_type_v");
    public IEnumerable<string> CacheTypeKOptions => new[] { "f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1" };
    public IEnumerable<string> CacheTypeVOptions => new[] { "f32", "f16", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1" };
    public string CacheRamLabel => $"{_loc.T("server.cache_ram")} (--cache-ram)";
    public string CacheRamTip => _loc.T("server.tip_cache_ram");
    public string CacheReuseLabel => $"{_loc.T("server.cache_reuse")} (--cache-reuse)";
    public string CacheReuseTip => _loc.T("server.tip_cache_reuse");
    public string ContextBatchTitle => _loc.T("server.context_batch");
    public string ContextLabel => $"{_loc.T("server.context_label")} (-c)";
    public string BatchLabel => $"{_loc.T("server.batch_label")} (-b)";
    public string UbatchLabel => $"{_loc.T("server.ubatch_label")} (-ub)";
    public string MaxTokensLabel => $"{_loc.T("server.max_tokens_label")} (-n)";
    public string SamplingTitle => _loc.T("server.sampling_title");
    public string TemperatureLabel => $"{_loc.T("server.temperature_label")} (--temp)";
    public string TopPLabel => $"{_loc.T("server.top_p_label")} (--top-p)";
    public string TopKLabel => $"{_loc.T("server.top_k_label")} (--top-k)";
    public string MinPLabel => $"{_loc.T("server.min_p_label")} (--min-p)";
    public string TypicalPLabel => $"{_loc.T("server.typical_p_label")} (--typical-p)";
    public string RepeatPenaltyLabel => $"{_loc.T("server.repeat_penalty_label")} (--repeat-penalty)";
    public string RepeatLastNLabel => $"{_loc.T("server.repeat_last_n_label")} (--repeat-last-n)";
    public string PresencePenaltyLabel => $"{_loc.T("server.presence_penalty_label")} (--presence-penalty)";
    public string FrequencyPenaltyLabel => $"{_loc.T("server.frequency_penalty_label")} (--frequency-penalty)";
    public string SeedLabel => $"{_loc.T("server.seed_label")} (-s)";
    public string MirostatSamplingCheck => $"{_loc.T("server.mirostat_sampling")} (--mirostat)";
    public string TauLabel => $"{_loc.T("server.tau_label")} (--mirostat-ent)";
    public string EtaLabel => $"{_loc.T("server.eta_label")} (--mirostat-lr)";
    public string MtpTitle => _loc.T("server.mtp_title");
    public string SpecTypeLabel => $"{_loc.T("server.spec_type_label")} (--spec-type)";
    public string TipSpecType => _loc.T("server.tip_spec_type");
    public string PredictCountLabel => $"{_loc.T("server.predict_count_label")} (-n)";
    public string SpecDraftParamsTitle => _loc.T("server.spec_draft_params");
    public string DraftGpuLayersLabel => $"{_loc.T("server.draft_gpu_layers_label")} (-ngld)";
    public string DraftNMaxLabel => $"{_loc.T("server.draft_n_max_label")} (--spec-draft-n-max)";
    public string DraftNMinLabel => $"{_loc.T("server.draft_n_min_label")} (--spec-draft-n-min)";
    public string DraftPSplitLabel => $"{_loc.T("server.draft_p_split_label")} (--spec-draft-p-split)";
    public string DraftPMinLabel => $"{_loc.T("server.draft_p_min_label")} (--spec-draft-p-min)";
    public string YarnRopeTitle => _loc.T("server.yarn_rope_title");
    public string RopeFreqBaseLabel => $"{_loc.T("server.rope_freq_base_label")} (--rope-freq-base)";
    public string RopeFreqScaleLabel => $"{_loc.T("server.rope_freq_scale_label")} (--rope-freq-scale)";
    public string YarnOrigCtxLabel => $"{_loc.T("server.yarn_orig_ctx_label")} (--yarn-orig-ctx)";
    public string AdvancedOptionsTitle => _loc.T("server.advanced_options_title");
    public string NumaCheck => $"{_loc.T("server.numa")} (--numa)";
    public string CachePromptCheck => $"{_loc.T("server.cache_prompt")} (--cache-prompt)";
    public string ContBatchingCheck => $"{_loc.T("server.cont_batching")} (-cb)";
    public string ContextShiftCheck => $"{_loc.T("server.context_shift")} (--context-shift)";
    public string VerboseLoggingCheck => $"{_loc.T("server.verbose_logging")} (-lv 4)";
    public string WebUICheck => $"{_loc.T("server.web_ui")} (--ui)";
    public string MetricsCheck => $"{_loc.T("server.metrics")} (--metrics)";
    public string ReasoningCheck => $"{_loc.T("server.reasoning")} (-rea)";
    public string EmbeddingModeCheck => $"{_loc.T("server.embedding_mode")} (--embedding)";
    public string PriorityHighCheck => $"{_loc.T("server.priority_high")} (process priority)";
    public string TipPriorityHigh => _loc.T("server.tip_priority_high");
    public string CustomArgsTitle => _loc.T("server.custom_args_title");
    public string CustomArgsWatermark => _loc.T("server.custom_args_placeholder");
    public string ExportTitle => _loc.T("server.export_title");
    public string ConnectionSettingsTitle => _loc.T("server.connection_settings");
    public string HostLabel => $"{_loc.T("server.host_label")} (--host)";
    public string PortLabel => $"{_loc.T("server.port_label")} (--port)";
    public string TimeoutLabel => $"{_loc.T("server.timeout_label")} (-to)";
    public string SlotsLabel => $"{_loc.T("server.slots_label")} (-np)";
    public string ProfileControlsTitle => _loc.T("server.profile_controls");
    public string ProfileLabel => _loc.T("server.profile_label");
    public string CmdLinePreviewTitle => _loc.T("server.cmdline_preview_title");
    public string UnsupportedFlagsTitle => _loc.T("server.unsupported_flags_title");
    public string UnsupportedFlagsHint => _loc.T("server.unsupported_flags_hint");

    // Tooltips
    public string TipModelPath => "Путь к файлу модели для загрузки (-m). Поддерживает перетаскивание.";
    public string TipMmprojPath => "Путь к файлу мультимодального проектора для моделей зрения (--mmproj). Необходим для моделей вроде LLaVA.";
    public string TipDraftModelPath => "Черновая модель для спекулятивного декодирования (--spec-draft-model, -md)";
    public string TipGpuLayers => "Слои на GPU (-ngl): число (например 32), 'all' — все слои, 'auto' — автоопределение. По умолчанию: all";
    public string TipThreads => "Количество потоков CPU для генерации (-t). Больше потоков = быстрее, но с убывающей отдачей.";
    public string TipThreadsBatch => "Количество потоков CPU для обработки батча (-tb). По умолчанию: равно -t.";
    public string TipMainGpu => "Основной GPU для вычислений (--main-gpu). Индекс GPU, начиная с 0.";
    public string TipFlashAttention => "Включить Flash Attention для более быстрого и экономичного по памяти вычисления внимания";
    public string TipMmap => "Использовать mmap для загрузки модели (--mmap). Отключить для медленной загрузки, но меньшего pageout. По умолчанию: включено";
    public string TipMlock => "Удерживать модель в RAM, предотвращая своппинг (--mlock)";
    public string TipMmq => "Использовать MMQ для кэширования. Отключить для меньшего потребления памяти. По умолчанию: включено";
    public string TipKvOffload => "Разгружать KV-кэш на GPU. Отключить если не хватает VRAM. По умолчанию: включено";
    public string TipContextSize => "Размер контекста промпта в токенах (-c). 0 = использовать значение из модели.";
    public string TipBatchSize => "Максимальное количество токенов, обрабатываемых за раз при оценке промпта (-b). По умолчанию: 2048";
    public string TipUBatchSize => "Физический размер батча для обработки (-ubatch). Должен быть ≤ размера батча. По умолчанию: 512";
    public string TipMaxTokens => "Максимальное количество токенов для генерации за ответ (-n). -1 = без явного лимита.";
    public string TipTemperature => "Управляет случайностью вывода (-temp). Выше = креативнее, ниже = точнее. По умолчанию: 0.8";
    public string TipTopP => "Порог nucleus-сэмплирования (--top-p). Оставляет токены с кумулятивной вероятностью ≤ P. По умолчанию: 0.95";
    public string TipTopK => "Ограничивает выбор токенов до K наиболее вероятных (--top-k). Меньше = меньше разнообразия. По умолчанию: 40";
    public string TipMinP => "Порог минимальной вероятности для сэмплирования токенов (--min-p). Отсеивает маловероятные токены. По умолчанию: 0.05";
    public string TipTypicalP => "Порог типичного сэмплирования (--typical-p). Значения < 1.0 увеличивают разнообразие. По умолчанию: 1.0";
    public string TipRepeatPenalty => "Штраф за повторение одинаковых токенов (--repeat-penalty). По умолчанию llama.cpp: 1.1.";
    public string TipRepeatLastN => "Окно токенов для проверки повторений (--repeat-last-n). 0 = отключено, -1 = весь контекст. По умолчанию: -1";
    public string TipPresencePenalty => "Штраф за присутствие токена для уменьшения повторений (--presence-penalty). По умолчанию: 0.0";
    public string TipFrequencyPenalty => "Штраф за частоту токена для уменьшения повторений (--frequency-penalty). По умолчанию: 0.0";
    public string TipSeed => "Seed ГСЧ для воспроизводимости вывода (-s, --seed). -1=случайный";
    public string TipMirostat => "Адаптивный алгоритм сэмплирования Mirostat для контроля perplexity";
    public string TipTau => "Целевая perplexity для Mirostat (--mirostat-tau). По умолчанию: 5";
    public string TipEta => "Скорость обучения Mirostat (--mirostat-eta). По умолчанию: 0.1";
    public string TipPredictCount => "Количество токенов для генерации (то же что и Макс. токенов в разделе Контекст). -1 = без ограничения";
    public string TipSpecDraftGpuLayers => "Макс. число слоёв драфта в VRAM (-ngld). Число, 'auto' или 'all'. По умолчанию: auto";
    public string TipSpecDraftNMax => "Число токенов для драфта (--spec-draft-n-max). По умолчанию: 3";
    public string TipSpecDraftNMin => "Минимальное число токенов драфта (--spec-draft-n-min). По умолчанию: 0";
    public string TipSpecDraftPSplit => "Вероятность разделения (--spec-draft-p-split). По умолчанию: 0.10";
    public string TipSpecDraftPMin => "Минимальная вероятность (greedy) (--spec-draft-p-min). По умолчанию: 0.00";
    public string TipRopeFreqBase => "Базовая частота RoPE (--rope-freq-base). По умолчанию: из модели.";
    public string TipRopeFreqScale => "Масштаб RoPE (--rope-freq-scale). По умолчанию: из модели.";
    public string TipYarnOriginalContext => "Оригинальный размер контекста для YARN (--yarn-orig-ctx). По умолчанию: из модели.";
    public string TipNuma => "Включить NUMA-aware распределение памяти (--numa)";
    public string TipCachePrompt => "Кэшировать промпты для повторных запросов (--cache-prompt). По умолчанию: включено";
    public string TipContBatching => "Включить непрерывный батчинг (-cb). По умолчанию: включено";
    public string TipContextShift => "Включить сдвиг контекста (--context-shift). По умолчанию: включено";
    public string TipVerboseLogging => "Подробное логирование сервера (--verbose)";
    public string TipWebUI => "Включить встроенный веб-чат, доступный из браузера";
    public string TipMetrics => "Включить метрики в формате Prometheus на /metrics для мониторинга";
    public string TipReasoning => "Включить режим reasoning/мышления (-rea). По умолчанию: авто (определяется из модели)";
    public string TipEmbeddingMode => "Ограничить сервер режимом эмбеддинга. Отключает endpoint чат-генерации.";
    public string TipHost => "IP-адрес сервера. По умолчанию: 127.0.0.1 (только localhost). Используйте 0.0.0.0 для всех интерфейсов.";
    public string TipPort => "TCP-порт сервера. По умолчанию: 8080";
    public string TipTimeout => "Таймаут чтения/записи сервера в секундах (-to). По умолчанию llama.cpp: 3600.";
    public string TipSlots => "Количество параллельных слотов запросов (-np, --parallel). По умолчанию: авто (-1)";
    public string TipCustomArgs => "Любые дополнительные аргументы CLI llama-server, по одному на строку. Напр. --special-flag value";

    // Server state
    [ObservableProperty] ServerState _state = ServerState.Stopped;
    [ObservableProperty] double _vramUsedGb;
    [ObservableProperty]
    double _ramUsedGb;
    static readonly double s_totalRamGb = GetTotalPhysicalMemory() / (1024.0 * 1024.0 * 1024.0);
    public string RamUsedText => RamUsedGb > 0
        ? $"{RamUsedGb:F1} / {s_totalRamGb:F1} GB"
        : $"— / {s_totalRamGb:F1} GB";

    partial void OnRamUsedGbChanged(double value)
    {
        OnPropertyChanged(nameof(RamUsedText));
    }

    static long GetTotalPhysicalMemory()
    {
        try
        {
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(64);
            try
            {
                System.Runtime.InteropServices.Marshal.WriteInt32(ptr, 64);
                if (GlobalMemoryStatusEx(ptr))
                    return System.Runtime.InteropServices.Marshal.ReadInt64(ptr, 8);
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr); }
        }
        catch { }
        return 64L * 1024 * 1024 * 1024;
    }

    [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true)]
    static extern bool GlobalMemoryStatusEx(System.IntPtr lpBuffer);
    [ObservableProperty] double _tokensPerSecond;
    [ObservableProperty] double _promptTokensPerSecond;
    [ObservableProperty] double _gpuVramUsedGb;
    [ObservableProperty] int _queueSize;
    [ObservableProperty] int _activeSlots;
    [ObservableProperty] TimeSpan _uptime;
    [ObservableProperty] string _processId = "—";
    [ObservableProperty] string _errorMessage = string.Empty;

    // Profile selection
    [ObservableProperty] ObservableCollection<ServerProfile> _profiles = new();
    [ObservableProperty] ServerProfile? _selectedProfile;
    [ObservableProperty] string _commandLinePreview = string.Empty;

    // Model paths
    [ObservableProperty] string _modelPath = string.Empty;
    [ObservableProperty] string _mmprojPath = string.Empty;
    [ObservableProperty] string _draftModelPath = string.Empty;
    [ObservableProperty] string _modelDisplayName = "No model selected";
    [ObservableProperty] string _mmprojDisplayName = "No mmproj selected";
    [ObservableProperty] string _draftDisplayName = "No draft model selected";

    // HuggingFace
    [ObservableProperty] string _hfRepo = string.Empty;
    [ObservableProperty] string _hfFile = string.Empty;
    [ObservableProperty] bool _hfOffline;
    [ObservableProperty] string _hfRepoDraft = string.Empty;
    [ObservableProperty] bool _isDownloadingHf;
    [ObservableProperty] double _hfDownloadProgress;
    [ObservableProperty] string _hfDownloadStatus = string.Empty;
    [ObservableProperty] string _hfDownloadDirectory = string.Empty;
    [ObservableProperty] string _modelDescription = string.Empty;
    [ObservableProperty] string _modelSizeInfo = string.Empty;
    [ObservableProperty] string _modelParamsInfo = string.Empty;

    // Recommended models for quick selection
    public ObservableCollection<RecommendedModel> RecommendedModels { get; } = new();

    partial void OnHfRepoChanged(string value)
    {
        var match = RecommendedModels.FirstOrDefault(m => m.Repo == value);
        if (match != null)
        {
            ModelDescription = match.Description;
            ModelSizeInfo = $"~{match.Size}";
            ModelParamsInfo = match.Params;
        }
        else
        {
            ModelDescription = string.Empty;
            ModelSizeInfo = string.Empty;
            ModelParamsInfo = string.Empty;
        }
    }

    partial void OnLlamaCppDirectoryChanged(string value)
    {
        UpdateExecutablePath();
    }

    // Connection
    [ObservableProperty] string _host = "127.0.0.1";
    [ObservableProperty] int _port = 8080;
    [ObservableProperty] int _timeout = 3600;
    [ObservableProperty] int _slots = -1;

    // GPU
    [ObservableProperty] string _gpuLayers = "all";
    [ObservableProperty] int _threads = -1;
    [ObservableProperty] int _threadsBatch = -1;
    [ObservableProperty] bool _flashAttention = true;
    [ObservableProperty] bool _mmap = true;
    [ObservableProperty] bool _mlock;
    [ObservableProperty] bool _mmqEnabled = true;
    [ObservableProperty] bool _kvOffloadEnabled = true;
    [ObservableProperty] int _mainGpu;
    [ObservableProperty] GpuSplitMode _gpuSplitMode;
    [ObservableProperty] string _tensorSplit = string.Empty;

    // Context & Batch
    [ObservableProperty] int _contextSize;
    [ObservableProperty] int _batchSize = 2048;
    [ObservableProperty] int _ubatchSize = 512;
    [ObservableProperty] int _maxTokens = -1;

    // Cache type
    [ObservableProperty] string _cacheTypeK = "f16";
    [ObservableProperty] string _cacheTypeV = "f16";

    // Cache management
    [ObservableProperty] int _cacheRam = -1;
    [ObservableProperty] int _cacheReuse = 32;

    // Sampling
    [ObservableProperty] float _temperature = 0.8f;
    [ObservableProperty] int _topK = 40;
    [ObservableProperty] float _topP = 0.95f;
    [ObservableProperty] float _minP = 0.05f;
    [ObservableProperty] float _typicalP = 1f;
    [ObservableProperty] float _repeatPenalty = 1.0f;
    [ObservableProperty] int _repeatLastN = 64;
    [ObservableProperty] float _presencePenalty;
    [ObservableProperty] float _frequencyPenalty;
    [ObservableProperty] int _seed = -1;

    // Mirostat
    [ObservableProperty] MirostatMode _mirostatMode;
    public int MirostatModeInt
    {
        get => (int)MirostatMode;
        set => MirostatMode = (MirostatMode)value;
    }
    [ObservableProperty] float _mirostatTau = 5f;
    [ObservableProperty] float _mirostatEta = 0.1f;

    // DRY
    [ObservableProperty] float _dryMultiplier;
    [ObservableProperty] float _dryBase = 1.75f;

    // Dynatemp
    [ObservableProperty] float _dynatempStddev;

    // XTC
    [ObservableProperty] float _xtcProbability;
    [ObservableProperty] float _xtcThreshold = 0.1f;

    // MTP / Speculative
    [ObservableProperty] string _specType = "none";

    public IEnumerable<string> SpecTypeOptions => new[]
    {
        "none", "draft-simple", "draft-eagle3", "draft-mtp",
        "ngram-simple", "ngram-map-k", "ngram-map-kdv",
        "ngram-mod", "ngram-cache", "draft"
    };
    [ObservableProperty] int _predictCount = -1;
    [ObservableProperty] bool _speculativeDecoding;
    [ObservableProperty] string _specDraftGpuLayers = string.Empty;
    [ObservableProperty] int _specDraftNMax = 3;
    [ObservableProperty] int _specDraftNMin;
    [ObservableProperty] float _specDraftPSplit = 0.1f;
    [ObservableProperty] float _specDraftPMin = 0.0f;

    // Advanced
    [ObservableProperty] bool _priorityHigh;
    [ObservableProperty] bool _embeddingMode;
    [ObservableProperty] string _poolingType = string.Empty;
    [ObservableProperty] bool _numa;
    [ObservableProperty] string _processPriority = "Normal";
    [ObservableProperty] bool _cachePrompt = true;
    [ObservableProperty] bool _contBatching = true;
    [ObservableProperty] bool _contextShift = true;
    [ObservableProperty] bool _verboseLogging;
    [ObservableProperty] bool _enableWebUI = true;
    [ObservableProperty] bool _enableSlots = true;
    [ObservableProperty] bool _enableMetrics;
    [ObservableProperty] bool _reasoning;
    [ObservableProperty] int _reasoningBudget;

    // Rope / YARN
    [ObservableProperty] string _ropeScaling = string.Empty;
    [ObservableProperty] double? _ropeFreqBase;
    [ObservableProperty] double? _ropeFreqScale;
    [ObservableProperty] int? _yarnOriginalContext;
    [ObservableProperty] double? _yarnExtFactor;
    [ObservableProperty] double? _yarnAttnFactor;
    [ObservableProperty] double? _yarnBetaFast;
    [ObservableProperty] double? _yarnBetaSlow;

    // Extra
    [ObservableProperty] string _apiKey = string.Empty;
    [ObservableProperty] string _alias = string.Empty;
    [ObservableProperty] string _additionalArgs = string.Empty;

    // Llama.cpp info
    [ObservableProperty] string _llamaCppDirectory = string.Empty;
    [ObservableProperty] string _executablePath = string.Empty;
    [ObservableProperty] bool _executableFound;
    [ObservableProperty] string _llamaVersionInfo = string.Empty;
    [ObservableProperty] string _cudaInfo = string.Empty;

    // Help parser / unsupported flags
    LlamaHelpInfo? _helpInfo;
    public List<string> UnsupportedFlags { get; private set; } = new();

    // CLI Validation
    [ObservableProperty] ValidationReport? _validationReport;
    [ObservableProperty] CliChangeReport? _cliChangeReport;
    [ObservableProperty] bool _isCliValidating;
    [ObservableProperty] string _validationSummary = string.Empty;
    [ObservableProperty] string _validationDetails = string.Empty;
    [ObservableProperty] Avalonia.Media.SolidColorBrush _validationSummaryForeground = new(Avalonia.Media.Color.Parse("#F87171"));

    // Scanned models for quick selection
    [ObservableProperty] ObservableCollection<GgufModelInfo> _scannedModels = new();
    [ObservableProperty] GgufModelInfo? _quickSelectedModel;
    [ObservableProperty] bool _isScanningModels;

    // Debounce for command line preview update
    System.Threading.Timer? _commandLineDebounceTimer;
    // Debounce for auto-save profile (500ms after last change)
    System.Threading.Timer? _autoSaveDebounceTimer;
    bool _isInitializing;

    // --- TextBox text wrappers for numeric properties ---
    public string ThreadsText
    {
        get => Threads.ToString();
        set => Threads = int.TryParse(value, out var v) ? v : -1;
    }
    public string ThreadsBatchText
    {
        get => ThreadsBatch.ToString();
        set => ThreadsBatch = int.TryParse(value, out var v) ? v : -1;
    }
    public string MainGpuText
    {
        get => MainGpu.ToString();
        set => MainGpu = int.TryParse(value, out var v) ? v : 0;
    }
    public string CacheRamText
    {
        get => CacheRam.ToString();
        set => CacheRam = int.TryParse(value, out var v) ? v : -1;
    }
    public string CacheReuseText
    {
        get => CacheReuse.ToString();
        set => CacheReuse = int.TryParse(value, out var v) ? v : 0;
    }
    public string ContextSizeText
    {
        get => ContextSize.ToString();
        set => ContextSize = int.TryParse(value, out var v) ? v : 0;
    }
    public string BatchSizeText
    {
        get => BatchSize.ToString();
        set => BatchSize = int.TryParse(value, out var v) ? v : 2048;
    }
    public string UbatchSizeText
    {
        get => UbatchSize.ToString();
        set => UbatchSize = int.TryParse(value, out var v) ? v : 512;
    }
    public string MaxTokensText
    {
        get => MaxTokens.ToString();
        set => MaxTokens = int.TryParse(value, out var v) ? v : -1;
    }
    public string TemperatureText
    {
        get => Temperature.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => Temperature = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.8f;
    }
    public string TopPText
    {
        get => TopP.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => TopP = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.95f;
    }
    public string TopKText
    {
        get => TopK.ToString();
        set => TopK = int.TryParse(value, out var v) ? v : 40;
    }
    public string MinPText
    {
        get => MinP.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => MinP = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.05f;
    }
    public string TypicalPText
    {
        get => TypicalP.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => TypicalP = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1f;
    }
    public string RepeatPenaltyText
    {
        get => RepeatPenalty.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => RepeatPenalty = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 1.1f;
    }
    public string RepeatLastNText
    {
        get => RepeatLastN.ToString();
        set => RepeatLastN = int.TryParse(value, out var v) ? v : -1;
    }
    public string PresencePenaltyText
    {
        get => PresencePenalty.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => PresencePenalty = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
    public string FrequencyPenaltyText
    {
        get => FrequencyPenalty.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => FrequencyPenalty = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;
    }
    public string SeedText
    {
        get => Seed.ToString();
        set => Seed = int.TryParse(value, out var v) ? v : -1;
    }
    public string MirostatTauText
    {
        get => MirostatTau.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => MirostatTau = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 5f;
    }
    public string MirostatEtaText
    {
        get => MirostatEta.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => MirostatEta = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.1f;
    }
    public string PredictCountText
    {
        get => PredictCount.ToString();
        set => PredictCount = int.TryParse(value, out var v) ? v : -1;
    }
    public string SpecDraftNMaxText
    {
        get => SpecDraftNMax.ToString();
        set => SpecDraftNMax = int.TryParse(value, out var v) ? v : 3;
    }
    public string SpecDraftNMinText
    {
        get => SpecDraftNMin.ToString();
        set => SpecDraftNMin = int.TryParse(value, out var v) ? v : 0;
    }
    public string SpecDraftPSplitText
    {
        get => SpecDraftPSplit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => SpecDraftPSplit = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.1f;
    }
    public string SpecDraftPMinText
    {
        get => SpecDraftPMin.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        set => SpecDraftPMin = float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0.0f;
    }
    public string PortText
    {
        get => Port.ToString();
        set => Port = int.TryParse(value, out var v) ? v : 8080;
    }
    public string TimeoutText
    {
        get => Timeout.ToString();
        set => Timeout = int.TryParse(value, out var v) ? v : 600;
    }
    public string SlotsText
    {
        get => Slots.ToString();
        set => Slots = int.TryParse(value, out var v) ? v : -1;
    }

    // --- Toggle labels (reuse existing *Check properties) ---
    public string FlashAttentionLabel => FlashAttentionCheck;
    public string MemoryMapLabel => MemoryMapCheck;
    public string MlockLabel => MlockCheck;
    public string MmqLabel => MmqCheck;
    public string KvOffloadLabel => KvOffloadCheck;
    public string NumaLabel => NumaCheck;
    public string CachePromptLabel => CachePromptCheck;
    public string ContBatchingLabel => ContBatchingCheck;
    public string ContextShiftLabel => ContextShiftCheck;
    public string VerboseLoggingLabel => VerboseLoggingCheck;
    public string WebUILabel => WebUICheck;
    public string MetricsLabel => MetricsCheck;
    public string ReasoningLabel => ReasoningCheck;
    public string EmbeddingModeLabel => EmbeddingModeCheck;
    public string PriorityHighLabel => PriorityHighCheck;

    public ServerViewModel(
        IServerManager serverManager,
        IProfileManager profileManager,
        ISettings settings,
        ILogService log,
        IDialogService dialog,
        IModelScanner scanner,
        ILlamaUpdater updater,
        ILocalizationService loc,
        IHelpParser helpParser,
        ICliValidator cliValidator,
        IHuggingFaceDownloader hfDownloader,
        IGpuMonitor gpuMonitor)
    {
        _serverManager = serverManager;
        _profileManager = profileManager;
        _settings = settings;
        _log = log;
        _dialog = dialog;
        _scanner = scanner;
        _updater = updater;
        _loc = loc;
        _helpParser = helpParser;
        _cliValidator = cliValidator;
        _hfDownloader = hfDownloader;
        _gpuMonitor = gpuMonitor;

        _gpuTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _gpuTimer.Tick += (_, _) => _ = RefreshGpuAsync();
        _gpuTimer.Start();

        _autoSaveDebounceTimer = new System.Threading.Timer(_ =>
        {
            Dispatcher.UIThread.Post(() => PerformAutoSaveProfile());
        }, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

        _serverManager.StatusChanged += OnStatusChanged;
        _loc.OnLanguageChanged += OnLanguageChanged;
        _profileManager.ProfileChanged += OnProfileChanged;
        PropertyChanged += OnViewModelPropertyChanged;
        _updater.CliChangesDetected += OnCliChangesDetected;

        Host = _settings.DefaultHost;
        Port = _settings.DefaultPort;
        GpuLayers = _settings.DefaultGpuLayers;
        FlashAttention = _settings.FlashAttention;
        LlamaCppDirectory = _settings.LlamaCppDirectory;
        HfDownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache\\huggingface\\hub");

        // Wire up HF download progress events (must be on UI thread)
        _hfDownloader.DownloadProgress += OnHfDownloadProgress;
        _hfDownloader.StatusMessage += OnHfStatusMessage;

        UpdateExecutablePath();
        PopulateRecommendedModels();
        LoadProfiles();
    }

    void OnHfDownloadProgress(object? sender, double progress) => HfDownloadProgress = progress;
    void OnHfStatusMessage(object? sender, string msg) => HfDownloadStatus = msg;

    void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        var propsToUpdatePreview = new[]
        {
            nameof(ModelPath), nameof(MmprojPath), nameof(DraftModelPath),
            nameof(Host), nameof(Port), nameof(Timeout), nameof(Slots),
            nameof(GpuLayers), nameof(Threads), nameof(ThreadsBatch), nameof(MainGpu),
            nameof(FlashAttention), nameof(Mmap), nameof(Mlock), nameof(MmqEnabled), nameof(KvOffloadEnabled),
            nameof(ContextSize), nameof(BatchSize), nameof(UbatchSize), nameof(MaxTokens),
            nameof(CacheTypeK), nameof(CacheTypeV), nameof(CacheRam), nameof(CacheReuse),
            nameof(Temperature), nameof(TopK), nameof(TopP), nameof(MinP), nameof(TypicalP),
            nameof(RepeatPenalty), nameof(RepeatLastN), nameof(PresencePenalty), nameof(FrequencyPenalty),
            nameof(Seed), nameof(MirostatMode), nameof(MirostatTau), nameof(MirostatEta),
            nameof(DryMultiplier), nameof(DryBase), nameof(DynatempStddev),
            nameof(XtcProbability), nameof(XtcThreshold),
            nameof(SpecType), nameof(PredictCount), nameof(SpeculativeDecoding),
            nameof(SpecDraftGpuLayers), nameof(SpecDraftNMax), nameof(SpecDraftNMin),
            nameof(SpecDraftPSplit), nameof(SpecDraftPMin),
            nameof(RopeScaling), nameof(RopeFreqBase), nameof(RopeFreqScale),
            nameof(YarnOriginalContext), nameof(YarnExtFactor), nameof(YarnAttnFactor),
            nameof(YarnBetaFast), nameof(YarnBetaSlow),
            nameof(EmbeddingMode), nameof(PoolingType), nameof(Numa),
            nameof(CachePrompt), nameof(ContBatching), nameof(ContextShift), nameof(VerboseLogging),
            nameof(EnableWebUI), nameof(EnableSlots), nameof(EnableMetrics),
            nameof(Reasoning), nameof(ReasoningBudget),
            nameof(ApiKey), nameof(Alias), nameof(PriorityHigh), nameof(AdditionalArgs)
        };

        if (propsToUpdatePreview.Contains(e.PropertyName))
        {
            _commandLineDebounceTimer?.Dispose();
            _commandLineDebounceTimer = new System.Threading.Timer(_ =>
            {
                _ = UpdateCommandLinePreviewAsync();
                if (_helpInfo != null)
                    Dispatcher.UIThread.Post(() => UpdateUnsupportedFlags());
            }, null, 300, System.Threading.Timeout.Infinite);

            // Auto-save profile with debounce
            _autoSaveDebounceTimer?.Change(500, System.Threading.Timeout.Infinite);
        }
    }

    async Task UpdateCommandLinePreviewAsync()
    {
        if (SelectedProfile == null) return;
        var preview = await System.Threading.Tasks.Task.Run(() =>
        {
            SyncSettingsToProfile();
            return SelectedProfile.BuildArgsString();
        });
        Dispatcher.UIThread.Post(() =>
        {
            CommandLinePreview = preview;
        });
    }

    async void OnProfileChanged(string profileId)
    {
        var freshProfile = await _profileManager.GetProfileAsync(profileId);
        if (freshProfile == null) return;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existing = Profiles.FirstOrDefault(p => p.Id == profileId);
            if (existing != null)
            {
                var idx = Profiles.IndexOf(existing);
                Profiles[idx] = freshProfile;
            }
            else
            {
                Profiles.Add(freshProfile);
            }

            if (SelectedProfile?.Id == profileId)
            {
                // Same profile — refresh UI with fresh data (model paths may have changed from ModelsPage)
                ApplyProfileToSettings(freshProfile);
                SelectedProfile.CustomArguments = new Dictionary<string, string>();
                CommandLinePreview = freshProfile.BuildArgsString();
            }
            else
            {
                // Different profile — switch
                SelectedProfile = freshProfile;
            }
        });
    }

    void OnLanguageChanged(object? sender, string language)
    {
        // Trigger PropertyChanged for all translated properties
        foreach (var prop in new[]
        {
            nameof(Title), nameof(StartBtn), nameof(StopBtn), nameof(HealthCheckBtn), nameof(SaveProfileBtn), nameof(LoadProfileBtn),
            nameof(CopyFromProfileBtn), nameof(RenameProfileBtn), nameof(CreateProfileBtn), nameof(DeleteProfileBtn), nameof(ExportBatBtn), nameof(CopyCmdLineBtn), nameof(NoProfileSelected),
            nameof(NoModelSelected), nameof(BrowseModelBtn), nameof(BrowseMmprojBtn), nameof(BrowseDraftBtn),
            nameof(QuickScanBtn), nameof(QuickSelectModelBtn), nameof(QuickSelectMmprojBtn), nameof(QuickSelectDraftBtn),
            nameof(CheckVersionBtn), nameof(SelectLlamaDirBtn), nameof(StateLabel), nameof(TabModel), nameof(TabGpu),
            nameof(TabContextSampling), nameof(TabAdvanced), nameof(TabConnection), nameof(ServerPathTitle),
            nameof(ExecHint), nameof(MainModelLabel), nameof(ModelWatermark), nameof(MmprojTitle), nameof(OptionalBadge),
            nameof(MmprojWatermark), nameof(DraftTitle), nameof(AdvancedBadge), nameof(EnableSpeculative),
            nameof(DraftWatermark), nameof(QuickSelectTitle), nameof(HfTitle), nameof(DownloadToLabel),
            nameof(SelectThisModelBtn), nameof(DownloadModelBtn), nameof(DownloadingStatus), nameof(OrManual), nameof(RepoLabel), nameof(OfflineMode),
            nameof(GpuAcceleration), nameof(GpuLayersLabel), nameof(ThreadsLabel), nameof(ThreadsBatchLabel),
            nameof(MainGpuLabel), nameof(FlashAttentionCheck), nameof(MemoryMapCheck), nameof(MlockCheck),
            nameof(MmqCheck), nameof(KvOffloadCheck), nameof(ContextBatchTitle), nameof(ContextLabel),
            nameof(BatchLabel), nameof(UbatchLabel), nameof(MaxTokensLabel), nameof(SamplingTitle),
            nameof(TemperatureLabel), nameof(TopPLabel), nameof(TopKLabel), nameof(MinPLabel), nameof(TypicalPLabel),
            nameof(RepeatPenaltyLabel), nameof(RepeatLastNLabel), nameof(PresencePenaltyLabel), nameof(FrequencyPenaltyLabel),
            nameof(SeedLabel), nameof(MirostatSamplingCheck), nameof(TauLabel), nameof(EtaLabel), nameof(MtpTitle),
            nameof(PredictCountLabel), nameof(SpecDraftParamsTitle), nameof(DraftGpuLayersLabel),
            nameof(DraftNMaxLabel), nameof(DraftNMinLabel), nameof(DraftPSplitLabel), nameof(DraftPMinLabel),
            nameof(YarnRopeTitle), nameof(RopeFreqBaseLabel), nameof(RopeFreqScaleLabel), nameof(YarnOrigCtxLabel),
            nameof(AdvancedOptionsTitle), nameof(NumaCheck), nameof(CachePromptCheck), nameof(ContBatchingCheck), nameof(ContextShiftCheck),
            nameof(VerboseLoggingCheck), nameof(WebUICheck), nameof(MetricsCheck), nameof(ReasoningCheck),
            nameof(EmbeddingModeCheck), nameof(PriorityHighCheck), nameof(CustomArgsTitle), nameof(CustomArgsWatermark), nameof(ExportTitle),
            nameof(ConnectionSettingsTitle), nameof(HostLabel), nameof(PortLabel), nameof(TimeoutLabel),
            nameof(SlotsLabel), nameof(ProfileControlsTitle), nameof(CmdLinePreviewTitle),
            nameof(UnsupportedFlagsTitle), nameof(UnsupportedFlagsHint),
            nameof(ExecutableFoundText), nameof(TipSpecType), nameof(TipPriorityHigh)
        })
            OnPropertyChanged(prop);
    }

    void PopulateRecommendedModels()
    {
        RecommendedModels.Add(new("bartowski/Llama-4-Maverick-17B-128E-Instruct-Q4_K_M.gguf", "Llama 4 Maverick 17B", "Q4_K_M", "~9.5 GB", "17B params, 128K context"));
        RecommendedModels.Add(new("bartowski/Llama-3.3-70B-Instruct-Q4_K_M.gguf", "Llama 3.3 70B Instruct", "Q4_K_M", "~43 GB", "70B params, general purpose"));
        RecommendedModels.Add(new("bartowski/Qwen2.5-Coder-32B-Instruct-Q4_K_M.gguf", "Qwen 2.5 Coder 32B", "Q4_K_M", "~20 GB", "32B params, code generation"));
        RecommendedModels.Add(new("bartowski/Mistral-Nemo-12B-Instruct-Q4_K_M.gguf", "Mistral Nemo 12B", "Q4_K_M", "~7.5 GB", "12B params, multilingual"));
        RecommendedModels.Add(new("bartowski/Llama-3.1-8B-Instruct-Q4_K_M.gguf", "Llama 3.1 8B Instruct", "Q4_K_M", "~5.5 GB", "8B params, lightweight"));
    }

    [RelayCommand]
    void SelectRecommendedModel(RecommendedModel model)
    {
        HfRepo = model.Repo;
    }

    [RelayCommand]
    async Task DownloadHfModelAsync()
    {
        if (string.IsNullOrWhiteSpace(HfRepo))
        {
            HfDownloadStatus = _loc.T("models.enter_repo_first");
            return;
        }

        IsDownloadingHf = true;
        HfDownloadProgress = 0;
        HfDownloadStatus = _loc.T("server.starting_download");

        try
        {
            var localPath = await _hfDownloader.DownloadModelAsync(HfRepo, HfOffline);

            // Auto-set model path after successful download
            ModelPath = localPath;
            UpdateDisplayNames();

            HfDownloadStatus = string.Format(_loc.T("server.download_complete"), Path.GetFileName(localPath));
            _log.Information($"HF model downloaded: {localPath}", "Server");
        }
        catch (OperationCanceledException)
        {
            HfDownloadStatus = _loc.T("server.download_cancelled");
        }
        catch (Exception ex)
        {
            HfDownloadStatus = string.Format(_loc.T("server.download_error"), ex.Message);
            _log.Error(ex, "HF download failed", "Server");
        }
        finally
        {
            IsDownloadingHf = false;
        }
    }

    void UpdateExecutablePath()
    {
        // Try versioned subdirectory first (e.g., LlamaStudio\b9553-cuda12x\)
        var serverDir = LlamaCppDirectory;
        if (!string.IsNullOrWhiteSpace(_settings.ActiveLlamaCppVersion))
        {
            var versionedPath = Path.Combine(LlamaCppDirectory, _settings.ActiveLlamaCppVersion);
            if (Directory.Exists(versionedPath) && File.Exists(Path.Combine(versionedPath, "llama-server.exe")))
                serverDir = versionedPath;
        }

        ExecutablePath = Path.Combine(serverDir, "llama-server.exe");
        ExecutableFound = File.Exists(ExecutablePath);

        if (ExecutableFound)
        {
            _ = ParseHelpAsync();
        }
        else
        {
            UnsupportedFlags.Clear();
            _helpInfo = null;
        }
    }

    partial void OnSelectedProfileChanged(ServerProfile? value)
    {
        if (value != null)
        {
            ApplyProfileToSettings(value);
            value.CustomArguments = new Dictionary<string, string>();
            CommandLinePreview = value.BuildArgsString();
            _settings.LastSelectedProfileId = value.Id.ToString();
            _ = _settings.SaveAsync();

            // Only restart server if profile actually changed (different ID)
            string currentId = value.Id.ToString();
            if (!_isInitializing && _previousSelectedProfileId != currentId)
                _ = CheckServerAndOfferRestart(value);
            _previousSelectedProfileId = currentId;
        }
        else
        {
            CommandLinePreview = string.Empty;
        }
    }

    async Task CheckServerAndOfferRestart(ServerProfile profile)
    {
        var status = await _serverManager.GetStatusAsync();
        if (status.State == ServerState.Running)
        {
            // Sync UI values to the target profile (not SelectedProfile!)
            SyncSettingsToProfileTarget(profile);
            // Clear stale custom args
            profile.CustomArguments = new Dictionary<string, string>();

            var loadingWin = _dialog.ShowLoading(
                string.Format(_loc.T("server.restart_profile"), profile.Name),
                "Загрузка");

            try
            {
                await _serverManager.StopAsync();
                await Task.Delay(1500);
                await _serverManager.StartAsync(profile);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to restart server", "Server");
                await _dialog.ShowErrorAsync(string.Format(_loc.T("server.restart_error"), ex.Message));
            }
            finally
            {
                loadingWin?.Dispose();
            }
        }
    }

    void LoadProfiles()
    {
        _isInitializing = true;
        try
        {
            var list = _profileManager.GetAllProfiles();
            _log.Information($"Loaded {list.Count} profiles", "Server");

            Profiles.Clear();
            foreach (var p in list)
                Profiles.Add(p);

            ServerProfile? toSelect = null;
            if (!string.IsNullOrWhiteSpace(_settings.LastSelectedProfileId))
                toSelect = Profiles.FirstOrDefault(p => p.Id == _settings.LastSelectedProfileId);
            if (toSelect == null && Profiles.Count > 0)
                toSelect = Profiles.FirstOrDefault();

            if (toSelect != null)
                SelectedProfile = toSelect;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    void ApplyProfileToSettings(ServerProfile profile)
    {
        // Model paths
        ModelPath = profile.ModelPath ?? string.Empty;
        MmprojPath = profile.MmprojPath ?? string.Empty;
        DraftModelPath = profile.DraftModelPath ?? string.Empty;
        UpdateDisplayNames();

        // HuggingFace
        HfRepo = profile.HfRepo ?? string.Empty;
        HfFile = profile.HfFile ?? string.Empty;
        HfOffline = profile.HfOffline;
        HfRepoDraft = profile.HfRepoDraft ?? string.Empty;

        // Connection — use profile values, fall back to settings only if profile is empty
        Host = !string.IsNullOrWhiteSpace(profile.Host) ? profile.Host : (_settings.DefaultHost ?? "127.0.0.1");
        Port = profile.Port > 0 ? profile.Port : (_settings.DefaultPort > 0 ? _settings.DefaultPort : 8080);
        Timeout = profile.Timeout;
        Slots = profile.Slots;

        // GPU
        GpuLayers = profile.GpuLayers;
        Threads = profile.Threads;
        ThreadsBatch = profile.ThreadsBatch;
        FlashAttention = profile.FlashAttention;
        Mmap = profile.Mmap;
        Mlock = profile.Mlock;
        MmqEnabled = profile.MmqEnabled;
        KvOffloadEnabled = profile.KvOffloadEnabled;
        MainGpu = profile.MainGpu;
        GpuSplitMode = profile.GpuSplitMode;
        TensorSplit = string.Join(",", profile.TensorSplit);

        // Context & Batch
        ContextSize = profile.ContextSize;
        BatchSize = profile.BatchSize;
        UbatchSize = profile.UbatchSize;
        MaxTokens = profile.MaxTokens;

        // Cache type
        CacheTypeK = profile.CacheTypeK != default ? profile.CacheTypeK.ToString().ToLower() : "f16";
        CacheTypeV = profile.CacheTypeV != default ? profile.CacheTypeV.ToString().ToLower() : "f16";

        // Cache management
        CacheRam = profile.CacheRam;
        CacheReuse = profile.CacheReuse;

        // Sampling
        Temperature = profile.Temperature;
        TopK = profile.TopK;
        TopP = profile.TopP;
        MinP = profile.MinP;
        TypicalP = profile.TypicalP;
        RepeatPenalty = profile.RepeatPenalty;
        RepeatLastN = profile.RepeatLastN;
        PresencePenalty = profile.PresencePenalty;
        FrequencyPenalty = profile.FrequencyPenalty;
        Seed = profile.Seed;

        // Mirostat
        MirostatMode = profile.Mirostat;
        MirostatTau = profile.MirostatTau;
        MirostatEta = profile.MirostatEta;

        // DRY
        DryMultiplier = profile.DryMultiplier;
        DryBase = profile.DryBase;

        // Dynatemp
        DynatempStddev = profile.DynatempStddev;

        // XTC
        XtcProbability = profile.XtcProbability;
        XtcThreshold = profile.XtcThreshold;

        // MTP / Speculative
        SpecType = string.IsNullOrWhiteSpace(profile.SpecType) ? "none" : profile.SpecType;
        PredictCount = profile.PredictCount;
        SpeculativeDecoding = profile.SpeculativeDecoding;
        SpecDraftGpuLayers = profile.SpecDraftGpuLayers;
        SpecDraftNMax = profile.SpecDraftNMax;
        SpecDraftNMin = profile.SpecDraftNMin;
        SpecDraftPSplit = profile.SpecDraftPSplit;
        SpecDraftPMin = profile.SpecDraftPMin;

        // Advanced
        PriorityHigh = profile.PriorityHigh;
        EmbeddingMode = profile.EmbeddingMode;
        PoolingType = profile.PoolingType;
        Numa = profile.Numa;
        ProcessPriority = profile.ProcessPriority;
        CachePrompt = profile.CachePrompt;
        ContBatching = profile.ContBatching;
        ContextShift = profile.ContextShift;
        VerboseLogging = profile.VerboseLogging;
        EnableWebUI = profile.EnableWebUI;
        EnableSlots = profile.EnableSlots;
        EnableMetrics = profile.EnableMetrics;
        Reasoning = profile.Reasoning;
        ReasoningBudget = profile.ReasoningBudget;

        // Rope / YARN
        RopeScaling = profile.RopeScaling;
        RopeFreqBase = profile.RopeFreqBase;
        RopeFreqScale = profile.RopeFreqScale;
        YarnOriginalContext = profile.YarnOriginalContext;
        YarnExtFactor = profile.YarnExtFactor;
        YarnAttnFactor = profile.YarnAttnFactor;
        YarnBetaFast = profile.YarnBetaFast;
        YarnBetaSlow = profile.YarnBetaSlow;

        // Extra
        ApiKey = profile.ApiKey ?? string.Empty;
        Alias = profile.Alias ?? string.Empty;
        AdditionalArgs = string.Empty;
    }

    void UpdateDisplayNames()
    {
        ModelDisplayName = string.IsNullOrWhiteSpace(ModelPath)
            ? _loc.T("server.model_not_selected")
            : Path.GetFileName(ModelPath);
        MmprojDisplayName = string.IsNullOrWhiteSpace(MmprojPath)
            ? _loc.T("server.mmproj_not_selected")
            : Path.GetFileName(MmprojPath);
        DraftDisplayName = string.IsNullOrWhiteSpace(DraftModelPath)
            ? _loc.T("server.draft_not_selected")
            : Path.GetFileName(DraftModelPath);
    }

    /// <summary>
    /// Called from ModelsPage when user clicks "Use in Server"
    /// </summary>
    public void SetModelFromScanner(GgufModelInfo model)
    {
        if (model == null) return;
        ModelPath = model.Path;
        UpdateDisplayNames();

        if (SelectedProfile != null)
        {
            SelectedProfile.ModelPath = model.Path;
            _ = _profileManager.SaveProfileAsync(SelectedProfile);
        }

        _log.Information($"Model set from scanner: {model.FileName}", "Server");
    }

    /// <summary>
    /// Called from ModelsPage when user clicks "Use as mmproj"
    /// </summary>
    public void SetMmprojFromScanner(GgufModelInfo model)
    {
        if (model == null) return;
        MmprojPath = model.Path;
        UpdateDisplayNames();

        if (SelectedProfile != null)
        {
            SelectedProfile.MmprojPath = model.Path;
            _ = _profileManager.SaveProfileAsync(SelectedProfile);
        }

        _log.Information($"mmproj set from scanner: {model.FileName}", "Server");
    }

    /// <summary>
    /// Called from ModelsPage when user clicks "Use as draft model"
    /// </summary>
    public void SetDraftModelFromScanner(GgufModelInfo model)
    {
        if (model == null) return;
        DraftModelPath = model.Path;
        UpdateDisplayNames();

        if (SelectedProfile != null)
        {
            SelectedProfile.DraftModelPath = model.Path;
            _ = _profileManager.SaveProfileAsync(SelectedProfile);
        }

        _log.Information($"Draft model set from scanner: {model.FileName}", "Server");
    }

    void OnStatusChanged(object? sender, ServerStatus status)
    {
        State = status.State;
        VramUsedGb = status.VramUsedGb;
        RamUsedGb = status.RamUsedGb;
        TokensPerSecond = status.TokensPerSecond;
        PromptTokensPerSecond = status.PromptTokensPerSecond;
        QueueSize = status.QueueSize;
        ActiveSlots = status.ActiveSlots;
        Uptime = status.Uptime;
        ProcessId = status.ProcessId?.ToString() ?? "—";
        ErrorMessage = status.ErrorMessage ?? string.Empty;


    }

    async Task RefreshGpuAsync()
    {
        try
        {
            var info = await _gpuMonitor.GetGpuInfoAsync();
            if (info != null)
            {
                GpuVramUsedGb = info.MemoryUsedGb;
                OnPropertyChanged(nameof(GpuVramUsedGb));
            }
        }
        catch { }
    }

    #region Model Browse Commands

    [RelayCommand]
    async Task BrowseModel()
    {
        var path = await _dialog.SelectFileAsync(
            "Select GGUF Model",
            ModelPath,
            "GGUF Files|*.gguf|All Files|*.*");

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            ModelPath = path;
            UpdateDisplayNames();
            _log.Information($"Model selected: {path}", "Server");
        }
    }

    [RelayCommand]
    async Task BrowseMmproj()
    {
        var path = await _dialog.SelectFileAsync(
            "Select mmproj Model",
            MmprojPath,
            "GGUF Files|*.gguf|All Files|*.*");

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            MmprojPath = path;
            UpdateDisplayNames();
            _log.Information($"mmproj selected: {path}", "Server");
        }
    }

    [RelayCommand]
    async Task BrowseDraftModel()
    {
        var path = await _dialog.SelectFileAsync(
            "Select Draft Model",
            DraftModelPath,
            "GGUF Files|*.gguf|All Files|*.*");

        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            DraftModelPath = path;
            UpdateDisplayNames();
            _log.Information($"Draft model selected: {path}", "Server");
        }
    }

    #endregion

    #region Quick Model Scan

    [RelayCommand]
    async Task QuickScanModels()
    {
        var dir = string.IsNullOrWhiteSpace(_settings.ModelsDirectory)
            ? LlamaCppDirectory
            : _settings.ModelsDirectory;

        if (!Directory.Exists(dir))
        {
            var selected = await _dialog.SelectFolderAsync("Select Models Directory", dir);
            if (string.IsNullOrEmpty(selected) || !Directory.Exists(selected))
                return;
            dir = selected;
        }

        IsScanningModels = true;
        ScannedModels.Clear();

        try
        {
            var results = await _scanner.ScanDirectoryAsync(dir);
            foreach (var m in results)
                ScannedModels.Add(m);

            _log.Information($"Quick scan: {results.Count} models found", "Server");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Quick scan failed", "Server");
        }
        finally
        {
            IsScanningModels = false;
        }
    }

    [RelayCommand]
    void QuickSelectModel()
    {
        if (QuickSelectedModel != null)
        {
            ModelPath = QuickSelectedModel.Path;
            UpdateDisplayNames();
            _log.Information($"Quick-selected model: {QuickSelectedModel.FileName}", "Server");
        }
    }

    [RelayCommand]
    void QuickSelectMmproj()
    {
        if (QuickSelectedModel != null)
        {
            MmprojPath = QuickSelectedModel.Path;
            UpdateDisplayNames();
            _log.Information($"Quick-selected mmproj: {QuickSelectedModel.FileName}", "Server");
        }
    }

    [RelayCommand]
    void QuickSelectDraft()
    {
        if (QuickSelectedModel != null)
        {
            DraftModelPath = QuickSelectedModel.Path;
            UpdateDisplayNames();
            _log.Information($"Quick-selected draft: {QuickSelectedModel.FileName}", "Server");
        }
    }

    #endregion

    #region Server Control

    [RelayCommand]
    public async Task StartServerAsync()
    {
        if (SelectedProfile == null)
        {
            ErrorMessage = _loc.T("server.no_profile_selected");
            State = ServerState.Error;
            return;
        }

        if (string.IsNullOrWhiteSpace(ModelPath))
        {
            ErrorMessage = _loc.T("server.no_model_selected");
            _log.Error(ErrorMessage, "Server");
            State = ServerState.Error;
            return;
        }

        var exePath = ExecutablePath;
        if (!File.Exists(exePath))
        {
            ErrorMessage = string.Format(_loc.T("server.exe_not_found"), exePath);
            _log.Error(ErrorMessage, "Server");
            State = ServerState.Error;
            return;
        }

      // Sync UI values to profile before starting (includes Host/Port from UI)
        SyncSettingsToProfile();
        // Clear stale custom args to prevent old overrides
        SelectedProfile.CustomArguments = new Dictionary<string, string>();
        // Sync preview to current settings
        CommandLinePreview = SelectedProfile.BuildArgsString();

        try
        {
            _log.Information($"Starting server with profile: {SelectedProfile.Name}", "Server");
            await _serverManager.StartAsync(SelectedProfile);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _log.Error(ex, "Failed to start server", "Server");
            State = ServerState.Error;
        }
    }

    [RelayCommand]
    async Task StopServer()
    {
        try
        {
            _log.Information("Stopping server...", "Server");
            await _serverManager.StopAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _log.Error(ex, "Failed to stop server", "Server");
            State = ServerState.Error;
        }
    }

    [RelayCommand]
    async Task HealthCheck()
    {
        var status = await _serverManager.HealthCheckAsync(Host, Port);
        State = status.State;
        ErrorMessage = status.ErrorMessage ?? string.Empty;
    }

    #endregion

    #region Profile Management

    [RelayCommand]
    async Task CreateNewProfile()
    {
        var name = await _dialog.ShowInputAsync(_loc.T("dialog.create_profile_title"), _loc.T("dialog.profile_name_prompt"), "New Profile");
        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim();
        if (Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
            return;
        }

        var profile = _profileManager.CreateProfile(name);

        await _profileManager.SaveProfileAsync(profile);
        LoadProfiles();
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == profile.Id);
        _log.Information($"Created profile: {profile.Name}", "Server");
    }

    [RelayCommand]
    async Task SaveToProfile()
    {
        if (SelectedProfile == null)
        {
            _log.Error("No profile selected to save settings.", "Server");
            return;
        }

        SyncSettingsToProfile();
        await _profileManager.SaveProfileAsync(SelectedProfile);
        UpdateCommandLinePreview();
        _log.Information($"Settings saved to profile: {SelectedProfile.Name}", "Server");
    }

    [RelayCommand]
    async Task RenameProfile()
    {
        if (SelectedProfile == null)
        {
            await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
            return;
        }

        var name = await _dialog.ShowInputAsync(_loc.T("dialog.rename_title"), _loc.T("dialog.rename_prompt"), SelectedProfile.Name);
        if (string.IsNullOrWhiteSpace(name)) return;

        name = name.Trim();
        if (Profiles.Any(p => p.Id != SelectedProfile.Id && p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            await _dialog.ShowErrorAsync(string.Format(_loc.T("dialog.profile_exists"), name));
            return;
        }

        SelectedProfile.Name = name;
        await _profileManager.SaveProfileAsync(SelectedProfile);
        LoadProfiles();
        _log.Information($"Renamed profile to: {name}", "Server");
    }

    [RelayCommand]
    async Task DeleteProfile()
    {
        if (SelectedProfile == null)
        {
            await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
            return;
        }

        var confirmed = await _dialog.ShowConfirmationAsync(
            string.Format(_loc.T("dialog.confirm_delete_profile"), SelectedProfile.Name),
            _loc.T("dialog.delete_profile_title"));

        if (!confirmed) return;

        await _profileManager.DeleteProfileAsync(SelectedProfile.Id);
        LoadProfiles();
        _log.Information($"Deleted profile: {SelectedProfile.Name}", "Server");
    }

    [RelayCommand]
    async Task CopyFromProfile()
    {
        if (SelectedProfile == null)
        {
            await _dialog.ShowErrorAsync(_loc.T("dialog.select_profile_first"));
            return;
        }

        var others = Profiles.Where(p => p.Id != SelectedProfile.Id).ToList();
        if (others.Count == 0)
        {
            await _dialog.ShowErrorAsync(_loc.T("dialog.no_other_profiles"));
            return;
        }

        var selectedName = await _dialog.ShowProfileSelectAsync(
            _loc.T("dialog.copy_settings_title"),
            $"В текущий профиль \"{SelectedProfile.Name}\"",
            others.Select(p => p.Name));

        if (selectedName == null) return;

        var source = others.FirstOrDefault(p => p.Name == selectedName);
        if (source != null)
        {
            ApplyProfileToSettings(source);
            UpdateCommandLinePreview();
            _log.Information($"Copied settings from \"{source.Name}\" to \"{SelectedProfile.Name}\"", "Server");
            await _dialog.ShowSuccessAsync(string.Format(_loc.T("dialog.copy_success"), source.Name));
        }
    }

    #endregion

    #region Llama.cpp Info

    [RelayCommand]
    async Task CheckLlamaCppVersion()
    {
        if (!ExecutableFound)
        {
            LlamaVersionInfo = _loc.T("server.exe_not_found_short");
            CudaInfo = string.Empty;
            UnsupportedFlags.Clear();
            _helpInfo = null;
            return;
        }

        try
        {
            var releases = await _updater.FetchReleasesAsync(false);
            var installedTag = DetectInstalledVersion();

            LlamaVersionInfo = installedTag ?? _loc.T("server.unknown_version");

            if (releases != null && releases.Count > 0)
            {
                var latest = releases[0].TagName;
                if (installedTag != null && installedTag != latest)
                    LlamaVersionInfo += $" (latest: {latest})";
            }

            CudaInfo = DetectCudaSupport();

            // Parse help to detect supported flags
            await ParseHelpAsync();
        }
        catch (Exception ex)
        {
            LlamaVersionInfo = string.Format(_loc.T("server.version_error"), ex.Message);
            _log.Error(ex, "Failed to check llama.cpp version", "Server");
        }
    }

    async Task ParseHelpAsync()
    {
        if (!ExecutableFound) return;

        try
        {
            var helpInfo = await _helpParser.ParseAsync(ExecutablePath);
            if (helpInfo != null)
            {
                _helpInfo = helpInfo;
                UpdateUnsupportedFlags();
                _log.Information($"Help parsed: {helpInfo.SupportedFlags.Count} flags detected", "Server");
            }
        }
        catch (Exception ex)
        {
            _log.Warning($"Failed to parse --help output: {ex.Message}", "Server");
        }
    }

    void UpdateUnsupportedFlags()
    {
        if (_helpInfo == null)
        {
            UnsupportedFlags.Clear();
            return;
        }

        var allFlags = new List<string>();

        // Model paths
        if (!string.IsNullOrWhiteSpace(ModelPath)) allFlags.Add("--model");
        if (!string.IsNullOrWhiteSpace(MmprojPath)) allFlags.Add("--mmproj");
        if (!string.IsNullOrWhiteSpace(DraftModelPath) || SpeculativeDecoding) allFlags.Add("--draft-model");

        // Connection
        allFlags.Add("--host");
        allFlags.Add("--port");
        allFlags.Add("-to");
        if (Slots > 0) allFlags.Add("-np");

        // GPU
        if (!string.IsNullOrWhiteSpace(GpuLayers)) allFlags.Add("-ngl");
        if (Threads > 0) allFlags.Add("-t");
        if (ThreadsBatch > 0) allFlags.Add("--threads-batch");
        if (MainGpu > 0) allFlags.Add("--main-gpu");
        if (FlashAttention) allFlags.Add("--flash-attn");
        if (!Mmap) allFlags.Add("--no-mmap");
        if (Mlock) allFlags.Add("--mlock");
        if (!MmqEnabled) allFlags.Add("--no-mmq");
        if (!KvOffloadEnabled) allFlags.Add("--no-kv-offload");

        // Context & Batch
        if (ContextSize > 0) allFlags.Add("-c");
        if (BatchSize != 2048) allFlags.Add("-b");
        if (UbatchSize != 512) allFlags.Add("--ubatch");
        if (MaxTokens > 0) allFlags.Add("-n");

        // Cache type
        if (CacheTypeK != "f16") allFlags.Add($"--cache-type-k {CacheTypeK}");
        if (CacheTypeV != "f16") allFlags.Add($"--cache-type-v {CacheTypeV}");

        // Cache management
        if (CacheRam >= 0) allFlags.Add($"--cache-ram {CacheRam}");
        if (CacheReuse != 32) allFlags.Add($"--cache-reuse {CacheReuse}");

        // Sampling
        if (Math.Abs(Temperature - 0.8f) > 0.001f) allFlags.Add("--temp");
        if (TopK != 40) allFlags.Add("--top-k");
        if (Math.Abs(TopP - 0.95f) > 0.001f) allFlags.Add("--top-p");
        if (MinP != 0.05f) allFlags.Add("--min-p");
        if (TypicalP < 1f) allFlags.Add("--typical-p");
        if (RepeatPenalty != 1.0f) allFlags.Add("--repeat-penalty");
        if (RepeatLastN != -1) allFlags.Add("--repeat-last-n");
        if (PresencePenalty != 0f) allFlags.Add("--presence-penalty");
        if (FrequencyPenalty != 0f) allFlags.Add("--frequency-penalty");

        // Mirostat
        if (MirostatMode > 0) allFlags.Add("--mirostat");
        if (MirostatTau != 5f) allFlags.Add("--mirostat-tau");
        if (MirostatEta != 0.1f) allFlags.Add("--mirostat-learn-rate");

        // DRY
        if (DryMultiplier > 0f) allFlags.Add("--dry-multiplier");
        if (Math.Abs(DryBase - 1.75f) > 0.001f) allFlags.Add("--dry-base");

        // Dynatemp
        if (DynatempStddev > 0f) allFlags.Add("--dynatemp-range");

        // XTC
        if (XtcProbability > 0f) allFlags.Add("--xtc-probability");
        if (Math.Abs(XtcThreshold - 0.1f) > 0.001f) allFlags.Add("--xtc-threshold");

        // Speculative / MTP
        if (!string.IsNullOrWhiteSpace(SpecType)) allFlags.Add("--spec-type");
        if (PredictCount > 0) allFlags.Add("--predict");
        if (SpeculativeDecoding) allFlags.Add("--speculative");
        if (!string.IsNullOrWhiteSpace(SpecDraftGpuLayers)) allFlags.Add("-ngld");
        if (SpecDraftNMax != 3) allFlags.Add("--spec-draft-n-max");
        if (SpecDraftNMin > 0) allFlags.Add("--spec-draft-n-min");

        // Rope / YARN
        if (!string.IsNullOrWhiteSpace(RopeScaling)) allFlags.Add("--rope-scaling");
        if (RopeFreqBase.HasValue) allFlags.Add("--rope-frequency-base");
        if (RopeFreqScale.HasValue) allFlags.Add("--rope-frequency-scale");
        if (YarnOriginalContext.HasValue) allFlags.Add("--yarn-orig-ctx");
        if (YarnExtFactor.HasValue) allFlags.Add("--yarn-ext-factor");
        if (YarnAttnFactor.HasValue) allFlags.Add("--yarn-attn-factor");
        if (YarnBetaFast.HasValue) allFlags.Add("--yarn-beta-fast");
        if (YarnBetaSlow.HasValue) allFlags.Add("--yarn-beta-slow");

        // Advanced
        if (Numa) allFlags.Add("--numa");
        if (!CachePrompt) allFlags.Add("--cache-prompt");
        if (!ContBatching) allFlags.Add("--cont-batching");
        if (VerboseLogging) allFlags.Add("--verbose");
        if (EnableWebUI) allFlags.Add("--ui");
        if (EnableMetrics) allFlags.Add("--metrics");
        if (Reasoning) allFlags.Add("-rea");
        if (EmbeddingMode) allFlags.Add("--embedding");
        // --priority-high is handled by ServerManager, not passed to llama-server

        UnsupportedFlags = _helpInfo.GetUnsupported(allFlags);

        if (UnsupportedFlags.Count > 0)
        {
            _log.Warning($"Unsupported flags: {string.Join(", ", UnsupportedFlags)}", "Server");
        }
    }

    /// <summary>
    /// Handle CLI changes detected after installing a new llama.cpp version.
    /// </summary>
    void OnCliChangesDetected(object? sender, CliChangeReport report)
    {
        Dispatcher.UIThread.Post(() =>
        {
            CliChangeReport = report;

            var parts = new List<string>
            {
                $"Version: {report.OldVersion} → {report.NewVersion}"
            };

            if (report.HasCriticalChanges)
                parts.Add($"⚠ Critical: {report.CriticalChanges.Count} used flags removed!");

            if (report.RemovedFlags.Count > 0)
                parts.Add($"Removed: {report.RemovedFlags.Count}");

            if (report.ModifiedFlags.Count > 0)
                parts.Add($"Changed defaults: {report.ModifiedFlags.Count}");

            if (report.AddedFlags.Count > 0)
                parts.Add($"+ New available: {report.AddedFlags.Count}");

            ValidationSummary = " | " + string.Join(" | ", parts);

            _log.Information($"CLI changes detected: {ValidationSummary}", "Server");
        });
    }

    /// <summary>
    /// Manually validate CLI flags against the current binary.
    /// </summary>
    [RelayCommand]
    async Task ValidateCliAsync()
    {
        if (!ExecutableFound)
        {
            _log.Warning("Cannot validate: llama-server.exe not found", "Server");
            return;
        }

        IsCliValidating = true;
        ValidationSummary = "Validating...";

        try
        {
            var version = _settings.ActiveLlamaCppVersion ?? "unknown";
            var report = await _cliValidator.ValidateAllFlagsAsync(ExecutablePath, version);
            ValidationReport = report;

            if (report.Status == ValidationStatus.Ok)
            {
                ValidationSummary = "✓ Все флаги корректны";
                ValidationDetails = report.NewAvailableFlags.Count > 0
                    ? $"+ {report.NewAvailableFlags.Count} новых флагов доступно в бинарнике"
                    : string.Empty;
                ValidationSummaryForeground = new(Avalonia.Media.Color.Parse("#34D399"));
            }
            else
            {
                ValidationSummary = $"✗ {report.RemovedFlags.Count} флага(ов) не поддерживаются текущим бинарником";

                var details = new List<string>();
                foreach (var rf in report.RemovedFlags)
                {
                    if (!string.IsNullOrEmpty(rf.SuggestedReplacement))
                        details.Add($"  ✗ {rf.OurFlag} → замените на {rf.SuggestedReplacement}");
                    else
                        details.Add($"  ✗ {rf.OurFlag} — удалён из llama.cpp");
                }

                if (report.NewAvailableFlags.Count > 0)
                    details.Add($"+ {report.NewAvailableFlags.Count} новых флагов доступно");

                ValidationDetails = string.Join("\n", details);
                ValidationSummaryForeground = new(Avalonia.Media.Color.Parse("#F87171"));
            }

            _log.Information($"CLI validation: {ValidationSummary}", "Server");
        }
        catch (Exception ex)
        {
            ValidationSummary = $"Error: {ex.Message}";
            ValidationSummaryForeground = new(Avalonia.Media.Color.Parse("#F87171"));
            _log.Error(ex, "CLI validation failed", "Server");
        }
        finally
        {
            IsCliValidating = false;
        }
    }

    [RelayCommand]
    async Task SelectLlamaCppDirectory()
    {
        var path = await _dialog.SelectFolderAsync(
            "Select llama.cpp Directory",
            LlamaCppDirectory);

        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
        {
            LlamaCppDirectory = path;
            _settings.LlamaCppDirectory = path;
            await _settings.SaveAsync();
            UpdateExecutablePath();
            _log.Information($"llama.cpp directory set: {path}", "Server");
        }
    }

    string? DetectInstalledVersion()
    {
        if (!ExecutableFound) return null;

        var dir = Path.GetDirectoryName(ExecutablePath)!;
        var ggmlVersionFile = Path.Combine(dir, "ggml-version.txt");
        if (File.Exists(ggmlVersionFile))
            return File.ReadAllText(ggmlVersionFile).Trim();

        var exeVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(ExecutablePath);
        if (!string.IsNullOrEmpty(exeVersion.ProductVersion))
            return exeVersion.ProductVersion;

        return _loc.T("server.installed_unknown");
    }

    string DetectCudaSupport()
    {
        if (!ExecutableFound) return "N/A";

        var dir = Path.GetDirectoryName(ExecutablePath)!;
        var cudaDlls = Directory.GetFiles(dir, "cudart*.dll");
        var cublasDlls = Directory.GetFiles(dir, "cublas*.dll");

        if (cudaDlls.Length > 0 || cublasDlls.Length > 0)
            return $"CUDA DLLs found: {cudaDlls.Length + cublasDlls.Length} files";

        var exeName = Path.GetFileName(ExecutablePath).ToLower();
        if (exeName.Contains("cuda") || exeName.Contains("cuBLAS"))
            return "CUDA build (by executable name)";

        return "CPU build (no CUDA DLLs detected)";
    }

    #endregion

    #region Export

    [RelayCommand]
    async Task ExportToBat()
    {
        if (SelectedProfile == null)
        {
            _log.Error("No profile selected to export.", "Server");
            return;
        }

        SyncSettingsToProfile();
        var args = BuildCommandLinePreview(SelectedProfile);
        var batContent = $"@echo off\r\ncd /d \"{Path.GetDirectoryName(ExecutablePath)}\"\r\n{Path.GetFileName(ExecutablePath)} {args}\r\npause";

        var path = await _dialog.SaveFileAsync(
            "Export as Batch File",
            $"{SelectedProfile.Name}.bat",
            "Batch Files|*.bat|All Files|*.*");

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, batContent);
            _log.Information($"Exported to: {path}", "Server");
        }
    }

    [RelayCommand]
    void CopyCommandLine()
    {
        if (SelectedProfile == null) return;

        SyncSettingsToProfile();
        var args = BuildCommandLinePreview(SelectedProfile);
        var fullCommand = $"\"{ExecutablePath}\" {args}";

        _log.Information($"Command line: {fullCommand}", "Server");
    }

    void UpdateCommandLinePreview()
    {
        if (SelectedProfile == null)
        {
            CommandLinePreview = string.Empty;
            return;
        }
        SyncSettingsToProfile();
        CommandLinePreview = BuildCommandLinePreview(SelectedProfile);
    }

    string BuildCommandLinePreview(ServerProfile profile)
    {
        return profile.BuildArgsString();
    }

    [RelayCommand]
    async Task OpenArgPicker()
    {
        if (_helpInfo == null)
        {
            await _dialog.ShowInfoAsync("Сначала нажмите «Проверить версию», чтобы загрузить список флагов.");
            return;
        }

        var vm = new ArgPickerViewModel();
        vm.LoadFlags(_helpInfo);

        var win = new Views.ArgPickerWindow();
        win.DataContext = vm;
        await win.ShowDialog(GetMainWindow());

        if (win.IsConfirmed)
        {
            var selectedArgs = vm.GetSelectedArgs();
            if (selectedArgs.Count > 0)
            {
                var existing = AdditionalArgs.Trim();
                var lines = new List<string>();
                if (!string.IsNullOrEmpty(existing))
                    lines.AddRange(existing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                foreach (var arg in selectedArgs)
                    lines.Add(arg);
                AdditionalArgs = string.Join("\n", lines);
                _log.Information($"Added {selectedArgs.Count} CLI args from picker", "Server");
            }
        }
    }

    static Views.MainWindow? GetMainWindow()
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime;
        return lifetime?.MainWindow as Views.MainWindow;
    }

    #endregion

    #region Settings Sync

    void ApplyCommandLineEdits(ServerProfile profile)
    {
        var generated = BuildCommandLinePreview(profile);
        var edited = CommandLinePreview?.Trim() ?? string.Empty;

        if (edited == generated)
            return;

        var generatedArgs = ParseCommandLineArgs(generated);
        var editedArgs = ParseCommandLineArgs(edited);

        var customArgs = new Dictionary<string, string>(profile.CustomArguments);

        foreach (var (flag, value) in editedArgs)
        {
            var genValue = generatedArgs.GetValueOrDefault(flag);
            if (genValue == null)
                customArgs[flag] = value;
            else if (genValue != value)
                customArgs[flag] = value;
            else if (customArgs.ContainsKey(flag))
                customArgs.Remove(flag);
        }

        profile.CustomArguments = customArgs;
    }

    static Dictionary<string, string?> ParseCommandLineArgs(string cmdline)
    {
        var result = new Dictionary<string, string?>();
        var i = 0;
        while (i < cmdline.Length)
        {
            // Skip whitespace
            while (i < cmdline.Length && char.IsWhiteSpace(cmdline[i])) i++;
            if (i >= cmdline.Length) break;

            // Read token
            string token;
            if (cmdline[i] == '"')
            {
                // Quoted string
                i++; // skip opening quote
                var start = i;
                while (i < cmdline.Length && cmdline[i] != '"') i++;
                token = cmdline.Substring(start, i - start);
                i++; // skip closing quote
            }
            else
            {
                // Unquoted string
                var start = i;
                while (i < cmdline.Length && !char.IsWhiteSpace(cmdline[i])) i++;
                token = cmdline.Substring(start, i - start);
            }

            if (token.StartsWith("-"))
            {
                // Read next token as value
                while (i < cmdline.Length && char.IsWhiteSpace(cmdline[i])) i++;
                if (i < cmdline.Length)
                {
                    string value;
                    if (cmdline[i] == '"')
                    {
                        i++;
                        var start = i;
                        while (i < cmdline.Length && cmdline[i] != '"') i++;
                        value = cmdline.Substring(start, i - start);
                        i++;
                    }
                    else
                    {
                        var start = i;
                        while (i < cmdline.Length && !char.IsWhiteSpace(cmdline[i])) i++;
                        value = cmdline.Substring(start, i - start);
                    }
                    result[token] = value;
                }
                else
                {
                    result[token] = string.Empty;
                }
            }
        }
        return result;
    }

    public void SyncSettingsToProfile()
    {
        if (SelectedProfile == null) return;
        SyncSettingsToProfileTarget(SelectedProfile);
    }

    void AutoSaveProfile()
    {
        if (_isInitializing || SelectedProfile == null) return;
        // Debounce: reset timer on each change, save 300ms after last change
        _autoSaveDebounceTimer?.Change(300, System.Threading.Timeout.Infinite);
    }

    void PerformAutoSaveProfile()
    {
        if (SelectedProfile == null) return;
        SyncSettingsToProfile();
        _ = _profileManager.SaveProfileAsync(SelectedProfile);
    }

    void SyncSettingsToProfileTarget(ServerProfile profile)
    {
        profile.ModelPath = string.IsNullOrWhiteSpace(ModelPath) ? null : ModelPath;
        profile.MmprojPath = string.IsNullOrWhiteSpace(MmprojPath) ? null : MmprojPath;
        profile.DraftModelPath = string.IsNullOrWhiteSpace(DraftModelPath) ? null : DraftModelPath;

        // Clear stale custom arguments — they cause overrides
        profile.CustomArguments = new Dictionary<string, string>();

        // HuggingFace
        profile.HfRepo = string.IsNullOrWhiteSpace(HfRepo) ? null : HfRepo;
        profile.HfFile = string.IsNullOrWhiteSpace(HfFile) ? null : HfFile;
        profile.HfOffline = HfOffline;
        profile.HfRepoDraft = string.IsNullOrWhiteSpace(HfRepoDraft) ? null : HfRepoDraft;

        // Connection
        profile.Host = Host;
        profile.Port = Port;
        profile.Timeout = Timeout;
        profile.Slots = Slots;

        // GPU
        profile.GpuLayers = GpuLayers;
        profile.Threads = Threads;
        profile.ThreadsBatch = ThreadsBatch;
        profile.FlashAttention = FlashAttention;
        profile.Mmap = Mmap;
        profile.Mlock = Mlock;
        profile.MmqEnabled = MmqEnabled;
        profile.KvOffloadEnabled = KvOffloadEnabled;
        profile.MainGpu = MainGpu;
        profile.GpuSplitMode = GpuSplitMode;
        profile.TensorSplit = string.IsNullOrWhiteSpace(TensorSplit)
            ? Array.Empty<int>()
            : TensorSplit.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse).ToArray();

        // Context & Batch
        profile.ContextSize = ContextSize;
        profile.BatchSize = BatchSize;
        profile.UbatchSize = UbatchSize;
        profile.MaxTokens = MaxTokens;

        // Cache type
        profile.CacheTypeK = ParseCacheTypeK(CacheTypeK);
        profile.CacheTypeV = ParseCacheTypeV(CacheTypeV);

        // Cache management
        profile.CacheRam = CacheRam;
        profile.CacheReuse = CacheReuse;

        // Sampling
        profile.Temperature = Temperature;
        profile.TopK = TopK;
        profile.TopP = TopP;
        profile.MinP = MinP;
        profile.TypicalP = TypicalP;
        profile.RepeatPenalty = RepeatPenalty;
        profile.RepeatLastN = RepeatLastN;
        profile.PresencePenalty = PresencePenalty;
        profile.FrequencyPenalty = FrequencyPenalty;
        profile.Seed = Seed;

        // Mirostat
        profile.Mirostat = MirostatMode;
        profile.MirostatTau = MirostatTau;
        profile.MirostatEta = MirostatEta;

        // DRY
        profile.DryMultiplier = DryMultiplier;
        profile.DryBase = DryBase;

        // Dynatemp
        profile.DynatempStddev = DynatempStddev;

        // XTC
        profile.XtcProbability = XtcProbability;
        profile.XtcThreshold = XtcThreshold;

        // MTP / Speculative
        profile.SpecType = (SpecType == "none" || string.IsNullOrWhiteSpace(SpecType)) ? string.Empty : SpecType;
        profile.PredictCount = PredictCount;
        profile.SpeculativeDecoding = SpeculativeDecoding;
        profile.SpecDraftGpuLayers = SpecDraftGpuLayers;
        profile.SpecDraftNMax = SpecDraftNMax;
        profile.SpecDraftNMin = SpecDraftNMin;
        profile.SpecDraftPSplit = SpecDraftPSplit;
        profile.SpecDraftPMin = SpecDraftPMin;

        // Advanced
        profile.PriorityHigh = PriorityHigh;
        profile.EmbeddingMode = EmbeddingMode;
        profile.PoolingType = PoolingType;
        profile.Numa = Numa;
        profile.ProcessPriority = ProcessPriority;
        profile.ContBatching = ContBatching;
        profile.ContextShift = ContextShift;
        profile.VerboseLogging = VerboseLogging;
        profile.EnableWebUI = EnableWebUI;
        profile.EnableSlots = EnableSlots;
        profile.EnableMetrics = EnableMetrics;
        profile.Reasoning = Reasoning;
        profile.ReasoningBudget = ReasoningBudget;

        // Rope / YARN
        profile.RopeScaling = RopeScaling;
        profile.RopeFreqBase = RopeFreqBase;
        profile.RopeFreqScale = RopeFreqScale;
        profile.YarnOriginalContext = YarnOriginalContext;
        profile.YarnExtFactor = YarnExtFactor;
        profile.YarnAttnFactor = YarnAttnFactor;
        profile.YarnBetaFast = YarnBetaFast;
        profile.YarnBetaSlow = YarnBetaSlow;

        // Extra
        profile.ApiKey = string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey;
        profile.Alias = string.IsNullOrWhiteSpace(Alias) ? null : Alias;
        // CustomArguments from AdditionalArgs
        if (!string.IsNullOrWhiteSpace(AdditionalArgs))
        {
            profile.CustomArguments = AdditionalArgs
                .Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? parts[1] : string.Empty);
        }
        else
        {
            profile.CustomArguments = new Dictionary<string, string>();
        }
        profile.UpdatedAt = DateTime.Now;
    }

    static Core.Enums.CacheTypeK ParseCacheTypeK(string value)
    {
        return value.ToLower() switch
        {
            "f16" => Core.Enums.CacheTypeK.F16,
            "f32" => Core.Enums.CacheTypeK.F32,
            "q8_0" => Core.Enums.CacheTypeK.Q8_0,
            "q4_0" => Core.Enums.CacheTypeK.Q4_0,
            _ => Core.Enums.CacheTypeK.F16
        };
    }

    static Core.Enums.CacheTypeV ParseCacheTypeV(string value)
    {
        return value.ToLower() switch
        {
            "f16" => Core.Enums.CacheTypeV.F16,
            "f32" => Core.Enums.CacheTypeV.F32,
            "q8_0" => Core.Enums.CacheTypeV.Q8_0,
            "q4_0" => Core.Enums.CacheTypeV.Q4_0,
            _ => Core.Enums.CacheTypeV.F16
        };
    }

    #endregion
}

public sealed class RecommendedModel
{
    public string Repo { get; }
    public string Name { get; }
    public string Quantization { get; }
    public string Size { get; }
    public string Params { get; }
    public string Description { get; }

    public RecommendedModel(string repo, string name, string quantization, string size, string description)
    {
        Repo = repo;
        Name = name;
        Quantization = quantization;
        Size = size;
        Params = description;
        Description = $"{name} | {quantization}";
    }
}
