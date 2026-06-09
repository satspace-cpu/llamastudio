# Llama Studio

**Llama Studio** — это удобное десктопное приложение для управления локальными AI-моделями (llama.cpp) на Windows. Позволяет запускать сервер, управлять профилями, мониторить GPU и общаться с моделями через встроенный чат.

## ✨ Возможности

*   **Управление сервером:** Запуск, остановка и перезагрузка `llama-server` в один клик.
*   **Профили настроек:** Создание и сохранение профилей для разных моделей (GPU слои, контекст, температура и т.д.).
*   **Мониторинг в реальном времени:** Отслеживание VRAM, загрузки GPU, скорости токенов (tok/s) и потребления памяти.
*   **Встроенный чат:** Удобный интерфейс для общения с локальной моделью.
*   **Поддержка Hugging Face:** Быстрый поиск и загрузка моделей.
*   **Плавающее окно мониторинга:** Всегда видные показатели работы системы поверх других окон.
*   **Работа с треєм:** Сворачивание в трей, автозапуск, работа без главного окна.

## 📸 Скриншоты

<figure>
<img src="screenshots/dashboard.png" width="900">
<figcaption><strong>Главная (Dashboard)</strong> — Обзор состояния сервера, мониторинг GPU (RTX 5090), активная модель, профиль.</figcaption>
</figure>

<figure>
<img src="screenshots/chat.png" width="900">
<figcaption><strong>Чат</strong> — Встроенный чат с AI, поддержка MCP инструментов.</figcaption>
</figure>

<figure>
<img src="screenshots/models.png" width="900">
<figcaption><strong>Модели</strong> — Список найденных моделей, загрузка с HuggingFace, контекстное меню.</figcaption>
</figure>

<figure>
<img src="screenshots/monitoring.png" width="900">
<figcaption><strong>Мониторинг</strong> — Скорость токенов (Ответ/Промпт), графики VRAM, температуры, мощности.</figcaption>
</figure>

<figure>
<img src="screenshots/releases.png" width="900">
<figcaption><strong>Релизы llama.cpp</strong> — Установка и управление версиями сервера (CUDA 12, CUDA 13, CPU).</figcaption>
</figure>

<figure>
<img src="screenshots/server-model.png" width="900">
<figcaption><strong>Сервер (Модель)</strong> — Выбор основной модели, mmproj (зрение), черновой модели.</figcaption>
</figure>

<figure>
<img src="screenshots/server-gpu.png" width="900">
<figcaption><strong>Сервер (GPU)</strong> — Слои GPU, потоки, Flash Attention, кэш.</figcaption>
</figure>

<figure>
<img src="screenshots/server-context.png" width="900">
<figcaption><strong>Сервер (Контекст и сэмплинг)</strong> — Размер контекста, температура, Top-P/K.</figcaption>
</figure>

<figure>
<img src="screenshots/server-advanced.png" width="900">
<figcaption><strong>Сервер (Продвинутое)</strong> — MTP, спекулятивное декодирование, YARN/Rope.</figcaption>
</figure>

<figure>
<img src="screenshots/logs.png" width="900">
<figcaption><strong>Логи</strong> — Вывод логов сервера в реальном времени.</figcaption>
</figure>

<figure>
<img src="screenshots/settings.png" width="900">
<figcaption><strong>Настройки</strong> — Язык, пути, тема, сворачивание в трей, автозапуск.</figcaption>
</figure>

<figure>
<img src="screenshots/support.png" width="900">
<figcaption><strong>Обсуждение и поддержка</strong> — Ссылка на Telegram канал, вдохновитель проекта.</figcaption>
</figure>

<figure>
<img src="screenshots/floating-window.png" width="900">
<figcaption><strong>Плавающее окно мониторинга</strong> — Компактный мониторинг поверх других окон.</figcaption>
</figure>

## 🚀 Установка

1.  Скачайте последнюю версию с вкладки **Releases**.
2.  Распакуйте архив или сразу запустите `LlamaStudio.exe`.
3.  Укажите путь к папке с `llama.cpp` и моделями в настройках.
4.  Готово!

## ⚙️ Системные требования

*   **OS:** Windows 10/11 (x64)
*   **GPU:** NVIDIA (рекомендуется для CUDA) или AMD/Intel (Vulkan)
*   **RAM:** Минимум 16 ГБ (зависит от размера модели)
*   **.NET Runtime:** Не требуется (приложение самодостаточное)

## 🛠 Для разработчиков

Проект написан на **C# (.NET 8)** с использованием **Avalonia UI**.

```bash
# Сборка проекта
dotnet build src/LlamaStudio/LlamaStudio.csproj

# Публикация в один exe файл
dotnet publish src/LlamaStudio/LlamaStudio.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
```

## 📜 Лицензия

MIT License

## 📞 Поддержка

*   Telegram канал: [Llama Studio App](https://t.me/LlamaStudioApp)
*   Обсуждение и баги: [Discussions](https://github.com/satspace-cpu/llamastudio/discussions)
