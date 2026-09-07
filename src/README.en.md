# LlamaStudio — Source Code

**[Русский](README.md)** · [English](README.en.md)

Source code of LlamaStudio (C# / .NET 8 + Avalonia 11.2.1). The code was recovered from the binary and cleaned up into a readable state (meaningful identifiers, no decompiler artifacts). The application is distributed as ready-made builds — see the [main README](https://github.com/satspace-cpu/llamastudio#readme).

## License

All rights reserved — see [LICENSE](LICENSE).

## Layout

| Project | Purpose |
|---|---|
| `LlamaStudio.Core` | Models, interfaces, contracts (no UI dependencies) |
| `LlamaStudio.Infrastructure` | Services: llama.cpp (server, updates, GPU), chat, MCP, HuggingFace, profiles, logging, auto-update |
| `LlamaStudio` | Application: Avalonia UI, ViewModels, web UI (embedded HTTP server), tray |

## Build requirements

- .NET 8 SDK (`dotnet` in PATH)
- Windows x64 or Linux x64

The build is fully self-contained: all dependencies (including Markdown.Avalonia, CommunityToolkit.Mvvm, SSH.NET) are pulled from NuGet; there are no local DLLs in the repository.

> **Important:** the tree contains checked-in generated files (`LlamaStudio/CommunityToolkit/Mvvm/ComponentModel/__Internals/`, `LlamaStudio/CompiledAvaloniaXaml/`) required for the build — the `DisableRecoveredSourceGenerators` target disables the source generators. Do not delete these folders.

## Build

### Linux (self-contained, x64)

```bash
dotnet publish LlamaStudio/LlamaStudio.Linux.csproj -c Release --self-contained true -r linux-x64 -o out/linux
# run:
./out/linux/LlamaStudio
```

### Windows (single-file EXE)

```bash
dotnet publish LlamaStudio/LlamaStudio.csproj -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o out/win
# result: out/win/LlamaStudio.exe (single file; native libraries are extracted at startup)
```

### Quick build check without publish

```bash
dotnet build LlamaStudio.Recovered.sln -c Release
```

## Versioning

The application version is set in `LlamaStudio/Properties/AssemblyInfo.cs` (`AssemblyVersion` is used by the auto-update comparison). Public versions match the GitHub release tags (current — 1.4.1).
