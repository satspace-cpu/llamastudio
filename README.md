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

<div align="center">

**Главный экран (Dashboard)**
![Dashboard](screenshots/dashboard.png)

**Настройки сервера**
![Server Settings](screenshots/server.png)

**Управление моделями**
![Models](screenshots/models.png)

**Чат с AI**
![Chat](screenshots/chat.png)

**Мониторинг GPU**
![Monitoring](screenshots/monitoring.png)

</div>

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
*   Обсуждение и баги: [Discussions](https://github.com/YOUR_USERNAME/LlamaStudio/discussions)
