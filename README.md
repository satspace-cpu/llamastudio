# Llama Studio

**Llama Studio** is a self-contained Windows x64 desktop manager for local GGUF models and [llama.cpp](https://github.com/ggml-org/llama.cpp) servers.

The public distribution is intentionally simple: download one `LlamaStudio.exe` from [Releases](https://github.com/satspace-cpu/llamastudio/releases) and run it. Development sources are maintained separately in the private source repository.

## Features

- Dashboard with server state, active model, profile, llama.cpp build and application update status.
- Profile-based configuration for models, vision projectors, GPU devices, tensor split, KV cache and server arguments.
- Multi-GPU setup for CUDA devices with selected-device controls, main GPU selection, manual distribution and llama.cpp automatic fitting.
- Backend-aware server management for CUDA 12, CUDA 13, Vulkan, CPU and other installed llama.cpp builds.
- Live multi-GPU monitoring on the dashboard, Server page, Monitoring page and floating window.
- VRAM, GPU load, total GPU power, GPU temperature, memory temperature when available, clock, fan and Compute Capability.
- Aggregate system RAM and total power bars for all GPUs.
- Prompt and generation speed measurement without blocking the user interface.
- Hugging Face search, repository selection, GGUF file listing and sharded model downloads.
- Pause, cancel, resume and recovery of large downloads after an unexpected shutdown.
- Download progress, speed, transferred size and remaining size.
- Browser-based local chat through the llama-server web interface.
- Logs, system-tray operation, autostart and persistent application settings.
- English and Russian localization.
- Built-in application update channel based on `latest.json`.

## Screenshots

### Dashboard

![Dashboard](screenshots/dashboard.png)

### Multi-GPU monitoring

![Monitoring](screenshots/monitoring.png)

### Floating monitor

![Floating monitor](screenshots/floating-window.png)

### Server configuration

![Server model](screenshots/server-model.png)
![Server GPU](screenshots/server-gpu.png)
![Server context](screenshots/server-context.png)
![Server advanced](screenshots/server-advanced.png)
![Server connection](screenshots/server-connect.png)

### Models and Hugging Face downloads

![Models](screenshots/models.png)

### llama.cpp releases

![Releases](screenshots/releases.png)

### Logs, settings and support

![Logs](screenshots/logs.png)
![Settings](screenshots/settings.png)
![Support](screenshots/support.png)

## Installation

1. Open the [latest release](https://github.com/satspace-cpu/llamastudio/releases/latest).
2. Download `LlamaStudio.exe`.
3. Run it on Windows 10/11 x64.
4. Select the llama.cpp server folder and model folder in the application settings.
5. Create or select a profile and start the server.

The release is self-contained and does not require installing .NET Desktop Runtime separately.

## Updating

The application checks the public `latest.json` file, compares the installed version with the available version, downloads the new `LlamaStudio.exe`, and replaces it after confirmation and restart. The update manifest is changed only together with an approved public release.

## Localization / Локализация

The interface and release notes are provided in **English and Russian**.

Интерфейс и описания релизов поддерживают **английский и русский языки**.

## Building from the private source repository

The public repository contains the application page, documentation, screenshots and release assets. The development source is kept in the private repository for backup and continued development.

The self-contained Windows build is produced with .NET 8:

```powershell
dotnet publish .\LlamaStudio.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false
```

The final public release must contain one asset only: `LlamaStudio.exe`.

## Release notes

Release notes are written in Russian and English and include changes to the dashboard, monitoring, Multi-GPU, profiles, downloads, server backends, speed measurement, localization and update behavior.

See the [release history](https://github.com/satspace-cpu/llamastudio/releases).

## Support

- [GitHub Discussions](https://github.com/satspace-cpu/llamastudio/discussions)
- [Telegram community](https://t.me/LlamaStudioApp)

## License

MIT License.
