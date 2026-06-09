# Llama.cpp Server CLI Parameters Reference

Критичные параметры для работы с проектом Llama Studio. Основано на документации llama.cpp tools/server/README.md.

---

## Критичные параметры для Llama Studio

### Model & Paths
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `ModelPath` | `-m, --model` | string | — | Путь к GGUF модели |
| `MmprojPath` | `-mm, --mmproj` | string | — | Multimodal projector |
| `DraftModelPath` | `-md, --spec-draft-model` | string | — | Draft модель для speculative decoding |
| `HfRepo` | `-hf, --hf-repo` | string | unused | HF repo: `user/model[:quant]` |
| `HfFile` | `-hff, --hf-file` | string | unused | Конкретный файл с HF |

### GPU & Memory
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `GpuLayers` | `-ngl, --gpu-layers` | int | auto | Кол-во слоёв в VRAM |
| `GpuSplitMode` | `-sm, --split-mode` | string | layer | none/layer/row/tensor |
| `TensorSplit` | `-ts, --tensor-split` | string | — | Пропорции на GPU (через запятую) |
| `MainGpu` | `-mg, --main-gpu` | int | 0 | Главный GPU |
| `FlashAttention` | `-fa, --flash-attn` | on\|off\|auto | auto | **ДВА аргумента: `-fa on`** |
| `Mmap` | `--mmap` / `--no-mmap` | bool | enabled | Memory-map модели |
| `Mlock` | `--mlock` | bool | false | Удерживать в RAM |
| `NoKvOffload` | `-nkvo, --no-kv-offload` | bool | disabled | Отключить KV offload |
| `Numa` | `--numa` | string | — | distribute/isolate/numactl |

### Context & Batch
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `ContextSize` | `-c, --ctx-size` | int | 0 (из модели) | Размер контекста |
| `BatchSize` | `-b, --batch-size` | int | 2048 | Логический max batch |
| `UbatchSize` | `-ub, --ubatch-size` | int | 512 | Физический max batch |
| `CacheTypeK` | `-ctk, --cache-type-k` | string | f16 | f32/f16/bf16/q8_0/q4_0/... |
| `CacheTypeV` | `-ctv, --cache-type-v` | string | f16 | f32/f16/bf16/q8_0/q4_0/... |

### Threads
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Threads` | `-t, --threads` | int | -1 | CPU-потoki генерации |
| `ThreadsBatch` | `-tb, --threads-batch` | int | =threads | Потoki batch/prompt |

### Server Network
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Host` | `--host` | string | `127.0.0.1` | IP для прослушивания |
| `Port` | `--port` | int | 8080 | Порт |
| `Timeout` | `-to, --timeout` | int | **3600** | Таймаут read/write (сек) |
| `ApiKey` | `--api-key` | string | none | API-ключ (через запятую) |
| `Alias` | `-a, --alias` | string | — | Алиасы имени модели |

### Slots & Batching
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Slots` | `-np, --parallel` | int | -1 (auto) | Кол-во слотов сервера |
| `ContBatching` | `-cb, --cont-batching` / `-nocb` | bool | enabled | Continuous batching |
| `CachePrompt` | `--cache-prompt` / `--no-cache-prompt` | bool | enabled | Prompt caching |

### Sampling
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Seed` | `-s, --seed` | int | -1 (random) | Seed генератора |
| `Temperature` | `--temp, --temperature` | float | 0.80 | Температура |
| `TopK` | `--top-k` | int | 40 | Top-K |
| `TopP` | `--top-p` | float | 0.95 | Top-P / Nucleus |
| `MinP` | `--min-p` | float | 0.05 | Min-P |
| `TypicalP` | `--typical, --typical-p` | float | 1.00 | Locally typical |
| `RepeatPenalty` | `--repeat-penalty` | float | 1.00 | Penalty за повторения |
| `RepeatLastN` | `--repeat-last-n` | int | 64 | Окно повторений |
| `PresencePenalty` | `--presence-penalty` | float | 0.00 | Presence penalty |
| `FrequencyPenalty` | `--frequency-penalty` | float | 0.00 | Frequency penalty |

### Mirostat
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Mirostat` | `--mirostat` | **int** | 0 | **0=disabled, 1=v1, 2=v2!** |
| `MirostatTau` | `--mirostat-ent` | float | 5.00 | Target entropy (tau) |
| `MirostatEta` | `--mirostat-lr` | float | 0.10 | Learning rate (eta) |

### DRY
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `DryMultiplier` | `--dry-multiplier` | float | 0.00 | DRY multiplier |
| `DryBase` | `--dry-base` | float | 1.75 | DRY base |

### Dynatemp
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `DynatempStddev` | `--dynatemp-range` | float | 0.00 | **НЕ --dynatemp-stddev!** |

### XTC
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `XtcProbability` | `--xtc-probability` | float | 0.00 | XTC probability |
| `XtcThreshold` | `--xtc-threshold` | float | 0.10 | XTC threshold |

### Predict
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `MaxTokens` | `-n, --predict, --n-predict` | int | -1 (infinity) | Кол-во токенов |
| `PredictCount` | `-n, --predict` | int | -1 | Тоже -n! |

### RoPE & YARN
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `RopeFreqBase` | `--rope-freq-base` | float | из модели | Базовая частота RoPE |
| `RopeFreqScale` | `--rope-freq-scale` | float | — | Масштаб частоты |
| `YarnOriginalContext` | `--yarn-orig-ctx` | int | 0 | YaRN orig ctx |
| `YarnExtFactor` | `--yarn-ext-factor` | float | -1.00 | YaRN экстраполяция |
| `YarnAttnFactor` | `--yarn-attn-factor` | float | -1.00 | YaRN внимание |
| `YarnBetaFast` | `--yarn-beta-fast` | float | -1.00 | YaRN beta |
| `YarnBetaSlow` | `--yarn-beta-slow` | float | -1.00 | YaRN alpha |

### Web UI
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `EnableWebUI` | `--ui` / `--no-ui` | bool | enabled | **НЕ --webui (deprecated)!** |
| `EnableSlots` | `--slots` / `--no-slots` | bool | enabled | Мониторинг слотов |
| `EnableMetrics` | `--metrics` | bool | disabled | Prometheus metrics |

### Reasoning
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `Reasoning` | `-rea, --reasoning` | on\|off\|auto | auto | **ДВА аргумента: `-rea on`** |
| `ReasoningBudget` | `--reasoning-budget` | int | -1 (unlimited) | Бюджет токенов thinking |

### Speculative Decoding
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `SpeculativeDecoding` | — | bool | false | Включает draft модель |
| `EnableMtp` | `--spec-type` | string | none | **ДВА аргумента: `--spec-type draft-eagle3`** |
| `SpecDraftGpuLayers` | `--spec-draft-ngl, -ngld` | int | auto | Слои draft в VRAM |
| `SpecDraftNMax` | `--spec-draft-n-max` | int | 3 | Макс токенов draft |
| `SpecDraftNMin` | `--spec-draft-n-min` | int | 0 | Мин токенов draft |
| `SpecDraftPSplit` | `--spec-draft-p-split` | float | 0.10 | Split probability |
| `SpecDraftPMin` | `--spec-draft-p-min` | float | 0.00 | Min probability |
| `HfRepoDraft` | `--spec-draft-hf, -hfd` | string | unused | HF repo draft |

### Embedding
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `EmbeddingMode` | `--embedding` | bool | disabled | Только embeddings |
| `PoolingType` | `--pooling` | string | из модели | none/mean/cls/last/rank |

### Process & System
| Свойство Llama Studio | Флаг | Тип | Дефолт | Примечание |
|---|---|---|---|---|
| `ProcessPriority` | `--prio` | int | 0 | -1/0/1/2/3 |
| `Numa` | `--numa` | string | — | distribute/isolate/numactl |
| `CpuAffinity` | `-C, --cpu-mask` | string (hex) | "" | CPU affinity mask |
| `VerboseLogging` | `-v, --verbose` | bool | — | Макс вербозность |
| `LogFilePath` | `--log-file` | string | — | Логирование в файл |

---

## Критичные ошибки в текущей реализации BuildArgsString()

### 1. `--mirostat` (КРИТИЧЕСКАЯ)
**Проблема:** Флаг `--mirostat` добавляется без значения. Это **int** (0/1/2), а не bool!
**Исправление:** Нужно добавить значение 1 или 2.

### 2. `--dynatemp-stddev` (КРИТИЧЕСКАЯ)
**Проблема:** Такого флага не существует в llama.cpp!
**Исправление:** Заменить на `--dynatemp-range`.

### 3. `--webui` (DEPRECATED)
**Проблема:** Используется deprecated флаг `--webui`.
**Исправление:** Заменить на `--ui`.

### 4. `--spec-type draft-eagle3` (ФОРМАТ)
**Проблема:** Добавляется как ОДИН аргумент вместо двух отдельных.
**Исправление:** `args.Add("--spec-type"); args.Add("draft-eagle3");`

### 5. `-rea on` (ФОРМАТ)
**Проблема:** Добавляется как ОДИН аргумент вместо двух.
**Исправление:** `args.Add("-rea"); args.Add("on");`

### 6. `--timeout` (ДЕФОЛТ)
**Проблема:** Дефолт 600, но в документации 3600.
**Исправление:** Изменить дефолт на 3600.

### 7. `CacheTypeK`/`CacheTypeV` (ДЕФОЛТ)
**Проблема:** Всегда добавляются, даже при дефолтном значении f16.
**Исправление:** Добавлять только если не дефолт.

### 8. `--slots` (ДЕФОЛТ)
**Проблема:** Добавляется при true, но true = дефолт (enabled).
**Исправление:** Не добавлять если true.

### 9. `--typical-p` (ДЕФОЛТ)
**Проблема:** Всегда добавляется даже при 1.0 (дефолт).
**Исправление:** Не добавлять если 1.0.

### 10. `-cb` (Логика)
**Проблема:** Добавляется при true, но true = дефолт (enabled).
**Исправление:** Не добавлять если true (дефолт).

---

## Pattern: Как правильно строить аргументы

### Bool флаги с on/off:
```csharp
// ПРАВИЛЬНО:
args.Add("-fa");
args.Add("on");

// НЕПРАВИЛЬНО:
args.Add("-fa on");  // один аргумент!
```

### Bool флаги с --no- инверсией:
```csharp
// ПРАВИЛЬНО:
if (!Mmap) args.Add("--no-mmap");
// НЕПРАВИЛЬНО:
if (Mmap) args.Add("--mmap");  // дефолт, не нужен

// Если нужно явно указать:
args.Add(Mmap ? "--mmap" : "--no-mmap");
```

### Int флаги (не 0 = дефолт):
```csharp
// ПРАВИЛЬНО:
if (value != defaultValue)
{
    args.Add("--flag");
    args.Add(value.ToString());
}

// НЕПРАВИЛЬНО:
if (value > 0) { ... }  // пропустит 0!
```

### String флаги (empty = skip):
```csharp
if (!string.IsNullOrEmpty(value))
{
    args.Add("--flag");
    args.Add(value);
}
```

---

## Process Management (по референсу)

### Запуск процесса:
```csharp
var startInfo = new ProcessStartInfo
{
    FileName = executablePath,
    Arguments = commandLine,
    WorkingDirectory = Path.GetDirectoryName(executablePath),
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true,
};
```

### Разрешение пути к исполняемому файлу:
```csharp
// Попробовать PATH
foreach (var dir in Environment.GetEnvironmentVariable("PATH")!.Split(Path.PathSeparator))
{
    var path = Path.Combine(dir, fileName);
    foreach (var ext in new[] { ".exe", ".cmd", ".bat" })
    {
        if (File.Exists(path + ext))
            return path + ext;
    }
}
// Попробовать прямой путь
foreach (var ext in new[] { "", ".exe", ".cmd", ".bat" })
{
    var fullPath = path + ext;
    if (File.Exists(fullPath))
        return fullPath;
}
```

### Остановка процесса:
```csharp
_isStoppingIntentionally = true;
process.Kill(entireProcessTree: true);
process.WaitForExit(timeoutMs);
```

---

## DuplicateProfile — ВСЕ свойства для копирования

```csharp
// Минимальный набор для точной копии:
Name = original.Name + " (Copy)",
Description = original.Description,
ModelPath = original.ModelPath,
MmprojPath = original.MmprojPath,
DraftModelPath = original.DraftModelPath,
LlamaCppVersion = original.LlamaCppVersion,
HfRepo = original.HfRepo,
HfFile = original.HfFile,
HfOffline = original.HfOffline,
HfRepoDraft = original.HfRepoDraft,
SpecDraftGpuLayers = original.SpecDraftGpuLayers,
SpecDraftNMax = original.SpecDraftNMax,
SpecDraftNMin = original.SpecDraftNMin,
SpecDraftPSplit = original.SpecDraftPSplit,
SpecDraftPMin = original.SpecDraftPMin,
ApiKey = original.ApiKey,
Alias = original.Alias,
LogFilePath = original.LogFilePath,
VerboseLogging = original.VerboseLogging,
EnableWebUI = original.EnableWebUI,
EnableSlots = original.EnableSlots,
EnableMetrics = original.EnableMetrics,
Reasoning = original.Reasoning,
ReasoningBudget = original.ReasoningBudget,
Seed = original.Seed,
PresencePenalty = original.PresencePenalty,
FrequencyPenalty = original.FrequencyPenalty,
MaxTokens = original.MaxTokens,
CachePrompt = original.CachePrompt,
ContBatching = original.ContBatching,
// + все остальные свойства
IsDefault = false,
```
