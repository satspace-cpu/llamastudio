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

**Главная (Dashboard)**
![Dashboard](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/dashboard.png)
*Обзор состояния сервера, мониторинг GPU (RTX 5090), активная модель, профиль.*

---

**Чат**
![Chat](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/chat.png)
*Встроенный чат с AI, поддержка MCP инструментов.*

---

**Модели**
![Models](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/models.png)
*Список найденных моделей, загрузка с HuggingFace, контекстное меню.*

---

**Мониторинг**
![Monitoring](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/monitoring.png)
*Скорость токенов (Ответ/Промпт), графики VRAM, температуры, мощности.*

---

**Релизы llama.cpp**
![Releases](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/releases.png)
*Установка и управление версиями сервера (CUDA 12, CUDA 13, CPU).*

---

**Сервер (Модель)**
![Server Model](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/server-model.png)
*Выбор основной модели, mmproj (зрение), черновой модели.*

---

**Сервер (GPU)**
![Server GPU](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/server-gpu.png)
*Слои GPU, потоки, Flash Attention, кэш.*

---

**Сервер (Контекст и сэмплинг)**
![Server Context](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/server-context.png)
*Размер контекста, температура, Top-P/K.*

---

**Сервер (Продвинутое)**
![Server Advanced](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/server-advanced.png)
*MTP, спекулятивное декодирование, YARN/Rope.*

---

**Логи**
![Logs](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/logs.png)
*Вывод логов сервера в реальном времени.*

---

**Настройки**
![Settings](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/settings.png)
*Язык, пути, тема, сворачивание в трей, автозапуск.*

---

**Обсуждение и поддержка**
![Support](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/support.png)
*Ссылка на Telegram канал, вдохновитель проекта.*

---

**Плавающее окно мониторинга**
![Floating Window](https://raw.githubusercontent.com/satspace-cpu/llamastudio/main/screenshots/floating-window.png)
*Компактный мониторинг поверх других окон.*

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
