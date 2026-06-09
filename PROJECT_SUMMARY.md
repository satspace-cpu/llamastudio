# Hermass — Полная техническая сводка

**Последнее обновление:** 2026-05-29 (сессия: параметры по умолчанию + управление профилями в ServerPage)

## 1. Архитектура проекта

**Hermass** — десктопное приложение на Avalonia (.NET 8.0, win-x64) для управления локальным `llama-server.exe` (llama.cpp). GUI позволяет выбирать модели GGUF, настраивать профили запуска с 60+ параметрами, управлять сервером и отслеживать логи.

### Слои (3 проекта в solution + 1 тестовый)
```
Hermass.sln
├── Hermass.Core                  — .NET 8.0, интерфейсы/модели/enum'ы/локализация
├── Hermass                       — Avalonia UI, ViewModels, Views, DI
├── Hermass.Infrastructure        — реализации: ServerManager, ModelScanner, ProfileManager, LlamaUpdater, LogService, AppSettings
└── tests/Hermass.Core.Tests      — unit тесты для Core
```

### NuGet зависимости
| Пакет | Версия | Назначение |
|-------|--------|------------|
| Avalonia | 11.2.1 | UI framework |
| Avalonia.Desktop | 11.2.1 | Desktop runner |
| Avalonia.Themes.Fluent | 11.2.1 | Fluent theme |
| Avalonia.Fonts.Inter | 11.2.1 | Шрифт Inter |
| Avalonia.Diagnostics | 11.2.1 | Dev tools |
| CommunityToolkit.Mvvm | 8.3.2 | ObservableObject, RelayCommand, [ObservableProperty] |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | DI container |
| Microsoft.Extensions.Hosting | 9.0.0 | Host builder |
| ScottPlot.Avalonia | 5.0.46 | Графики |
| Material.Icons.Avalonia | 2.1.10 | Иконки Material Design |
| Serilog + Sinks | 4.1.0 | Структурированное логирование |
| System.Reactive | 6.0.1 | Reactive streams для логов |
| SharpZipLib | 1.4.2 | Распаковка zip/7z при обновлении |

---

## 2. Что уже реализовано

### Полностью рабочие компоненты:
- **DI контейнер** через `Microsoft.Extensions.Hosting` в `App.axaml.cs` — все сервисы, ViewModels и MainWindow зарегистрированы как singletons
- **Навигация** — `MainViewModel.NavigateTo()` / `NavigationService`, переключение страниц через `ContentControl`
- **6 ViewModels**: Main, Dashboard, Models, Server, Profiles, Logs, Settings — все с `[ObservableProperty]` и `[RelayCommand]`
- **6 Views (Pages)**: DashboardPage, ModelsPage, ServerPage, ProfilesPage, SettingsPage, LogsPage
- **LocalizationService** — 280+ ключей RU/EN, `T(key)` метод, `ChangeLanguage()`, `LanguageChanged` event
- **ServerManager** — запуск/стоп `llama-server.exe`, сбор аргументов CLI из ServerProfile, мониторинг `/health`, парсинг stdout/stderr
- **ModelScanner** — бинарный парсер GGUF (magic number, architecture, quantization, VRAM estimate)
- **ProfileManager** — JSON CRUD для профилей в `profiles/` директории, импорт/экспорт, дублирование, set-default
- **LlamaUpdater** — GitHub API (`ggml-org/llama.cpp`), скачивание zip/7z, распаковка
- **LogService** — Serilog + reactive `IObservable<LogEntry>` stream, in-memory буфер
- **DialogService** — file/folder picker, message/confirmation/error/success/input диалоги через кастомные `MessageDialogWindow` и `InputDialogWindow`
- **MainWindow** — sidebar навигация, toolbar с кнопками Start/Stop сервера, динамический заголовок окна

### Что реализовано в ViewModels:
- Все ViewModels имеют переведенные строки через `_loc.T("key")` properties
- ServerViewModel: 1148 строк, полный набор параметров сервера, синхронизация UI ↔ Profile, экспорт в .bat, командная строка
- SettingsViewModel: 379 строк, автосохранение с debounce (1 сек таймер), скачивание релизов llama.cpp
- LogsViewModel: фильтрация по уровню и тексту, экспорт/очистка логов

---

## 3. Принятые решения

1. **MVVM с DI** — ViewModels как singletons, Views создаются на лету в `MainViewModel.NavigateTo()` с передачей ViewModel через конструктор
2. **Нет ViewLocator** — используется прямая передача ViewModel в конструктор Page
3. **Локализация** — все строки UI идут через `_loc.T("key")` в ViewModel, AXAML биндится к свойствам ViewModel
4. **Навигация** — императивная, через `MainViewModel.NavigateTo("PageName")`, без сторонних библиотек
5. **Профили** — JSON файлы в `profiles/` директории, один профиль может быть "default"
6. **Авто-сохранение настроек** — debounce таймер 1 секунда в SettingsViewModel
7. **Портативный режим** — определяется по наличию `portable.txt`, данные хранятся рядом с exe
8. **JSON settings** — camelCase ключи (`llamaCppDirectory`), сохранение через `JsonNamingPolicy.CamelCase`
9. **UI Layout** — Grid с фиксированной колонкой лейблов 180px + Margin="24,0,0,0" на элементах второй колонки (ColumnSpacing недоступен в Avalonia 11.2.1)

---

## 4. Баги, найденные и исправленные

### ✅ Исправлено: AppSettings LoadAsync — camelCase ключи (2025-07-13)
**Проблема:** `LoadAsync()` искал ключи в PascalCase (`LlamaCppDirectory`), но JSON содержит camelCase (`llamaCppDirectory`). `PropertyNameCaseInsensitive` влияет только на десериализацию объектов, а не на `Dictionary.TryGetValue()`. Настройки не загружались при запуске.
**Решение:** Заменить все `TryGetValue("LlamaCppDirectory", ...)` на `TryGetValue("llamaCppDirectory", ...)` и т.д. для всех 16 свойств.

### ✅ Исправлено: UI Layout — Grid для всех страниц (2025-07-13)
**Проблема:** Все страницы использовали `StackPanel Orientation="Horizontal"` с фиксированными `Width="100"`/`Width="120"` для лейблов. Поля не растягивались, названия обрезались, элементы слипались.
**Решение:**
- Все Grid переделаны на `ColumnDefinitions="180,*"` (фиксированная колонка 180px для лейблов)
- `ColumnSpacing` недоступен в Avalonia 11.2.1 — заменён на `Margin="24,0,0,0"` у элементов Grid.Column="1"
- Шрифт лейблов увеличен с 13 до 14, цвет с `#94A3B8` на `#CBD5E1`
- Spacing внутри секций увеличен с 12 до 16
- Spacing между CheckBox в строках увеличен с 20 до 24
- Исправлены файлы: `ServerPage.axaml` (все 5 табов), `SettingsPage.axaml`

---

## 5. Найденные баги и проблемы (актуальные)

### Критические:
1. **LocalizationService не реагирует на смену языка в AXAML** — `_loc.T()` вызывается один раз при создании свойства, но `PropertyChanged` не поднимается для translated properties. Нужно либо поднимать `PropertyChanged` вручную при смене языка (как сделано в SettingsViewModel.OnLanguageChanged), либо использовать binding к `LocalizationService` напрямую

### Средние:
2. **Debug logging повсюду** — `Console.WriteLine("[DEBUG] ...")` и `System.Diagnostics.Debug.WriteLine()` в App.axaml.cs, DashboardPage.axaml.cs, MainViewModel.cs — нужно убрать или сделать опциональным
3. **ServerPage.axaml** — огромный файл (534 строки) с 5 табами, много дублирующихся структур

### Низкие:
4. **SettingsPage.axaml ComboBox** — Language, Theme items используют хардкодные строки ("en", "ru", "Dark", "Light") вместо переведенных
5. **Рефакторинг ServerPage.axaml** — вынести табы в отдельные UserControl'ы

---

## 6. Что осталось сделать

### Высокий приоритет:
1. **Исправить реактивность локализации** — при смене языка все UI строки должны обновиться автоматически (поднять `PropertyChanged` для всех translated properties)
2. **Убрать debug Console.WriteLine** из production кода

### Средний приоритет:
3. **SettingsPage.axaml ComboBox** — перевести items ComboBox (Language, Theme)
4. **Рефакторинг ServerPage.axaml** — вынести табы в отдельные UserControl'ы

### Низкий приоритет:
5. **Unit тесты** для инфраструктуры

---

## 7. UI Layout — текущее состояние

### ✅ ЗАВЕРШЕНО: Все страницы переделаны на Grid layout

**Итоговый паттерн (Avalonia 11.2.1):**
```xml
<Grid ColumnDefinitions="180,*">
    <TextBlock Grid.Column="0" Text="{Binding Label}" FontSize="14" Foreground="#CBD5E1" VerticalAlignment="Center"/>
    <NumericUpDown Grid.Column="1" Margin="24,0,0,0" Value="{Binding Value}" .../>
</Grid>
```

**Важно:** `ColumnSpacing` НЕ работает в Avalonia 11.2.1 (ошибка AVLN2000). Вместо него используется `Margin="24,0,0,0"` на элементах второй колонки.

### Страницы, которые переделаны:
| Страница | Таб/Секция | Статус |
|----------|-----------|--------|
| ServerPage.axaml | Model | ✅ Grid 180,* + Margin |
| ServerPage.axaml | GPU | ✅ Grid 180,* + Margin |
| ServerPage.axaml | Context & Sampling | ✅ Grid 180,* + Margin |
| ServerPage.axaml | Advanced (MTP, Speculative, YARN, Options) | ✅ Grid 180,* + Margin |
| ServerPage.axaml | Connection | ✅ Grid 180,* + Margin |
| SettingsPage.axaml | Language | ✅ Grid 180,* + Margin |
| SettingsPage.axaml | Paths (с кнопками Browse) | ✅ Grid 180,*,Auto + Margin |
| SettingsPage.axaml | General (Theme, Update Channel) | ✅ Grid 180,* + Margin |
| SettingsPage.axaml | Server Defaults | ✅ Grid 180,* + Margin |

---

## 8. Важные настройки и параметры

### ISettings (AppSettings):
- `LlamaCppDirectory` — путь к llama.cpp
- `ModelsDirectory` — директория для сканирования GGUF моделей
- `ActiveLlamaCppVersion` — текущая версия
- `Theme` — "Dark" / "Light"
- `Language` — "en" / "ru"
- `PortableMode` — портативный режим
- `AutoCheckUpdates` — автопроверка обновлений
- `DefaultHost`, `DefaultPort`, `DefaultGpuLayers` — дефолтные настройки сервера
- `FlashAttention` — включить flash attention по умолчанию
- `UpdateChannel` — Stable / PreRelease / Nightly

### ServerProfile ключевые параметры:
- 60+ свойств, покрывающих все CLI флаги `llama-server.exe`
- GPU: GpuLayers, Threads, FlashAttention, Mmap, Mlock, TensorSplit
- Context: ContextSize (4096), BatchSize (2048), UbatchSize (512)
- Sampling: Temperature (0.8), TopK (40), TopP (0.95), MinP (0.05)
- Advanced: MTP, SpeculativeDecoding, YARN/Rope scaling

---

## 9. Список файлов проекта

### Hermass.Core
| Файл | Описание |
|------|----------|
| `Interfaces/ILogService.cs` | Интерфейс логирования с reactive stream |
| `Interfaces/IServerManager.cs` | Управление процессом llama-server |
| `Interfaces/IModelScanner.cs` | Сканирование GGUF моделей |
| `Interfaces/IProfileManager.cs` | CRUD профилей сервера |
| `Interfaces/ILlamaUpdater.cs` | Обновление llama.cpp |
| `Interfaces/ISettings.cs` | Глобальные настройки приложения |
| `Interfaces/ILocalizationService.cs` | Локализация RU/EN |
| `Interfaces/IDialogService.cs` | Диалоги: file picker, message, confirmation, input |
| `Interfaces/INavigationService.cs` | Навигация между страницами |
| `Models/ServerProfile.cs` | 268 строк, полная конфигурация запуска сервера |
| `Models/GgufModelInfo.cs` | 87 строк, метаданные GGUF модели |
| `Models/ServerStatus.cs` | Снимок состояния работающего сервера |
| `Models/LogEntry.cs` | Структурированная запись лога |
| `Models/LlamaCppRelease.cs` | DTO GitHub release + asset |
| `Enums/Enums.cs` | 9 enum'ов: ModelType, QuantizationType, GpuSplitMode, CacheTypeK/V, ServerState, LogLevel, SamplerStrategy, UpdateChannel |
| `Services/LocalizationService.cs` | 298 строк, RU/EN переводы (~280 ключей) |

### Hermass (UI)
| Файл | Описание |
|------|----------|
| `Program.cs` | Entry point, Avalonia builder |
| `App.axaml.cs` | 109 строк, DI регистрация через Microsoft.Extensions.Hosting |
| `MainWindow.axaml` | Shell: sidebar (240px) + toolbar + ContentControl. MinWidth=1100, MinHeight=700 |
| `ViewModels/MainViewModel.cs` | 197 строк, навигация, start/stop сервера |
| `ViewModels/DashboardViewModel.cs` | 106 строк, обзорная страница |
| `ViewModels/ModelsViewModel.cs` | 165 строк, сканирование и фильтрация GGUF |
| `ViewModels/ServerViewModel.cs` | 1346 строк, полная конфигурация сервера + управление профилями |
| `ViewModels/ProfilesViewModel.cs` | 211 строк, CRUD профилей |
| `ViewModels/LogsViewModel.cs` | 95 строк, фильтрация и экспорт логов |
| `ViewModels/SettingsViewModel.cs` | 379 строк, настройки + обновления llama.cpp |
| `Services/NavigationService.cs` | 19 строк, обертка над MainViewModel.NavigateTo |
| `Services/DialogService.cs` | 108 строк, file/folder picker + message/input dialogs |
| `Views/Pages/DashboardPage.axaml/.cs` | Grid 4x2 карточки + Quick Actions |
| `Views/Pages/ModelsPage.axaml/.cs` | Список моделей GGUF (ListBox) |
| `Views/Pages/ServerPage.axaml/.cs` | 606 строк, 5 табов + управление профилями (8 кнопок) |
| `Views/Pages/ProfilesPage.axaml/.cs` | 117 строк, управление профилями |
| `Views/Pages/SettingsPage.axaml/.cs` | 238 строк, настройки приложения |
| `Views/Pages/LogsPage.axaml/.cs` | 80 строк, просмотр логов |
| `Converters/` | 6 конвертеров значений для AXAML |
| `Controls/MessageDialogWindow.axaml/.cs` | Кастомный диалог сообщения/подтверждения |
| `Controls/InputDialogWindow.axaml.cs` | 135 строк, кастомный диалог ввода текста |

### Hermass.Infrastructure
| Файл | Описание |
|------|----------|
| `Logging/LogService.cs` | Serilog wrapper + reactive log stream |
| `Llama/ServerManager.cs` | 735 строк, процесс llama-server + arg builder + health monitor |
| `Llama/ModelScanner.cs` | 333 строки, бинарный парсер GGUF |
| `Profiles/ProfileManager.cs` | JSON CRUD профилей |
| `Updater/LlamaUpdater.cs` | 262 строки, GitHub API + download/extract |
| `Models/AppSettings.cs` | 135 строк, реализация ISettings, JSON persistence (camelCase) |

---

## 10. Что нельзя ломать при дальнейшей работе

1. **DI регистрация в App.axaml.cs** — все ViewModels и сервисы зарегистрированы как singletons. MainWindow резолвится через DI. Не менять порядок регистрации (Settings → LogService → остальные)
2. **Конструкторы Pages** — каждый Page имеет два конструктора: параметрический (для InitializeComponent) и с ViewModel (для DataContext). Не удалять паттерн `: this()`
3. **MainViewModel.NavigateTo()** — создает новые экземпляры Pages при каждой навигации. Если изменить на переиспользование, нужно учитывать жизненный цикл
4. **ServerManager.StartAsync/StopAsync** — асинхронные, с CancellationToken. Process управляется внутри lock'а
5. **ProfileManager JSON формат** — профили хранятся как отдельные JSON файлы. Изменение структуры ServerProfile потребует миграции существующих файлов
6. **LocalizationService.T()** — используется во всех ViewModels. Ключи имеют формат `section.key`. Не удалять ключи без проверки всех ссылок
7. **LogService.LogStream** — reactive Observable, на который подписываются LogsViewModel и MainWindow. Нельзя менять тип или удалять подписку
8. **SettingsViewModel auto-save timer** — System.Timers.Timer с debounce 1 сек. Dispose вызывается при выходе из приложения через `desktop.Exit`
9. **AppSettings JSON keys** — camelCase (`llamaCppDirectory`, `modelsDirectory` и т.д.). `LoadAsync()` использует `TryGetValue` по camelCase ключам
10. **Avalonia 11.2.1 Grid** — НЕ поддерживает `ColumnSpacing`. Использовать `Margin="24,0,0,0"` на элементах второй колонки
11. **InputDialogWindow** — создаётся программно (без AXAML), использует StyledProperty для реактивности. Не удалять паттерн с `ResultText`/`ResultConfirmed`

---

## 11. Команды сборки и тестирования

### Сборка:
```powershell
# Полная сборка solution
dotnet build L:\1c_modul\hermass\Hermass.sln

# Сборка только UI проекта (без restore)
dotnet build L:\1c_modul\hermass\src\Hermass\Hermass.csproj --no-restore

# Публикация (single file, win-x64)
dotnet publish L:\1c_modul\hermass\src\Hermass\Hermass.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o L:\1c_modul\hermass\publish
```

### Тесты:
```powershell
dotnet test L:\1c_modul\hermass\tests\Hermass.Core.Tests\Hermass.Core.Tests.csproj
```

---

## 12. Рабочий процесс (workflow)

- **Отладка:** пока работаем с Debug-сборкой (`bin\Debug\net8.0`). Все правки вносятся в исходники, затем компилируются для проверки через запуск exe.
- **Правило «взять и добавить»:** когда пользователь говорит «добавь X», нужно:
  1. Править все необходимые файлы (XAML, ViewModel, локализация и т.д.)
  2. Компилировать проект (`dotnet build`)
  3. Результат — готовый exe в `bin\Debug\net8.0` для проверки
- **Не трогать bin/Debug напрямую** — правки только в исходниках (`src/Hermass/`).

## 13. Текущая задача (на момент сохранения)

### ✅ Выполнено: ToolTip и PlaceholderText (Watermark) для всех полей ServerPage
- Добавлены тултипы ко всем полям ввода на всех 5 табах (Model, GPU, Context/Sampling, Advanced, Connection)
- Добавлен `Watermark` для текстовых полей без placeholder'а (Host, RopeFreqBase, RopeFreqScale, YarnOriginalContext)
- Тултипы привязаны через `controls:ToolTip.Tip="{Binding Tip...}"`
- Обновлено: `ServerViewModel.cs` (+47 tooltip properties), `ServerPage.axaml` (ToolTip + Watermark на всех полях)

### Следующие задачи:
1. Исправить реактивность локализации — PropertyChanged при смене языка
2. Убрать debug Console.WriteLine из production кода
3. Рефакторинг ServerPage.axaml — вынести табы в отдельные UserControl'ы

---

## 14. Сессия от 29.05.2026 — Параметры по умолчанию + Управление профилями

### ✅ Выполнено: Изменены значения по умолчанию в ServerViewModel.cs
| Параметр | Было | Стало | Описание |
|----------|------|-------|----------|
| `PredictCount` | 16 | -1 (авто) | Количество токенов для предсказания за шаг (--predict-count) |
| `SpecDraftNMax` | 16 | 3 | Число токенов для драфта (--spec-draft-n-max) |
| `SpecDraftPSplit` | 0.3f | 0.1f | Вероятность разделения (--spec-draft-p-split) |
| `SpecDraftPMin` | 0.9f | 0.0f | Минимальная вероятность (greedy) (--spec-draft-p-min) |
| `MaxTokens` | -1 | 256 | Максимальное количество токенов для генерации (-n) |
| `Timeout` | 300 | 600 | Таймаут чтения/записи сервера в секундах (-to) |

**Важно:** Значения по умолчанию применяются только при создании нового профиля. Существующие профили загружают сохранённые значения из JSON.

### ✅ Выполнено: Управление профилями в ServerPage (вкладка Connection)
Добавлены 8 кнопок управления профилями в блок "Profile & Controls" на вкладке Connection страницы Сервер:

| Кнопка | Команда | Действие |
|--------|---------|----------|
| Сохранить в профиль | `SaveToProfileCommand` | Синхронизирует UI → профиль, сохраняет JSON |
| Загрузить | `LoadProfileCommand` | Применяет профиль → UI поля |
| Создать | `CreateProfileCommand` | Открывает диалог ввода имени, создаёт новый профиль |
| Удалить | `DeleteProfileCommand` | Подтверждение → удаление профиля из JSON |
| Переименовать | `RenameProfileCommand` | Диалог ввода нового имени |
| Очистить | `ClearProfileCommand` | Очищает все поля модели/промпта/аргументов |
| Экспорт | `ExportProfileCommand` | Сохраняет профиль в JSON файл (file picker) |
| Импорт | `ImportProfileCommand` | Загружает профиль из JSON файла (file picker) |

### ✅ Выполнено: InputDialogWindow — диалог ввода текста
Создан новый контроль `Controls/InputDialogWindow.axaml.cs` (148 строк):
- Наследуется от `Window`, создаётся программно (без AXAML файла)
- StyledProperty: `Message`, `DefaultValue`
- Свойства результата: `ResultText`, `ResultConfirmed`
- Кнопки: Cancel / OK
- Стиль: тёмная тема (#0F172A фон, #1E293B бордюр, #3B82F6 кнопка OK)

### ✅ Выполнено: Обновлён IDialogService и DialogService
**IDialogService.cs** — добавлен метод:
```csharp
Task<string?> ShowInputAsync(string title, string message, string defaultValue = "");
```

**DialogService.cs** — реализован `ShowInputAsync` с использованием `InputDialogWindow`.

### ✅ Выполнено: Локализация новых кнопок
Добавлены ключи в `LocalizationService.cs`:
| Ключ | RU | EN |
|------|----|----|
| `server.load_profile_btn` | Загрузить | Load Profile |
| `server.create_profile_btn` | Создать | New Profile |
| `server.delete_profile_btn` | Удалить | Delete |
| `server.rename_profile_btn` | Переименовать | Rename |
| `server.clear_profile_btn` | Очистить | Clear |
| `server.export_profile_btn` | Экспорт | Export |
| `server.import_profile_btn` | Импорт | Import |

### ✅ Выполнено: ServerViewModel.cs — новые команды и свойства
Добавлены translated properties:
- `LoadProfileBtn`, `CreateProfileBtn`, `DeleteProfileBtn`, `RenameProfileBtn`
- `ClearProfileBtn`, `ExportProfileBtn`, `ImportProfileBtn`

Добавлены `[RelayCommand]` методы:
- `LoadProfile()` — применяет профиль к UI
- `CreateProfile()` — диалог → новый профиль → сохранение
- `DeleteProfile()` — подтверждение → удаление
- `RenameProfile()` — диалог → переименование
- `ClearProfile()` — очистка полей модели/промпта/аргументов
- `ExportProfile()` → SaveFileAsync → запись JSON
- `ImportProfile()` → SelectFileAsync → чтение JSON → добавление в список

### Обновлённые файлы:
| Файл | Изменения |
|------|-----------|
| `ViewModels/ServerViewModel.cs` | 6 новых значений по умолчанию, 7 команд профилей, 7 translated properties |
| `Views/Pages/ServerPage.axaml` | 8 кнопок управления профилями (2 строки × StackPanel) |
| `Controls/InputDialogWindow.axaml.cs` | Новый файл, диалог ввода текста |
| `Services/DialogService.cs` | Метод `ShowInputAsync` |
| `Core/Interfaces/IDialogService.cs` | Метод `ShowInputAsync` |
| `Core/Services/LocalizationService.cs` | 7 новых ключей локализации |

### Сборка:
```powershell
dotnet build "L:\1c_modul\hermass\Hermass.sln" --no-restore
# Результат: успешно, 0 ошибок, 0 предупреждений
# exe: L:\1c_modul\hermass\src\Hermass\bin\Debug\net8.0\Hermass.exe (29.05.2026 23:14:48)
```

### Примечание для нового чата:
- Рабочий путь проекта: `L:\1c_modul\hermass\`
- Solution файл: `L:\1c_modul\hermass\Hermass.sln`
- Debug exe: `L:\1c_modul\hermass\src\Hermass\bin\Debug\net8.0\Hermass.exe`
- Диск E: не используется, проект только на L:

---

## 15. Сессия от 30.05.2026 — Упрощение ServerPage + Кнопки действий в Профили

### ✅ Выполнено: Убраны кнопки CRUD профилей из ServerPage
Из вкладки "Подключение" страницы Сервер удалены кнопки управления профилями, оставлены только:
- **Запустить сервер** / **Остановить сервер** — управление сервером
- **Сохранить в профиль** — сохранение текущих настроек UI в выбранный профиль

Удалено из ServerPage.axaml:
- ComboBox профилей (выбор профиля)
- Кнопки: Создать, Удалить, Переименовать, Очистить, Экспорт, Импорт, Загрузить, Обновить профили

Удалено из ServerViewModel.cs:
- Свойства: `CreateProfileBtn`, `DeleteProfileBtn`, `RenameProfileBtn`, `ClearProfileBtn`, `ExportProfileBtn`, `ImportProfileBtn`
- Команды: `CreateProfile`, `DeleteProfile`, `RenameProfile`, `ClearProfile`, `ExportProfile`, `ImportProfile`

### ✅ Выполнено: Исправлены значения по умолчанию в ServerProfile.cs
**Критическая проблема:** Значения по умолчанию были изменены только в ViewModel, но не в модели. Новые профили создавались со старыми значениями (16 вместо -1 и т.д.).

| Параметр | Было (в модели) | Стало | Описание |
|----------|-----------------|-------|----------|
| `PredictCount` | 16 | -1 | Авто (количество токенов предсказания) |
| `SpecDraftNMax` | 16 | 3 | Макс. токенов драфта |
| `SpecDraftPSplit` | 0.3f | 0.1f | Вероятность разделения |
| `SpecDraftPMin` | 0.9f | 0.0f | Мин. вероятность (greedy) |
| `MaxTokens` | -1 | 256 | Макс. токенов генерации |
| `Timeout` | 300 | 600 | Таймаут сервера (сек) |

### ✅ Выполнено: Добавлены кнопки действий на страницу Профили
На каждый профиль в списке добавлен второй ряд кнопок:

| Кнопка | Команда | Действие |
|--------|---------|----------|
| Сохранить настройки | `SaveSettingsToProfileCommand` | Применяет текущие настройки UI из ServerViewModel к профилю и сохраняет |
| Запустить сервер | `StartServerWithProfileCommand` | Устанавливает профиль, синхронизирует настройки и запускает сервер |

### ✅ Выполнено: Интеграция ProfilesViewModel ↔ ServerViewModel
- В `ProfilesViewModel` добавлена зависимость от `ServerViewModel` через DI
- `ServerViewModel.SyncSettingsToProfile()` сделан публичным
- `ServerViewModel.StartServerAsync()` сделан публичным (переименован из приватного `StartServer`)
- Добавлены ключи локализации: `profiles.save_settings`, `profiles.start_server`

### Архитектурное решение
**Разделение ответственности:**
- **Страница "Сервер"** — настройка параметров сервера, запуск/остановка, сохранение в профиль
- **Страница "Профили"** — полный CRUD профилей + быстрый запуск с выбранным профилем

**Порядок работы кнопок на странице Профили:**
1. *Сохранить настройки* → `ServerViewModel.SelectedProfile = profile` → `SyncSettingsToProfile()` → сохранение JSON
2. *Запустить сервер* → то же самое + `StartServerAsync()`

### Обновлённые файлы:
| Файл | Изменения |
|------|-----------|
| `ViewModels/ServerViewModel.cs` | Удалены 6 свойств, 6 команд; `SyncSettingsToProfile` → public; `StartServer` → `StartServerAsync` public |
| `ViewModels/ProfilesViewModel.cs` | Добавлена зависимость `ServerViewModel`; 2 новые команды; 2 translated properties |
| `Views/Pages/ServerPage.axaml` | Удалён блок управления профилями (ComboBox, 8 кнопок) |
| `Views/Pages/ProfilesPage.axaml` | Добавлен второй ряд кнопок на каждый профиль |
| `Core/Models/ServerProfile.cs` | Исправлены значения по умолчанию (6 параметров) |
| `Core/Services/LocalizationService.cs` | 2 новых ключа: `profiles.save_settings`, `profiles.start_server` |

### Сборка:
```powershell
dotnet build "L:\1c_modul\hermass\Hermass.sln" --no-restore
# Результат: успешно, 0 ошибок, 0 предупреждений
```

### Примечание для нового чата:
- Рабочий путь проекта: `L:\1c_modul\hermass\`
- Solution файл: `L:\1c_modul\hermass\Hermass.sln`
- Debug exe: `L:\1c_modul\hermass\src\Hermass\bin\Debug\net8.0\Hermass.exe`
- Диск E: не используется, проект только на L:
- **Важно:** При создании нового профиля значения по умолчанию берутся из `ServerProfile.cs`. Если нужно сбросить существующий профиль к дефолтам — удалить JSON файл профиля или создать новый.
