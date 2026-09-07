# LlamaStudio — исходный код / Source code

**[Русский](README.md)** · [English](README.en.md)

Исходный код LlamaStudio (C# / .NET 8 + Avalonia 11.2.1). Код восстановлен из бинарной версии и приведён в читаемое состояние (осмысленные имена переменных, без артефактов декомпилятора). Сборки распространяются как готовые приложения — см. [главный README](https://github.com/satspace-cpu/llamastudio#readme).

## Лицензия / License

Все права защищены / All rights reserved — см. [LICENSE](LICENSE).

## Структура / Layout

| Проект | Назначение |
|---|---|
| `LlamaStudio.Core` | Модели, интерфейсы, контракты (без зависимостей от UI) |
| `LlamaStudio.Infrastructure` | Сервисы: llama.cpp (сервер, обновления, GPU), чат, MCP, HuggingFace, профили, логирование, автообновление |
| `LlamaStudio` | Приложение: Avalonia UI, ViewModels, веб-UI (встроенный HTTP-сервер), tray |

## Требования к сборке / Build requirements

- .NET 8 SDK (`dotnet` в PATH)
- Windows x64 или Linux x64

Сборка полностью self-contained: все зависимости (включая Markdown.Avalonia, CommunityToolkit.Mvvm, SSH.NET) берутся из NuGet, локальных DLL в репозитории нет.

> **Важно:** в дереве хранятся сгенерированные файлы (`LlamaStudio/CommunityToolkit/Mvvm/ComponentModel/__Internals/`, `LlamaStudio/CompiledAvaloniaXaml/`), необходимые для сборки — целевая задача `DisableRecoveredSourceGenerators` отключает source generators. Не удаляйте эти папки.
>
> **Important:** the tree contains checked-in generated files required for the build — the `DisableRecoveredSourceGenerators` target disables source generators. Do not delete them.

## Сборка / Build

### Linux (self-contained, x64)

```bash
dotnet publish LlamaStudio/LlamaStudio.Linux.csproj -c Release --self-contained true -r linux-x64 -o out/linux
# запуск / run:
./out/linux/LlamaStudio
```

### Windows (single-file EXE)

```bash
dotnet publish LlamaStudio/LlamaStudio.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o out/win
# результат / result: out/win/LlamaStudio.exe (один файл, нативные библиотеки распаковываются при старте)
```

### Проверка без publish / Quick build check

```bash
dotnet build LlamaStudio.Recovered.sln -c Release
```

## Версионирование / Versioning

Версия приложения задаётся в `LlamaStudio/Properties/AssemblyInfo.cs` (`AssemblyVersion` — участвует в сравнении автообновления). Публичные версии совпадают с тегами GitHub-релизов (текущая — 1.4.1).

---
