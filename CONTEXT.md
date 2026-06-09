# Llama Studio — CONTEXT.md (08.06.2026)

## ⚠️ ВАЖНО
- **Все общение с пользователем — только на русском языке.**
- Работаем по шагам, после каждого — останавливаться для проверки.
- После значимых изменений — обновлять CONTEXT.md.
- **MSB3027/MSB3021** → `taskkill /F /IM LlamaStudio.exe` без вопросов.

---

## 📋 ТЕКУЩЕЕ СОСТОЯНИЕ (08.06.2026)

### ✅ Приложение запускается успешно
Последние исправления: убрана рекурсия `SaveProfileSync → GetAllProfiles` в ProfileManager.cs + ViewLocator с поддержкой `{Name}Page`.

### Сборка
- EXE: `LlamaStudio.exe` (без пробела), AssemblyTitle = `"Llama Studio"` (с пробелом)
- Все пути данных используют `"LlamaStudio"` без пробела
- Чистая сборка, 0 ошибок

### Как запустить
```powershell
& "L:\1c_modul\hermass\src\LlamaStudio\bin\Debug\net8.0\LlamaStudio.exe"
# или: dotnet build src/LlamaStudio/LlamaStudio.csproj && dotnet run --project src/LlamaStudio/LlamaStudio.csproj
```

### Диагностические логи (при проблемах с запуском)
- `bin/Debug/net8.0/logs/debug_app.txt` — DI регистрация + MainWindow
- `bin/Debug/net8.0/logs/debug_server_vm.txt` — конструктор ServerViewModel
- `bin/Debug/net8.0/crash.log` — фатальные исключения Avalonia

 ### Папки данных (portable mode)
- Профили: `bin/Debug/net8.0/profiles/*.json`
- Настройки: `%AppData%\Roaming\LlamaStudio\settings.json`
- Бинарники llama.cpp: `%AppData%\Roaming\LlamaStudio\bXXXX\`

---

## 🏗 АРХИТЕКТУРА (Clean Architecture, .NET 8, Avalonia)

```
src/
├── LlamaStudio/                    # UI: Views + ViewModels + App Shell
│   ├── Program.cs                  # Host builder → DI регистрация всех сервисов и ViewModels
│   ├── App.axaml(.cs)             # Styles + MainWindow (x:FieldModifier="public")
│   ├── Services/ViewLocator.cs     # ViewModel→Page mapping (исправлен 08.06!)
│   ├── Services/TrayManager.cs     # Tray icon, minimize-to-tray
│   ├── Views/MainWindow.axaml(.cs) # Grid: Sidebar + NavigationView
│   │   └── Pages/                  # DashboardPage, ModelsPage, ServerPage, ProfilesPage,
│   │                               # LogsPage, ApiTestPage, SettingsPage, ChatPage,
│   │                               # MonitoringPage, LlamaReleasesPage
│   ├── ViewModels/                 # MainViewModel (навигация), *ViewModel для каждой страницы
│   └── Converters/Converters.cs    # Все конвертеры: PercentToDash, ReasoningChevron и др.
├── LlamaStudio.Core/               # Domain: Models, Interfaces, Enums, Services
│   ├── Models/                     # GgufModelInfo, ServerProfile, ServerStatus, LogEntry,
│   │                               # ChatMessage, ChatSession, McpTool, McpServerConfig, ContentPart
│   ├── Interfaces/                 # IServerManager, IModelScanner, IProfileManager, IUpdateManager,
│   │                               # ISettings, ILogService, ILocalizationService, IPageViewModel,
│   │                               # IChatService, IMcpToolsService, IFilePickerService, IMcpClient
│   ├── Enums/                      # ServerState (Stopped/Starting/Running/Stopping/Error), LogLevel
│   └── Services/LocalizationService.cs  # 320+ ключей EN/RU, T("key"), OnLanguageChanged
└── LlamaStudio.Infrastructure/     # Реализации интерфейсов
    ├── Llama/ServerManager.cs      # Process-based запуск llama-server.exe, health polling (1.5s)
    ├── Llama/ModelScanner.cs       # GGUF header parsing (layers, RAM, VRAM)
    ├── Profiles/ProfileManager.cs  # JSON profiles, CRUD, Import/Export ⚠️ SaveProfileSync без рекурсии!
    ├── Updater/LlamaUpdater.cs     # GitHub API, download+extract, ConfigureAwait(false)
    ├── Chat/ChatService.cs         # HTTP SSE, multi-round tool loop (10 iter), multi-modal
    ├── Chat/ChatSessionStore.cs    # JSON persistence chats/{id}.json
    ├── Mcp/McpToolsService.cs      # 6 встроенных: read/write/list/search + web_fetch/fetch (Jina+Bing)
    ├── Mcp/McpStdioClient.cs       # JSON-RPC over stdin/stdout
    ├── Mcp/McpSseClient.cs         # JSON-RPC over HTTP SSE
    └── Logging/FileLogService.cs   # File logging: app.log, chat.log, server_output.log
```

Зависимости: `UI → Core ← Infrastructure`

### NuGet
Avalonia 11.2.1+, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection+Hosting, Markdown.Avalonia 11.0.3, SharpCompress, System.Drawing.Common

---

## 🔑 КЛЮЧЕВЫЕ ПАТТЕРНЫ

### Navigation
`MainViewModel.CurrentPage` маппит `Type → IPageViewModel`. Sidebar привязан к `IsSelected` каждого ViewModel.

### DI (Program.cs)
- Singleton: все сервисы (ISettings, IServerManager и т.д.) + все ViewModels
- Transient: IPageViewModel

### ServerProfile (`Core/Models/ServerProfile.cs`)
- **Equals/GetHashCode на основе `Id`** — обязательно для ComboBox!
- `string GpuLayers` (не int!) — поддерживает `"all"`, `"auto"`, числа
- `BuildFullArgsString()` — единственный источник аргументов для llama-server.exe

### ProfileManager (`Infrastructure/Profiles/ProfileManager.cs`) ⚠️ КРИТИЧНО
- **SaveProfileSync НЕ вызывает GetAllProfiles()** — рекурсия вызывает deadlock! Использует прямой обход файлов.
- `ReadJsonFile(path, out migrated)` с fallback CP1251→UTF-8
- `GetAllProfiles()` синхронная версия для UI-потока (избегает deadlock)

### ViewLocator (`Services/ViewLocator.cs`) ⚠️ КРИТИЧНО
Поддержка двух naming conventions: сначала `{Name}Page`, затем fallback на `{Name}View`. Без этого контент пустой.

### Health Polling (ServerManager)
- `while + Task.Delay(1500)`, тик каждые 1.5 сек
- Условие: `_cts?.Token.IsCancellationRequested != true` (НЕ `!_cts...`)
- RAM через `PrivateMemorySize64`, fallback поиск по имени процесса

### ServerProfile.GpuLayers = string
Поддерживает `"all"`, `"auto"`, числа. Дефолт `"all"`. UI: TextBox с watermark "Число, 'all' или 'auto'".

---

## ⚠️ КРИТИЧЕСКИЕ ЗАМЕЧАНИЯ (НЕ НАРУШАТЬ)

1. **Никогда не заменять computed properties на `{ get; set; }`** — `VramPercent => GpuInfo?.MemoryPercent ?? 0` и т.д.
2. **Никогда не использовать `Opacity=0` для скрытия** — Avalonia оставляет элемент в layout. Только `IsVisible`.
3. **MonitoringWindow.axaml склонен к повреждениям** при многократных правках через edit — проще переписать целиком.
4. **Git без коммитов** — нельзя `git checkout`, восстановление только вручную.
5. **TrayManager**: одна статическая иконка, динамические удалены из-за краша.
6. **PercentToDashConverter**: принимает радиус через ConverterParameter (36 для страницы, 23 для окна).
7. **Круги как спидометр**: обязательный `RotateTransform(-90°)` на каждом цветном Ellipse.

---

## ✅ ЗАВЕРШЁННЫЕ ФИЧИ (список)

| Фича | Статус | Ключевые файлы |
|------|--------|----------------|
| Переименование Hermass → Llama Studio | ✅ | .csproj, AppSettings, TrayManager, MainWindow |
| ViewLocator Page naming | ✅ | Services/ViewLocator.cs |
| Fix: рекурсия SaveProfileSync | ✅ | ProfileManager.cs |
| -ngl all (string GpuLayers) | ✅ | ServerProfile, ISettings, AppSettings, ServerVM |
| Плавающий мониторинг v5 | ✅ | MonitoringWindow/Page, TpsGaugeControl, PercentToDashConverter |
| Мониторинг ОЗУ сервера | ✅ | ServerManager GetProcessRamGb, GlobalMemoryStatusEx P/Invoke |
| Страница релизов llama.cpp | ✅ | LlamaReleasesViewModel/Page, BuildTypeToBrushConverter |
| Корневая папка llama.cpp | ✅ | ISettings LlamaCppBaseDirectory |
| Автоперезапуск сервера при смене версии | ✅ | LlamaReleasesViewModel SwitchVersion |
| Изоляция профилей + ComboBox | ✅ | ServerProfile Equals/GetHashCode, ReadJsonFile CP1251→UTF-8 |
| Чат: стриминг + скролл | ✅ | ChatMessage INPC, StackPanel вместо Panel |
| Чат: reasoning спойлер | ✅ | IsReasoningCollapsed, ReasoningChevronConverter |
| Чат: код блоки | ✅ | ContentPart, ContentParser, CopyCode/ExpandCode |
| MCP web_search (Bing) + web_fetch (Jina) | ✅ | McpToolsService |
| Иконка приложения и трей | ✅ | app-icon.png/.ico, TrayManager CreateTrayIcon |

---

## 📄 СТРУКТУРА ГЛАВНЫХ ФАЙЛОВ

### MainWindow.axaml
Grid 3 колонки (72\* Auto Auto): Sidebar + NavigationView. Sidebar: логотип + меню (Dashboard, Models, Мониторинг, Релизы llama.cpp, Server, Profiles, Logs, ApiTest, Settings) + статус сервера/модели снизу.

### ServerPage.axaml
TabControl 4 таба: Model, GPU, Context, Advanced. Модель: путь сервера, 3 модели (main/mmproj/draft), HuggingFace recommended. GPU: layers, threads, batch, flags. Context: context, sampling params. Advanced: MTP, speculative, YARN/Rope, custom args.

### DashboardPage.axaml
6 карточек: Server, Models, Profiles, llama.cpp, VRAM Usage, GPU Memory + Quick Actions (3 кнопки).

### MonitoringPage + MonitoringWindow
2 стиля (Bars/Circles), TPS gauge, 6 метрик (VRAM, RAM, Temp, Power, Core, Fan). Настройки: прозрачность, Always on Top, галочки видимости.

---

## 🔄 ПОСЛЕДНИЕ ИЗМЕНЕНИЯ (08.06.2026)

### Fix: Пути сервера не совпадали между компонентами
**Причина**: `DashboardViewModel.UpdateLlamaCppAsync` скачивал сервер в `LlamaStudioLlamaServer`, а `ServerViewModel`, `LlamaReleasesViewModel` и `AppSettings` искали в `LlamaStudio`.  
**Решение**: 
- `DashboardViewModel.GetBaseLlamaDir()` → `LlamaStudio`
- `LlamaReleasesViewModel.GetBaseLlamaDir()` → `LlamaStudio`
- `AppSettings.GetDefaultServerPath()` → `LlamaStudio`
- `AppSettings.LoadAsync()` → автокоррекция `LlamaCppDirectory` на основе `ActiveLlamaCppVersion`
- `ServerViewModel.UpdateExecutablePath()` → ищет в versioned subdirectory
- `ServerPage.axaml` → показывает `ServerDirectory` (реальный путь с подпапкой версии)
**Файлы**: `DashboardViewModel.cs`, `LlamaReleasesViewModel.cs`, `AppSettings.cs`, `ServerViewModel.cs`, `ServerPage.axaml`

### Fix: Приложение зависало при запуске
**Причина**: `SaveProfileSync` вызывал `GetAllProfiles()` для проверки default профиля → рекурсия при создании дефолтных профилей → deadlock на UI-потоке.
**Решение**: заменён на прямой обход файлов через `Directory.GetFiles()`. Переменная `json` переименована в `fileJson` (CS0136).
**Файл**: `Infrastructure/Profiles/ProfileManager.cs:SaveProfileSync`

### Fix: ViewLocator не находил страницы
**Причина**: искал `{Name}View`, а страницы называются `{Name}Page`.  
**Решение**: сначала ищет `{Name}Page`, затем fallback на `{Name}View`.  
**Файл**: `Services/ViewLocator.cs`

### Переименование Hermass → Llama Studio
Полное переименование. EXE: `LlamaStudio.exe`, AssemblyTitle: `"Llama Studio"`. Все пути данных без пробела.

---

## План (оставшиеся задачи)

| Шаг | Статус | Описание |
|-----|--------|----------|
| P3.* | ⏳ Ожидание | Nice-to-have (token counter, speed, themes и т.д.) |
| P4.* | ✅/⏳ См. ниже | Очистка + релиз-подготовка — см. раздел ниже |

---

## 🚀 РЕЛИЗ-ПОДГОТОВКА (08.06.2026)

### Стратегия дистрибуции: Single-file Self-contained EXE
**Как у референса** (`pytraveler/LlamaServerLauncherAvalonia`): один `.exe`, самодостаточный, не требует .NET Runtime. Пользователь копирует файл куда хочет — при первом запуске создаётся `%AppData%\Roaming\LlamaStudio\` для настроек и данных.

- **Обфускация (Obfuscar) и Inno Setup отменены** — не нужны для текущего подхода.
- Команда publish:
  ```powershell
  dotnet publish "src/LlamaStudio/LlamaStudio.csproj" -c Release -r win-x64 --self-contained true -o "publish/win-x64"
  ```
- **Результат:** `publish\win-x64\LlamaStudio.exe` — **103.7 MB**, один файл.

### Telegram канал
- Создан канал: `https://t.me/LlamaStudioApp`

### Страница «Discussion & Support»
- Новая страница в сайдбаре с ссылкой на Telegram канал.
- **Файлы:**
  - `src/LlamaStudio/Views/Pages/SupportPage.axaml(.cs)` — UI страницы
  - `src/LlamaStudio/ViewModels/SupportViewModel.cs` — ViewModel
  - `src/LlamaStudio/App.axaml.cs` — регистрация SupportViewModel в DI
  - `src/LlamaStudio/ViewModels/MainViewModel.cs` — навигация на новую страницу
  - `src/LlamaStudio/Views/MainWindow.axaml` — кнопка в сайдбаре
  - `src/LlamaStudio.Core/Services/LocalizationService.cs` — ключи локализации

### Очистка debug-логирования (для Release)
| Файл | Что сделано |
|------|-------------|
| `AppSettings.cs` | Удалены все `Debug.WriteLine` |
| `DashboardViewModel.cs` | Удалены все `Debug.WriteLine` |
| `ChatPage.axaml.cs` | Удалена функция `LogChat` (ad-hoc file logging) + debug traces |
| `ClipboardImageHelper.cs` | Удалена функция `Log` (ad-hoc file logging) |
| `ChatService.cs` | Токен-логирование понижено с `_log.Information` до `_log.Debug` |
| `ServerManager.cs` | Убран хардкодированный путь в комментарии |

### Восстановление иконок из бэкапа
- **Бэкап:** `L:\1c_modul\hermass_backup_20260608_172756`
- `app-icon.ico`, `app-icon.png`, `TrayManager.cs` — восстановлены из бэкапа.
- Все попытки заменить иконки вызывали `AccessViolationException` в `ExtractAssociatedIcon`. Оригинальные иконки стабильны.

### Release сборка
- **Single-file publish:** `publish\win-x64\LlamaStudio.exe` — **103.7 MB** (самодостаточный, 0 зависимостей).
- Обычная build: `src/LlamaStudio/bin/Release/net8.0/` (125 MB, DLL + зависимости).

---

## 📋 ТЕКУЩИЙ ПРОФИЛЬ И БИНАРНИКИ (08.06.2026)

- **Активный бинарник:** `b9559-cuda12x` → `%AppData%\Roaming\LlamaStudio\b9559-cuda12x\llama-server.exe`
- **Профиль:** `389bd3ed.json` (восстановлен, intact)

---

## 📊 PROMPT TOK/S — Отображение скорости обработки промпта (09.06.2026)

### Что добавлено
Отображение скорости обработки промпта (`PromptTokensPerSecond`) рядом со скоростью генерации во всех местах мониторинга:

| Место | UI-элемент | Цвет | Источник данных |
|-------|-----------|------|----------------|
| Dashboard (GPU Monitor) | 3-я карточка в ряду "Stats Row" | Фиолетовый (#8B5CF6) | `DashboardViewModel.GpuPromptThroughputText` |
| Server Page | Текст рядом с "Токенов/сек" | Фиолетовый (#8B5CF6) | `ServerViewModel.PromptTokensPerSecond` |
| Monitoring Page | Правая часть TPS Gauge (разделитель по центру) | Фиолетовый (#8B5CF6) | `MonitoringViewModel.PromptTpsText` |
| MonitoringWindow (floating) | Правая часть TpsGaugeControl (разделитель по центру) | Фиолетовый (#8B5CF6) | `MonitoringViewModel.PromptTpsText` |

### Изменённые файлы
- **Backend:** `ServerStatus.cs` — свойство `PromptTokensPerSecond`; `ServerManager.cs` — парсинг из логов сервера.
- **ViewModels:** `DashboardViewModel.cs`, `MonitoringViewModel.cs`, `ServerViewModel.cs` — добавлены свойства для Prompt tok/s.
- **Views:** `DashboardPage.axaml`, `MonitoringPage.axaml`, `ServerPage.axaml`, `TpsGaugeControl.axaml` — UI с разделителем и двумя метриками.
- **Локализация:** `LocalizationService.cs` — ключи `dash.prompt_throughput`, `mon.prompt_tps`.

### Локализация (EN/RU)
```csharp
["dash.throughput"] = new() { ["en"] = "Tokens/sec", ["ru"] = "Токенов/сек" },
["dash.prompt_throughput"] = new() { ["en"] = "Prompt tok/s", ["ru"] = "Промпт ток/с" },
["mon.tps"] = new() { ["en"] = "Tokens/sec", ["ru"] = "Токенов/сек" },
["mon.prompt_tps"] = new() { ["en"] = "Prompt tok/s", ["ru"] = "Промпт ток/с" },
```

### Сборка
- Чистая сборка, 0 ошибок.
- `taskkill /F /IM LlamaStudio.exe` требуется перед пересборкой (PID блокирует DLL).
