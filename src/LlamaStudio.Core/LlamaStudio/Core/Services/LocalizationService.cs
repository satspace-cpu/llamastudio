using System;
using System.Collections.Generic;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.Core.Services;

public class LocalizationService : ILocalizationService
{
	private string _language = "ru";

	private readonly Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>
	{
		["main.dashboard"] = new Dictionary<string, string>
		{
			["en"] = "Dashboard",
			["ru"] = "Главная"
		},
		["main.models"] = new Dictionary<string, string>
		{
			["en"] = "Models",
			["ru"] = "Модели"
		},
		["main.server"] = new Dictionary<string, string>
		{
			["en"] = "Server",
			["ru"] = "Сервер"
		},
		["main.profiles"] = new Dictionary<string, string>
		{
			["en"] = "Profiles",
			["ru"] = "Профили"
		},
		["main.logs"] = new Dictionary<string, string>
		{
			["en"] = "Logs",
			["ru"] = "Логи"
		},
		["main.apitest"] = new Dictionary<string, string>
		{
			["en"] = "API Test",
			["ru"] = "Тест API"
		},
		["main.chat"] = new Dictionary<string, string>
		{
			["en"] = "Chat",
			["ru"] = "Чат"
		},
		["main.settings"] = new Dictionary<string, string>
		{
			["en"] = "Settings",
			["ru"] = "Настройки"
		},
		["main.support"] = new Dictionary<string, string>
		{
			["en"] = "Discussion & Support",
			["ru"] = "Обсуждение и поддержка"
		},
		["main.start_server"] = new Dictionary<string, string>
		{
			["en"] = "Start Server",
			["ru"] = "Запустить сервер"
		},
		["main.server_running"] = new Dictionary<string, string>
		{
			["en"] = "Running",
			["ru"] = "✓ Запущен"
		},
		["main.stop_server"] = new Dictionary<string, string>
		{
			["en"] = "Stop Server",
			["ru"] = "Остановить сервер"
		},
		["main.server_stopped"] = new Dictionary<string, string>
		{
			["en"] = "Stopped",
			["ru"] = "Остановлен"
		},
		["main.restart_server"] = new Dictionary<string, string>
		{
			["en"] = "Restart Server",
			["ru"] = "Перезапустить"
		},
		["main.subtitle"] = new Dictionary<string, string>
		{
			["en"] = " — llama.cpp Manager",
			["ru"] = " — менеджер llama.cpp"
		},
		["main.monitoring"] = new Dictionary<string, string>
		{
			["en"] = "Monitoring",
			["ru"] = "Мониторинг"
		},
		["main.attach_external_server_confirm"] = new Dictionary<string, string>
		{
			["en"] = "External server detected at {0}:{1}. Connect to it?",
			["ru"] = "Обнаружен внешний сервер на {0}:{1}. Подключиться?"
		},
		["main.attach_external_server_title"] = new Dictionary<string, string>
		{
			["en"] = "Connect to external server",
			["ru"] = "Подключение к внешнему серверу"
		},
		["main.llama_releases"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp Releases",
			["ru"] = "Релизы llama.cpp"
		},
		["llama_releases.title"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp Releases",
			["ru"] = "Релизы llama.cpp"
		},
		["llama_releases.releases_section"] = new Dictionary<string, string>
		{
			["en"] = "Available Releases",
			["ru"] = "Доступные релизы"
		},
		["llama_releases.check_updates"] = new Dictionary<string, string>
		{
			["en"] = "Check for Updates",
			["ru"] = "Проверить обновления"
		},
		["llama_releases.show_only_latest"] = new Dictionary<string, string>
		{
			["en"] = "Only latest version",
			["ru"] = "Только последняя версия"
		},
		["llama_releases.checking"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["llama_releases.installed_versions"] = new Dictionary<string, string>
		{
			["en"] = "Installed Versions",
			["ru"] = "Установленные версии"
		},
		["llama_releases.refresh"] = new Dictionary<string, string>
		{
			["en"] = "Refresh",
			["ru"] = "Обновить"
		},
		["llama_releases.install"] = new Dictionary<string, string>
		{
			["en"] = "Install",
			["ru"] = "Установить"
		},
		["llama_releases.installing"] = new Dictionary<string, string>
		{
			["en"] = "Installing...",
			["ru"] = "Установка..."
		},
		["llama_releases.installed"] = new Dictionary<string, string>
		{
			["en"] = "✓ Installed",
			["ru"] = "✓ Установлено"
		},
		["llama_releases.use"] = new Dictionary<string, string>
		{
			["en"] = "Use",
			["ru"] = "Использовать"
		},
		["llama_releases.remove"] = new Dictionary<string, string>
		{
			["en"] = "Remove",
			["ru"] = "Удалить"
		},
		["llama_releases.active"] = new Dictionary<string, string>
		{
			["en"] = "Active",
			["ru"] = "Активна"
		},
		["llama_releases.latest"] = new Dictionary<string, string>
		{
			["en"] = "Latest",
			["ru"] = "Последняя"
		},
		["llama_releases.no_releases"] = new Dictionary<string, string>
		{
			["en"] = "No releases found",
			["ru"] = "Релизы не найдены"
		},
		["llama_releases.restart_server"] = new Dictionary<string, string>
		{
			["en"] = "Restarting server with version \"{0}\"...",
			["ru"] = "Перезапуск сервера с версией «{0}»..."
		},
		["llama_releases.cannot_delete_active"] = new Dictionary<string, string>
		{
			["en"] = "Cannot delete the active version",
			["ru"] = "Нельзя удалить активную версию"
		},
		["llama_releases.delete_failed_folder"] = new Dictionary<string, string>
		{
			["en"] = "Failed to delete {0}:\nFolder was not removed.\n\nCheck that files are not in use by another process.",
			["ru"] = "Не удалось удалить {0}:\nПапка не была удалена.\n\nПроверьте, что файлы не используются другим процессом."
		},
		["llama_releases.delete_failed_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to delete {0}:\n{1}\n\nCheck that files are not in use by another process.",
			["ru"] = "Не удалось удалить {0}:\n{1}\n\nПроверьте, что файлы не используются другим процессом."
		},
		["llama_releases.switch_version"] = new Dictionary<string, string>
		{
			["en"] = "Switch Version",
			["ru"] = "Сменить версию"
		},
		["llama_releases.tab_official"] = new Dictionary<string, string>
		{
			["en"] = "Official",
			["ru"] = "Официальные"
		},
		["llama_releases.tab_experimental"] = new Dictionary<string, string>
		{
			["en"] = "Experimental",
			["ru"] = "Экспериментальные"
		},
		["llama_releases.select_repo"] = new Dictionary<string, string>
		{
			["en"] = "Select repository:",
			["ru"] = "Выберите репозиторий:"
		},
		["llama_releases.select_release"] = new Dictionary<string, string>
		{
			["en"] = "Select release:",
			["ru"] = "Выберите релиз:"
		},
		["llama_releases.fetching_repo"] = new Dictionary<string, string>
		{
			["en"] = "Fetching releases...",
			["ru"] = "Загрузка релизов..."
		},
		["llama_releases.no_experimental_repos"] = new Dictionary<string, string>
		{
			["en"] = "No experimental repos configured",
			["ru"] = "Экспериментальные репозитории не настроены"
		},
		["llama_releases.no_assets"] = new Dictionary<string, string>
		{
			["en"] = "No compatible assets found",
			["ru"] = "Совместимые сборки не найдены"
		},
		["dash.update_version"] = new Dictionary<string, string>
		{
			["en"] = "Update ({0})",
			["ru"] = "Обновить ({0})"
		},
		["dash.not_installed"] = new Dictionary<string, string>
		{
			["en"] = "Not installed",
			["ru"] = "Не установлено"
		},
		["dash.no_model_selected"] = new Dictionary<string, string>
		{
			["en"] = "No model selected",
			["ru"] = "Модель не выбрана"
		},
		["dash.saved"] = new Dictionary<string, string>
		{
			["en"] = "saved",
			["ru"] = "сохранено"
		},
		["dash.default_badge"] = new Dictionary<string, string>
		{
			["en"] = "(default)",
			["ru"] = "(по умолчанию)"
		},
		["dialog.create_profile_title"] = new Dictionary<string, string>
		{
			["en"] = "Create Profile",
			["ru"] = "Создать профиль"
		},
		["dialog.profile_name_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Enter profile name:",
			["ru"] = "Введите имя профиля:"
		},
		["dialog.profile_exists"] = new Dictionary<string, string>
		{
			["en"] = "A profile with the name \"{0}\" already exists.",
			["ru"] = "Профиль с именем «{0}» уже существует."
		},
		["dialog.profile_created"] = new Dictionary<string, string>
		{
			["en"] = "Profile \"{0}\" created.",
			["ru"] = "Профиль «{0}» создан."
		},
		["dialog.profile_updated"] = new Dictionary<string, string>
		{
			["en"] = "Profile \"{0}\" updated.",
			["ru"] = "Профиль «{0}» обновлён."
		},
		["dialog.settings_saved"] = new Dictionary<string, string>
		{
			["en"] = "Settings saved to profile \"{0}\".",
			["ru"] = "Настройки сохранены в профиль «{0}»."
		},
		["dialog.confirm_delete_profile"] = new Dictionary<string, string>
		{
			["en"] = "Are you sure you want to delete profile \"{0}\"?",
			["ru"] = "Вы уверены, что хотите удалить профиль «{0}»?"
		},
		["dialog.delete_profile_title"] = new Dictionary<string, string>
		{
			["en"] = "Delete Profile",
			["ru"] = "Удалить профиль"
		},
		["dialog.select_profile_first"] = new Dictionary<string, string>
		{
			["en"] = "Select a profile first.",
			["ru"] = "Сначала выберите профиль."
		},
		["dialog.no_other_profiles"] = new Dictionary<string, string>
		{
			["en"] = "No other profiles to copy from.",
			["ru"] = "Нет других профилей для копирования."
		},
		["dialog.copy_settings_title"] = new Dictionary<string, string>
		{
			["en"] = "Copy Settings",
			["ru"] = "Копировать параметры"
		},
		["dialog.rename_title"] = new Dictionary<string, string>
		{
			["en"] = "Rename",
			["ru"] = "Переименовать"
		},
		["dialog.rename_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Enter new profile name:",
			["ru"] = "Введите новое имя профиля:"
		},
		["dialog.model_not_found"] = new Dictionary<string, string>
		{
			["en"] = "Model file not found:\n{0}",
			["ru"] = "Файл модели не найден:\n{0}"
		},
		["server.restart_profile"] = new Dictionary<string, string>
		{
			["en"] = "Restarting server with profile \"{0}\"...",
			["ru"] = "Перезапуск сервера с профилем «{0}»..."
		},
		["server.already_running"] = new Dictionary<string, string>
		{
			["en"] = "Server is already running!",
			["ru"] = "Сервер уже запущен!"
		},
		["server.start_failed"] = new Dictionary<string, string>
		{
			["en"] = "Server failed to start.\n\n{0}",
			["ru"] = "Не удалось запустить сервер.\n\n{0}"
		},
		["server.start_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to start server:\n{0}",
			["ru"] = "Ошибка запуска сервера:\n{0}"
		},
		["server.stop_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to stop server:\n{0}",
			["ru"] = "Ошибка остановки сервера:\n{0}"
		},
		["server.restart_failed"] = new Dictionary<string, string>
		{
			["en"] = "Server failed to restart.\n\n{0}",
			["ru"] = "Не удалось перезапустить сервер.\n\n{0}"
		},
		["server.restart_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to restart server:\n{0}",
			["ru"] = "Ошибка перезапуска сервера:\n{0}"
		},
		["server.unknown_error"] = new Dictionary<string, string>
		{
			["en"] = "Unknown error",
			["ru"] = "Неизвестная ошибка"
		},
		["server.no_profile_selected"] = new Dictionary<string, string>
		{
			["en"] = "No profile selected.",
			["ru"] = "Профиль не выбран."
		},
		["server.no_model_selected"] = new Dictionary<string, string>
		{
			["en"] = "No model selected.",
			["ru"] = "Модель не выбрана."
		},
		["server.exe_not_found"] = new Dictionary<string, string>
		{
			["en"] = "llama-server.exe not found: {0}",
			["ru"] = "llama-server.exe не найден: {0}"
		},
		["server.exe_not_found_short"] = new Dictionary<string, string>
		{
			["en"] = "llama-server.exe not found",
			["ru"] = "llama-server.exe не найден"
		},
		["server.unknown_version"] = new Dictionary<string, string>
		{
			["en"] = "Unknown version",
			["ru"] = "Неизвестная версия"
		},
		["server.version_error"] = new Dictionary<string, string>
		{
			["en"] = "Error checking version: {0}",
			["ru"] = "Ошибка проверки версии: {0}"
		},
		["server.installed_unknown"] = new Dictionary<string, string>
		{
			["en"] = "Installed (version unknown)",
			["ru"] = "Установлено (версия неизвестна)"
		},
		["models.download_complete"] = new Dictionary<string, string>
		{
			["en"] = "Download complete",
			["ru"] = "Загрузка завершена"
		},
		["models.hf_token_instructions"] = new Dictionary<string, string>
		{
			["en"] = "Open your browser, create or copy your HuggingFace token, then paste it below.",
			["ru"] = "Откройте браузер, создайте или скопируйте токен HuggingFace, затем вставьте его ниже."
		},
		["models.token_cleared"] = new Dictionary<string, string>
		{
			["en"] = "Auth token cleared",
			["ru"] = "Токен очищен"
		},
		["models.token_saved"] = new Dictionary<string, string>
		{
			["en"] = "Auth token saved",
			["ru"] = "Токен сохранён"
		},
		["models.select_dir"] = new Dictionary<string, string>
		{
			["en"] = "Select Download Directory",
			["ru"] = "Выберите папку загрузки"
		},
		["models.enter_repo"] = new Dictionary<string, string>
		{
			["en"] = "Enter a HuggingFace URL or repo ID",
			["ru"] = "Введите URL или ID репозитория HuggingFace"
		},
		["models.fetching_files"] = new Dictionary<string, string>
		{
			["en"] = "Fetching repository files...",
			["ru"] = "Получение файлов репозитория..."
		},
		["models.already_downloaded"] = new Dictionary<string, string>
		{
			["en"] = "Already downloaded",
			["ru"] = "Уже загружено"
		},
		["models.no_gguf"] = new Dictionary<string, string>
		{
			["en"] = "No .gguf files found",
			["ru"] = "Файлы .gguf не найдены"
		},
		["models.no_files_selected"] = new Dictionary<string, string>
		{
			["en"] = "No files selected",
			["ru"] = "Файлы не выбраны"
		},
		["models.select_dir_first"] = new Dictionary<string, string>
		{
			["en"] = "Select a valid download directory first",
			["ru"] = "Сначала выберите папку для загрузки"
		},
		["models.downloads_cancelled"] = new Dictionary<string, string>
		{
			["en"] = "Downloads cancelled",
			["ru"] = "Загрузки отменены"
		},
		["models.invalid_directory"] = new Dictionary<string, string>
		{
			["en"] = "Invalid directory",
			["ru"] = "Неверная папка"
		},
		["models.scanning"] = new Dictionary<string, string>
		{
			["en"] = "Scanning...",
			["ru"] = "Сканирование..."
		},
		["models.select_models_dir"] = new Dictionary<string, string>
		{
			["en"] = "Select Models Directory",
			["ru"] = "Выберите папку моделей"
		},
		["models.enter_repo_first"] = new Dictionary<string, string>
		{
			["en"] = "Enter a model repo first",
			["ru"] = "Сначала введите репозиторий модели"
		},
		["models.save_token"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udcbe Save Token",
			["ru"] = "\ud83d\udcbe Сохранить токен"
		},
		["models.hf_download"] = new Dictionary<string, string>
		{
			["en"] = "HuggingFace Download",
			["ru"] = "Загрузка из HuggingFace"
		},
		["apitest.checking"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["apitest.status"] = new Dictionary<string, string>
		{
			["en"] = "Status",
			["ru"] = "Статус"
		},
		["apitest.error"] = new Dictionary<string, string>
		{
			["en"] = "ERROR",
			["ru"] = "ОШИБКА"
		},
		["apitest.fetching_models"] = new Dictionary<string, string>
		{
			["en"] = "Fetching models...",
			["ru"] = "Получение списка моделей..."
		},
		["apitest.sending_chat"] = new Dictionary<string, string>
		{
			["en"] = "Sending chat request...",
			["ru"] = "Отправка запроса чата..."
		},
		["apitest.sending_completion"] = new Dictionary<string, string>
		{
			["en"] = "Sending completion request...",
			["ru"] = "Отправка запроса завершения..."
		},
		["apitest.empty_path"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: Path is empty",
			["ru"] = "ОШИБКА: Путь пуст"
		},
		["apitest.sending_custom"] = new Dictionary<string, string>
		{
			["en"] = "Sending custom request...",
			["ru"] = "Отправка пользовательского запроса..."
		},
		["apitest.unsupported_method"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: Unsupported method: {0}",
			["ru"] = "ОШИБКА: Неподдерживаемый метод: {0}"
		},
		["apitest.suite_header"] = new Dictionary<string, string>
		{
			["en"] = "=== API Test Suite ===",
			["ru"] = "=== Набор тестов API ==="
		},
		["apitest.base_url"] = new Dictionary<string, string>
		{
			["en"] = "Base URL",
			["ru"] = "Базовый URL"
		},
		["apitest.time"] = new Dictionary<string, string>
		{
			["en"] = "Time",
			["ru"] = "Время"
		},
		["apitest.health_section"] = new Dictionary<string, string>
		{
			["en"] = "--- Health Check ---",
			["ru"] = "--- Проверка состояния ---"
		},
		["apitest.models_section"] = new Dictionary<string, string>
		{
			["en"] = "--- Models List ---",
			["ru"] = "--- Список моделей ---"
		},
		["dialog.ok"] = new Dictionary<string, string>
		{
			["en"] = "OK",
			["ru"] = "ОК"
		},
		["dialog.cancel"] = new Dictionary<string, string>
		{
			["en"] = "Cancel",
			["ru"] = "Отмена"
		},
		["dialog.yes"] = new Dictionary<string, string>
		{
			["en"] = "Yes",
			["ru"] = "Да"
		},
		["dialog.no"] = new Dictionary<string, string>
		{
			["en"] = "No",
			["ru"] = "Нет"
		},
		["dialog.information"] = new Dictionary<string, string>
		{
			["en"] = "Information",
			["ru"] = "Информация"
		},
		["dialog.confirmation"] = new Dictionary<string, string>
		{
			["en"] = "Confirmation",
			["ru"] = "Подтверждение"
		},
		["dialog.error"] = new Dictionary<string, string>
		{
			["en"] = "Error",
			["ru"] = "Ошибка"
		},
		["dialog.success"] = new Dictionary<string, string>
		{
			["en"] = "Success",
			["ru"] = "Успешно"
		},
		["picker.save_chat"] = new Dictionary<string, string>
		{
			["en"] = "Save chat session",
			["ru"] = "Сохранить сессию чата"
		},
		["picker.select_images"] = new Dictionary<string, string>
		{
			["en"] = "Select Images",
			["ru"] = "Выбрать изображения"
		},
		["picker.select_files"] = new Dictionary<string, string>
		{
			["en"] = "Select Files",
			["ru"] = "Выбрать файлы"
		},
		["chat.add_mcp_server"] = new Dictionary<string, string>
		{
			["en"] = "Add MCP Server",
			["ru"] = "Добавить MCP-сервер"
		},
		["chat.mcp_name"] = new Dictionary<string, string>
		{
			["en"] = "Name:",
			["ru"] = "Имя:"
		},
		["chat.mcp_type"] = new Dictionary<string, string>
		{
			["en"] = "Type:",
			["ru"] = "Тип:"
		},
		["chat.mcp_command"] = new Dictionary<string, string>
		{
			["en"] = "Command:",
			["ru"] = "Команда:"
		},
		["chat.mcp_args"] = new Dictionary<string, string>
		{
			["en"] = "Arguments:",
			["ru"] = "Аргументы:"
		},
		["chat.mcp_url"] = new Dictionary<string, string>
		{
			["en"] = "URL:",
			["ru"] = "URL:"
		},
		["chat.mcp_add"] = new Dictionary<string, string>
		{
			["en"] = "Add",
			["ru"] = "Добавить"
		},
		["chat.mcp_blocked_command"] = new Dictionary<string, string>
		{
			["en"] = "This command is not allowed for security reasons.",
			["ru"] = "Эта команда запрещена по соображениям безопасности."
		},
		["chat.mcp_https_required"] = new Dictionary<string, string>
		{
			["en"] = "HTTPS URL is required for SSE transport.",
			["ru"] = "Для SSE транспорта требуется HTTPS URL."
		},
		["chat.tools_count"] = new Dictionary<string, string>
		{
			["en"] = "tools",
			["ru"] = "инструментов"
		},
		["chat.experimental"] = new Dictionary<string, string>
		{
			["en"] = "⚠ Experimental feature — errors may occur. The chat is still being refined.",
			["ru"] = "⚠ Экспериментальная функция — возможны ошибки. Чат находится в стадии доработки."
		},
		["chat.remote_server_warning"] = new Dictionary<string, string>
		{
			["en"] = "You are connecting to a remote server ({0}) over HTTP. Data is sent unencrypted. Consider using HTTPS for remote servers.",
			["ru"] = "Вы подключаетесь к удалённому серверу ({0}) через HTTP. Данные передаются без шифрования. Для удалённых серверов рекомендуется использовать HTTPS."
		},
		["chat.remote_server_warning_title"] = new Dictionary<string, string>
		{
			["en"] = "Remote Server Warning",
			["ru"] = "Предупреждение: удалённый сервер"
		},
		["chat.welcome"] = new Dictionary<string, string>
		{
			["en"] = "Hello!",
			["ru"] = "Привет!"
		},
		["chat.welcome_sub"] = new Dictionary<string, string>
		{
			["en"] = "Enter a message or load files to get started",
			["ru"] = "Введите сообщение или загрузите файлы, чтобы начать работу"
		},
		["chat.streaming"] = new Dictionary<string, string>
		{
			["en"] = "(running...)",
			["ru"] = "(выполняется...)"
		},
		["chat.input_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Type a message...",
			["ru"] = "Введите сообщение..."
		},
		["chat.cut"] = new Dictionary<string, string>
		{
			["en"] = "Cut",
			["ru"] = "Вырезать"
		},
		["chat.copy"] = new Dictionary<string, string>
		{
			["en"] = "Copy",
			["ru"] = "Копировать"
		},
		["chat.paste"] = new Dictionary<string, string>
		{
			["en"] = "Paste",
			["ru"] = "Вставить"
		},
		["chat.select_all"] = new Dictionary<string, string>
		{
			["en"] = "Select All",
			["ru"] = "Выбрать всё"
		},
		["monitoring.tok_per_sec"] = new Dictionary<string, string>
		{
			["en"] = "tok/s",
			["ru"] = "ток/с"
		},
		["tray.tooltip"] = new Dictionary<string, string>
		{
			["en"] = "Llama Studio — llama.cpp Manager",
			["ru"] = "Llama Studio — менеджер llama.cpp"
		},
		["tray.no_profiles"] = new Dictionary<string, string>
		{
			["en"] = "(no profiles)",
			["ru"] = "(нет профилей)"
		},
		["tray.monitoring"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udcca Monitoring",
			["ru"] = "\ud83d\udcca Мониторинг"
		},
		["server.exec_found"] = new Dictionary<string, string>
		{
			["en"] = "Executable found: {0}",
			["ru"] = "Исполняемый файл найден: {0}"
		},
		["server.exec_not_found"] = new Dictionary<string, string>
		{
			["en"] = "Executable not found",
			["ru"] = "Исполняемый файл не найден"
		},
		["mon.title"] = new Dictionary<string, string>
		{
			["en"] = "Monitoring",
			["ru"] = "Мониторинг"
		},
		["mon.server_controls"] = new Dictionary<string, string>
		{
			["en"] = "Server Controls",
			["ru"] = "Управление сервером"
		},
		["mon.model_name"] = new Dictionary<string, string>
		{
			["en"] = "Model",
			["ru"] = "Модель"
		},
		["mon.start"] = new Dictionary<string, string>
		{
			["en"] = "Start",
			["ru"] = "Запустить"
		},
		["mon.stop"] = new Dictionary<string, string>
		{
			["en"] = "Stop",
			["ru"] = "Остановить"
		},
		["mon.restart"] = new Dictionary<string, string>
		{
			["en"] = "Restart",
			["ru"] = "Перезапуск"
		},
		["mon.tps"] = new Dictionary<string, string>
		{
			["en"] = "Response",
			["ru"] = "Ответ"
		},
		["mon.prompt_tps"] = new Dictionary<string, string>
		{
			["en"] = "Prompt",
			["ru"] = "Промпт"
		},
		["mon.gen_label"] = new Dictionary<string, string>
		{
			["en"] = "Response",
			["ru"] = "Ответ"
		},
		["mon.vram"] = new Dictionary<string, string>
		{
			["en"] = "VRAM",
			["ru"] = "VRAM"
		},
		["mon.ram"] = new Dictionary<string, string>
		{
			["en"] = "RAM",
			["ru"] = "ОЗУ"
		},
		["mon.gpu_temp"] = new Dictionary<string, string>
		{
			["en"] = "GPU Temp",
			["ru"] = "Темп. GPU"
		},
		["mon.mem_temperature"] = new Dictionary<string, string>
		{
			["en"] = "GPU Memory Temp",
			["ru"] = "Темп. памяти GPU"
		},
		["mon.power"] = new Dictionary<string, string>
		{
			["en"] = "Power",
			["ru"] = "Мощность"
		},
		["mon.all_gpus"] = new Dictionary<string, string>
		{
			["en"] = "All GPUs",
			["ru"] = "Все GPU"
		},
		["mon.gpu_load"] = new Dictionary<string, string>
		{
			["en"] = "GPU Load",
			["ru"] = "Загрузка GPU"
		},
		["mon.total_gpu_power"] = new Dictionary<string, string>
		{
			["en"] = "Total GPU Power",
			["ru"] = "Мощность всех GPU"
		},
		["mon.clock"] = new Dictionary<string, string>
		{
			["en"] = "Clock",
			["ru"] = "Частота"
		},
		["mon.compute_capability"] = new Dictionary<string, string>
		{
			["en"] = "Compute Capability",
			["ru"] = "Вычислительная способность"
		},
		["mon.gpu_monitoring"] = new Dictionary<string, string>
		{
			["en"] = "GPU Monitoring",
			["ru"] = "Мониторинг GPU"
		},
		["mon.gpu_core"] = new Dictionary<string, string>
		{
			["en"] = "GPU Core",
			["ru"] = "Ядро GPU"
		},
		["mon.fan"] = new Dictionary<string, string>
		{
			["en"] = "Fan",
			["ru"] = "Вентилятор"
		},
		["mon.settings_title"] = new Dictionary<string, string>
		{
			["en"] = "Display Settings",
			["ru"] = "Настройки отображения"
		},
		["mon.opacity"] = new Dictionary<string, string>
		{
			["en"] = "Opacity",
			["ru"] = "Прозрачность"
		},
		["mon.always_on_top"] = new Dictionary<string, string>
		{
			["en"] = "Always on Top",
			["ru"] = "Всегда сверху"
		},
		["mon.visible_metrics"] = new Dictionary<string, string>
		{
			["en"] = "Visible metrics:",
			["ru"] = "Видимые метрики:"
		},
		["mon.style_title"] = new Dictionary<string, string>
		{
			["en"] = "Display Style",
			["ru"] = "Стиль отображения"
		},
		["mon.style_bars"] = new Dictionary<string, string>
		{
			["en"] = "Bars",
			["ru"] = "Полосы"
		},
		["mon.style_circles"] = new Dictionary<string, string>
		{
			["en"] = "Circles",
			["ru"] = "Круги"
		},
		["mon.style_compact"] = new Dictionary<string, string>
		{
			["en"] = "Compact",
			["ru"] = "Компактный"
		},
		["mon.open_floating"] = new Dictionary<string, string>
		{
			["en"] = "Open Floating Window",
			["ru"] = "Плавающее окно"
		},
		["mon.monitoring_on"] = new Dictionary<string, string>
		{
			["en"] = "Monitoring: ON",
			["ru"] = "Мониторинг: ВКЛ"
		},
		["mon.monitoring_off"] = new Dictionary<string, string>
		{
			["en"] = "Monitoring: OFF",
			["ru"] = "Мониторинг: ВЫКЛ"
		},
		["mon.check_speed"] = new Dictionary<string, string>
		{
			["en"] = "Check Speed",
			["ru"] = "Проверить скорость"
		},
		["mon.check_speed_running"] = new Dictionary<string, string>
		{
			["en"] = "Testing...",
			["ru"] = "Тестирование..."
		},
		["mon.benchmark_running"] = new Dictionary<string, string>
		{
			["en"] = "Testing speed...",
			["ru"] = "Тестирую скорость..."
		},
		["mon.benchmark_complete"] = new Dictionary<string, string>
		{
			["en"] = "Speed Test Results:",
			["ru"] = "Результаты теста скорости:"
		},
		["mon.benchmark_no_profile"] = new Dictionary<string, string>
		{
			["en"] = "No active profile",
			["ru"] = "Нет активного профиля"
		},
		["mon.benchmark_error"] = new Dictionary<string, string>
		{
			["en"] = "Error",
			["ru"] = "Ошибка"
		},
		["mon.benchmark_server_loading"] = new Dictionary<string, string>
		{
			["en"] = "Server is loading model. Please wait and try again.",
			["ru"] = "Сервер загружает модель. Подождите и попробуйте снова."
		},
		["mon.benchmark_tokens"] = new Dictionary<string, string>
		{
			["en"] = "Tokens generated",
			["ru"] = "Токенов сгенерировано"
		},
		["mon.benchmark_speed"] = new Dictionary<string, string>
		{
			["en"] = "Generation speed",
			["ru"] = "Скорость генерации"
		},
		["mon.benchmark_time_per_token"] = new Dictionary<string, string>
		{
			["en"] = "Time per token",
			["ru"] = "Время на токен"
		},
		["mon.benchmark_total_time"] = new Dictionary<string, string>
		{
			["en"] = "Total time",
			["ru"] = "Общее время"
		},
		["mon.benchmark_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Prompt Processing",
			["ru"] = "Обработка промпта"
		},
		["mon.benchmark_generation"] = new Dictionary<string, string>
		{
			["en"] = "Generation",
			["ru"] = "Генерация"
		},
		["mon.benchmark_prompt_tokens"] = new Dictionary<string, string>
		{
			["en"] = "Prompt tokens",
			["ru"] = "Токенов промпта"
		},
		["mon.benchmark_prompt_speed"] = new Dictionary<string, string>
		{
			["en"] = "Prompt speed",
			["ru"] = "Скорость промпта"
		},
		["mon.benchmark_prompt_time"] = new Dictionary<string, string>
		{
			["en"] = "Prompt time",
			["ru"] = "Время обработки"
		},
		["mon.benchmark_gen_time"] = new Dictionary<string, string>
		{
			["en"] = "Generation time",
			["ru"] = "Время генерации"
		},
		["dash.title"] = new Dictionary<string, string>
		{
			["en"] = "Dashboard",
			["ru"] = "Главная"
		},
		["dash.server_label"] = new Dictionary<string, string>
		{
			["en"] = "Server",
			["ru"] = "Сервер"
		},
		["dash.uptime"] = new Dictionary<string, string>
		{
			["en"] = "Uptime: {0}",
			["ru"] = "Работает: {0}"
		},
		["dash.models_card"] = new Dictionary<string, string>
		{
			["en"] = "Models",
			["ru"] = "Модели"
		},
		["dash.indexed"] = new Dictionary<string, string>
		{
			["en"] = "Indexed",
			["ru"] = "Найдено"
		},
		["dash.profiles_card"] = new Dictionary<string, string>
		{
			["en"] = "Profiles",
			["ru"] = "Профили"
		},
		["dash.llama_cpp"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp",
			["ru"] = "llama.cpp"
		},
		["dash.active"] = new Dictionary<string, string>
		{
			["en"] = "Active",
			["ru"] = "Активна"
		},
		["dash.vram_usage"] = new Dictionary<string, string>
		{
			["en"] = "VRAM Usage",
			["ru"] = "Использование VRAM"
		},
		["dash.gpu_memory"] = new Dictionary<string, string>
		{
			["en"] = "GPU Memory",
			["ru"] = "Память GPU"
		},
		["dash.active_model"] = new Dictionary<string, string>
		{
			["en"] = "Active Model",
			["ru"] = "Активная модель"
		},
		["dash.quick_actions"] = new Dictionary<string, string>
		{
			["en"] = "Quick Actions",
			["ru"] = "Быстрые действия"
		},
		["dash.scan_models"] = new Dictionary<string, string>
		{
			["en"] = "Scan Models",
			["ru"] = "Сканировать модели"
		},
		["dash.discover_gguf"] = new Dictionary<string, string>
		{
			["en"] = "Discover GGUF files",
			["ru"] = "Найти файлы GGUF"
		},
		["dash.create_profile"] = new Dictionary<string, string>
		{
			["en"] = "Create Profile",
			["ru"] = "Создать профиль"
		},
		["dash.launch_config"] = new Dictionary<string, string>
		{
			["en"] = "Launch config",
			["ru"] = "Конфиг запуска"
		},
		["dash.check_updates"] = new Dictionary<string, string>
		{
			["en"] = "Check Updates",
			["ru"] = "Проверить обновления"
		},
		["dash.llama_version"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp version",
			["ru"] = "Версия llama.cpp"
		},
		["dash.up_to_date"] = new Dictionary<string, string>
		{
			["en"] = "Up to date",
			["ru"] = "Версия актуальна"
		},
		["dash.update_available"] = new Dictionary<string, string>
		{
			["en"] = "Update available",
			["ru"] = "Доступно обновление"
		},
		["dash.checking_version"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["dash.switching_version"] = new Dictionary<string, string>
		{
			["en"] = "Switching to {0}... Restarting server",
			["ru"] = "Переключение на {0}... Перезапуск сервера"
		},
		["dash.restart_server"] = new Dictionary<string, string>
		{
			["en"] = "Restarting Server",
			["ru"] = "Перезапуск сервера"
		},
		["dash.update"] = new Dictionary<string, string>
		{
			["en"] = "Update",
			["ru"] = "Обновить"
		},
		["dash.updating"] = new Dictionary<string, string>
		{
			["en"] = "Updating",
			["ru"] = "Обновление"
		},
		["dash.update_installed"] = new Dictionary<string, string>
		{
			["en"] = "Update Installed",
			["ru"] = "Обновление установлено"
		},
		["dash.update_installed_msg"] = new Dictionary<string, string>
		{
			["en"] = "Version {0} has been installed. Start the server to use it.",
			["ru"] = "Версия {0} установлена. Запустите сервер для использования."
		},
		["dash.update_restart_server"] = new Dictionary<string, string>
		{
			["en"] = "Version {0} has been installed. Restart the server now?",
			["ru"] = "Версия {0} установлена. Перезапустить сервер сейчас?"
		},
		["dash.server_restarted"] = new Dictionary<string, string>
		{
			["en"] = "Server restarted with new version",
			["ru"] = "Сервер перезапущен с новой версией"
		},
		["dash.restart_failed"] = new Dictionary<string, string>
		{
			["en"] = "Failed to restart server",
			["ru"] = "Не удалось перезапустить сервер"
		},
		["dash.success"] = new Dictionary<string, string>
		{
			["en"] = "Success",
			["ru"] = "Успешно"
		},
		["dash.error"] = new Dictionary<string, string>
		{
			["en"] = "Error",
			["ru"] = "Ошибка"
		},
		["dash.app_version"] = new Dictionary<string, string>
		{
			["en"] = "Llama Studio",
			["ru"] = "Llama Studio"
		},
		["dash.new_version"] = new Dictionary<string, string>
		{
			["en"] = "New version",
			["ru"] = "Новая версия"
		},
		["dash.downloading"] = new Dictionary<string, string>
		{
			["en"] = "Downloading",
			["ru"] = "Загрузка"
		},
		["dash.checking_update"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["dash.gpu_monitor"] = new Dictionary<string, string>
		{
			["en"] = "GPU Monitor",
			["ru"] = "Использование GPU"
		},
		["dash.live"] = new Dictionary<string, string>
		{
			["en"] = "Live",
			["ru"] = "Live"
		},
		["dash.vram_label"] = new Dictionary<string, string>
		{
			["en"] = "VRAM Usage",
			["ru"] = "VRAM Usage"
		},
		["dash.gpu_core"] = new Dictionary<string, string>
		{
			["en"] = "GPU Core",
			["ru"] = "GPU Core"
		},
		["dash.power_draw"] = new Dictionary<string, string>
		{
			["en"] = "Power Draw",
			["ru"] = "Power Draw"
		},
		["dash.fan_speed"] = new Dictionary<string, string>
		{
			["en"] = "Fan Speed",
			["ru"] = "Fan Speed"
		},
		["dash.clock"] = new Dictionary<string, string>
		{
			["en"] = "Clock",
			["ru"] = "Clock"
		},
		["dash.model"] = new Dictionary<string, string>
		{
			["en"] = "Model",
			["ru"] = "Model"
		},
		["dash.throughput"] = new Dictionary<string, string>
		{
			["en"] = "Tokens/sec",
			["ru"] = "Токенов/сек"
		},
		["dash.prompt_throughput"] = new Dictionary<string, string>
		{
			["en"] = "Prompt tok/s",
			["ru"] = "Промпт ток/с"
		},
		["dash.gpu_temperature"] = new Dictionary<string, string>
		{
			["en"] = "GPU Temperature",
			["ru"] = "Температура GPU"
		},
		["dash.ram_usage"] = new Dictionary<string, string>
		{
			["en"] = "RAM Usage",
			["ru"] = "Загрузка ОЗУ"
		},
		["dash.mem_temperature"] = new Dictionary<string, string>
		{
			["en"] = "GPU Memory Temperature",
			["ru"] = "Температура памяти GPU"
		},
		["dash.profile"] = new Dictionary<string, string>
		{
			["en"] = "Profile",
			["ru"] = "Профиль"
		},
		["dash.no_profile"] = new Dictionary<string, string>
		{
			["en"] = "No profile",
			["ru"] = "Нет профиля"
		},
		["dash.main_model"] = new Dictionary<string, string>
		{
			["en"] = "Main Model",
			["ru"] = "Основная модель"
		},
		["dash.mmproj"] = new Dictionary<string, string>
		{
			["en"] = "Vision (mmproj)",
			["ru"] = "Зрение (mmproj)"
		},
		["dash.draft_model"] = new Dictionary<string, string>
		{
			["en"] = "Draft Model",
			["ru"] = "Черновик"
		},
		["dash.context"] = new Dictionary<string, string>
		{
			["en"] = "Context",
			["ru"] = "Контекст"
		},
		["dash.temperature_label"] = new Dictionary<string, string>
		{
			["en"] = "Temperature",
			["ru"] = "Температура"
		},
		["dash.gpu_layers"] = new Dictionary<string, string>
		{
			["en"] = "GPU Layers",
			["ru"] = "Слои GPU"
		},
		["dash.threads"] = new Dictionary<string, string>
		{
			["en"] = "Threads",
			["ru"] = "Потоки"
		},
		["dash.mtp"] = new Dictionary<string, string>
		{
			["en"] = "MTP",
			["ru"] = "MTP"
		},
		["dash.reasoning"] = new Dictionary<string, string>
		{
			["en"] = "Reasoning",
			["ru"] = "Рассуждение"
		},
		["dash.flash_attn"] = new Dictionary<string, string>
		{
			["en"] = "Flash Attention",
			["ru"] = "Flash Attention"
		},
		["dash.cache_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Cache Prompt",
			["ru"] = "Кэш промпта"
		},
		["dash.cont_batching"] = new Dictionary<string, string>
		{
			["en"] = "Cont. Batching",
			["ru"] = "Непрерывный батч"
		},
		["dash.select_profile"] = new Dictionary<string, string>
		{
			["en"] = "Select Profile",
			["ru"] = "Выбрать профиль"
		},
		["dash.default"] = new Dictionary<string, string>
		{
			["en"] = "default",
			["ru"] = "по умолчанию"
		},
		["dash.set_default"] = new Dictionary<string, string>
		{
			["en"] = "Set Default",
			["ru"] = "Сделать дефолтным"
		},
		["dash.set_default_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Set Default",
			["ru"] = "Сделать дефолтным"
		},
		["models.title"] = new Dictionary<string, string>
		{
			["en"] = "Models",
			["ru"] = "Модели"
		},
		["models.count"] = new Dictionary<string, string>
		{
			["en"] = "models",
			["ru"] = "моделей"
		},
		["models.scan"] = new Dictionary<string, string>
		{
			["en"] = "Scan Models",
			["ru"] = "Сканировать"
		},
		["models.browse"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["models.search"] = new Dictionary<string, string>
		{
			["en"] = "Search models...",
			["ru"] = "Поиск моделей..."
		},
		["models.no_models"] = new Dictionary<string, string>
		{
			["en"] = "No models found. Set a models directory and click Scan.",
			["ru"] = "Модели не найдены. Укажите папку и нажмите Сканировать."
		},
		["models.use_default_profile"] = new Dictionary<string, string>
		{
			["en"] = "Use in Default Profile",
			["ru"] = "В профиль по умолчанию"
		},
		["models.to_server"] = new Dictionary<string, string>
		{
			["en"] = "→ Server Model",
			["ru"] = "→ Модель сервера"
		},
		["models.to_mmproj"] = new Dictionary<string, string>
		{
			["en"] = "→ Server mmproj",
			["ru"] = "→ Сервер mmproj"
		},
		["models.to_draft"] = new Dictionary<string, string>
		{
			["en"] = "→ Server Draft",
			["ru"] = "→ Черновик сервера"
		},
		["models.connected"] = new Dictionary<string, string>
		{
			["en"] = "✓ Connected",
			["ru"] = "✓ Подключено"
		},
		["hf.title"] = new Dictionary<string, string>
		{
			["en"] = "HuggingFace Download",
			["ru"] = "Загрузка с HuggingFace"
		},
		["hf.url_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "HuggingFace repo URL or ID (e.g. unsloth/Qwen3.6-27B-MTP-GGUF)",
			["ru"] = "URL или ID репозитория HuggingFace (например unsloth/Qwen3.6-27B-MTP-GGUF)"
		},
		["hf.auth_token_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "HF auth token (for private repos)",
			["ru"] = "Токен авторизации HF (для приватных репозиториев)"
		},
		["hf.browse"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["hf.fetch"] = new Dictionary<string, string>
		{
			["en"] = "Fetch",
			["ru"] = "Загрузить список"
		},
		["hf.download_selected"] = new Dictionary<string, string>
		{
			["en"] = "Download Selected",
			["ru"] = "Скачать выбранные"
		},
		["hf.cancel"] = new Dictionary<string, string>
		{
			["en"] = "Cancel",
			["ru"] = "Отмена"
		},
		["hf.select_all"] = new Dictionary<string, string>
		{
			["en"] = "Select All",
			["ru"] = "Выбрать все"
		},
		["hf.deselect_all"] = new Dictionary<string, string>
		{
			["en"] = "Deselect All",
			["ru"] = "Снять выбор"
		},
		["hf.login"] = new Dictionary<string, string>
		{
			["en"] = "Login on HF",
			["ru"] = "Войти на HF"
		},
		["hf.select_dir"] = new Dictionary<string, string>
		{
			["en"] = "Download to:",
			["ru"] = "Скачать в:"
		},
		["hf.files_found"] = new Dictionary<string, string>
		{
			["en"] = "files found",
			["ru"] = "файлов найдено"
		},
		["hf.search_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "Type a name (e.g. MTP or Qwen MTP), pick from list — model auto-loads for download",
			["ru"] = "Введите название (например MTP или Qwen MTP), выбирайте из списка, модель автоматически загрузится для скачивания"
		},
		["profiles.title"] = new Dictionary<string, string>
		{
			["en"] = "Launch Profiles",
			["ru"] = "Профили запуска"
		},
		["profiles.new"] = new Dictionary<string, string>
		{
			["en"] = "+ New Profile",
			["ru"] = "+ Новый профиль"
		},
		["profiles.import"] = new Dictionary<string, string>
		{
			["en"] = "Import JSON",
			["ru"] = "Импорт JSON"
		},
		["profiles.name_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "Profile name...",
			["ru"] = "Имя профиля..."
		},
		["profiles.desc_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "Description...",
			["ru"] = "Описание..."
		},
		["profiles.select_model"] = new Dictionary<string, string>
		{
			["en"] = "Select Model...",
			["ru"] = "Выбрать модель..."
		},
		["profiles.default_badge"] = new Dictionary<string, string>
		{
			["en"] = "DEFAULT",
			["ru"] = "ПО УМОЛЧАНИЮ"
		},
		["profiles.set_default"] = new Dictionary<string, string>
		{
			["en"] = "Set Default",
			["ru"] = "По умолчанию"
		},
		["profiles.edit"] = new Dictionary<string, string>
		{
			["en"] = "Edit",
			["ru"] = "Изменить"
		},
		["profiles.duplicate"] = new Dictionary<string, string>
		{
			["en"] = "Duplicate",
			["ru"] = "Дублировать"
		},
		["profiles.export"] = new Dictionary<string, string>
		{
			["en"] = "Export",
			["ru"] = "Экспорт"
		},
		["profiles.delete"] = new Dictionary<string, string>
		{
			["en"] = "Delete",
			["ru"] = "Удалить"
		},
		["profiles.save_settings"] = new Dictionary<string, string>
		{
			["en"] = "Save Settings",
			["ru"] = "Сохранить настройки"
		},
		["profiles.start_server"] = new Dictionary<string, string>
		{
			["en"] = "Start Server",
			["ru"] = "Запустить сервер"
		},
		["settings.title"] = new Dictionary<string, string>
		{
			["en"] = "Settings",
			["ru"] = "Настройки"
		},
		["settings.general_section"] = new Dictionary<string, string>
		{
			["en"] = "General",
			["ru"] = "Общие"
		},
		["settings.paths_section"] = new Dictionary<string, string>
		{
			["en"] = "Paths",
			["ru"] = "Пути"
		},
		["settings.llama_cpp_base_dir_label"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp Base Directory:",
			["ru"] = "Корневая папка llama.cpp:"
		},
		["settings.llama_cpp_dir_label"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp:",
			["ru"] = "llama.cpp:"
		},
		["settings.models_dir_label"] = new Dictionary<string, string>
		{
			["en"] = "Models:",
			["ru"] = "Модели:"
		},
		["settings.browse_btn"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["settings.auto_check_updates"] = new Dictionary<string, string>
		{
			["en"] = "Auto-check updates",
			["ru"] = "Автопроверка обновлений"
		},
		["settings.minimize_to_tray"] = new Dictionary<string, string>
		{
			["en"] = "Minimize to tray on close",
			["ru"] = "Сворачивать в трей при закрытии"
		},
		["settings.auto_start_with_os"] = new Dictionary<string, string>
		{
			["en"] = "Auto-start with system",
			["ru"] = "Автозапуск с системой"
		},
		["settings.auto_start_server"] = new Dictionary<string, string>
		{
			["en"] = "Start server on app launch",
			["ru"] = "Запускать сервер при запуске приложения"
		},
		["settings.start_minimized"] = new Dictionary<string, string>
		{
			["en"] = "Start minimized to tray",
			["ru"] = "Запускать свёрнутым в трей"
		},
		["settings.start_monitoring_window"] = new Dictionary<string, string>
		{
			["en"] = "Start monitoring window",
			["ru"] = "Запускать окно мониторинга"
		},

		["settings.show_desktop_window"] = new Dictionary<string, string>
		{
			["en"] = "Show desktop window",
			["ru"] = "Показывать окно программы"
		},
		["settings.keep_server_on_exit"] = new Dictionary<string, string>
		{
			["en"] = "Keep server running on exit",
			["ru"] = "Не выгружать сервер при закрытии"
		},
		["settings.clear_clipboard_on_exit"] = new Dictionary<string, string>
		{
			["en"] = "Clear clipboard on exit",
			["ru"] = "Очистить буфер обмена при выходе"
		},
		["settings.portable_mode"] = new Dictionary<string, string>
		{
			["en"] = "Portable mode",
			["ru"] = "Портативный режим"
		},
		["settings.theme_label"] = new Dictionary<string, string>
		{
			["en"] = "Theme:",
			["ru"] = "Тема:"
		},
		["settings.dark"] = new Dictionary<string, string>
		{
			["en"] = "Dark",
			["ru"] = "Тёмная"
		},
		["settings.light"] = new Dictionary<string, string>
		{
			["en"] = "Light",
			["ru"] = "Светлая"
		},
		["settings.system"] = new Dictionary<string, string>
		{
			["en"] = "System",
			["ru"] = "Системная"
		},
		["settings.active_version"] = new Dictionary<string, string>
		{
			["en"] = "Active version:",
			["ru"] = "Активная версия:"
		},
		["settings.update_channel_label"] = new Dictionary<string, string>
		{
			["en"] = "Update channel:",
			["ru"] = "Канал обновлений:"
		},
		["settings.stable"] = new Dictionary<string, string>
		{
			["en"] = "Stable",
			["ru"] = "Стабильная"
		},
		["settings.prerelease"] = new Dictionary<string, string>
		{
			["en"] = "PreRelease",
			["ru"] = "Предварительная"
		},
		["settings.nightly"] = new Dictionary<string, string>
		{
			["en"] = "Nightly",
			["ru"] = "Ночная сборка"
		},
		["settings.server_defaults_section"] = new Dictionary<string, string>
		{
			["en"] = "Server Defaults",
			["ru"] = "Параметры сервера по умолчанию"
		},
		["settings.host_label"] = new Dictionary<string, string>
		{
			["en"] = "Default host:",
			["ru"] = "Хост по умолч.:"
		},
		["settings.port_label"] = new Dictionary<string, string>
		{
			["en"] = "Default port:",
			["ru"] = "Порт по умолч.:"
		},
		["settings.gpu_layers_label"] = new Dictionary<string, string>
		{
			["en"] = "Default GPU layers:",
			["ru"] = "Слои GPU по умолч.:"
		},
		["settings.flash_attention"] = new Dictionary<string, string>
		{
			["en"] = "Enable Flash Attention by default",
			["ru"] = "Flash Attention по умолчанию"
		},
		["settings.releases"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp Releases",
			["ru"] = "Релизы llama.cpp"
		},
		["settings.check_updates_btn"] = new Dictionary<string, string>
		{
			["en"] = "Check for Updates",
			["ru"] = "Проверить обновления"
		},
		["settings.checking_status"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["settings.download"] = new Dictionary<string, string>
		{
			["en"] = "Download",
			["ru"] = "Скачать"
		},
		["settings.save_settings_btn"] = new Dictionary<string, string>
		{
			["en"] = "Save Settings",
			["ru"] = "Сохранить настройки"
		},
		["settings.published"] = new Dictionary<string, string>
		{
			["en"] = "Published: {0}",
			["ru"] = "Дата: {0}"
		},
		["settings.language_label"] = new Dictionary<string, string>
		{
			["en"] = "Language:",
			["ru"] = "Язык:"
		},
		["settings.russian"] = new Dictionary<string, string>
		{
			["en"] = "Русский",
			["ru"] = "Русский"
		},
		["settings.english"] = new Dictionary<string, string>
		{
			["en"] = "English",
			["ru"] = "English"
		},
		["settings.select_llama_base_dir"] = new Dictionary<string, string>
		{
			["en"] = "Select llama.cpp Base Directory",
			["ru"] = "Выберите корневую папку llama.cpp"
		},
		["settings.select_llama_dir"] = new Dictionary<string, string>
		{
			["en"] = "Select llama.cpp Directory",
			["ru"] = "Выберите папку llama.cpp"
		},
		["settings.not_checked"] = new Dictionary<string, string>
		{
			["en"] = "Not checked",
			["ru"] = "Не проверено"
		},
		["settings.checking_updates"] = new Dictionary<string, string>
		{
			["en"] = "Checking for updates...",
			["ru"] = "Проверка обновлений..."
		},
		["settings.no_update_available"] = new Dictionary<string, string>
		{
			["en"] = "No updates available (current: {0})",
			["ru"] = "Обновления не найдены (текущая: {0})"
		},
		["settings.update_available"] = new Dictionary<string, string>
		{
			["en"] = "Update available: {0}",
			["ru"] = "Доступно обновление: {0}"
		},
		["settings.error_prefix"] = new Dictionary<string, string>
		{
			["en"] = "Error: {0}",
			["ru"] = "Ошибка: {0}"
		},
		["settings.downloading_update"] = new Dictionary<string, string>
		{
			["en"] = "Downloading update...",
			["ru"] = "Загрузка обновления..."
		},
		["settings.no_update_available_now"] = new Dictionary<string, string>
		{
			["en"] = "No update available",
			["ru"] = "Обновление недоступно"
		},
		["settings.update_downloaded"] = new Dictionary<string, string>
		{
			["en"] = "Update downloaded",
			["ru"] = "Обновление скачано"
		},
		["settings.ready_restart_later"] = new Dictionary<string, string>
		{
			["en"] = "Update {0} ready. Restart later to apply.",
			["ru"] = "Обновление {0} готово. Перезапустите позже для применения."
		},
		["logs.title"] = new Dictionary<string, string>
		{
			["en"] = "Logs",
			["ru"] = "Логи"
		},
		["logs.entries"] = new Dictionary<string, string>
		{
			["en"] = "entries",
			["ru"] = "записей"
		},
		["logs.search_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "Filter...",
			["ru"] = "Фильтр..."
		},
		["logs.min_level_label"] = new Dictionary<string, string>
		{
			["en"] = "Min level:",
			["ru"] = "Уровень:"
		},
		["logs.export_btn"] = new Dictionary<string, string>
		{
			["en"] = "Export",
			["ru"] = "Экспорт"
		},
		["logs.clear_btn"] = new Dictionary<string, string>
		{
			["en"] = "Clear",
			["ru"] = "Очистить"
		},
		["logs.copy_btn"] = new Dictionary<string, string>
		{
			["en"] = "Copy",
			["ru"] = "Копировать"
		},
		["logs.save_btn"] = new Dictionary<string, string>
		{
			["en"] = "Save",
			["ru"] = "Сохранить"
		},
		["logs.autoscroll_label"] = new Dictionary<string, string>
		{
			["en"] = "Auto-scroll",
			["ru"] = "Автопрокрутка"
		},
		["logs.listening_label"] = new Dictionary<string, string>
		{
			["en"] = "Listening",
			["ru"] = "Получение"
		},
		["chat.title"] = new Dictionary<string, string>
		{
			["en"] = "Chat",
			["ru"] = "Чат"
		},
		["chat.new_session"] = new Dictionary<string, string>
		{
			["en"] = "New",
			["ru"] = "Новый"
		},
		["chat.delete_session"] = new Dictionary<string, string>
		{
			["en"] = "Delete",
			["ru"] = "Удалить"
		},
		["chat.rename_session"] = new Dictionary<string, string>
		{
			["en"] = "Rename",
			["ru"] = "Переименовать"
		},
		["chat.duplicate_session"] = new Dictionary<string, string>
		{
			["en"] = "Duplicate",
			["ru"] = "Дублировать"
		},
		["chat.export_session"] = new Dictionary<string, string>
		{
			["en"] = "Export",
			["ru"] = "Экспорт"
		},
		["chat.clear_session"] = new Dictionary<string, string>
		{
			["en"] = "Clear messages",
			["ru"] = "Очистить сообщения"
		},
		["chat.delete_session_ctx"] = new Dictionary<string, string>
		{
			["en"] = "Delete",
			["ru"] = "Удалить"
		},
		["chat.send"] = new Dictionary<string, string>
		{
			["en"] = "Send",
			["ru"] = "Отправить"
		},
		["chat.stop"] = new Dictionary<string, string>
		{
			["en"] = "Stop",
			["ru"] = "Стоп"
		},
		["chat.settings"] = new Dictionary<string, string>
		{
			["en"] = "Settings",
			["ru"] = "Параметры"
		},
		["chat.mcp_tools"] = new Dictionary<string, string>
		{
			["en"] = "MCP Tools",
			["ru"] = "MCP-инструменты"
		},
		["chat.mcp_servers"] = new Dictionary<string, string>
		{
			["en"] = "MCP Servers",
			["ru"] = "MCP"
		},
		["chat.mcp_servers_title"] = new Dictionary<string, string>
		{
			["en"] = "MCP Servers",
			["ru"] = "MCP-серверы"
		},
		["chat.mcp_guide_title"] = new Dictionary<string, string>
		{
			["en"] = "MCP Setup Guide",
			["ru"] = "Руководство по настройке MCP"
		},
		["chat.mcp_guide_btn"] = new Dictionary<string, string>
		{
			["en"] = "Setup Guide",
			["ru"] = "\ud83d\udcd6 Инструкция"
		},
		["mcp.cmd_not_found"] = new Dictionary<string, string>
		{
			["en"] = "Command \"{0}\" not found in PATH. Install Node.js from https://nodejs.org (choose LTS), then restart LlamaStudio.",
			["ru"] = "Команда \"{0}\" не найдена в PATH. Установите Node.js с https://nodejs.org (выберите LTS), затем перезапустите LlamaStudio."
		},
		["mcp.sse_connect_failed"] = new Dictionary<string, string>
		{
			["en"] = "Failed to connect to SSE server at {0}. Check that the server is running and accessible.",
			["ru"] = "Не удалось подключиться к SSE-серверу по адресу {0}. Проверьте, что сервер запущен и доступен."
		},
		["chat.select_fs_folder"] = new Dictionary<string, string>
		{
			["en"] = "Select folder for file management",
			["ru"] = "Выберите папку для управления файлами"
		},
		["chat.fs_description"] = new Dictionary<string, string>
		{
			["en"] = "File system access",
			["ru"] = "Доступ к файловой системе"
		},
		["chat.fs_configured"] = new Dictionary<string, string>
		{
			["en"] = "Filesystem MCP configured successfully!",
			["ru"] = "Filesystem MCP успешно настроен!"
		},
		["chat.fs_blocked_folder"] = new Dictionary<string, string>
		{
			["en"] = "This folder is not allowed for security reasons. Choose a user folder instead.",
			["ru"] = "Эта папка запрещена по соображениям безопасности. Выберите пользовательскую папку."
		},
		["chat.success"] = new Dictionary<string, string>
		{
			["en"] = "Success",
			["ru"] = "Успешно"
		},
		["chat.setup_filesystem"] = new Dictionary<string, string>
		{
			["en"] = "Setup Filesystem",
			["ru"] = "Настроить Filesystem"
		},
		["chat.system_prompt"] = new Dictionary<string, string>
		{
			["en"] = "System Prompt",
			["ru"] = "Системный промпт"
		},
		["chat.system_prompt_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Enter system instructions...",
			["ru"] = "Введите системные инструкции..."
		},
		["chat.params"] = new Dictionary<string, string>
		{
			["en"] = "Generation Parameters",
			["ru"] = "Параметры генерации"
		},
		["chat.sessions"] = new Dictionary<string, string>
		{
			["en"] = "Sessions",
			["ru"] = "Сессии"
		},
		["chat.no_session"] = new Dictionary<string, string>
		{
			["en"] = "Create a new session to start chatting",
			["ru"] = "Создайте новую сессию для начала разговора"
		},
		["chat.no_server"] = new Dictionary<string, string>
		{
			["en"] = "Server is not running",
			["ru"] = "Сервер не запущен"
		},
		["chat.edit_message"] = new Dictionary<string, string>
		{
			["en"] = "Edit",
			["ru"] = "Редактировать"
		},
		["chat.regenerate"] = new Dictionary<string, string>
		{
			["en"] = "Regenerate",
			["ru"] = "Перегенерировать"
		},
		["chat.bookmark"] = new Dictionary<string, string>
		{
			["en"] = "Bookmark",
			["ru"] = "Закладка"
		},
		["chat.unbookmark"] = new Dictionary<string, string>
		{
			["en"] = "Remove bookmark",
			["ru"] = "Убрать закладку"
		},
		["chat.delete_message"] = new Dictionary<string, string>
		{
			["en"] = "Delete message",
			["ru"] = "Удалить сообщение"
		},
		["chat.undo_message"] = new Dictionary<string, string>
		{
			["en"] = "Undo",
			["ru"] = "Вернуть"
		},
		["chat.reasoning"] = new Dictionary<string, string>
		{
			["en"] = "Model reasoning",
			["ru"] = "Рассуждение модели"
		},
		["chat.hide_reasoning"] = new Dictionary<string, string>
		{
			["en"] = "Hide reasoning",
			["ru"] = "Скрыть рассуждения"
		},
		["chat.server_connected"] = new Dictionary<string, string>
		{
			["en"] = "Connected",
			["ru"] = "Подключено"
		},
		["chat.server_disconnected"] = new Dictionary<string, string>
		{
			["en"] = "Disconnected",
			["ru"] = "Не подключено"
		},
		["chat.copy_code"] = new Dictionary<string, string>
		{
			["en"] = "Copy code",
			["ru"] = "Копировать код"
		},
		["chat.expand_code"] = new Dictionary<string, string>
		{
			["en"] = "Expand code",
			["ru"] = "Развернуть код"
		},
		["chat.pick_images"] = new Dictionary<string, string>
		{
			["en"] = "Select images",
			["ru"] = "Выбрать изображения"
		},
		["chat.pick_files"] = new Dictionary<string, string>
		{
			["en"] = "Select files",
			["ru"] = "Выбрать файлы"
		},
		["server.title"] = new Dictionary<string, string>
		{
			["en"] = "Server",
			["ru"] = "Сервер"
		},
		["server.start_btn"] = new Dictionary<string, string>
		{
			["en"] = "Start",
			["ru"] = "Запустить"
		},
		["server.stop_btn"] = new Dictionary<string, string>
		{
			["en"] = "Stop",
			["ru"] = "Остановить"
		},
		["server.measure_speed_btn"] = new Dictionary<string, string>
		{
			["en"] = "Measure Speed",
			["ru"] = "Измерить скорость"
		},
		["server.save_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Save to Profile",
			["ru"] = "Сохранить в профиль"
		},
		["server.load_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Load Profile",
			["ru"] = "Загрузить"
		},
		["server.copy_from_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Copy",
			["ru"] = "Копировать"
		},
		["server.create_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "New Profile",
			["ru"] = "Создать"
		},
		["server.delete_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Delete",
			["ru"] = "Удалить"
		},
		["server.set_default_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Set Default",
			["ru"] = "Сделать дефолтным"
		},
		["server.rename_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Rename",
			["ru"] = "Переименовать"
		},
		["dash.switch_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Switch Profile",
			["ru"] = "Переключить профиль"
		},
		["dash.active_profile_label"] = new Dictionary<string, string>
		{
			["en"] = "Active",
			["ru"] = "Активный"
		},
		["server.clear_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Clear",
			["ru"] = "Очистить"
		},
		["server.export_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Export",
			["ru"] = "Экспорт"
		},
		["server.import_profile_btn"] = new Dictionary<string, string>
		{
			["en"] = "Import",
			["ru"] = "Импорт"
		},
		["server.refresh_profiles_btn"] = new Dictionary<string, string>
		{
			["en"] = "Refresh Profiles",
			["ru"] = "Обновить профили"
		},
		["server.export_bat_btn"] = new Dictionary<string, string>
		{
			["en"] = "Export .bat",
			["ru"] = "Экспорт .bat"
		},
		["server.copy_cmdline_btn"] = new Dictionary<string, string>
		{
			["en"] = "Copy CmdLine",
			["ru"] = "Копировать команду"
		},
		["server.browse_model_btn"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["server.browse_mmproj_btn"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["server.browse_draft_btn"] = new Dictionary<string, string>
		{
			["en"] = "Browse...",
			["ru"] = "Обзор..."
		},
		["server.quick_scan_btn"] = new Dictionary<string, string>
		{
			["en"] = "Scan Models Dir",
			["ru"] = "Сканировать папку"
		},
		["server.quick_select_model_btn"] = new Dictionary<string, string>
		{
			["en"] = "Select Model",
			["ru"] = "Выбрать модель"
		},
		["server.quick_select_mmproj_btn"] = new Dictionary<string, string>
		{
			["en"] = "Select mmproj",
			["ru"] = "Выбрать mmproj"
		},
		["server.quick_select_draft_btn"] = new Dictionary<string, string>
		{
			["en"] = "Select Draft",
			["ru"] = "Выбрать черновик"
		},
		["server.check_version_btn"] = new Dictionary<string, string>
		{
			["en"] = "Check Version",
			["ru"] = "Проверить версию"
		},
		["server.select_llama_dir_btn"] = new Dictionary<string, string>
		{
			["en"] = "Select Directory...",
			["ru"] = "Выбрать папку..."
		},
		["server.no_llama_server_exe"] = new Dictionary<string, string>
		{
			["en"] = "llama-server.exe not found in the selected directory. Please select the llama.cpp build directory.",
			["ru"] = "llama-server.exe не найден в выбранной папке. Выберите папку сборки llama.cpp."
		},
		["server.state_label"] = new Dictionary<string, string>
		{
			["en"] = "State:",
			["ru"] = "Состояние:"
		},
		["server.uptime_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Uptime: {0}",
			["ru"] = "Работает: {0}"
		},
		["server.vram_fmt"] = new Dictionary<string, string>
		{
			["en"] = "VRAM: {0:F1} GB",
			["ru"] = "VRAM: {0:F1} ГБ"
		},
		["server.ram_fmt"] = new Dictionary<string, string>
		{
			["en"] = "RAM: {0:F1} GB",
			["ru"] = "ОЗУ: {0:F1} ГБ"
		},
		["server.tps_fmt"] = new Dictionary<string, string>
		{
			["en"] = "TPS: {0}",
			["ru"] = "ток/с: {0}"
		},
		["server.prompt_tps_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Prompt tok/s: {0}",
			["ru"] = "Промпт ток/с: {0}"
		},
		["server.health_check"] = new Dictionary<string, string>
		{
			["en"] = "Health Check",
			["ru"] = "Проверка"
		},
		["server.tab_model"] = new Dictionary<string, string>
		{
			["en"] = "Model",
			["ru"] = "Модель"
		},
		["server.server_path_title"] = new Dictionary<string, string>
		{
			["en"] = "llama.cpp Server Path",
			["ru"] = "Путь к llama.cpp серверу"
		},
		["server.build_label"] = new Dictionary<string, string>
		{
			["en"] = "Select llama.cpp server",
			["ru"] = "Выбор сервера llama.cpp"
		},
		["server.browse"] = new Dictionary<string, string>
		{
			["en"] = "Browse",
			["ru"] = "Обзор"
		},
		["server.exec_hint"] = new Dictionary<string, string>
		{
			["en"] = "Server executable will be: llama-server.exe in this folder. Download llama.cpp build and extract here.",
			["ru"] = "Исполняемый файл сервера: llama-server.exe в этой папке. Скачайте сборку llama.cpp и распакуйте сюда."
		},
		["server.exec_found_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Executable found: {0}",
			["ru"] = "Файл найден: {0}"
		},
		["server.check_version"] = new Dictionary<string, string>
		{
			["en"] = "Check Version",
			["ru"] = "Проверить версию"
		},
		["server.main_model"] = new Dictionary<string, string>
		{
			["en"] = "Main Model",
			["ru"] = "Основная модель"
		},
		["server.model_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Path to .gguf model file",
			["ru"] = "Путь к файлу модели .gguf"
		},
		["server.mmproj_title"] = new Dictionary<string, string>
		{
			["en"] = "mmproj (Vision)",
			["ru"] = "mmproj (Зрение)"
		},
		["server.optional"] = new Dictionary<string, string>
		{
			["en"] = "Optional",
			["ru"] = "Необязательно"
		},
		["server.mmproj_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Path to mmproj .gguf file",
			["ru"] = "Путь к файлу mmproj .gguf"
		},
		["server.draft_title"] = new Dictionary<string, string>
		{
			["en"] = "Draft Model (Speculative Decoding)",
			["ru"] = "Черновая модель (Спекулятивное декодирование)"
		},
		["server.advanced_badge"] = new Dictionary<string, string>
		{
			["en"] = "Advanced",
			["ru"] = "Продвинуто"
		},
		["server.enable_speculative"] = new Dictionary<string, string>
		{
			["en"] = "Use external draft model (-md)",
			["ru"] = "Использовать черновую модель (-md)"
		},
		["server.draft_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Path to draft .gguf model",
			["ru"] = "Путь к черновой модели .gguf"
		},
		["server.quick_select_title"] = new Dictionary<string, string>
		{
			["en"] = "Quick Model Selection (Local)",
			["ru"] = "Быстрый выбор модели (локально)"
		},
		["server.scan_models_dir"] = new Dictionary<string, string>
		{
			["en"] = "Scan Models Directory",
			["ru"] = "Сканировать папку моделей"
		},
		["server.to_main"] = new Dictionary<string, string>
		{
			["en"] = "→ Main Model",
			["ru"] = "→ Основная модель"
		},
		["server.to_mmproj_btn"] = new Dictionary<string, string>
		{
			["en"] = "→ mmproj",
			["ru"] = "→ mmproj"
		},
		["server.to_draft_btn"] = new Dictionary<string, string>
		{
			["en"] = "→ Draft",
			["ru"] = "→ Черновик"
		},
		["server.hf_title"] = new Dictionary<string, string>
		{
			["en"] = "HuggingFace — Recommended Models",
			["ru"] = "HuggingFace — Рекомендуемые модели"
		},
		["server.download_to_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Models will be downloaded to:",
			["ru"] = "Модели будут скачаны в:"
		},
		["server.select_this_model"] = new Dictionary<string, string>
		{
			["en"] = "Select This Model",
			["ru"] = "Выбрать эту модель"
		},
		["server.or_manual"] = new Dictionary<string, string>
		{
			["en"] = "Or enter manually:",
			["ru"] = "Или ввести вручную:"
		},
		["server.repo_label"] = new Dictionary<string, string>
		{
			["en"] = "Repo:",
			["ru"] = "Репозиторий:"
		},
		["server.offline_mode"] = new Dictionary<string, string>
		{
			["en"] = "Offline mode",
			["ru"] = "Офлайн режим"
		},
		["server.tab_gpu"] = new Dictionary<string, string>
		{
			["en"] = "GPU",
			["ru"] = "GPU"
		},
		["server.gpu_acceleration"] = new Dictionary<string, string>
		{
			["en"] = "GPU Acceleration",
			["ru"] = "Ускорение GPU"
		},
		["server.multi_gpu_title"] = new Dictionary<string, string>
		{
			["en"] = "Multi-GPU",
			["ru"] = "Работа с multi-GPU"
		},
		["server.used_gpus"] = new Dictionary<string, string>
		{
			["en"] = "Selected GPUs",
			["ru"] = "Используемые GPU"
		},
		["server.main_gpu_disabled_tensor_split"] = new Dictionary<string, string>
		{
			["en"] = "Main GPU (not used with tensor split)",
			["ru"] = "Основной GPU (не используется при tensor-split)"
		},
		["server.main_gpu_split_tip"] = new Dictionary<string, string>
		{
			["en"] = "With model distribution, the main GPU is not used; GPU order and tensor split matter.",
			["ru"] = "При распределении модели основной GPU не используется; важны порядок карт и tensor-split."
		},
		["server.main_gpu_tip"] = new Dictionary<string, string>
		{
			["en"] = "Selects the main GPU for the mode without tensor split.",
			["ru"] = "Выбирает основную видеокарту для режима без tensor-split."
		},
		["server.model_distribution"] = new Dictionary<string, string>
		{
			["en"] = "Model distribution",
			["ru"] = "Распределение модели"
		},
		["server.auto_distribution"] = new Dictionary<string, string>
		{
			["en"] = "Automatic distribution",
			["ru"] = "Автоматическое распределение"
		},
		["server.gpu_monitoring"] = new Dictionary<string, string>
		{
			["en"] = "GPU Monitoring",
			["ru"] = "Мониторинг GPU"
		},
		["server.tensor_split_label"] = new Dictionary<string, string>
		{
			["en"] = "Tensor split ratio",
			["ru"] = "Распределение тензоров"
		},
		["server.gpu_layers_label"] = new Dictionary<string, string>
		{
			["en"] = "GPU Layers:",
			["ru"] = "Слои GPU:"
		},
		["server.gpu_layers_mode_auto"] = new Dictionary<string, string>
		{
			["en"] = "Auto",
			["ru"] = "Авто"
		},
		["server.gpu_layers_mode_all"] = new Dictionary<string, string>
		{
			["en"] = "All",
			["ru"] = "Все"
		},
		["server.threads_label"] = new Dictionary<string, string>
		{
			["en"] = "Threads:",
			["ru"] = "Потоки:"
		},
		["server.threads_batch_label"] = new Dictionary<string, string>
		{
			["en"] = "Threads Batch:",
			["ru"] = "Потоков в батче:"
		},
		["server.main_gpu_label"] = new Dictionary<string, string>
		{
			["en"] = "Main GPU:",
			["ru"] = "Основной GPU:"
		},
		["server.flash_attention"] = new Dictionary<string, string>
		{
			["en"] = "Flash Attention",
			["ru"] = "Flash Attention"
		},
		["server.memory_map"] = new Dictionary<string, string>
		{
			["en"] = "Memory Map",
			["ru"] = "Карта памяти"
		},
		["server.mlock"] = new Dictionary<string, string>
		{
			["en"] = "Mlock",
			["ru"] = "Mlock"
		},
		["server.mmq"] = new Dictionary<string, string>
		{
			["en"] = "MMQ",
			["ru"] = "MMQ"
		},
		["server.kv_offload"] = new Dictionary<string, string>
		{
			["en"] = "KV Offload",
			["ru"] = "Разгрузка KV"
		},
		["server.cache_type_k"] = new Dictionary<string, string>
		{
			["en"] = "Cache type K:",
			["ru"] = "Тип кэша K:"
		},
		["server.cache_type_v"] = new Dictionary<string, string>
		{
			["en"] = "Cache type V:",
			["ru"] = "Тип кэша V:"
		},
		["server.tip_cache_type_k"] = new Dictionary<string, string>
		{
			["en"] = "KV cache type for K tensors",
			["ru"] = "Тип квантования KV-кэша для тензоров K (-ctk)\n\nФлаг: --cache-type-k / -ctk\n\nЧто это:\nОпределяет точность хранения ключевых (Key) тензоров в KV-кэше — структуре, которая хранит контекст диалога.\n\nДоступные значения:\n• f16 — высокая точность, больше памяти (по умолчанию)\n• q8_0 — хороший баланс скорости и качества\n• q4_0 — максимальная экономия памяти, возможна потеря качества\n\nРекомендации:\n• Стандарт: f16 (по умолчанию llama.cpp)\n• Баланс скорость/качество: q8_0\n• Если не хватает VRAM: переходите на q4_0"
		},
		["server.tip_cache_type_v"] = new Dictionary<string, string>
		{
			["en"] = "KV cache type for V tensors",
			["ru"] = "Тип квантования KV-кэша для тензоров V (-ctv)\n\nФлаг: --cache-type-v / -ctv\n\nЧто это:\nОпределяет точность хранения значащих (Value) тензоров в KV-кэше.\n\nДоступные значения:\n• f16 — высокая точность, больше памяти (по умолчанию)\n• q8_0 — хороший баланс скорости и качества\n• q4_0 — максимальная экономия памяти, возможна потеря качества\n\nРекомендации:\n• Стандарт: f16 (по умолчанию llama.cpp)\n• Обычно ставьте то же значение что и для Cache Type K"
		},
		["server.cache_ram"] = new Dictionary<string, string>
		{
			["en"] = "Cache RAM (MiB):",
			["ru"] = "Кэш RAM (МиБ):"
		},
		["server.tip_cache_ram"] = new Dictionary<string, string>
		{
			["en"] = "Max KV cache size in MiB",
			["ru"] = "Макс. размер KV-кэша в МиБ (--cache-ram)\n\nФлаг: --cache-ram\n\nЧто это:\nОграничивает максимальный объём оперативной памяти, который сервер может использовать для хранения KV-кэша (контекста диалога).\n\nЗначения:\n• -1 = без ограничения (по умолчанию)\n• 0 = отключить кэш\n• Число — лимит в мегабайтах\n\nРекомендации:\n• Стандарт: -1 (без ограничения, по умолчанию llama.cpp)\n• Если нужно ограничить память сервера: укажите значение в МиБ\n• Оставьте -1 для большинства случаев"
		},
		["server.cache_reuse"] = new Dictionary<string, string>
		{
			["en"] = "Cache reuse (tokens):",
			["ru"] = "Переиспользование кэша (токены):"
		},
		["server.tip_cache_reuse"] = new Dictionary<string, string>
		{
			["en"] = "Min chunk size for cache reuse",
			["ru"] = "Мин. размер чанка для переиспользования кэша (--cache-reuse)\n\nФлаг: --cache-reuse\n\nЧто это:\nМинимальный размер фрагмента контекста (в токенах), который сервер попытается переиспользовать из кэша через сдвиг KV-блоков.\n\nЗначения:\n• 32 — стандартное значение (по умолчанию)\n• 0 = отключить переиспользование\n• Больше = более агрессивное кеширование\n\nРекомендации:\n• Оставьте 32 для большинства задач\n• Увеличивайте до 256 если контекст очень длинный и часто повторяется"
		},
		["server.tab_context_sampling"] = new Dictionary<string, string>
		{
			["en"] = "Context & Sampling",
			["ru"] = "Контекст и сэмплинг"
		},
		["server.context_batch"] = new Dictionary<string, string>
		{
			["en"] = "Context & Batch",
			["ru"] = "Контекст и батч"
		},
		["server.context_label"] = new Dictionary<string, string>
		{
			["en"] = "Context:",
			["ru"] = "Контекст:"
		},
		["server.batch_label"] = new Dictionary<string, string>
		{
			["en"] = "Batch:",
			["ru"] = "Батч:"
		},
		["server.ubatch_label"] = new Dictionary<string, string>
		{
			["en"] = "U-Batch:",
			["ru"] = "U-батч:"
		},
		["server.max_tokens_label"] = new Dictionary<string, string>
		{
			["en"] = "Max Tokens:",
			["ru"] = "Макс токенов:"
		},
		["server.sampling_section_title"] = new Dictionary<string, string>
		{
			["en"] = "Sampling Section",
			["ru"] = "Раздел сэмплинга"
		},
		["server.sampling_title"] = new Dictionary<string, string>
		{
			["en"] = "Sampling",
			["ru"] = "Сэмплинг"
		},
		["server.temperature_label"] = new Dictionary<string, string>
		{
			["en"] = "Temperature:",
			["ru"] = "Температура:"
		},
		["server.top_p_label"] = new Dictionary<string, string>
		{
			["en"] = "Top-P:",
			["ru"] = "Top-P:"
		},
		["server.top_k_label"] = new Dictionary<string, string>
		{
			["en"] = "Top-K:",
			["ru"] = "Top-K:"
		},
		["server.min_p_label"] = new Dictionary<string, string>
		{
			["en"] = "Min-P:",
			["ru"] = "Min-P:"
		},
		["server.typical_p_label"] = new Dictionary<string, string>
		{
			["en"] = "Typical-P:",
			["ru"] = "Typical-P:"
		},
		["server.repeat_penalty_label"] = new Dictionary<string, string>
		{
			["en"] = "Repeat Penalty:",
			["ru"] = "Штраф повторов:"
		},
		["server.repeat_last_n_label"] = new Dictionary<string, string>
		{
			["en"] = "Repeat Last N:",
			["ru"] = "Повтор. последних N:"
		},
		["server.presence_penalty_label"] = new Dictionary<string, string>
		{
			["en"] = "Presence Penalty:",
			["ru"] = "Штраф присутствия:"
		},
		["server.frequency_penalty_label"] = new Dictionary<string, string>
		{
			["en"] = "Frequency Penalty:",
			["ru"] = "Штраф частоты:"
		},
		["server.seed_label"] = new Dictionary<string, string>
		{
			["en"] = "Seed:",
			["ru"] = "Сид:"
		},
		["server.mirostat_sampling"] = new Dictionary<string, string>
		{
			["en"] = "Mirostat Sampling",
			["ru"] = "Mirostat сэмплинг"
		},
		["server.tau_label"] = new Dictionary<string, string>
		{
			["en"] = "Tau:",
			["ru"] = "Тау:"
		},
		["server.eta_label"] = new Dictionary<string, string>
		{
			["en"] = "Eta:",
			["ru"] = "Эта:"
		},
		["server.tab_advanced"] = new Dictionary<string, string>
		{
			["en"] = "Advanced",
			["ru"] = "Продвинутое"
		},
		["server.mtp_title"] = new Dictionary<string, string>
		{
			["en"] = "MTP / Multi-Token Prediction",
			["ru"] = "MTP / Много-токенное предсказание"
		},
		["server.spec_type_label"] = new Dictionary<string, string>
		{
			["en"] = "Spec Type:",
			["ru"] = "Тип спекуляции:"
		},
		["server.predict_count_label"] = new Dictionary<string, string>
		{
			["en"] = "Max Tokens (-n):",
			["ru"] = "Макс. токенов (-n):"
		},
		["server.spec_draft_params"] = new Dictionary<string, string>
		{
			["en"] = "Speculative Draft Parameters",
			["ru"] = "Параметры спекулятивного черновика"
		},
		["server.draft_gpu_layers_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft GPU Layers:",
			["ru"] = "Слои GPU черновика:"
		},
		["server.draft_n_max_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft N Max:",
			["ru"] = "Черновик N макс:"
		},
		["server.draft_n_min_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft N Min:",
			["ru"] = "Черновик N мин:"
		},
		["server.draft_p_split_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft P Split:",
			["ru"] = "Черновик P разд.:"
		},
		["server.draft_p_min_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft P Min:",
			["ru"] = "Черновик P мин:"
		},
		["server.yarn_rope_title"] = new Dictionary<string, string>
		{
			["en"] = "YARN / Rope Scaling",
			["ru"] = "YARN / Масштабирование Rope"
		},
		["server.rope_freq_base_label"] = new Dictionary<string, string>
		{
			["en"] = "Rope Freq Base:",
			["ru"] = "Частота Rope база:"
		},
		["server.rope_freq_scale_label"] = new Dictionary<string, string>
		{
			["en"] = "Rope Freq Scale:",
			["ru"] = "Масштаб частоты Rope:"
		},
		["server.yarn_orig_ctx_label"] = new Dictionary<string, string>
		{
			["en"] = "Yarn Orig Ctx:",
			["ru"] = "Исх. контекст Yarn:"
		},
		["server.advanced_options_title"] = new Dictionary<string, string>
		{
			["en"] = "Advanced Options",
			["ru"] = "Дополнительные опции"
		},
		["server.numa"] = new Dictionary<string, string>
		{
			["en"] = "NUMA",
			["ru"] = "NUMA"
		},
		["server.cache_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Cache Prompt",
			["ru"] = "Кэш промпта"
		},
		["server.cont_batching"] = new Dictionary<string, string>
		{
			["en"] = "Cont. Batching",
			["ru"] = "Непрерывный батч"
		},
		["server.context_shift"] = new Dictionary<string, string>
		{
			["en"] = "Context Shift",
			["ru"] = "Сдвиг контекста"
		},
		["server.verbose_logging"] = new Dictionary<string, string>
		{
			["en"] = "Verbose Logging",
			["ru"] = "Подробные логи"
		},
		["server.web_ui"] = new Dictionary<string, string>
		{
			["en"] = "Web UI",
			["ru"] = "Веб-интерфейс"
		},
		["server.metrics"] = new Dictionary<string, string>
		{
			["en"] = "Metrics",
			["ru"] = "Метрики"
		},
		["server.reasoning"] = new Dictionary<string, string>
		{
			["en"] = "Reasoning",
			["ru"] = "Рассуждения"
		},
		["server.embedding_mode"] = new Dictionary<string, string>
		{
			["en"] = "Embedding Mode",
			["ru"] = "Режим эмбеддинга"
		},
		["server.priority_high"] = new Dictionary<string, string>
		{
			["en"] = "High Priority",
			["ru"] = "Высокий приоритет"
		},
		["server.custom_args_title"] = new Dictionary<string, string>
		{
			["en"] = "Custom Arguments",
			["ru"] = "Пользовательские аргументы"
		},
		["server.model_path_label"] = new Dictionary<string, string>
		{
			["en"] = "Model path:",
			["ru"] = "Путь к модели:"
		},
		["server.mmproj_path_label"] = new Dictionary<string, string>
		{
			["en"] = "mmproj path:",
			["ru"] = "Путь к mmproj:"
		},
		["server.draft_path_label"] = new Dictionary<string, string>
		{
			["en"] = "Draft path:",
			["ru"] = "Путь к draft:"
		},
		["server.kv_cache_type_title"] = new Dictionary<string, string>
		{
			["en"] = "KV Cache Type",
			["ru"] = "Тип KV-кэша"
		},
		["server.mtp_multi_token_title"] = new Dictionary<string, string>
		{
			["en"] = "MTP / Multi-Token Prediction",
			["ru"] = "MTP / Много-токеновое предсказание"
		},
		["server.custom_args_placeholder"] = new Dictionary<string, string>
		{
			["en"] = "Additional command-line arguments...",
			["ru"] = "Дополнительные аргументы командной строки..."
		},
		["server.export_title"] = new Dictionary<string, string>
		{
			["en"] = "Export",
			["ru"] = "Экспорт"
		},
		["server.export_bat"] = new Dictionary<string, string>
		{
			["en"] = "Export as .bat",
			["ru"] = "Экспорт в .bat"
		},
		["server.copy_cmdline"] = new Dictionary<string, string>
		{
			["en"] = "Copy Command Line",
			["ru"] = "Копировать команду"
		},
		["server.import_bat_btn"] = new Dictionary<string, string>
		{
			["en"] = "Import .bat",
			["ru"] = "Импорт .bat"
		},
		["server.custom_args_desc"] = new Dictionary<string, string>
		{
			["en"] = "Parameters not available in the main settings — select via the \"Add Arguments\" button.",
			["ru"] = "Параметры, которых нет в основных настройках — выберите через кнопку «Добавить аргументы»."
		},
		["server.custom_args_note"] = new Dictionary<string, string>
		{
			["en"] = "These arguments are saved in the current profile and do not duplicate the primary settings.",
			["ru"] = "Аргументы сохраняются в текущем профиле и не дублируют основные настройки."
		},
		["server.add_args_btn"] = new Dictionary<string, string>
		{
			["en"] = "Add Arguments",
			["ru"] = "Добавить аргументы"
		},
		["server.stop_server_btn_text"] = new Dictionary<string, string>
		{
			["en"] = "Stop Server",
			["ru"] = "Остановить сервер"
		},
		["server.tab_connection"] = new Dictionary<string, string>
		{
			["en"] = "Connection",
			["ru"] = "Подключение"
		},
		["server.tab_context"] = new Dictionary<string, string>
		{
			["en"] = "Context",
			["ru"] = "Контекст"
		},
		["server.tab_sampling"] = new Dictionary<string, string>
		{
			["en"] = "Sampling",
			["ru"] = "Сэмплинг"
		},
		["server.tab_kv_cache"] = new Dictionary<string, string>
		{
			["en"] = "KV Cache",
			["ru"] = "KV Кэш"
		},
		["server.tab_mtp"] = new Dictionary<string, string>
		{
			["en"] = "MTP",
			["ru"] = "MTP"
		},
		["server.tab_speculative"] = new Dictionary<string, string>
		{
			["en"] = "MTP & Speculative",
			["ru"] = "MTP и Спекулятивное"
		},
		["server.yarn_ext_factor_label"] = new Dictionary<string, string>
		{
			["en"] = "YARN ext factor",
			["ru"] = "Масштаб расширения YARN"
		},
		["server.yarn_attn_factor_label"] = new Dictionary<string, string>
		{
			["en"] = "YARN attn factor",
			["ru"] = "Фактор внимания YARN"
		},
		["server.yarn_beta_fast_label"] = new Dictionary<string, string>
		{
			["en"] = "YARN beta fast",
			["ru"] = "Бета быстрый YARN"
		},
		["server.yarn_beta_slow_label"] = new Dictionary<string, string>
		{
			["en"] = "YARN beta slow",
			["ru"] = "Бета медленный YARN"
		},
		["server.tab_rope_yarn"] = new Dictionary<string, string>
		{
			["en"] = "Rope/YARN",
			["ru"] = "Rope/YARN"
		},
		["server.tab_custom_args"] = new Dictionary<string, string>
		{
			["en"] = "Custom arguments",
			["ru"] = "Пользовательские аргументы"
		},
		["server.tab_rpc"] = new Dictionary<string, string>
		{
			["en"] = "Remote RPC",
			["ru"] = "Удалённый RPC"
		},
		["server.tab_server"] = new Dictionary<string, string>
		{
			["en"] = "Server",
			["ru"] = "Сервер"
		},
		["server.default_hint"] = new Dictionary<string, string>
		{
			["en"] = "Default: {0}",
			["ru"] = "По умолчанию: {0}"
		},
		["server.reset_btn"] = new Dictionary<string, string>
		{
			["en"] = "Reset to default",
			["ru"] = "Сбросить"
		},
		["server.rope_scaling_label"] = new Dictionary<string, string>
		{
			["en"] = "Rope Scaling:",
			["ru"] = "Rope масштабирование:"
		},
		["server.connection_settings"] = new Dictionary<string, string>
		{
			["en"] = "Connection Settings",
			["ru"] = "Настройки подключения"
		},
		["server.rpc_title"] = new Dictionary<string, string>
		{
			["en"] = "Remote llama.cpp RPC",
			["ru"] = "Удалённый RPC llama.cpp"
		},
		["server.rpc_description"] = new Dictionary<string, string>
		{
			["en"] = "Connect a trusted remote ggml-rpc-server. RPC is experimental and must stay inside a private network.",
			["ru"] = "Подключение к доверенному удалённому ggml-rpc-server. RPC экспериментальный и должен использоваться только в закрытой сети."
		},
		["server.rpc_ssh_title"] = new Dictionary<string, string>
		{
			["en"] = "SSH connection",
			["ru"] = "\u041f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u0435 SSH"
		},
		["server.rpc_ssh_instruction"] = new Dictionary<string, string>
		{
			["en"] = "The remote computer must have the OpenSSH Server service enabled and allow SSH connections from this computer. The SSH account must be allowed to start and stop the RPC server. RPC is experimental: use it only on a trusted private network and allow the RPC port through the remote firewall.",
			["ru"] = "\u041d\u0430 \u0443\u0434\u0430\u043b\u0451\u043dном \u043aомпьюте \u0434олжна \u0431ыть \u0432ключена \u0441лужба OpenSSH Server, \u0430 SSH-\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044e \u0440\u0430\u0437\u0440\u0435\u0448\u0435\u043d\u043e \u0437\u0430\u043f\u0443\u0441\u043a\u0430\u0442\u044c \u0438 \u043e\u0441\u0442\u0430\u043d\u0430\u0432\u043b\u0438\u0432\u0430\u0442\u044c RPC-\u0441\u0435\u0440\u0432\u0435\u0440. RPC \u044d\u043a\u0441\u043f\u0435\u0440\u0438\u043c\u0435\u043d\u0442\u0430\u043b\u0435\u043d: \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0439\u0442\u0435 \u0435\u0433\u043e \u0442\u043e\u043b\u044c\u043aо \u0432 \u0434о\u0432\u0435\u0440\u0435\u043d\u043d\u043e\u0439 \u043b\u043e\u043a\u0430\u043b\u044c\u043d\u043e\u0439 \u0441\u0435\u0442\u0438 \u0438 \u0440\u0430\u0437\u0440\u0435\u0448\u0438\u0442\u0435 RPC-\u043f\u043eр\u0442 \u0432 \u0431\u0440\u0430\u043d\u0434\u043c\u0430\u0443\u044d\u0440\u0435."
		},
		["server.rpc_management_title"] = new Dictionary<string, string>
		{
			["en"] = "RPC server management",
			["ru"] = "\u0423\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u0435 RPC-\u0441\u0435\u0440\u0432\u0435\u0440\u043e\u043c"
		},
		["server.rpc_server_path"] = new Dictionary<string, string>
		{
			["en"] = "Remote ggml-rpc-server path",
			["ru"] = "\u041f\u0443\u0442\u044c \u043a \u0443\u0434\u0430\u043b\u0451\u043d\u043d\u043e\u043c\u0443 ggml-rpc-server"
		},
		["server.rpc_server_path_instruction"] = new Dictionary<string, string>
		{
			["en"] = "Enter the full path to ggml-rpc-server on the remote computer (.exe on Windows). The local and remote llama.cpp versions should match.",
			["ru"] = "\u0423\u043a\u0430\u0436\u0438\u0442\u0435 \u043f\u043e\u043b\u043d\u044b\u0439 \u043f\u0443\u0442\u044c \u043a ggml-rpc-server \u043d\u0430 \u0443\u0434\u0430\u043b\u0451\u043d\u043d\u043e\u043c \u043a\u043e\u043c\u043f\u044c\u044e\u0442\u0435 (\u0434\u043b\u044f Windows \u0434\u043e\u0431\u0430\u0432\u044c\u0442\u0435 .exe). \u0416\u0435\u043b\u0430\u0442\u0435\u043b\u044c\u043d\u043e, \u0447\u0442\u043e\u0431\u044b \u0432\u0435\u0440\u0441\u0438\u0438 llama.cpp \u043d\u0430 \u043e\u0431\u043e\u0438\u0445 \u043a\u043e\u043c\u043f\u044c\u044e\u0442\u0435\u0440\u0430\u0445 \u0441\u043e\u0432\u043f\u0430\u0434\u0430\u043b\u0438."
		},
		["server.rpc_enabled"] = new Dictionary<string, string>
		{
			["en"] = "Use remote RPC server",
			["ru"] = "Использовать удалённый RPC-сервер"
		},
		["server.rpc_endpoint"] = new Dictionary<string, string>
		{
			["en"] = "RPC address",
			["ru"] = "Адрес RPC"
		},
		["server.rpc_host"] = new Dictionary<string, string>
		{
			["en"] = "Remote host",
			["ru"] = "Удалённый компьютер"
		},
		["server.rpc_port"] = new Dictionary<string, string>
		{
			["en"] = "RPC port",
			["ru"] = "Порт RPC"
		},
		["server.rpc_start"] = new Dictionary<string, string>
		{
			["en"] = "Start remote server",
			["ru"] = "Запустить удалённый сервер"
		},
		["server.rpc_stop"] = new Dictionary<string, string>
		{
			["en"] = "Stop remote server",
			["ru"] = "Остановить удалённый сервер"
		},
		["server.rpc_status_ssh_key_missing"] = new Dictionary<string, string>
		{
			["en"] = "SSH key was not found in .ssh\\id_ed25519_server.",
			["ru"] = "SSH-ключ не найден в .ssh\\id_ed25519_server."
		},
		["server.rpc_ssh_username"] = new Dictionary<string, string>
		{
			["en"] = "SSH username",
			["ru"] = "\u041b\u043e\u0433\u0438\u043d SSH"
		},
		["server.rpc_ssh_password"] = new Dictionary<string, string>
		{
			["en"] = "SSH password",
			["ru"] = "\u041f\u0430\u0440\u043e\u043b\u044c SSH"
		},
		["server.rpc_save_password"] = new Dictionary<string, string>
		{
			["en"] = "Save password in profile",
			["ru"] = "\u0421\u043e\u0445\u0440\u0430\u043d\u0438\u0442\u044c \u043f\u0430\u0440\u043e\u043b\u044c \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435"
		},
		["server.rpc_use_cache"] = new Dictionary<string, string>
		{
			["en"] = "Use remote RPC cache",
			["ru"] = "\u0418\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u044c \u043a\u044d\u0448 \u0443\u0434\u0430\u043b\u0451\u043d\u043d\u043e\u0433\u043e RPC"
		},
		["server.rpc_remote_telemetry"] = new Dictionary<string, string>
		{
			["en"] = "Send telemetry from RPC",
			["ru"] = "\u041f\u0435\u0440\u0435\u0434\u0430\u0432\u0430\u0442\u044c \u0442\u0435\u043b\u0435\u043c\u0435\u0442\u0440\u0438\u044e \u0441 RPC"
		},
		["server.rpc_check_connection"] = new Dictionary<string, string>
		{
			["en"] = "Check connection",
			["ru"] = "Проверить подключение"
		},
		["server.rpc_status_not_checked"] = new Dictionary<string, string>
		{
			["en"] = "Connection has not been checked.",
			["ru"] = "Подключение ещё не проверялось."
		},
		["server.rpc_status_credentials_missing"] = new Dictionary<string, string>
		{
			["en"] = "Enter the SSH username and password.",
			["ru"] = "\u0423\u043a\u0430\u0436\u0438\u0442\u0435 \u043b\u043e\u0433\u0438\u043d \u0438 \u043f\u0430\u0440\u043e\u043b\u044c SSH."
		},
		["server.rpc_status_ssh_connected"] = new Dictionary<string, string>
		{
			["en"] = "SSH connection successful.",
			["ru"] = "SSH подключён."
		},
		["server.rpc_status_rpc_unavailable"] = new Dictionary<string, string>
		{
			["en"] = "RPC server port is unavailable.",
			["ru"] = "Порт RPC недоступен."
		},
		["server.rpc_status_ssh_failed"] = new Dictionary<string, string>
		{
			["en"] = "SSH connection failed. Check the host, username, password and SSH service.",
			["ru"] = "Не удалось подключиться по SSH. Проверьте адрес, логин, пароль и службу SSH."
		},
		["server.rpc_status_remote_command_failed"] = new Dictionary<string, string>
		{
			["en"] = "SSH connected, but the remote command failed.",
			["ru"] = "SSH подключён, но удалённая команда завершилась ошибкой."
		},
		["server.rpc_status_start_failed"] = new Dictionary<string, string>
		{
			["en"] = "SSH connected, but the RPC server did not start. Check rpc-server.launch.log.",
			["ru"] = "SSH подключён, но RPC-сервер не запустился. Проверьте rpc-server.launch.log."
		},
		["server.rpc_status_server_version_missing"] = new Dictionary<string, string>
		{
			["en"] = "The selected llama.cpp release does not contain ggml-rpc-server.exe.",
			["ru"] = "\u0412 \u0432\u044b\u0431\u0440\u0430\u043d\u043d\u043e\u0439 \u0432\u0435\u0440\u0441\u0438\u0438 llama.cpp \u043d\u0435\u0442 \u0444\u0430\u0439\u043b\u0430 ggml-rpc-server.exe."
		},
		["server.rpc_status_server_path_missing"] = new Dictionary<string, string>
		{
			["en"] = "Enter the path to ggml-rpc-server.exe on the remote computer.",
			["ru"] = "\u0423\u043a\u0430\u0436\u0438\u0442\u0435 \u043f\u0443\u0442\u044c \u043a ggml-rpc-server.exe \u043d\u0430 \u0443\u0434\u0430\u043b\u0451\u043d\u043d\u043e\u043c \u043a\u043e\u043c\u043f\u044c\u044e\u0442\u0435."
		},
		["server.rpc_check_ssh"] = new Dictionary<string, string>
		{
			["en"] = "Check SSH",
			["ru"] = "Проверить SSH"
		},
		["server.rpc_ssh_connected_button"] = new Dictionary<string, string>
		{
			["en"] = "Connected",
			["ru"] = "\u041f\u043e\u0434\u043a\u043b\u044e\u0447\u0435\u043d\u043e"
		},
		["server.rpc_ssh_connect_button"] = new Dictionary<string, string>
		{
			["en"] = "Connect SSH",
			["ru"] = "\u041f\u043e\u0434\u043a\u043b\u044e\u0447\u0438\u0442\u044c SSH"
		},
		["server.rpc_status_ssh_checking"] = new Dictionary<string, string>
		{
			["en"] = "Checking SSH connection...",
			["ru"] = "Проверка SSH-подключения..."
		},
		["server.rpc_show_password"] = new Dictionary<string, string>
		{
			["en"] = "Show password",
			["ru"] = "Показать пароль"
		},
		["server.rpc_status_invalid_endpoint"] = new Dictionary<string, string>
		{
			["en"] = "Enter an address in the form IP:port.",
			["ru"] = "Укажите адрес в формате IP:порт."
		},
		["server.rpc_status_connected"] = new Dictionary<string, string>
		{
			["en"] = "Connected: {0}",
			["ru"] = "Подключено: {0}"
		},
		["server.rpc_status_failed"] = new Dictionary<string, string>
		{
			["en"] = "Connection failed: {0}",
			["ru"] = "Подключение не удалось: {0}"
		},
		["server.host_label"] = new Dictionary<string, string>
		{
			["en"] = "Host:",
			["ru"] = "Хост:"
		},
		["server.port_label"] = new Dictionary<string, string>
		{
			["en"] = "Port:",
			["ru"] = "Порт:"
		},
		["server.timeout_label"] = new Dictionary<string, string>
		{
			["en"] = "Timeout:",
			["ru"] = "Таймаут:"
		},
		["server.slots_label"] = new Dictionary<string, string>
		{
			["en"] = "Slots:",
			["ru"] = "Слоты:"
		},
		["server.profile_label"] = new Dictionary<string, string>
		{
			["en"] = "Profile:",
			["ru"] = "Профиль:"
		},
		["server.cmdline_preview_title"] = new Dictionary<string, string>
		{
			["en"] = "Command Line",
			["ru"] = "Командная строка"
		},
		["server.unsupported_flags_title"] = new Dictionary<string, string>
		{
			["en"] = "Unsupported Flags",
			["ru"] = "Неподдерживаемые флаги"
		},
		["server.unsupported_flags_hint"] = new Dictionary<string, string>
		{
			["en"] = "These flags may not be supported by your llama.cpp build",
			["ru"] = "Эти флаги могут не поддерживаться вашей сборкой llama.cpp"
		},
		["server.cli_validation_title"] = new Dictionary<string, string>
		{
			["en"] = "CLI Validation",
			["ru"] = "Проверка CLI"
		},
		["server.validate_cli_btn"] = new Dictionary<string, string>
		{
			["en"] = "Validate Flags",
			["ru"] = "Проверить флаги"
		},
		["server.validating"] = new Dictionary<string, string>
		{
			["en"] = "Validating...",
			["ru"] = "Проверка..."
		},
		["server.cli_ok"] = new Dictionary<string, string>
		{
			["en"] = "All flags valid",
			["ru"] = "Все флаги корректны"
		},
		["server.cli_issues_found"] = new Dictionary<string, string>
		{
			["en"] = "Issues found",
			["ru"] = "Обнаружены проблемы"
		},
		["server.cli_removed_flags"] = new Dictionary<string, string>
		{
			["en"] = "Removed flags",
			["ru"] = "Удалённые флаги"
		},
		["server.cli_new_flags"] = new Dictionary<string, string>
		{
			["en"] = "New flags available",
			["ru"] = "Доступны новые флаги"
		},
		["server.cli_changes_title"] = new Dictionary<string, string>
		{
			["en"] = "CLI Changes Detected",
			["ru"] = "Изменения CLI обнаружены"
		},
		["server.cli_critical"] = new Dictionary<string, string>
		{
			["en"] = "Critical changes",
			["ru"] = "Критические изменения"
		},
		["server.download_model_btn"] = new Dictionary<string, string>
		{
			["en"] = "⬇ Download Model",
			["ru"] = "⬇ Скачать модель"
		},
		["server.downloading_status"] = new Dictionary<string, string>
		{
			["en"] = "Downloading...",
			["ru"] = "Скачивание..."
		},
		["server.profile_controls"] = new Dictionary<string, string>
		{
			["en"] = "Profile & Controls",
			["ru"] = "Профиль и управление"
		},
		["server.refresh_profiles"] = new Dictionary<string, string>
		{
			["en"] = "Refresh",
			["ru"] = "Обновить"
		},
		["server.start_server_btn"] = new Dictionary<string, string>
		{
			["en"] = "▶ Start Server",
			["ru"] = "▶ Запустить сервер"
		},
		["server.stop_server_btn"] = new Dictionary<string, string>
		{
			["en"] = "⏹ Stop Server",
			["ru"] = "⏹ Остановить сервер"
		},
		["server.save_to_profile"] = new Dictionary<string, string>
		{
			["en"] = "Save to Profile",
			["ru"] = "Сохранить в профиль"
		},
		["dialog.no_default_profile"] = new Dictionary<string, string>
		{
			["en"] = "No default profile exists. Create a profile first.",
			["ru"] = "Нет профиля по умолчанию. Создайте профиль сначала."
		},
		["dialog.no_model_selected"] = new Dictionary<string, string>
		{
			["en"] = "Default profile has no model selected.",
			["ru"] = "В профиле по умолчанию не выбрана модель."
		},
		["dialog.no_profile_to_save"] = new Dictionary<string, string>
		{
			["en"] = "No profile selected to save settings.",
			["ru"] = "Не выбран профиль для сохранения настроек."
		},
		["dialog.no_profile_to_export"] = new Dictionary<string, string>
		{
			["en"] = "No profile selected to export.",
			["ru"] = "Не выбран профиль для экспорта."
		},
		["dialog.copy_success"] = new Dictionary<string, string>
		{
			["en"] = "Settings copied from \"{0}\". Change what you need and click \"Save to Profile\".",
			["ru"] = "Параметры скопированы из «{0}». Измените что нужно и нажмите «Сохранить в профиль»."
		},
		["dialog.server_already_running"] = new Dictionary<string, string>
		{
			["en"] = "Server is already running!",
			["ru"] = "Сервер уже запущен!"
		},
		["dialog.server_start_failed"] = new Dictionary<string, string>
		{
			["en"] = "Server failed to start.\n\n{0}",
			["ru"] = "Не удалось запустить сервер.\n\n{0}"
		},
		["dialog.server_start_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to start server:\n{0}",
			["ru"] = "Ошибка запуска сервера:\n{0}"
		},
		["dialog.server_stop_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to stop server:\n{0}",
			["ru"] = "Ошибка остановки сервера:\n{0}"
		},
		["dialog.server_restart_failed"] = new Dictionary<string, string>
		{
			["en"] = "Server failed to restart.\n\n{0}",
			["ru"] = "Не удалось перезапустить сервер.\n\n{0}"
		},
		["dialog.server_restart_error"] = new Dictionary<string, string>
		{
			["en"] = "Failed to restart server:\n{0}",
			["ru"] = "Ошибка перезапуска сервера:\n{0}"
		},
		["server.model_not_selected"] = new Dictionary<string, string>
		{
			["en"] = "No model selected",
			["ru"] = "Модель не выбрана"
		},
		["server.mmproj_not_selected"] = new Dictionary<string, string>
		{
			["en"] = "No mmproj selected",
			["ru"] = "mmproj не выбран"
		},
		["server.draft_not_selected"] = new Dictionary<string, string>
		{
			["en"] = "No draft model selected",
			["ru"] = "Черновая модель не выбрана"
		},
		["server.starting_download"] = new Dictionary<string, string>
		{
			["en"] = "Starting download...",
			["ru"] = "Начинаю загрузку..."
		},
		["server.download_complete"] = new Dictionary<string, string>
		{
			["en"] = "Downloaded: {0}",
			["ru"] = "Скачано: {0}"
		},
		["server.download_cancelled"] = new Dictionary<string, string>
		{
			["en"] = "Download cancelled",
			["ru"] = "Загрузка отменена"
		},
		["server.download_error"] = new Dictionary<string, string>
		{
			["en"] = "Error: {0}",
			["ru"] = "Ошибка: {0}"
		},
		["server.tip_spec_type"] = new Dictionary<string, string>
		{
			["en"] = "Speculative decoding type: ngram (built-in), draft-mtp (MTP models), draft (external model)",
			["ru"] = "Тип спекулятивного декодирования: ngram (встроенный), draft-mtp (MTP-модели), draft (внешняя модель)"
		},
		["server.tip_priority_high"] = new Dictionary<string, string>
		{
			["en"] = "Set high process priority for llama-server",
			["ru"] = "Установить высокий приоритет процесса llama-server"
		},
		["models.hf_status_download_complete"] = new Dictionary<string, string>
		{
			["en"] = "Download complete",
			["ru"] = "Загрузка завершена"
		},
		["models.hf_status_token_instructions"] = new Dictionary<string, string>
		{
			["en"] = "Open the browser, create/copy your token, then paste it below.",
			["ru"] = "Откройте браузер, создайте/скопируйте токен, затем вставьте ниже."
		},
		["models.hf_status_token_cleared"] = new Dictionary<string, string>
		{
			["en"] = "Auth token cleared",
			["ru"] = "Токен очищен"
		},
		["models.hf_status_token_saved"] = new Dictionary<string, string>
		{
			["en"] = "Auth token saved",
			["ru"] = "Токен сохранён"
		},
		["models.hf_status_enter_repo"] = new Dictionary<string, string>
		{
			["en"] = "Enter a HuggingFace URL or repo ID",
			["ru"] = "Введите URL или ID репозитория HuggingFace"
		},
		["models.hf_status_fetching"] = new Dictionary<string, string>
		{
			["en"] = "Fetching repository files...",
			["ru"] = "Получение файлов репозитория..."
		},
		["models.hf_status_already_downloaded"] = new Dictionary<string, string>
		{
			["en"] = "Already downloaded",
			["ru"] = "Уже загружено"
		},
		["models.hf_status_files_found_dl"] = new Dictionary<string, string>
		{
			["en"] = "{0} files found ({1} already downloaded)",
			["ru"] = "{0} файлов найдено ({1} уже загружено)"
		},
		["models.hf_status_files_found"] = new Dictionary<string, string>
		{
			["en"] = "Found {0} model files",
			["ru"] = "Найдено {0} моделей"
		},
		["models.hf_status_no_gguf"] = new Dictionary<string, string>
		{
			["en"] = "No .gguf files found",
			["ru"] = "Файлы .gguf не найдены"
		},
		["models.hf_status_no_files_selected"] = new Dictionary<string, string>
		{
			["en"] = "No files selected",
			["ru"] = "Файлы не выбраны"
		},
		["models.hf_status_select_dir_first"] = new Dictionary<string, string>
		{
			["en"] = "Select a valid download directory first",
			["ru"] = "Сначала выберите папку для загрузки"
		},
		["models.hf_status_downloading"] = new Dictionary<string, string>
		{
			["en"] = "Downloading {0} file(s)...",
			["ru"] = "Скачивание {0} файл(ов)..."
		},
		["models.hf_status_cancelled"] = new Dictionary<string, string>
		{
			["en"] = "Downloads cancelled",
			["ru"] = "Загрузки отменены"
		},
		["models.hf_status_error"] = new Dictionary<string, string>
		{
			["en"] = "Error: {0}",
			["ru"] = "Ошибка: {0}"
		},
		["models.scan_status_invalid_dir"] = new Dictionary<string, string>
		{
			["en"] = "Invalid directory",
			["ru"] = "Неверная папка"
		},
		["models.scan_status_scanning"] = new Dictionary<string, string>
		{
			["en"] = "Scanning...",
			["ru"] = "Сканирование..."
		},
		["models.scan_status_found"] = new Dictionary<string, string>
		{
			["en"] = "Found {0} models",
			["ru"] = "Найдено {0} моделей"
		},
		["models.scan_status_set_main"] = new Dictionary<string, string>
		{
			["en"] = "Set as main model: {0}",
			["ru"] = "Установлена как основная модель: {0}"
		},
		["models.scan_status_set_mmproj"] = new Dictionary<string, string>
		{
			["en"] = "Set as mmproj: {0}",
			["ru"] = "Установлен как mmproj: {0}"
		},
		["models.scan_status_directory"] = new Dictionary<string, string>
		{
			["en"] = "Directory: {0}",
			["ru"] = "Папка: {0}"
		},
		["apitest.status_checking"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["apitest.status_fetching"] = new Dictionary<string, string>
		{
			["en"] = "Fetching models...",
			["ru"] = "Загрузка моделей..."
		},
		["apitest.status_sending_chat"] = new Dictionary<string, string>
		{
			["en"] = "Sending chat request...",
			["ru"] = "Отправка chat-запроса..."
		},
		["apitest.status_sending_completion"] = new Dictionary<string, string>
		{
			["en"] = "Sending completion request...",
			["ru"] = "Отправка completion-запроса..."
		},
		["apitest.status_sending_custom"] = new Dictionary<string, string>
		{
			["en"] = "Sending custom request...",
			["ru"] = "Отправка пользовательского запроса..."
		},
		["apitest.result_error"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: {0}",
			["ru"] = "ОШИБКА: {0}"
		},
		["log.debug"] = new Dictionary<string, string>
		{
			["en"] = "Debug",
			["ru"] = "Отладка"
		},
		["log.information"] = new Dictionary<string, string>
		{
			["en"] = "Information",
			["ru"] = "Информация"
		},
		["log.warning"] = new Dictionary<string, string>
		{
			["en"] = "Warning",
			["ru"] = "Предупреждение"
		},
		["log.error"] = new Dictionary<string, string>
		{
			["en"] = "Error",
			["ru"] = "Ошибка"
		},
		["model.ctx_fmt"] = new Dictionary<string, string>
		{
			["en"] = "ctx: {0}",
			["ru"] = "контекст: {0}"
		},
		["model.vram_fmt"] = new Dictionary<string, string>
		{
			["en"] = "~{0:F1} GB VRAM",
			["ru"] = "~{0:F1} ГБ VRAM"
		},
		["model.layers_fmt"] = new Dictionary<string, string>
		{
			["en"] = "{0} layers",
			["ru"] = "{0} слоёв"
		},
		["profile.ctx_fmt"] = new Dictionary<string, string>
		{
			["en"] = "ctx: {0}",
			["ru"] = "контекст: {0}"
		},
		["profile.gpu_fmt"] = new Dictionary<string, string>
		{
			["en"] = "gpu: {0}",
			["ru"] = "gpu: {0}"
		},
		["profile.temp_fmt"] = new Dictionary<string, string>
		{
			["en"] = "temp: {0:F2}",
			["ru"] = "температура: {0:F2}"
		},
		["dash.vram_gb_fmt"] = new Dictionary<string, string>
		{
			["en"] = " {0:F1} GB",
			["ru"] = " {0:F1} ГБ"
		},
		["settings.build_type_label"] = new Dictionary<string, string>
		{
			["en"] = "Build type:",
			["ru"] = "Тип сборки:"
		},
		["settings.cuda12x"] = new Dictionary<string, string>
		{
			["en"] = "CUDA 12.x (Recommended)",
			["ru"] = "CUDA 12.x (рекомендуется)"
		},
		["settings.cuda13x"] = new Dictionary<string, string>
		{
			["en"] = "CUDA 13.x",
			["ru"] = "CUDA 13.x"
		},
		["settings.vulkan"] = new Dictionary<string, string>
		{
			["en"] = "Vulkan",
			["ru"] = "Vulkan"
		},
		["settings.cpu_only"] = new Dictionary<string, string>
		{
			["en"] = "CPU Only",
			["ru"] = "Только CPU"
		},
		["settings.download_install_btn"] = new Dictionary<string, string>
		{
			["en"] = "Download & Install",
			["ru"] = "Скачать и установить"
		},
		["settings.install_btn"] = new Dictionary<string, string>
		{
			["en"] = "Install",
			["ru"] = "Установить"
		},
		["settings.installing_btn"] = new Dictionary<string, string>
		{
			["en"] = "Installing...",
			["ru"] = "Установка..."
		},
		["api_test.title"] = new Dictionary<string, string>
		{
			["en"] = "API Test",
			["ru"] = "Тест API"
		},
		["api_test.health"] = new Dictionary<string, string>
		{
			["en"] = "Health Check",
			["ru"] = "Проверка здоровья"
		},
		["api_test.models"] = new Dictionary<string, string>
		{
			["en"] = "Models List",
			["ru"] = "Список моделей"
		},
		["api_test.chat"] = new Dictionary<string, string>
		{
			["en"] = "Chat Completion",
			["ru"] = "Chat Completion"
		},
		["api_test.completion"] = new Dictionary<string, string>
		{
			["en"] = "Completion",
			["ru"] = "Completion"
		},
		["api_test.custom"] = new Dictionary<string, string>
		{
			["en"] = "Custom Request",
			["ru"] = "Пользовательский запрос"
		},
		["api_test.test"] = new Dictionary<string, string>
		{
			["en"] = "Test",
			["ru"] = "Тест"
		},
		["api_test.run_all"] = new Dictionary<string, string>
		{
			["en"] = "Run All Tests",
			["ru"] = "Запустить все тесты"
		},
		["api_test.connection_settings"] = new Dictionary<string, string>
		{
			["en"] = "Connection Settings",
			["ru"] = "Настройки подключения"
		},
		["api_test.host"] = new Dictionary<string, string>
		{
			["en"] = "Host:",
			["ru"] = "Хост:"
		},
		["api_test.port"] = new Dictionary<string, string>
		{
			["en"] = "Port:",
			["ru"] = "Порт:"
		},
		["api_test.api_key"] = new Dictionary<string, string>
		{
			["en"] = "API Key:",
			["ru"] = "API-ключ:"
		},
		["api_test.api_key_optional"] = new Dictionary<string, string>
		{
			["en"] = "Optional",
			["ru"] = "Необязательно"
		},
		["api_test.timeout"] = new Dictionary<string, string>
		{
			["en"] = "Timeout (s):",
			["ru"] = "Таймаут (с):"
		},
		["api_test.method"] = new Dictionary<string, string>
		{
			["en"] = "Method:",
			["ru"] = "Метод:"
		},
		["api_test.path"] = new Dictionary<string, string>
		{
			["en"] = "Path:",
			["ru"] = "Путь:"
		},
		["api_test.path_watermark"] = new Dictionary<string, string>
		{
			["en"] = "/v1/models or full URL",
			["ru"] = "/v1/models или полный URL"
		},
		["api_test.body"] = new Dictionary<string, string>
		{
			["en"] = "Body:",
			["ru"] = "Тело:"
		},
		["api_test.body_watermark"] = new Dictionary<string, string>
		{
			["en"] = "JSON body optional",
			["ru"] = "JSON-тело (необязательно)"
		},
		["api_test.send"] = new Dictionary<string, string>
		{
			["en"] = "Send",
			["ru"] = "Отправить"
		},
		["api_test.chat_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Enter a message to send...",
			["ru"] = "Введите сообщение..."
		},
		["api_test.completion_watermark"] = new Dictionary<string, string>
		{
			["en"] = "Enter text to complete...",
			["ru"] = "Введите текст для завершения..."
		},
		["api_test.checking"] = new Dictionary<string, string>
		{
			["en"] = "Checking...",
			["ru"] = "Проверка..."
		},
		["api_test.fetching"] = new Dictionary<string, string>
		{
			["en"] = "Fetching models...",
			["ru"] = "Загрузка моделей..."
		},
		["api_test.sending_chat"] = new Dictionary<string, string>
		{
			["en"] = "Sending chat request...",
			["ru"] = "Отправка chat-запроса..."
		},
		["api_test.sending_completion"] = new Dictionary<string, string>
		{
			["en"] = "Sending completion request...",
			["ru"] = "Отправка completion-запроса..."
		},
		["api_test.sending_custom"] = new Dictionary<string, string>
		{
			["en"] = "Sending custom request...",
			["ru"] = "Отправка пользовательского запроса..."
		},
		["api_test.error"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: {0}",
			["ru"] = "ОШИБКА: {0}"
		},
		["api_test.path_empty"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: Path is empty",
			["ru"] = "ОШИБКА: Путь пуст"
		},
		["api_test.method_unsupported"] = new Dictionary<string, string>
		{
			["en"] = "ERROR: Unsupported method: {0}",
			["ru"] = "ОШИБКА: Неподдерживаемый метод: {0}"
		},
		["api_test.test_suite"] = new Dictionary<string, string>
		{
			["en"] = "=== API Test Suite ===",
			["ru"] = "=== Набор тестов API ==="
		},
		["api_test.base_url"] = new Dictionary<string, string>
		{
			["en"] = "Base URL: {0}",
			["ru"] = "Базовый URL: {0}"
		},
		["api_test.time"] = new Dictionary<string, string>
		{
			["en"] = "Time: {0:HH:mm:ss}",
			["ru"] = "Время: {0:HH:mm:ss}"
		},
		["api_test.health_section"] = new Dictionary<string, string>
		{
			["en"] = "--- Health Check ---",
			["ru"] = "--- Проверка здоровья ---"
		},
		["api_test.models_section"] = new Dictionary<string, string>
		{
			["en"] = "--- Models List ---",
			["ru"] = "--- Список моделей ---"
		},
		["api_test.last_test"] = new Dictionary<string, string>
		{
			["en"] = "Last test: {0:HH:mm:ss}",
			["ru"] = "Последний тест: {0:HH:mm:ss}"
		},
		["tt.language"] = new Dictionary<string, string>
		{
			["en"] = "Select application language (changes apply immediately)",
			["ru"] = "Выберите язык приложения (применяется сразу)"
		},
		["tt.llama_cpp_base_dir"] = new Dictionary<string, string>
		{
			["en"] = "Root directory where all llama.cpp versions are stored. Leave empty for default (AppData).",
			["ru"] = "Корневая папка для всех версий llama.cpp. Оставьте пустым для использования AppData."
		},
		["tt.llama_cpp_dir"] = new Dictionary<string, string>
		{
			["en"] = "Directory where llama.cpp binaries are installed. Auto-set after download.",
			["ru"] = "Папка с бинарниками llama.cpp. Заполняется автоматически после установки."
		},
		["tt.models_dir"] = new Dictionary<string, string>
		{
			["en"] = "Directory where GGUF model files are stored.",
			["ru"] = "Папка для хранения файлов моделей GGUF."
		},
		["tt.auto_check_updates"] = new Dictionary<string, string>
		{
			["en"] = "Automatically check for new llama.cpp releases on startup (sends a network request to GitHub)",
			["ru"] = "Автоматически проверять новые версии llama.cpp при запуске (отправляет сетевой запрос к GitHub)"
		},
		["tt.minimize_to_tray"] = new Dictionary<string, string>
		{
			["en"] = "When closing the window, minimize to system tray instead of exiting. Server keeps running.",
			["ru"] = "При закрытии окна сворачивать в трей вместо выхода. Сервер продолжает работать."
		},
		["tt.auto_start_with_os"] = new Dictionary<string, string>
		{
			["en"] = "Automatically start Llama Studio when you sign in. Uses the system startup mechanism.",
			["ru"] = "Автоматически запускать Llama Studio при входе в систему. Используется штатный механизм автозапуска ОС."
		},
		["tt.start_minimized"] = new Dictionary<string, string>
		{
			["en"] = "When starting, open the window minimized to system tray instead of showing it.",
			["ru"] = "При запуске сворачивать окно в системный трей вместо отображения на экране."
		},
		["tt.start_monitoring_window"] = new Dictionary<string, string>
		{
			["en"] = "Show floating monitoring window on startup when running minimized to tray. Disable if you don't want the monitor to appear.",
			["ru"] = "Показывать плавающее окно мониторинга при запуске в свёрнутом режиме. Отключите, если не хотите видеть монитор при старте."
		},

		["tt.show_desktop_window"] = new Dictionary<string, string>
		{
			["en"] = "Show the main application window on startup. Disable to run in headless mode (web interface only).",
			["ru"] = "Показывать главное окно программы при запуске. Отключите для headless-режима (только веб-интерфейс)."
		},
		["tt.keep_server_on_exit"] = new Dictionary<string, string>
		{
			["en"] = "When closing the application, keep the llama server running in the background. Useful for continuous chat sessions.",
			["ru"] = "При закрытии приложения не останавливать llama сервер. Полезно для непрерывных сессий чата."
		},
		["tt.auto_start_server"] = new Dictionary<string, string>
		{
			["en"] = "Automatically start the llama server with the default profile when the application launches.",
			["ru"] = "Автоматически запускать llama сервер с дефолтным профилем при старте приложения."
		},
		["tt.clear_clipboard_on_exit"] = new Dictionary<string, string>
		{
			["en"] = "Clear clipboard content when closing the application to prevent accidental paste of sensitive data.",
			["ru"] = "Очистить буфер обмена при выходе из приложения, чтобы предотвратить случайную вставку конфиденциальных данных."
		},
		["tt.portable_mode"] = new Dictionary<string, string>
		{
			["en"] = "Store all data in the application directory instead of AppData",
			["ru"] = "Хранить все данные в папке приложения вместо AppData"
		},
		["tt.theme"] = new Dictionary<string, string>
		{
			["en"] = "Application appearance theme",
			["ru"] = "Тема оформления приложения"
		},
		["tt.update_channel"] = new Dictionary<string, string>
		{
			["en"] = "Stable = official releases only. Prerelease includes beta builds.",
			["ru"] = "Стабильная = только официальные релизы. Предварительная включает бета-сборки."
		},
		["tt.host"] = new Dictionary<string, string>
		{
			["en"] = "Server listen address. 127.0.0.1 = local only, 0.0.0.0 = all interfaces.",
			["ru"] = "Адрес сервера. 127.0.0.1 = только локально, 0.0.0.0 = все интерфейсы."
		},
		["tt.host_default"] = new Dictionary<string, string>
		{
			["en"] = "Default: 127.0.0.1",
			["ru"] = "По умолчанию: 127.0.0.1"
		},
		["tt.port"] = new Dictionary<string, string>
		{
			["en"] = "Server port number (1-65535). Default: 8080",
			["ru"] = "Порт сервера (1-65535). По умолчанию: 8080"
		},
		["tt.port_manual"] = new Dictionary<string, string>
		{
			["en"] = "Enter port number manually",
			["ru"] = "Введите номер порта вручную"
		},
		["tt.gpu_layers"] = new Dictionary<string, string>
		{
			["en"] = "Number of layers to offload to GPU. 99 = all layers. 0 = CPU only.",
			["ru"] = "Количество слоёв для загрузки на GPU. 99 = все слои. 0 = только CPU."
		},
		["tt.gpu_layers_default"] = new Dictionary<string, string>
		{
			["en"] = "Default: 99 (all layers on GPU)",
			["ru"] = "По умолчанию: 99 (все слои на GPU)"
		},
		["tt.flash_attention"] = new Dictionary<string, string>
		{
			["en"] = "Enable Flash Attention for faster inference with lower memory usage",
			["ru"] = "Включить Flash Attention для ускорения инференса с меньшим расходом памяти"
		},
		["tt.install_build"] = new Dictionary<string, string>
		{
			["en"] = "Download and install this build",
			["ru"] = "Скачать и установить эту сборку"
		},
		["tray.start_server"] = new Dictionary<string, string>
		{
			["en"] = "Start Server",
			["ru"] = "Запустить сервер"
		},
		["tray.stop_server"] = new Dictionary<string, string>
		{
			["en"] = "Stop Server",
			["ru"] = "Остановить сервер"
		},
		["tray.restart_server"] = new Dictionary<string, string>
		{
			["en"] = "Restart Server",
			["ru"] = "Перезапустить сервер"
		},
		["tray.profiles"] = new Dictionary<string, string>
		{
			["en"] = "Profiles",
			["ru"] = "Профили"
		},
		["tray.show"] = new Dictionary<string, string>
		{
			["en"] = "Show",
			["ru"] = "Показать"
		},
		["tray.exit"] = new Dictionary<string, string>
		{
			["en"] = "Exit",
			["ru"] = "Выход"
		},
		["chat.generating"] = new Dictionary<string, string>
		{
			["en"] = "Model is generating response...",
			["ru"] = "Модель генерирует ответ..."
		},
		["chat.generating_base"] = new Dictionary<string, string>
		{
			["en"] = "Model is generating response",
			["ru"] = "Модель генерирует ответ"
		},
		["dialog.loading"] = new Dictionary<string, string>
		{
			["en"] = "Loading",
			["ru"] = "Загрузка"
		},
		["update.downloaded_restart"] = new Dictionary<string, string>
		{
			["en"] = "{0} downloaded.\n\nRestart the application now to apply updates?",
			["ru"] = "Обновление {0} скачано.\n\nПерезапустить приложение сейчас для применения обновлений?"
		},
		["update.ready_title"] = new Dictionary<string, string>
		{
			["en"] = "Update Ready",
			["ru"] = "Обновление готово"
		},
		["update.linux_cannot_update"] = new Dictionary<string, string>
		{
			["en"] = "Cannot apply the update automatically: the application is installed in a folder that cannot be replaced (missing permissions or folder too high in the directory tree).\n\nUpdate manually: download the new Linux tar.gz from the Releases page, extract it next to the current folder, and start LlamaStudio from the new folder.",
			["ru"] = "Не удалось применить обновление автоматически: приложение установлено в папку, которую нельзя заменить (нет прав на запись или папка слишком высоко в дереве каталогов).\n\nОбновите вручную: скачайте новый Linux tar.gz со страницы Releases, распакуйте рядом с текущей папкой и запустите LlamaStudio из новой папки."
		},
		["update.ready_restart_later"] = new Dictionary<string, string>
		{
			["en"] = "{0} is ready to install.\n\nThe application will restart on next launch.",
			["ru"] = "Обновление {0} готово к установке.\n\nПриложение будет перезапущено при следующем запуске."
		},
		["argpicker.requires_value_enum"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a value: [{1}]",
			["ru"] = "Аргумент {0} требует значение: [{1}]"
		},
		["argpicker.invalid_value_enum"] = new Dictionary<string, string>
		{
			["en"] = "Invalid value for {0}: \"{1}\". Allowed: [{2}]",
			["ru"] = "Недопустимое значение для {0}: «{1}». Допустимо: [{2}]"
		},
		["argpicker.requires_numeric"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a numeric value",
			["ru"] = "Аргумент {0} требует числовое значение"
		},
		["argpicker.invalid_numeric"] = new Dictionary<string, string>
		{
			["en"] = "Invalid value for {0}: \"{1}\". A number is required.",
			["ru"] = "Недопустимое значение для {0}: «{1}». Требуется число."
		},
		["argpicker.requires_value"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a value",
			["ru"] = "Аргумент {0} требует значение"
		},
		["cli_args.title"] = new Dictionary<string, string>
		{
			["en"] = "CLI Args Picker",
			["ru"] = "Выбор аргументов CLI"
		},
		["cli_args.filter"] = new Dictionary<string, string>
		{
			["en"] = "Filter flags...",
			["ru"] = "Фильтр флагов..."
		},
		["cli_args.add_btn"] = new Dictionary<string, string>
		{
			["en"] = "Add",
			["ru"] = "Добавить"
		},
		["cli_args.cancel_btn"] = new Dictionary<string, string>
		{
			["en"] = "Cancel",
			["ru"] = "Отмена"
		},
		["cli_args.selected_count_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Selected: {0}",
			["ru"] = "Выбрано: {0}"
		},
		["cli_args.no_args"] = new Dictionary<string, string>
		{
			["en"] = "Failed to load arguments. Check llama-server.exe path",
			["ru"] = "Не удалось загрузить аргументы. Проверьте путь к llama-server.exe"
		},
		["cli_args.cat.common"] = new Dictionary<string, string>
		{
			["en"] = "Common",
			["ru"] = "Общие"
		},
		["cli_args.cat.sampling"] = new Dictionary<string, string>
		{
			["en"] = "Sampling",
			["ru"] = "Сэмплирование"
		},
		["cli_args.cat.speculative"] = new Dictionary<string, string>
		{
			["en"] = "Speculative Decoding",
			["ru"] = "Спекулятивное декодирование"
		},
		["cli_args.cat.server"] = new Dictionary<string, string>
		{
			["en"] = "Server",
			["ru"] = "Сервер"
		},
		["cli_args.cat.other"] = new Dictionary<string, string>
		{
			["en"] = "Other",
			["ru"] = "Другое"
		},
		["server.select_profile_first"] = new Dictionary<string, string>
		{
			["en"] = "Select a profile first.",
			["ru"] = "Сначала выберите профиль."
		},
		["server.bat_save_folder_title"] = new Dictionary<string, string>
		{
			["en"] = "Select folder to save .bat file",
			["ru"] = "Выберите папку для сохранения .bat файла"
		},
		["server.bat_saved_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Batch file saved:\n{0}",
			["ru"] = "Батник сохранён:\n{0}"
		},
		["server.command_copied"] = new Dictionary<string, string>
		{
			["en"] = "Command copied to clipboard",
			["ru"] = "Команда скопирована в буфер обмена"
		},
		["server.bat_import_title"] = new Dictionary<string, string>
		{
			["en"] = "Select .bat file to import",
			["ru"] = "Выберите .bat файл для импорта"
		},
		["server.no_command_in_bat"] = new Dictionary<string, string>
		{
			["en"] = "No command found in batch file",
			["ru"] = "Не найдена команда в батнике"
		},
		["server.imported_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Imported {0} parameters from batch file",
			["ru"] = "Импортировано {0} параметров из батника"
		},
		["server.import_error_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Import error: {0}",
			["ru"] = "Ошибка импорта: {0}"
		},
		["server.check_version_first"] = new Dictionary<string, string>
		{
			["en"] = "First click «Check Version» to load the flag list.",
			["ru"] = "Сначала нажмите «Проверить версию», чтобы загрузить список флагов."
		},
		["server.arg_requires_value_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a value: {1}",
			["ru"] = "Аргумент {0} требует значение: {1}"
		},
		["server.arg_invalid_value_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Invalid value for {0}: \"{1}\". Allowed: {2}",
			["ru"] = "Недопустимое значение для {0}: \"{1}\". Допустимо: {2}"
		},
		["server.cli_all_valid"] = new Dictionary<string, string>
		{
			["en"] = "✓ All flags valid",
			["ru"] = "✓ Все флаги корректны"
		},
		["server.cli_new_flags_fmt"] = new Dictionary<string, string>
		{
			["en"] = "+ {0} new flags available in binary",
			["ru"] = "+ {0} новых флагов доступно в бинарнике"
		},
		["server.cli_unsupported_fmt"] = new Dictionary<string, string>
		{
			["en"] = "✗ {0} flag(s) not supported by current binary",
			["ru"] = "✗ {0} флага(ов) не поддерживаются текущим бинарником"
		},
		["server.cli_replace_with_fmt"] = new Dictionary<string, string>
		{
			["en"] = "  ✗ {0} → replace with {1}",
			["ru"] = "  ✗ {0} → замените на {1}"
		},
		["server.cli_removed_fmt"] = new Dictionary<string, string>
		{
			["en"] = "  ✗ {0} — removed from llama.cpp",
			["ru"] = "  ✗ {0} — удалён из llama.cpp"
		},
		["server.cli_new_available_fmt"] = new Dictionary<string, string>
		{
			["en"] = "+ {0} new flags available",
			["ru"] = "+ {0} новых флагов доступно"
		},
		["server.copy_to_profile_fmt"] = new Dictionary<string, string>
		{
			["en"] = "Into current profile \"{0}\"",
			["ru"] = "В текущий профиль «{0}»"
		},
		["cli_args.desc.yarn_ext_factor"] = new Dictionary<string, string>
		{
			["en"] = "YARN extension factor — increases context through attention scaling",
			["ru"] = "Множитель расширения YARN — увеличивает контекст за счёт масштабирования внимания"
		},
		["cli_args.desc.yarn_attn_factor"] = new Dictionary<string, string>
		{
			["en"] = "YARN attention factor — correction coefficient for attention during context expansion",
			["ru"] = "Фактор внимания YARN — коэффициент для коррекции внимания при расширении контекста"
		},
		["cli_args.desc.yarn_beta_fast"] = new Dictionary<string, string>
		{
			["en"] = "YARN fast beta parameter — convergence rate for fast Rope frequencies",
			["ru"] = "Быстрый бета-параметр YARN — скорость сходимости для быстрых частот Rope"
		},
		["cli_args.desc.yarn_beta_slow"] = new Dictionary<string, string>
		{
			["en"] = "YARN slow beta parameter — convergence rate for slow Rope frequencies",
			["ru"] = "Медленный бета-параметр YARN — скорость сходимости для медленных частот Rope"
		},
		["cli_args.desc.spec_type"] = new Dictionary<string, string>
		{
			["en"] = "Speculative decoding type: ngram, draft-mtp, or draft",
			["ru"] = "Тип спекулятивного декодирования: ngram, draft-mtp или draft"
		},
		["cli_args.desc.spec_max_chunks"] = new Dictionary<string, string>
		{
			["en"] = "Maximum number of chunks for speculation",
			["ru"] = "Макс. число чанков для спекуляции"
		},
		["cli_args.desc.spec_max_n_tokens"] = new Dictionary<string, string>
		{
			["en"] = "Maximum tokens per speculation step",
			["ru"] = "Макс. токенов на один шаг спекуляции"
		},
		["cli_args.desc.spec_p_split"] = new Dictionary<string, string>
		{
			["en"] = "Chunk split probability during speculation",
			["ru"] = "Вероятность разделения чанков при спекуляции"
		},
		["cli_args.desc.spec_p_min"] = new Dictionary<string, string>
		{
			["en"] = "Minimum draft token acceptance probability",
			["ru"] = "Мин. вероятность принятия токена черновика"
		},
		["cli_args.desc.draft_max_n_tokens"] = new Dictionary<string, string>
		{
			["en"] = "Maximum tokens per step for the draft model",
			["ru"] = "Макс. токенов для модели-черновика за шаг"
		},
		["cli_args.desc.cache_type_k"] = new Dictionary<string, string>
		{
			["en"] = "KV cache quantization type for K tensors (f16, q8_0, q4_0)",
			["ru"] = "Тип квантования KV-кэша для тензоров K (f16, q8_0, q4_0)"
		},
		["cli_args.desc.cache_type_v"] = new Dictionary<string, string>
		{
			["en"] = "KV cache quantization type for V tensors (f16, q8_0, q4_0)",
			["ru"] = "Тип квантования KV-кэша для тензоров V (f16, q8_0, q4_0)"
		},
		["cli_args.desc.cache_ram"] = new Dictionary<string, string>
		{
			["en"] = "Maximum KV cache size in MiB (-1 = unlimited)",
			["ru"] = "Макс. размер KV-кэша в МиБ (-1 = без ограничения)"
		},
		["cli_args.desc.cache_reuse"] = new Dictionary<string, string>
		{
			["en"] = "Minimum chunk size for cache reuse (tokens)",
			["ru"] = "Мин. размер чанка для переиспользования кэша (токены)"
		},
		["cli_args.desc.tensor_split"] = new Dictionary<string, string>
		{
			["en"] = "Tensor distribution across GPUs (comma-separated, e.g. 1,0.5)",
			["ru"] = "Распределение тензоров между GPU (через запятую, напр. 1,0.5)"
		},
		["cli_args.desc.logits_file"] = new Dictionary<string, string>
		{
			["en"] = "File to write logits to",
			["ru"] = "Файл для записи логитов"
		},
		["cli_args.desc.infill_prefix_len"] = new Dictionary<string, string>
		{
			["en"] = "Prefix length for code infill generation",
			["ru"] = "Длина префикса для infill-генерации кода"
		},
		["support.title"] = new Dictionary<string, string>
		{
			["en"] = "Discussion & Support",
			["ru"] = "Обсуждение и поддержка"
		},
		["support.telegram_title"] = new Dictionary<string, string>
		{
			["en"] = "Telegram: Llama Studio",
			["ru"] = "Telegram: Llama Studio"
		},
		["support.telegram_desc"] = new Dictionary<string, string>
		{
			["en"] = "Join our Telegram community. Get help, suggest improvements, and follow program updates here.",
			["ru"] = "Присоединяйтесь к нашему сообществу в Telegram. Здесь вы можете получить помощь, предложить улучшения и следить за обновлениями программы."
		},
		["support.open_channel"] = new Dictionary<string, string>
		{
			["en"] = "Open Channel",
			["ru"] = "Открыть канал"
		},
		["support.support_chat_btn"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udcac Support Chat",
			["ru"] = "\ud83d\udcac Чат поддержки"
		},
		["support.helper_bot_btn"] = new Dictionary<string, string>
		{
			["en"] = "\ud83e\udd16 Helper Bot",
			["ru"] = "\ud83e\udd16 Бот-помощник"
		},
		["support.github_title"] = new Dictionary<string, string>
		{
			["en"] = "GitHub: Llama Studio",
			["ru"] = "GitHub: Llama Studio"
		},
		["support.github_desc"] = new Dictionary<string, string>
		{
			["en"] = "Project repository, releases and documentation.",
			["ru"] = "Репозиторий проекта, релизы и документация."
		},
		["support.open_repo"] = new Dictionary<string, string>
		{
			["en"] = "Open Repository",
			["ru"] = "Открыть репозиторий"
		},
		["support.download_latest"] = new Dictionary<string, string>
		{
			["en"] = "Download Latest Version",
			["ru"] = "Скачать последнюю версию"
		},
		["support.where_to_find"] = new Dictionary<string, string>
		{
			["en"] = "Where to find what?",
			["ru"] = "Где что искать?"
		},
		["support.news_channel"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udce2 News and updates — in the channel",
			["ru"] = "\ud83d\udce2 Новости и обновления — в канале"
		},
		["support.questions_chat"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udcac Questions and help — in the support chat",
			["ru"] = "\ud83d\udcac Вопросы и помощь — в чате поддержки"
		},
		["support.quick_answers_bot"] = new Dictionary<string, string>
		{
			["en"] = "\ud83e\udd16 Quick answers — via bot @LlamaStudioSupport_bot",
			["ru"] = "\ud83e\udd16 Быстрые ответы — через бота @LlamaStudioSupport_bot"
		},
		["support.report_bug"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udc1b Bug report — describe the issue in chat or via bot",
			["ru"] = "\ud83d\udc1b Сообщение об ошибке — опишите проблему в чате или через бота"
		},
		["dash.saved_count_fmt"] = new Dictionary<string, string>
		{
			["en"] = "{0} saved",
			["ru"] = "{0} сохранено"
		},
		["dialog.close_btn"] = new Dictionary<string, string>
		{
			["en"] = "Close",
			["ru"] = "Закрыть"
		},
		["common.loading"] = new Dictionary<string, string>
		{
			["en"] = "Loading",
			["ru"] = "Загрузка"
		},
		["chat.server_not_ready"] = new Dictionary<string, string>
		{
			["en"] = "⚠\ufe0f Server is not running or the model is still loading. Wait and try again.",
			["ru"] = "⚠\ufe0f Сервер не запущен или модель ещё загружается. Подождите и попробуйте снова."
		},
		["chat.model_loading"] = new Dictionary<string, string>
		{
			["en"] = "⚠\ufe0f Model is still loading on the GPU. Wait a minute and try again.",
			["ru"] = "⚠\ufe0f Модель ещё загружается на видеокарту. Подождите минуту и попробуйте снова."
		},
		["chat.error_prefix"] = new Dictionary<string, string>
		{
			["en"] = "⚠\ufe0f Error: ",
			["ru"] = "⚠\ufe0f Ошибка: "
		},
		["chat.compressing"] = new Dictionary<string, string>
		{
			["en"] = "⏳ Compressing chat history to save context...",
			["ru"] = "⏳ Сжатие истории чата для экономии контекста..."
		},
		["chat.compress_failed"] = new Dictionary<string, string>
		{
			["en"] = "[Compression failed, history kept as-is]",
			["ru"] = "[Сжатие не удалось, история сохранена полностью]"
		},
		["chat.compressed_count"] = new Dictionary<string, string>
		{
			["en"] = "\ud83d\udccb Compressed {0} messages. Summary: {1}",
			["ru"] = "\ud83d\udccb Сжато {0} сообщений. Резюме: {1}"
		},
		["chat.summary_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Summarize this dialogue in a brief summary (3-5 sentences):\n{0}\n---\nSummary:",
			["ru"] = "Сожми этот диалог в краткое резюме (3-5 предложений):\n{0}\n---\nРезюме:"
		},
		["dialog.pick_images"] = new Dictionary<string, string>
		{
			["en"] = "Select images",
			["ru"] = "Выбрать изображения"
		},
		["dialog.pick_files"] = new Dictionary<string, string>
		{
			["en"] = "Select files",
			["ru"] = "Выбрать файлы"
		},
		["dialog.error_title"] = new Dictionary<string, string>
		{
			["en"] = "Error",
			["ru"] = "Ошибка"
		},
		["tip.title"] = new Dictionary<string, string>
		{
			["en"] = "Tip",
			["ru"] = "Подсказка"
		},
		["releases.experimental"] = new Dictionary<string, string>
		{
			["en"] = "Experimental",
			["ru"] = "Экспериментальная"
		},
		["argpicker.requires_value_enum_fb"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a value: [{1}]",
			["ru"] = "Аргумент {0} требует значение: [{1}]"
		},
		["argpicker.invalid_value_enum_fb"] = new Dictionary<string, string>
		{
			["en"] = "Invalid value for {0}: \"{1}\". Allowed: [{2}]",
			["ru"] = "Недопустимое значение для {0}: \"{1}\". Допустимо: [{2}]"
		},
		["argpicker.requires_numeric_fb"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a numeric value",
			["ru"] = "Аргумент {0} требует числовое значение"
		},
		["argpicker.invalid_numeric_fb"] = new Dictionary<string, string>
		{
			["en"] = "Invalid value for {0}: \"{1}\". A number is required.",
			["ru"] = "Недопустимое значение для {0}: \"{1}\". Требуется число."
		},
		["argpicker.requires_value_fb"] = new Dictionary<string, string>
		{
			["en"] = "Argument {0} requires a value",
			["ru"] = "Аргумент {0} требует значение"
		},
		["server.tip.additional_args"] = new Dictionary<string, string>
		{
			["en"] = "What is this:\nA field for entering any additional llama-server command-line arguments not available in the GUI.\n\nValues:\n• Any valid llama-server flag, one per line\n• Examples: --flash-attn-best-fit, --ctx-size-ext 65536\n\nRecommendations:\n• Use for experimental flags from the llama.cpp documentation\n• Each argument on a new line",
			["ru"] = "Что это:\nПоле для ввода любых дополнительных аргументов командной строки llama-server, которых нет в графическом интерфейсе.\n\nЗначения:\n• Любой валидный флаг llama-server, по одному на строку\n• Примеры: --flash-attn-best-fit, --ctx-size-ext 65536\n\nРекомендации:\n• Используйте для экспериментальных флагов из документации llama.cpp\n• Каждый аргумент с новой строки"
		},
		["server.tip.chip_remove"] = new Dictionary<string, string>
		{
			["en"] = "LMB — remove argument",
			["ru"] = "ЛКМ — удалить аргумент"
		},
		["server.tip.model_path"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -m\n\nWhat is this:\nPath to the GGUF model file to load. This is the main neural network that will respond to your requests.\n\nValues:\n• Full path to .gguf file\n• Drag and drop file into the field is supported\n\nRecommendations:\n• Use models with Instruct in the name for chat\n• Model size depends on VRAM/RAM: 7B (~4GB), 13B (~8GB), 70B (~40GB)",
			["ru"] = "Флаг: -m\n\nЧто это:\nПуть к файлу GGUF модели для загрузки. Это основная нейросеть, которая будет отвечать на ваши запросы.\n\nЗначения:\n• Полный путь к .gguf файлу\n• Поддерживается перетаскивание файла в поле\n\nРекомендации:\n• Используйте модели с Instruct в названии для чата\n• Размер модели зависит от VRAM/RAM: 7B (~4GB), 13B (~8GB), 70B (~40GB)"
		},
		["server.tip.mmproj"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mmproj\n\nWhat is this:\nMultimodal projector file. Allows a text model to see and analyze images.\n\nWhen to enable:\n• Only for vision models (LLaVA, Bakllava, etc.)\n• Normal language models do NOT need this\n\nRecommendations:\n• The projector file comes with the vision model when downloading\n• Without this file, the model cannot process images",
			["ru"] = "Флаг: --mmproj\n\nЧто это:\nФайл мультимодального проектора. Позволяет текстовой модели видеть и анализировать изображения.\n\nКогда включать:\n• Только для vision-моделей (LLaVA, Bakllava и т.д.)\n• Обычным языковым моделям НЕ нужен\n\nРекомендации:\n• Файл проектора идёт вместе с vision-моделью при скачивании\n• Без этого файла модель не сможет обрабатывать картинки"
		},
		["server.tip.gpu_layers"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ngl\n\nWhat is this:\nDetermines how many neural network layers will run on the GPU. More layers on GPU = faster text generation.\n\nValues:\n• Number (e.g. 32) — exactly that many layers on GPU\n• all — all layers on GPU (maximum speed)\n• auto — auto-detect based on available VRAM\n• 0 — all on CPU (slow, but works without a GPU)\n\nRecommendations:\n• For most tasks, set to all\n• If VRAM is insufficient — decrease the number or use auto",
			["ru"] = "Флаг: -ngl\n\nЧто это:\nОпределяет, сколько слоёв нейросети будет работать на видеокарте. Больше слоёв на GPU = быстрее генерация текста.\n\nЗначения:\n• Число (например 32) — точно столько слоёв на GPU\n• all — все слои на GPU (максимальная скорость)\n• auto — автоопределение по доступной VRAM\n• 0 — всё на CPU (медленно, но работает без видеокарты)\n\nРекомендации:\n• Для большинства задач ставьте all\n• Если не хватает VRAM — уменьшите число или используйте auto"
		},
		["server.tip.threads"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -t\n\nWhat is this:\nNumber of CPU threads for text generation. Affects speed when using CPU.\n\nValues:\n• Number (1-128) — exact thread count\n• 0 or not set — auto-detect\n\nRecommendations:\n• Leave default for automatic selection\n• For CPU generation, set to the number of CPU cores",
			["ru"] = "Флаг: -t\n\nЧто это:\nКоличество потоков процессора для генерации текста. Влияет на скорость работы при использовании CPU.\n\nЗначения:\n• Число (1-128) — точное количество потоков\n• 0 или не задано — автоопределение\n\nРекомендации:\n• Оставьте по умолчанию для автоматического выбора\n• Для CPU-генерации ставьте число равное ядрам процессора"
		},
		["server.tip.threads_batch"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -tb\n\nWhat is this:\nNumber of threads for processing incoming prompts (text you send to the model).\n\nValues:\n• Number (1-128) — exact thread count\n• 0 or not set — equals -t value\n\nRecommendations:\n• Usually leave default (equals -t)\n• Increase if long messages are processed slowly",
			["ru"] = "Флаг: -tb\n\nЧто это:\nКоличество потоков для обработки входящего промпта (текста, который вы отправляете модели).\n\nЗначения:\n• Число (1-128) — точное количество потоков\n• 0 или не задано — равно значению -t\n\nРекомендации:\n• Обычно оставляйте по умолчанию (равно -t)\n• Увеличьте если медленная обработка длинных сообщений"
		},
		["server.tip.flash_attention"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -fa / --flash-attn\n\nWhat is this:\nOptimized attention computation algorithm. Significantly speeds up processing and saves memory.\n\nBenefits:\n• 2-4x faster prompt processing\n• Less VRAM usage for the same context\n• Allows larger context sizes\n\nRecommendations:\n• Keep enabled for maximum performance\n• Only disable if you encounter errors (rare)",
			["ru"] = "Флаг: -fa / --flash-attn\n\nЧто это:\nОптимизированный алгоритм вычисления внимания (attention). Значительно ускоряет работу и экономит память.\n\nПреимущества:\n• Ускорение обработки промпта в 2-4 раза\n• Меньше потребление VRAM при том же контексте\n• Позволяет использовать больший размер контекста\n\nРекомендации:\n• Оставьте включённым для максимальной производительности\n• Если возникают ошибки — попробуйте отключить (редко нужно)"
		},
		["server.tip.mmap"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mmap\n\nWhat is this:\nModel loading via memory-mapped files. The OS manages caching of model data in memory.\n\nBenefits:\n• Fast model loading\n• OS can swap out unused parts when needed\n• Less peak RAM usage on startup\n\nRecommendations:\n• Keep enabled (default)\n• Only disable if the model gets evicted from memory during multitasking",
			["ru"] = "Флаг: --mmap\n\nЧто это:\nЗагрузка модели через memory-mapped файлы. Операционная система сама управляет кэшированием данных модели в памяти.\n\nПреимущества:\n• Быстрая загрузка модели\n• ОС может выгрузить неиспользуемые части в swap при необходимости\n• Меньше пиковое потребление RAM при старте\n\nРекомендации:\n• Оставьте включённым (по умолчанию)\n• Отключите только если модель вылетает из памяти при многозадачности"
		},
		["server.tip.mlock"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mlock\n\nWhat is this:\nKeeps the model in RAM, preventing swapping to disk when memory is low.\n\nBenefits:\n• Stable performance without stuttering\n• Model doesn't slow down under system load\n\nRecommendations:\n• Enable if you have enough free RAM (1.5-2x the model size)\n• On systems with 16GB+ RAM for models up to 7B — definitely enable",
			["ru"] = "Флаг: --mlock\n\nЧто это:\nУдерживает модель в оперативной памяти, предотвращая своппинг (перемещение на диск) при нехватке RAM.\n\nПреимущества:\n• Стабильная скорость работы без замираний\n• Модель не замедляется когда система загружена\n\nРекомендации:\n• Включите если у вас достаточно свободной RAM (в 1.5-2 раза больше модели)\n• На системах с 16GB+ RAM для моделей до 7B — обязательно включите"
		},
		["server.tip.kv_offload"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --kv-offload\n\nWhat is this:\nOffloading KV cache (intermediate results) to the GPU.\n\nBenefits:\n• Faster generation when VRAM is available\n• Less CPU and RAM load\n\nRecommendations:\n• Keep enabled for better performance\n• Only disable with critical VRAM shortage",
			["ru"] = "Флаг: --kv-offload\n\nЧто это:\nВыгрузка KV-кэша (промежуточных результатов) на видеокарту.\n\nПреимущества:\n• Быстрее генерация при наличии VRAM\n• Меньше нагрузка на CPU и RAM\n\nРекомендации:\n• Оставьте включённым для лучшей производительности\n• Отключите только при критической нехватке VRAM"
		},
		["server.tip.main_gpu"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --main-gpu\n\nWhat is this:\nIndex of the primary GPU for computation when multiple GPUs are present.\n\nValues:\n• 0 — first GPU (default)\n• 1, 2, 3... — second, third, etc.\n\nRecommendations:\n• Leave 0 if you have one GPU\n• With two cards: set the index of the more powerful card",
			["ru"] = "Флаг: --main-gpu\n\nЧто это:\nИндекс основной видеокарты для вычислений при наличии нескольких GPU.\n\nЗначения:\n• 0 — первая видеокарта (по умолчанию)\n• 1, 2, 3... — вторая, третья и т.д.\n\nРекомендации:\n• Оставьте 0 если у вас одна видеокарта\n• При двух картах: поставьте индекс более мощной карты"
		},
		["server.tip.tensor_split"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --tensor-split\n\nWhat is this:\nDistribution of compute load across multiple GPUs.\n\nValues:\n• Format: 50:50 — equal distribution on 2 GPUs\n• 70:30 — 70% on first, 30% on second\n\nRecommendations:\n• For two identical cards: 50:50\n• For different cards: proportional to their power",
			["ru"] = "Флаг: --tensor-split\n\nЧто это:\nРаспределение вычислительной нагрузки между несколькими видеокартами.\n\nЗначения:\n• Формат: 50:50 — равное распределение на 2 GPU\n• 70:30 — 70% на первый, 30% на второй\n\nРекомендации:\n• Для двух одинаковых карт: 50:50\n• Для разных карт: пропорционально их мощности"
		},
		["server.tip.ctx_size"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -c / --ctx-size\n\nWhat is this:\nMaximum dialog length in tokens. 1 token ≈ 0.75 words. Determines how much text the model can remember at once.\n\nValues:\n• 256–131072 (default: 4096)\n• Examples: 4096 ≈ 3000 words, 8192 ≈ 6000 words, 32768 ≈ 24000 words\n\nRecommendations:\n• Normal chat: 4096-8192\n• Long documents: 16384+\n• More context = more VRAM/RAM (proportional)",
			["ru"] = "Флаг: -c / --ctx-size\n\nЧто это:\nМаксимальная длина диалога в токенах. 1 токен ≈ 0.75 слова. Определяет, сколько текста модель может запомнить за раз.\n\nЗначения:\n• 256–131072 (по умолчанию: 4096)\n• Примеры: 4096 ≈ 3000 слов, 8192 ≈ 6000 слов, 32768 ≈ 24000 слов\n\nРекомендации:\n• Обычный чат: 4096-8192\n• Длинные документы: 16384+\n• Больше контекст = больше VRAM/RAM (пропорционально)"
		},
		["server.tip.batch_size"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -b / --batch-size\n\nWhat is this:\nMaximum number of prompt tokens processed at once. Affects the speed of reading input text by the model.\n\nValues:\n• 64–131072 (default: 2048)\n\nBenefits:\n• Larger batch = faster processing of long prompts\n• More efficient GPU utilization\n\nRecommendations:\n• Leave 2048 for most tasks\n• Increase to 4096-8192 if long messages are processed slowly",
			["ru"] = "Флаг: -b / --batch-size\n\nЧто это:\nМаксимальное количество токенов промпта, обрабатываемых за раз. Влияет на скорость чтения входного текста моделью.\n\nЗначения:\n• 64–131072 (по умолчанию: 2048)\n\nПреимущества:\n• Больший батч = быстрее обработка длинных промптов\n• Эффективнее использование GPU\n\nРекомендации:\n• Оставьте 2048 для большинства задач\n• Увеличьте до 4096-8192 если медленно обрабатываются длинные сообщения"
		},
		["server.tip.ubatch_size"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ubatch / --u-batch-size\n\nWhat is this:\nPhysical batch size at the GPU level. Should be ≤ batch size (-b).\n\nValues:\n• 1–131072 (default: 512)\n\nRecommendations:\n• Usually leave 512 or equal to Batch value\n• Don't increase unnecessarily — may cause memory errors",
			["ru"] = "Флаг: -ubatch / --u-batch-size\n\nЧто это:\nФизический размер батча на уровне GPU. Должен быть ≤ размера батча (-b).\n\nЗначения:\n• 1–131072 (по умолчанию: 512)\n\nРекомендации:\n• Обычно оставляйте 512 или равным значению Batch\n• Не увеличивайте без необходимости — может вызвать ошибки памяти"
		},
		["server.tip.predict"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -n / --predict\n\nWhat is this:\nMaximum length of generated response in tokens. Limits how much text the model can write at once.\n\nValues:\n• -1 — unlimited (default)\n• 256–131072 — exact limit in tokens\n\nRecommendations:\n• For chat: 256-512 (short answers)\n• For long answers and articles: 1024+\n• -1 = no limit, the model will stop on its own",
			["ru"] = "Флаг: -n / --predict\n\nЧто это:\nМаксимальная длина генерируемого ответа в токенах. Ограничивает, сколько текста модель может написать за один раз.\n\nЗначения:\n• -1 — без ограничений (по умолчанию)\n• 256–131072 — точное ограничение в токенах\n\nРекомендации:\n• Для чата: 256-512 (короткие ответы)\n• Для длинных ответов и статей: 1024+\n• -1 = без ограничений, модель остановится сама"
		},
		["server.tip.temperature"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --temp / -temp\n\nWhat is this:\nControls creativity and randomness of model responses. The most important parameter for controlling response style.\n\nValues:\n• 0.0 — fully deterministic\n• 0.1-0.3 — very precise, factual answers\n• 0.4-0.6 — balance between accuracy and variety\n• 0.7-1.0 — creative, diverse answers\n• 1.5+ — very random\n\nRecommendations:\n• For code and facts: 0.2-0.4\n• For normal chat: 0.6-0.8\n• For creativity: 0.8-1.2",
			["ru"] = "Флаг: --temp / -temp\n\nЧто это:\nУправляет креативностью и случайностью ответов модели. Самый важный параметр для контроля стиля ответов.\n\nЗначения:\n• 0.0 — полностью детерминированный ответ\n• 0.1-0.3 — очень точные, фактические ответы\n• 0.4-0.6 — баланс между точностью и разнообразием\n• 0.7-1.0 — креативные, разнообразные ответы\n• 1.5+ — очень случайные\n\nРекомендации:\n• Для работы с кодом и фактами: 0.2-0.4\n• Для обычного чата: 0.6-0.8\n• Для творчества: 0.8-1.2"
		},
		["server.tip.top_p"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --top-p\n\nWhat is this:\nNucleus sampling — keeps only tokens whose cumulative probability does not exceed the given value P.\n\nValues:\n• 0.0–1.0 (default: 0.95)\n• 0.9 = more focused answers\n• 0.98 = more variety\n\nRecommendations:\n• 0.9-0.95 for most tasks (recommended)\n• Decrease to 0.8 if answers are too vague",
			["ru"] = "Флаг: --top-p\n\nЧто это:\nNucleus сэмплинг — оставляет только токены, чья суммарная вероятность не превышает заданное значение P.\n\nЗначения:\n• 0.0–1.0 (по умолчанию: 0.95)\n• 0.9 = более фокусированные ответы\n• 0.98 = больше разнообразия\n\nРекомендации:\n• 0.9-0.95 для большинства задач (рекомендуется)\n• Уменьшите до 0.8 если ответы слишком размытые"
		},
		["server.tip.top_k"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --top-k\n\nWhat is this:\nLimits the next token choice to only the K most probable options from the entire model vocabulary.\n\nValues:\n• 0 — disabled (all tokens considered)\n• 10-20 — very limited choice, more predictable\n• 40 — balance (default)\n• 100+ — almost unlimited\n\nRecommendations:\n• Leave 40 for most tasks\n• Decrease to 20 if answers are too random",
			["ru"] = "Флаг: --top-k\n\nЧто это:\nОграничивает выбор следующего токена только K наиболее вероятными вариантами из всего словаря модели.\n\nЗначения:\n• 0 — отключено (рассматриваются все токены)\n• 10-20 — очень ограниченный выбор, более предсказуемо\n• 40 — баланс (по умолчанию)\n• 100+ — почти без ограничений\n\nРекомендации:\n• Оставьте 40 для большинства задач\n• Уменьшите до 20 если ответы слишком случайные"
		},
		["server.tip.min_p"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --min-p\n\nWhat is this:\nMinimum token probability relative to the most probable. Filters out tokens significantly worse than the best option.\n\nValues:\n• 0.0 — disabled (default)\n• 0.05-0.1 — mild filtering (recommended)\n• 0.2+ — strict filtering\n\nRecommendations:\n• 0.05-0.1 works well for most tasks",
			["ru"] = "Флаг: --min-p\n\nЧто это:\nМинимальная вероятность токена относительно самого вероятного. Отсеивает токены, которые значительно хуже лучшего варианта.\n\nЗначения:\n• 0.0 — отключено (по умолчанию)\n• 0.05-0.1 — мягкая фильтрация (рекомендуется)\n• 0.2+ — строгая фильтрация\n\nРекомендации:\n• 0.05-0.1 хорошо работает для большинства задач"
		},
		["server.tip.typical_p"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --typical-p\n\nWhat is this:\nLocally typical sampling — keeps tokens that are typical for the given context.\n\nValues:\n• 1.0 — disabled (default)\n• 0.2-0.5 — more natural text\n\nRecommendations:\n• Leave 1.0 (disabled) for most tasks\n• Try 0.3-0.5 if you want a more human-like text style",
			["ru"] = "Флаг: --typical-p\n\nЧто это:\nЛокально типичное сэмплирование — оставляет токены, которые типичны для данного контекста.\n\nЗначения:\n• 1.0 — отключено (по умолчанию)\n• 0.2-0.5 — более естественный текст\n\nРекомендации:\n• Оставьте 1.0 (отключено) для большинства задач\n• Попробуйте 0.3-0.5 если хотите более человеческий стиль текста"
		},
		["server.tip.repeat_penalty"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --repeat-penalty / -rep-pen\n\nWhat is this:\nPenalty for repeating the same tokens. Prevents the model from looping on the same phrases.\n\nValues:\n• 1.0 — disabled (no penalty)\n• 1.1 — mild penalty (default, recommended)\n• 1.2-1.3 — moderate penalty\n• 1.5+ — strong penalty (may distort text)\n\nRecommendations:\n• Leave 1.1 for most tasks\n• Increase to 1.2-1.3 if the model repeats",
			["ru"] = "Флаг: --repeat-penalty / -rep-pen\n\nЧто это:\nШтраф за повторение одинаковых токенов. Предотвращает зацикливание модели на одних и тех же фразах.\n\nЗначения:\n• 1.0 — отключено (нет штрафа)\n• 1.1 — лёгкий штраф (по умолчанию, рекомендуется)\n• 1.2-1.3 — умеренный штраф\n• 1.5+ — сильный штраф (может исказить текст)\n\nРекомендации:\n• Оставьте 1.1 для большинства задач\n• Увеличьте до 1.2-1.3 если модель повторяется"
		},
		["server.tip.repeat_last_n"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --repeat-last-n\n\nWhat is this:\nToken window for checking repetitions. Determines how many last words the model will check for duplication.\n\nValues:\n• 64 — default in llama.cpp\n• -1 — entire context\n• 0 — disabled\n• Number — only the last N tokens\n\nRecommendations:\n• Leave 64 for most tasks\n• Set -1 to check the entire context",
			["ru"] = "Флаг: --repeat-last-n\n\nЧто это:\nОкно токенов для проверки повторений. Определяет, сколько последних слов модель будет проверять на дублирование.\n\nЗначения:\n• 64 — по умолчанию в llama.cpp\n• -1 — весь контекст\n• 0 — отключено\n• Число — только последние N токенов\n\nРекомендации:\n• Оставьте 64 для большинства задач\n• Поставьте -1 для проверки всего контекста"
		},
		["server.tip.presence_penalty"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --presence-penalty\n\nWhat is this:\nPenalty for the presence of a token in context. Encourages the model to use new words and topics (like GPT-4).\n\nValues:\n• -2.0–2.0 (default: 0.0)\n• 0.1-0.5 — mild topic diversity\n\nRecommendations:\n• Leave 0.0 for most tasks\n• 0.1-0.3 if you want more diverse answers",
			["ru"] = "Флаг: --presence-penalty\n\nЧто это:\nШтраф за присутствие токена в контексте. Поощряет модель использовать новые слова и темы (как в GPT-4).\n\nЗначения:\n• -2.0–2.0 (по умолчанию: 0.0)\n• 0.1-0.5 — лёгкое разнообразие тем\n\nРекомендации:\n• Оставьте 0.0 для большинства задач\n• 0.1-0.3 если хотите более разнообразные ответы"
		},
		["server.tip.frequency_penalty"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --frequency-penalty\n\nWhat is this:\nPenalty for token frequency. The more often a word appeared, the less likely it is to repeat (like GPT-4).\n\nValues:\n• -2.0–2.0 (default: 0.0)\n• 0.1-0.5 — reducing repetitive phrases\n\nRecommendations:\n• Leave 0.0 for most tasks",
			["ru"] = "Флаг: --frequency-penalty\n\nЧто это:\nШтраф за частоту появления токена. Чем чаще слово встречалось, тем меньше вероятность его повторения (как в GPT-4).\n\nЗначения:\n• -2.0–2.0 (по умолчанию: 0.0)\n• 0.1-0.5 — уменьшение повторяющихся фраз\n\nРекомендации:\n• Оставьте 0.0 для большинства задач"
		},
		["server.tip.seed"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --seed / -s\n\nWhat is this:\nInitial random number generator seed. Determines the starting point for response randomness.\n\nValues:\n• Any number (0, 42, 123...) — reproducible results\n• -1 or not set — random seed each time\n\nBenefits:\n• Same seed = same response to the same prompt\n• Useful for testing and debugging",
			["ru"] = "Флаг: --seed / -s\n\nЧто это:\nНачальное число генератора случайных чисел. Определяет точку старта для случайности ответов.\n\nЗначения:\n• Любое число (0, 42, 123...) — воспроизводимые результаты\n• -1 или не задано — случайный seed каждый раз\n\nПреимущества:\n• Один и тот же seed = один и тот же ответ на тот же промпт\n• Полезно для тестирования и отладки"
		},
		["server.tip.mirostat"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mirostat / --mirostat2\n\nWhat is this:\nAdaptive sampling algorithm that automatically adjusts response randomness to maintain a given perplexity level.\n\nBenefits:\n• Automatic temperature adjustment during generation\n• May improve quality with incorrect temp/top-p settings\n\nRecommendations:\n• Disabled by default — works fine without it\n• Try enabling if you want smart parameter adjustment",
			["ru"] = "Флаг: --mirostat / --mirostat2\n\nЧто это:\nАдаптивный алгоритм сэмплирования, который автоматически подстраивает случайность ответов для поддержания заданного уровня perplexity (непредсказуемости текста).\n\nПреимущества:\n• Автоматическая подстройка температуры в процессе генерации\n• Может улучшить качество при неправильных настройках temp/top-p\n\nРекомендации:\n• По умолчанию выключено — работает и без него\n• Попробуйте включить если хотите умную подстройку параметров"
		},
		["server.tip.mirostat_tau"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mirostat-tau\n\nWhat is this:\nTarget perplexity for Mirostat. Determines how surprising the text should be.\n\nValues:\n• 1.0 — very predictable text\n• 5.0 — balance (default)\n• 10.0 — more diverse and unexpected text\n\nRecommendations:\n• Leave 5.0 for most tasks",
			["ru"] = "Флаг: --mirostat-tau\n\nЧто это:\nЦелевая perplexity для Mirostat. Определяет, насколько удивительным должен быть текст.\n\nЗначения:\n• 1.0 — очень предсказуемый текст\n• 5.0 — баланс (по умолчанию)\n• 10.0 — более разнообразный и неожиданный текст\n\nРекомендации:\n• Оставьте 5.0 для большинства задач"
		},
		["server.tip.mirostat_eta"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --mirostat-eta\n\nWhat is this:\nMirostat algorithm adaptation speed. How quickly it adjusts parameters toward target perplexity.\n\nValues:\n• 0.0–1.0 (default: 0.1)\n\nRecommendations:\n• Leave 0.1 — optimal value",
			["ru"] = "Флаг: --mirostat-eta\n\nЧто это:\nСкорость адаптации алгоритма Mirostat. Как быстро он подстраивает параметры под целевую perplexity.\n\nЗначения:\n• 0.0–1.0 (по умолчанию: 0.1)\n\nРекомендации:\n• Оставьте 0.1 — оптимальное значение"
		},
		["server.tip.cache_type_k"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ctk\n\nWhat is this:\nData type for key (K) cache in the attention mechanism. Affects memory usage and speed.\n\nValues:\n• f16 — full precision, more VRAM\n• q8_0 — 8-bit quantization, balance\n• q4_0 — 4-bit quantization, VRAM savings\n\nRecommendations:\n• f16 for best quality\n• q8_0 for quality/memory balance",
			["ru"] = "Флаг: -ctk\n\nЧто это:\nТип данных для кэша ключей (K) в механизме внимания. Влияет на потребление памяти и скорость.\n\nЗначения:\n• f16 — полная точность, больше VRAM\n• q8_0 — 8-бит квантование, баланс\n• q4_0 — 4-бит квантование, экономия VRAM\n\nРекомендации:\n• f16 для лучшего качества\n• q8_0 для баланса качество/память"
		},
		["server.tip.cache_ram"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --cache-ram\n\nWhat is this:\nAmount of RAM in megabytes allocated for KV cache.\n\nRecommendations:\n• Leave empty for automatic detection\n• Specify if you need to limit RAM usage",
			["ru"] = "Флаг: --cache-ram\n\nЧто это:\nКоличество RAM в мегабайтах, выделенное для кэширования KV-кэша.\n\nРекомендации:\n• Оставьте пустым для автоматического определения\n• Укажите если нужно ограничить использование RAM"
		},
		["server.tip.cache_type_v"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ctv\n\nWhat is this:\nData type for value (V) cache in the attention mechanism. Affects memory usage and speed.\n\nValues:\n• f16 — full precision, more VRAM\n• q8_0 — 8-bit quantization, balance\n• q4_0 — 4-bit quantization, VRAM savings\n\nRecommendations:\n• f16 for best quality\n• q8_0 for quality/memory balance",
			["ru"] = "Флаг: -ctv\n\nЧто это:\nТип данных для кэша значений (V) в механизме внимания. Влияет на потребление памяти и скорость.\n\nЗначения:\n• f16 — полная точность, больше VRAM\n• q8_0 — 8-бит квантование, баланс\n• q4_0 — 4-бит квантование, экономия VRAM\n\nРекомендации:\n• f16 для лучшего качества\n• q8_0 для баланса качество/память"
		},
		["server.tip.cache_reuse"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --cache-reuse\n\nWhat is this:\nCache reuse buffer size in tokens. Allows reusing computations for common request prefixes.\n\nValues:\n• Number of tokens (default: 0 = disabled)\n\nRecommendations:\n• Enable for faster repeated queries with common prefixes\n• 0.6-0.8 of context size — good balance",
			["ru"] = "Флаг: --cache-reuse\n\nЧто это:\nРазмер буфера переиспользования кэша в токенах. Позволяет переиспользовать вычисления для общих префиксов запросов.\n\nЗначения:\n• Число токенов (по умолчанию: 0 = отключено)\n\nРекомендации:\n• Включите для ускорения повторных запросов с общими префиксами\n• 0.6-0.8 от размера контекста — хороший баланс"
		},
		["server.tip.speculative_decoding"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-model / -md\n\nWhat is this:\nA second, small assistant model. It quickly generates a draft response, and the main model checks and corrects it.\n\nBenefits:\n• 1.5-3x speedup with proper settings\n• Main model works more efficiently\n\nWhen to enable:\n• With a powerful GPU (8+ GB VRAM)\n• For large models (13B+) where speed matters\n\nRecommendations:\n• Use a light model (Phi-2, Qwen2-1.5B) as draft\n• Draft model should be significantly smaller than main",
			["ru"] = "Флаг: --spec-draft-model / -md\n\nЧто это:\nВторая, маленькая модель-ассистент. Она быстро генерирует черновик ответа, а основная модель его проверяет и исправляет.\n\nПреимущества:\n• Ускорение генерации в 1.5-3 раза при правильных настройках\n• Основная модель работает эффективнее\n\nКогда включать:\n• При наличии мощного GPU (8+ GB VRAM)\n• Для больших моделей (13B+) где скорость важна\n\nРекомендации:\n• Используйте лёгкую модель (Phi-2, Qwen2-1.5B) как draft\n• Draft модель должна быть значительно меньше основной"
		},
		["server.tip.draft_model_path"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-model\n\nWhat is this:\nPath to the draft model file for speculative decoding.\n\nRecommendations:\n• Use a light model (Phi-2, Qwen2-1.5B) as draft\n• Draft model should be significantly smaller than main\n• Format: .gguf",
			["ru"] = "Флаг: --spec-draft-model\n\nЧто это:\nПуть к файлу черновой (draft) модели для спекулятивного декодирования.\n\nРекомендации:\n• Используйте лёгкую модель (Phi-2, Qwen2-1.5B) как draft\n• Draft модель должна быть значительно меньше основной\n• Формат: .gguf"
		},
		["server.tip.spec_type"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-type\n\nWhat is this:\nType of speculative decoding. Determines the method for acceleration.\n\nValues:\n• draft — using a draft model\n• mtp — multi-token prediction (MTP)\n\nRecommendations:\n• draft — for most cases with a separate draft model\n• mtp — for models with built-in MTP support",
			["ru"] = "Флаг: --spec-type\n\nЧто это:\nТип спекулятивного декодирования. Определяет метод ускорения генерации.\n\nЗначения:\n• draft — использование черновой модели\n• mtp — много-токеновое предсказание (MTP)\n\nРекомендации:\n• draft — для большинства случаев с отдельной draft моделью\n• mtp — для моделей с встроенной поддержкой MTP"
		},
		["server.tip.mtp_predict"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -n / --predict\n\nWhat is this:\nMaximum length of generated response in tokens.\n\nValues:\n• -1 — unlimited (default)\n• 256–131072 — exact limit in tokens\n\nRecommendations:\n• For chat: 256-512 (short answers)\n• For long answers and articles: 1024+",
			["ru"] = "Флаг: -n / --predict\n\nЧто это:\nМаксимальная длина генерируемого ответа в токенах.\n\nЗначения:\n• -1 — без ограничений (по умолчанию)\n• 256–131072 — точное ограничение в токенах\n\nРекомендации:\n• Для чата: 256-512 (короткие ответы)\n• Для длинных ответов и статей: 1024+"
		},
		["server.tip.draft_gpu_layers"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ngld / --draft-ngl\n\nWhat is this:\nHow many layers of the draft model to offload to GPU.\n\nValues:\n• Number — exactly that many layers\n• auto — auto-detect based on VRAM (default)\n• all — all layers on GPU\n\nRecommendations:\n• Set to all for maximum speculative decoding speed",
			["ru"] = "Флаг: -ngld / --draft-ngl\n\nЧто это:\nСколько слоёв черновой (draft) модели выгрузить на видеокарту.\n\nЗначения:\n• Число — точно столько слоёв\n• auto — автоопределение по VRAM (по умолчанию)\n• all — все слои на GPU\n\nРекомендации:\n• Ставьте all для максимальной скорости спекулятивного декодирования"
		},
		["server.tip.spec_draft_n_max"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-n-max\n\nWhat is this:\nMaximum number of tokens the draft model generates per step.\n\nValues:\n• 1–20 (default: 3)\n\nRecommendations:\n• 3-5 works well for most cases\n• More = faster, but may be accepted less by the main model",
			["ru"] = "Флаг: --spec-draft-n-max\n\nЧто это:\nМаксимальное число токенов, которые draft модель генерирует за шаг.\n\nЗначения:\n• 1–20 (по умолчанию: 3)\n\nРекомендации:\n• 3-5 хорошо работает для большинства случаев\n• Больше = быстрее, но может быть меньше принятия основной моделью"
		},
		["server.tip.spec_draft_n_min"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-n-min\n\nWhat is this:\nMinimum number of tokens for draft per step.\n\nValues:\n• 0–10 (default: 0)\n\nRecommendations:\n• Leave 0 — optimal",
			["ru"] = "Флаг: --spec-draft-n-min\n\nЧто это:\nМинимальное число токенов для draft за шаг.\n\nЗначения:\n• 0–10 (по умолчанию: 0)\n\nРекомендации:\n• Оставьте 0 — оптимально"
		},
		["server.tip.spec_draft_p_split"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-p-split\n\nWhat is this:\nSplit probability for speculative decoding. Experimental setting.\n\nValues:\n• 0.0–1.0 (default: 0.10)\n\nRecommendations:\n• Leave default — experimental parameter",
			["ru"] = "Флаг: --spec-draft-p-split\n\nЧто это:\nВероятность разделения для спекулятивного декодирования. Экспериментальная настройка.\n\nЗначения:\n• 0.0–1.0 (по умолчанию: 0.10)\n\nРекомендации:\n• Оставьте по умолчанию — экспериментальный параметр"
		},
		["server.tip.spec_draft_p_min"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --spec-draft-p-min\n\nWhat is this:\nMinimum probability (greedy threshold) for accepting draft tokens.\n\nValues:\n• 0.0–1.0 (default: 0.00)\n\nRecommendations:\n• Leave 0 — optimal",
			["ru"] = "Флаг: --spec-draft-p-min\n\nЧто это:\nМинимальная вероятность (greedy threshold) для принятия draft токенов.\n\nЗначения:\n• 0.0–1.0 (по умолчанию: 0.00)\n\nРекомендации:\n• Оставьте 0 — оптимально"
		},
		["server.tip.rope_scaling"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --rope-scaling\n\nWhat is this:\nType of RoPE positional encoding scaling. Determines how the model handles context beyond training size.\n\nValues:\n• none — no scaling\n• linear — linear scaling\n• yarn — YARN (better quality extrapolation)\n\nRecommendations:\n• Use yarn for best quality when increasing context\n• linear — simple alternative",
			["ru"] = "Флаг: --rope-scaling\n\nЧто это:\nТип масштабирования позиционного кодирования RoPE. Определяет, как модель обрабатывает контекст за пределами тренировочного размера.\n\nЗначения:\n• none — без масштабирования\n• linear — линейное масштабирование\n• yarn — YARN (более качественная экстраполяция)\n\nРекомендации:\n• Используйте yarn для лучшего качества при увеличении контекста\n• linear — простая альтернатива"
		},
		["server.tip.rope_freq_base"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --rope-freq-base\n\nWhat is this:\nBase frequency of RoPE positional encoding. Affects how the model understands word order in text.\n\nValues:\n• Floating point number (default: from model)\n\nRecommendations:\n• Don't change unless necessary — used for fine-tuning to other context lengths",
			["ru"] = "Флаг: --rope-freq-base\n\nЧто это:\nБазовая частота позиционного кодирования RoPE. Влияет на то, как модель понимает порядок слов в тексте.\n\nЗначения:\n• Число с плавающей точкой (по умолчанию: из модели)\n\nРекомендации:\n• Не меняйте без необходимости — используется для fine-tuning под другие длины контекста"
		},
		["server.tip.rope_freq_scale"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --rope-freq-scale\n\nWhat is this:\nRoPE frequency scale. Allows extending model context beyond original size.\n\nValues:\n• Number > 0 (default: from model)\n• 2.0 — doubles effective context\n\nBenefits:\n• Allows using a model with more context than it was trained on\n• Alternative to YARN for context extrapolation",
			["ru"] = "Флаг: --rope-freq-scale\n\nЧто это:\nМасштаб частоты RoPE. Позволяет растянуть контекст модели за пределы оригинального размера.\n\nЗначения:\n• Число > 0 (по умолчанию: из модели)\n• 2.0 — удвоение эффективного контекста\n\nПреимущества:\n• Позволяет использовать модель с большим контекстом чем она обучена\n• Альтернатива YARN для экстраполяции контекста"
		},
		["server.tip.yarn_orig_ctx"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --yarn-orig-ctx\n\nWhat is this:\nOriginal model context size for YARN extrapolation. Required for correct YARN operation when increasing context.\n\nValues:\n• Number (default: from model)\n• For example, 8192 for Llama 3, 4096 for Llama 2\n\nRecommendations:\n• Set the model's original context size\n• Only needed when using YARN to increase context",
			["ru"] = "Флаг: --yarn-orig-ctx\n\nЧто это:\nОригинальный размер контекста модели для YARN-экстраполяции. Необходим для корректной работы YARN при увеличении контекста.\n\nЗначения:\n• Число (по умолчанию: из модели)\n• Например, 8192 для Llama 3, 4096 для Llama 2\n\nРекомендации:\n• Ставьте оригинальный размер контекста модели\n• Нужен только при использовании YARN для увеличения контекста"
		},
		["server.tip.yarn_ext_factor"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --yarn-ext-factor\n\nWhat is this:\nContext expansion factor for YARN. Determines how much context can be extended beyond original size.\n\nRecommendations:\n• Leave default for automatic detection\n• Only needed for fine YARN tuning",
			["ru"] = "Флаг: --yarn-ext-factor\n\nЧто это:\nКоэффициент расширения контекста для YARN. Определяет, насколько можно расширить контекст за пределы оригинального размера.\n\nРекомендации:\n• Оставьте по умолчанию для автоматического определения\n• Нужен только при тонкой настройке YARN"
		},
		["server.tip.yarn_attn_factor"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --yarn-attn-factor\n\nWhat is this:\nAttention scaling factor for YARN. Corrects attention weights during context extrapolation.\n\nRecommendations:\n• Leave default for automatic detection\n• Only needed for fine YARN tuning",
			["ru"] = "Флаг: --yarn-attn-factor\n\nЧто это:\nФактор масштабирования внимания для YARN. Корректирует веса внимания при экстраполяции контекста.\n\nРекомендации:\n• Оставьте по умолчанию для автоматического определения\n• Нужен только при тонкой настройке YARN"
		},
		["server.tip.yarn_beta_fast"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --yarn-beta-fast\n\nWhat is this:\nFast beta parameter for YARN interpolation. Affects context extrapolation quality.\n\nRecommendations:\n• Leave default for automatic detection\n• Only needed for fine YARN tuning",
			["ru"] = "Флаг: --yarn-beta-fast\n\nЧто это:\nБыстрый параметр бета для YARN-интерполяции. Влияет на качество экстраполяции контекста.\n\nРекомендации:\n• Оставьте по умолчанию для автоматического определения\n• Нужен только при тонкой настройке YARN"
		},
		["server.tip.yarn_beta_slow"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --yarn-beta-slow\n\nWhat is this:\nSlow beta parameter for YARN interpolation. Affects context extrapolation quality.\n\nRecommendations:\n• Leave default for automatic detection\n• Only needed for fine YARN tuning",
			["ru"] = "Флаг: --yarn-beta-slow\n\nЧто это:\nМедленный параметр бета для YARN-интерполяции. Влияет на качество экстраполяции контекста.\n\nРекомендации:\n• Оставьте по умолчанию для автоматического определения\n• Нужен только при тонкой настройке YARN"
		},
		["server.tip.host"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --host\n\nWhat is this:\nIP address on which the server will listen for incoming connections.\n\nValues:\n• 127.0.0.1 — local access only (default, safe)\n• 0.0.0.0 — network access (all interfaces)\n• Specific IP — only from a certain address\n\nRecommendations:\n• Leave 127.0.0.1 for security\n• Use 0.0.0.0 if you need to connect from other devices on the network",
			["ru"] = "Флаг: --host\n\nЧто это:\nIP-адрес, на котором сервер будет слушать входящие подключения.\n\nЗначения:\n• 127.0.0.1 — только локальный доступ (по умолчанию, безопасно)\n• 0.0.0.0 — доступ из сети (все интерфейсы)\n• Конкретный IP — только с определённого адреса\n\nРекомендации:\n• Оставьте 127.0.0.1 для безопасности\n• Используйте 0.0.0.0 если нужно подключаться с других устройств в сети"
		},
		["server.tip.port"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --port\n\nWhat is this:\nTCP port on which the server will run.\n\nValues:\n• 1–65535 (default: 8080)\n\nRecommendations:\n• Leave 8080 or change if the port is occupied by another program",
			["ru"] = "Флаг: --port\n\nЧто это:\nTCP-порт, на котором будет работать сервер.\n\nЗначения:\n• 1–65535 (по умолчанию: 8080)\n\nРекомендации:\n• Оставьте 8080 или измените если порт занят другой программой"
		},
		["server.tip.timeout"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -to / --timeout\n\nWhat is this:\nRead/write timeout in seconds. How long to wait for a server response.\n\nValues:\n• Number of seconds (default: 600 = 10 minutes)\n\nRecommendations:\n• For short responses: 60-300 seconds is enough\n• For long generations, increase to 1200+",
			["ru"] = "Флаг: -to / --timeout\n\nЧто это:\nТаймаут чтения/записи в секундах. Время ожидания ответа от сервера.\n\nЗначения:\n• Число секунд (по умолчанию: 600 = 10 минут)\n\nРекомендации:\n• Для коротких ответов: 60-300 секунд достаточно\n• Для длинных генераций увеличьте до 1200+"
		},
		["server.tip.parallel"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --parallel / -np\n\nWhat is this:\nNumber of parallel request slots. Determines how many requests the server can process simultaneously.\n\nValues:\n• Number — exact slot count (default: 4)\n• -1 — auto-detect\n\nBenefits:\n• More slots = more simultaneous requests\n• Each slot consumes VRAM/RAM proportional to context size\n\nRecommendations:\n• For one user: 2-4 is enough\n• Increase if working with multiple clients simultaneously",
			["ru"] = "Флаг: --parallel / -np\n\nЧто это:\nКоличество параллельных слотов запросов. Определяет, сколько запросов сервер может обрабатывать одновременно.\n\nЗначения:\n• Число — точное количество слотов (по умолчанию: 4)\n• -1 — автоопределение\n\nПреимущества:\n• Больше слотов = больше одновременных запросов\n• Каждый слот потребляет VRAM/RAM пропорционально размеру контекста\n\nРекомендации:\n• Для одного пользователя: 2-4 достаточно\n• Увеличьте если работаете с несколькими клиентами одновременно"
		},
		["server.tip.numa"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --numa\n\nWhat is this:\nNUMA-aware memory distribution. Optimizes memory operations on servers with multiple CPU sockets.\n\nWhen to enable:\n• Only on servers with 2+ CPU sockets (multi-processor systems)\n\nRecommendations:\n• On regular PCs and laptops — do NOT enable\n• On server hardware, may provide performance boost",
			["ru"] = "Флаг: --numa\n\nЧто это:\nNUMA-aware распределение памяти. Оптимизирует работу с памятью на серверах с несколькими CPU сокетами.\n\nКогда включать:\n• Только на серверах с 2+ CPU сокетами (многопроцессорные системы)\n\nРекомендации:\n• На обычных ПК и ноутбуках НЕ нужно включать\n• На серверном оборудовании может дать прирост производительности"
		},
		["server.tip.cache_prompt"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --cache-prompt\n\nWhat is this:\nPrompt caching for repeated queries. Saves intermediate computation results for speedup.\n\nBenefits:\n• Significantly speeds up dialog operation (repeated messages)\n• Saves time when editing prompts\n\nRecommendations:\n• Keep enabled — significantly speeds up dialog operation",
			["ru"] = "Флаг: --cache-prompt\n\nЧто это:\nКэширование промптов для повторных запросов. Сохраняет промежуточные результаты вычислений для ускорения.\n\nПреимущества:\n• Значительно ускоряет работу в диалогах (повторные сообщения)\n• Экономит время при редактировании промпта\n\nРекомендации:\n• Оставьте включённым — значительно ускоряет работу в диалогах"
		},
		["server.tip.cont_batching"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -cb / --no-cb\n\nWhat is this:\nAllows the server to process new requests without waiting for current ones to finish. Critically important for multi-user operation!\n\nBenefits:\n• Better throughput with many simultaneous requests\n• Less latency for new clients\n• Smooth server operation under load\n\nRecommendations:\n• Keep enabled for better performance\n• Only disable if stability issues arise",
			["ru"] = "Флаг: -cb / --no-cb\n\nЧто это:\nПозволяет серверу обрабатывать новые запросы, не дожидаясь завершения текущих. Критически важный параметр для многопользовательской работы!\n\nПреимущества:\n• Лучшая пропускная способность при множестве одновременных запросов\n• Меньше задержек для новых клиентов\n• Плавная работа сервера под нагрузкой\n\nРекомендации:\n• Оставьте включённым для лучшей производительности\n• Отключайте только если возникают проблемы со стабильностью"
		},
		["server.tip.context_shift"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --context-shift\n\nWhat is this:\nAutomatic context shift on overflow. Allows continuing the dialog without losing history.\n\nRecommendations:\n• Keep enabled (default)",
			["ru"] = "Флаг: --context-shift\n\nЧто это:\nАвтоматический сдвиг контекста при переполнении. Позволяет продолжать диалог без потери истории.\n\nРекомендации:\n• Оставьте включённым (по умолчанию)"
		},
		["server.tip.verbose_logging"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --verbose / -lv 4\n\nWhat is this:\nEnables maximum log detail level for the server.\n\n⚠\ufe0f CRITICALLY IMPORTANT:\n• When enabled, the server outputs a huge amount of debug messages to stdout/stderr\n• This can block the output buffer and result in EMPTY RESPONSES for requests\n• Logs get mixed with SSE response stream\n\nRecommendations:\n• ❌ NEVER enable for production!\n• Enable ONLY for debugging server issues\n• After diagnostics, definitely disable",
			["ru"] = "Флаг: --verbose / -lv 4\n\nЧто это:\nВключает максимальный уровень детализации логов сервера.\n\n⚠\ufe0f КРИТИЧЕСКИ ВАЖНО:\n• При включении сервер выводит огромный объём отладочных сообщений в stdout/stderr\n• Это может заблокировать буфер вывода и привести к ПУСТОМУ ОТВЕТУ при запросах\n• Логи перемешиваются с потоком ответов SSE\n\nРекомендации:\n• ❌ НИКОГДА не включайте для продакшн-работы!\n• Включайте ТОЛЬКО для отладки проблем с сервером\n• После диагностики обязательно выключите"
		},
		["server.tip.slots_monitoring"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --slots / --no-slots\n\nWhat is this:\nRequest slot monitoring. When enabled, the server provides slot information via the /slots endpoint.\n\nBenefits:\n• Ability to monitor slot status via API\n• Manage parallel requests\n• Integration with external systems\n\nRecommendations:\n• Enable by default for full monitoring",
			["ru"] = "Флаг: --slots / --no-slots\n\nЧто это:\nМониторинг слотов запросов. При включении сервер отдаёт информацию о слотах через endpoint /slots.\n\nПреимущества:\n• Возможность отслеживать состояние слотов через API\n• Управление параллельными запросами\n• Интеграция с внешними системами\n\nРекомендации:\n• Включите по умолчанию для полноценного мониторинга"
		},
		["server.tip.webui"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --webui\n\nWhat is this:\nBuilt-in web chat accessible from browser at http://localhost:8080 (or your port).\n\nBenefits:\n• Convenient API alternative for model testing\n• No need to install additional software\n• Works directly in the browser\n\nRecommendations:\n• Enable for convenient testing and demonstration",
			["ru"] = "Флаг: --webui\n\nЧто это:\nВстроенный веб-чат, доступный из браузера по адресу http://localhost:8080 (или вашему порту).\n\nПреимущества:\n• Удобная альтернатива API для тестирования модели\n• Не нужно устанавливать дополнительные программы\n• Работает прямо в браузере\n\nРекомендации:\n• Включите для удобного тестирования и демонстрации"
		},
		["server.tip.metrics"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --metrics\n\nWhat is this:\nOpens /metrics endpoint with data in Prometheus format for server performance monitoring.\n\nBenefits:\n• Track VRAM/RAM usage in real time\n• Monitor generation speed (tokens/sec)\n• Integration with visualization systems\n\nWhen to enable:\n• For integration with Grafana, Zabbix, or other monitoring systems\n\nRecommendations:\n• Leave disabled if not using monitoring",
			["ru"] = "Флаг: --metrics\n\nЧто это:\nОткрывает endpoint /metrics с данными в формате Prometheus для мониторинга производительности сервера.\n\nПреимущества:\n• Отслеживание использования VRAM/RAM в реальном времени\n• Мониторинг скорости генерации (токенов/сек)\n• Интеграция с системами визуализации\n\nКогда включать:\n• Для интеграции с Grafana, Zabbix или другими системами мониторинга\n\nРекомендации:\n• Оставьте отключённым если не используете мониторинг"
		},
		["server.tip.reasoning"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -rea / --reasoning\n\nWhat is this:\nReasoning/thinking mode. Allows the model to think before answering, showing the reasoning process (like DeepSeek R1, OpenAI o1).\n\nBenefits:\n• Better quality answers for complex tasks\n• Model shows its thought process before the final answer\n\nWhen to enable:\n• For reasoning models (DeepSeek R1, o1, etc.)\n• Auto — determined from the model automatically",
			["ru"] = "Флаг: -rea / --reasoning\n\nЧто это:\nРежим reasoning/мышления. Позволяет модели думать перед ответом, показывая процесс рассуждений (как в DeepSeek R1, OpenAI o1).\n\nПреимущества:\n• Лучшее качество ответов на сложные задачи\n• Модель показывает ход мыслей перед финальным ответом\n\nКогда включать:\n• Для reasoning-моделей (DeepSeek R1, о1 и т.д.)\n• Авто — определяется из модели автоматически"
		},
		["server.tip.embedding_mode"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --embedding\n\nWhat is this:\nLimits the server to embedding mode only. The model generates only vector representations of text, not responses.\n\nWhen to enable:\n• Only for embedding models (text-embedding-3-small, etc.)\n• For text similarity search, classification, RAG systems\n\nRecommendations:\n• Leave disabled for normal language models",
			["ru"] = "Флаг: --embedding\n\nЧто это:\nОграничивает сервер режимом эмбеддинга. Модель генерирует только векторные представления текста, а не ответы.\n\nКогда включать:\n• Только для моделей эмбеддингов (text-embedding-3-small и т.д.)\n• Для поиска похожих текстов, классификации, RAG-систем\n\nРекомендации:\n• Оставьте выключенным для обычных языковых моделей"
		},
		["server.tip.priority_high"] = new Dictionary<string, string>
		{
			["en"] = "Flag: process priority\n\nWhat is this:\nSets high process priority for the server in the operating system.\n\nBenefits:\n• Better response under high system load\n• Priority CPU time allocation\n\nRecommendations:\n• Enable if the server runs on a dedicated machine\n• On workstations, may slow down other applications",
			["ru"] = "Флаг: process priority\n\nЧто это:\nУстанавливает высокий приоритет процесса сервера в операционной системе.\n\nПреимущества:\n• Лучший отклик при высокой загрузке системы\n• Приоритетное распределение CPU времени\n\nРекомендации:\n• Включите если сервер работает на выделенной машине\n• На рабочих станциях может замедлить другие приложения"
		},
		["server.tip.mmq"] = new Dictionary<string, string>
		{
			["en"] = "Flag: --no-mmq\n\nWhat is this:\nDisables Multi-Modal Quantization (MMQ). When enabled, MMQ allows mixed quantization of model layers.\n\nRecommendations:\n• Keep disabled for best compatibility",
			["ru"] = "Флаг: --no-mmq\n\nЧто это:\nОтключает многоуровневое квантование (MMQ). При включении MMQ позволяет смешанное квантование слоёв модели.\n\nРекомендации:\n• Оставьте отключённым для лучшей совместимости"
		},
		["server.tip.predict_count"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -np / --predict-count\n\nWhat is this:\nNumber of predictions to generate per request in MTP mode.\n\nRecommendations:\n• Leave default for automatic detection",
			["ru"] = "Флаг: -np / --predict-count\n\nЧто это:\nКоличество предсказаний на запрос в режиме MTP.\n\nРекомендации:\n• Оставьте по умолчанию для автоматического определения"
		},
		["server.tip.spec_draft_gpu_layers"] = new Dictionary<string, string>
		{
			["en"] = "Flag: -ngld / --draft-ngl\n\nWhat is this:\nHow many layers of the draft model to offload to GPU.\n\nValues:\n• Number — exactly that many layers\n• auto — auto-detect based on VRAM (default)\n• all — all layers on GPU\n\nRecommendations:\n• Set to all for maximum speculative decoding speed",
			["ru"] = "Флаг: -ngld / --draft-ngl\n\nЧто это:\nСколько слоёв черновой (draft) модели выгрузить на видеокарту.\n\nЗначения:\n• Число — точно столько слоёв\n• auto — автоопределение по VRAM (по умолчанию)\n• all — все слои на GPU\n\nРекомендации:\n• Ставьте all для максимальной скорости спекулятивного декодирования"
		},
		["chat.mcp_guide"] = new Dictionary<string, string>
		{
			["en"] = "\r\n# MCP — Setup Guide\r\n\r\nMCP (Model Context Protocol) allows connecting external tools to the chat: file management, web search, API access, and much more.\r\n\r\n---\r\n\r\n## Quick Start\r\n\r\nOn first launch, Llama Studio automatically creates 2 MCP servers for web search. For file management, use the **\"Configure Filesystem\"** button in the MCP panel.\r\n\r\n---\r\n\r\n## Filesystem — File Management\r\n\r\n**Quick setup (recommended):**\r\n1. In the MCP panel, click **\"Choose a folder for file management\"**\r\n2. Select a folder — everything will be configured automatically\r\n\r\n**Manual setup:**\r\n1. Install Node.js: https://nodejs.org/\r\n2. In the MCP panel, fill in the form:\r\n   - **Name:** Filesystem\r\n   - **Type:** Stdio\r\n   - **Command:** npx\r\n   - **Arguments:** -y @modelcontextprotocol/server-filesystem C:\\Your\\Folder\r\n3. Click **\"Add\"**\r\n\r\n---\r\n\r\n## Web Search\r\n\r\n**Built-in search (no setup):**\r\nLlama Studio has a built-in `web_search` tool working through DuckDuckGo and Bing. Enable MCP tools with the \"MCP\" button at the bottom of the chat.\r\n\r\n**Jina Web Search (MCP):**\r\n1. In the MCP panel → Add server:\r\n   - **Name:** Web Search\r\n   - **Type:** SSE\r\n   - **URL:** https://chat.mcp.so/http/app.mcp.so\r\n2. Click **\"Add\"**\r\n\r\n**Brave Search (MCP):**\r\n1. Get an API key: https://brave.com/search/api/\r\n2. In the MCP panel → Add server:\r\n   - **Name:** Brave Search\r\n   - **Type:** Stdio\r\n   - **Command:** npx\r\n   - **Arguments:** -y @modelcontextprotocol/server-brave-search\r\n3. Add the BRAVE_API_KEY environment variable\r\n\r\n---\r\n\r\n## Other Popular MCP Servers\r\n\r\n| Server | Command | Note |\r\n|--------|---------|------|\r\n| GitHub | npx -y @modelcontextprotocol/server-github | Requires GITHUB_TOKEN |\r\n| Puppeteer (browser) | npx -y @modelcontextprotocol/server-puppeteer | Browser automation |\r\n| Google Maps | npx -y @modelcontextprotocol/server-google-maps | Requires API key |\r\n| PostgreSQL | npx -y @modelcontextprotocol/server-postgres | Specify DSN |\r\n| Fetch (pages) | npx -y @modelcontextprotocol/server-fetch | Web page loading |\r\n\r\n---\r\n\r\n## MCP Server Management\r\n\r\n**Adding a new server:**\r\n1. Open the MCP panel (\"MCP\" button at the top of the chat)\r\n2. Fill in the form:\r\n   - **Name** — any convenient name\r\n   - **Type:** Stdio (local program) or SSE (remote server)\r\n   - **Command/URL** — path to the program or server address\r\n   - **Arguments** — launch parameters (for Stdio)\r\n3. Click **\"Add\"**\r\n\r\n**Managing servers:**\r\n- ⚙\ufe0f **Green gear** — disable server\r\n- ⚙\ufe0f **Gray gear** — enable server\r\n- ✕ **Red cross** — delete server\r\n\r\n**Global toggle:**\r\nThe \"MCP\" button at the bottom of the chat (blue) — enables/disables all MCP tools\r\n\r\n---\r\n\r\n## Useful Tips\r\n\r\n• **Built-in tools** (web_search, web_fetch, time, calculator) work without MCP — just enable the \"MCP\" button\r\n• **MCP tools** are added automatically when connecting a server\r\n• **Servers are saved** between launches in mcp_servers.json\r\n• **Error \"npx not found\"** — install Node.js from https://nodejs.org/\r\n• **Server not connecting** — check the path to the program and launch arguments\\n",
			["ru"] = "\r\n# MCP — Руководство по настройке\r\n\r\nMCP (Model Context Protocol) позволяет подключать внешние инструменты к чату: управление файлами, поиск в интернете, доступ к API и многое другое.\r\n\r\n---\r\n\r\n## Быстрый старт\r\n\r\nПри первом запуске Llama Studio автоматически создаёт 2 MCP сервера для поиска в интернете. Для управления файлами используйте кнопку **«Настроить Filesystem»** в панели MCP.\r\n\r\n---\r\n\r\n## Filesystem — управление файлами\r\n\r\n**Быстрая настройка (рекомендуется):**\r\n1. В панели MCP нажмите кнопку **«Выберите папку для управления файлами»**\r\n2. Выберите папку — всё настроится автоматически\r\n\r\n**Ручная настройка:**\r\n1. Установите Node.js: https://nodejs.org/\r\n2. В панели MCP заполните форму:\r\n   - **Имя:** Filesystem\r\n   - **Тип:** Stdio\r\n   - **Команда:** npx\r\n   - **Аргументы:** -y @modelcontextprotocol/server-filesystem C:\\Ваша\\Папка\r\n3. Нажмите **«Добавить»**\r\n\r\n---\r\n\r\n## Поиск в интернете\r\n\r\n**Встроенный поиск (без настройки):**\r\nLlama Studio имеет встроенный инструмент `web_search`, работающий через DuckDuckGo и Bing. Включите MCP-инструменты кнопкой «MCP» внизу чата.\r\n\r\n**Jina Web Search (MCP):**\r\n1. В панели MCP → Добавить сервер:\r\n   - **Имя:** Web Search\r\n   - **Тип:** SSE\r\n   - **URL:** https://chat.mcp.so/http/app.mcp.so\r\n2. Нажмите **«Добавить»**\r\n\r\n**Brave Search (MCP):**\r\n1. Получите API ключ: https://brave.com/search/api/\r\n2. В панели MCP → Добавить сервер:\r\n   - **Имя:** Brave Search\r\n   - **Тип:** Stdio\r\n   - **Команда:** npx\r\n   - **Аргументы:** -y @modelcontextprotocol/server-brave-search\r\n3. Добавьте переменную среды BRAVE_API_KEY\r\n\r\n---\r\n\r\n## Другие популярные MCP серверы\r\n\r\n| Сервер | Команда | Примечание |\r\n|--------|---------|------------|\r\n| GitHub | npx -y @modelcontextprotocol/server-github | Нужен GITHUB_TOKEN |\r\n| Puppeteer (браузер) | npx -y @modelcontextprotocol/server-puppeteer | Автоматизация браузера |\r\n| Google Maps | npx -y @modelcontextprotocol/server-google-maps | Нужен API ключ |\r\n| PostgreSQL | npx -y @modelcontextprotocol/server-postgres | Укажите DSN |\r\n| Fetch (страницы) | npx -y @modelcontextprotocol/server-fetch | Загрузка веб-страниц |\r\n\r\n---\r\n\r\n## Управление MCP серверами\r\n\r\n**Добавление нового сервера:**\r\n1. Откройте панель MCP (кнопка «MCP» вверху чата)\r\n2. Заполните форму:\r\n   - **Имя** — любое удобное\r\n   - **Тип:** Stdio (локальная программа) или SSE (удалённый сервер)\r\n   - **Команда/URL** — путь к программе или адрес сервера\r\n   - **Аргументы** — параметры запуска (для Stdio)\r\n3. Нажмите **«Добавить»**\r\n\r\n**Управление серверами:**\r\n- ⚙\ufe0f **Зелёная шестерёнка** — отключить сервер\r\n- ⚙\ufe0f **Серая шестерёнка** — включить сервер\r\n- ✕ **Красный крестик** — удалить сервер\r\n\r\n**Глобальное включение:**\r\nКнопка «MCP» внизу чата (синяя) — включает/выключает все MCP-инструменты\r\n\r\n---\r\n\r\n## Полезные советы\r\n\r\n• **Встроенные инструменты** (web_search, web_fetch, time, calculator) работают без MCP — просто включите кнопку «MCP»\r\n• **MCP-инструменты** добавляются автоматически при подключении сервера\r\n• **Серверы сохраняются** между запусками в mcp_servers.json\r\n• **Ошибка «npx не найдена»** — установите Node.js с https://nodejs.org/\r\n• **Сервер не подключается** — проверьте путь к программе и аргументы запуска"
		},
		["web.api.server_already_running"] = new Dictionary<string, string>
		{
			["en"] = "The server is already running",
			["ru"] = "Сервер уже запущен"
		},
		["web.api.already_installed"] = new Dictionary<string, string>
		{
			["en"] = "This version is already installed",
			["ru"] = "Эта версия уже установлена"
		},
		["web.api.install_failed"] = new Dictionary<string, string>
		{
			["en"] = "The installation could not be started",
			["ru"] = "Не удалось запустить установку"
		},
		["web.api.install_in_progress"] = new Dictionary<string, string>
		{
			["en"] = "An installation is already in progress",
			["ru"] = "Установка уже выполняется"
		},
		["web.api.version_not_found"] = new Dictionary<string, string>
		{
			["en"] = "The installed version was not found",
			["ru"] = "Установленная версия не найдена"
		},
		["web.api.release_build_not_found"] = new Dictionary<string, string>
		{
			["en"] = "The release build was not found",
			["ru"] = "Сборка релиза не найдена"
		},
		["web.api.cannot_delete_active"] = new Dictionary<string, string>
		{
			["en"] = "The active version cannot be deleted",
			["ru"] = "Нельзя удалить активную версию"
		},
		["web.api.profile_required"] = new Dictionary<string, string>
		{
			["en"] = "No profile specified",
			["ru"] = "Не указан профиль"
		},
		["web.api.two_profiles_required"] = new Dictionary<string, string>
		{
			["en"] = "Specify two different existing profiles",
			["ru"] = "Укажите два разных существующих профиля"
		},
		["web.api.path_required"] = new Dictionary<string, string>
		{
			["en"] = "path is required",
			["ru"] = "path обязателен"
		},
		["web.api.version_asset_required"] = new Dictionary<string, string>
		{
			["en"] = "version and asset are required",
			["ru"] = "version и asset обязательны"
		},
		["web.api.models_dir_not_found"] = new Dictionary<string, string>
		{
			["en"] = "The models directory was not found",
			["ru"] = "Каталог моделей не найден"
		},
		["web.api.bat_empty"] = new Dictionary<string, string>
		{
			["en"] = "The BAT file is empty",
			["ru"] = "Файл BAT пуст"
		},
		["web.api.search_too_short"] = new Dictionary<string, string>
		{
			["en"] = "Enter at least two characters",
			["ru"] = "Введите минимум два символа"
		},
		["web.api.empty_message"] = new Dictionary<string, string>
		{
			["en"] = "The message is empty",
			["ru"] = "Пустое сообщение"
		},
		["web.api.hf_repo_required"] = new Dictionary<string, string>
		{
			["en"] = "No HuggingFace repository specified",
			["ru"] = "Репозиторий HuggingFace не указан"
		},
		["web.api.hf_download_required"] = new Dictionary<string, string>
		{
			["en"] = "Specify the repository, directory, and files",
			["ru"] = "Укажите репозиторий, каталог и файлы"
		},
		["web.api.cuda_install_in_progress"] = new Dictionary<string, string>
		{
			["en"] = "A CUDA Toolkit installation is already in progress.",
			["ru"] = "Установка CUDA Toolkit уже выполняется."
		},
		["web.api.cuda_linux_only"] = new Dictionary<string, string>
		{
			["en"] = "Automatic CUDA Toolkit installation is available in the Linux version of Studio only.",
			["ru"] = "Автоматическая установка CUDA Toolkit доступна только в Linux версии Studio."
		},
		["web.api.cuda_already_installed"] = new Dictionary<string, string>
		{
			["en"] = "CUDA Toolkit 12 is already installed.",
			["ru"] = "CUDA Toolkit 12 уже установлен."
		},
		["web.api.login_required"] = new Dictionary<string, string>
		{
			["en"] = "A login and password are required.",
			["ru"] = "Требуется логин и пароль."
		},
		["web.api.folder_not_exists"] = new Dictionary<string, string>
		{
			["en"] = "The specified folder does not exist",
			["ru"] = "Указанная папка не существует"
		},
		["web.api.llama_root_disk"] = new Dictionary<string, string>
		{
			["en"] = "Select the llama.cpp versions folder, not the drive root",
			["ru"] = "Выберите папку с версиями llama.cpp, а не корень диска"
		},
		["web.api.llama_folder_required"] = new Dictionary<string, string>
		{
			["en"] = "Select the folder containing the llama.cpp versions",
			["ru"] = "Укажите папку с версиями llama.cpp"
		},
		["web.api.new_profile"] = new Dictionary<string, string>
		{
			["en"] = "New profile",
			["ru"] = "Новый профиль"
		},
		["web.api.invalid_web_port"] = new Dictionary<string, string>
		{
			["en"] = "Invalid web interface port.",
			["ru"] = "Некорректный порт веб-интерфейса."
		},
		["web.llama.preparing"] = new Dictionary<string, string>
		{
			["en"] = "Preparing installation…",
			["ru"] = "Подготовка установки…"
		},
		["web.llama.downloading"] = new Dictionary<string, string>
		{
			["en"] = "Downloading: ",
			["ru"] = "Загрузка: "
		},
		["web.llama.installed"] = new Dictionary<string, string>
		{
			["en"] = "Installed: ",
			["ru"] = "Установлено: "
		},
		["web.llama.cancelled"] = new Dictionary<string, string>
		{
			["en"] = "Installation cancelled",
			["ru"] = "Установка отменена"
		},
		["web.llama.error"] = new Dictionary<string, string>
		{
			["en"] = "Installation error: ",
			["ru"] = "Ошибка установки: "
		},
		["web.cuda.downloading_official"] = new Dictionary<string, string>
		{
			["en"] = "Downloading the official CUDA Toolkit 12.8.1 (~5 GB)…",
			["ru"] = "Скачивание официального CUDA Toolkit 12.8.1 (около 5 ГБ)…"
		},
		["web.cuda.downloading_installing"] = new Dictionary<string, string>
		{
			["en"] = "Downloading and installing CUDA Toolkit 12.8.1… the NVIDIA driver will not be changed.",
			["ru"] = "Скачивание и установка CUDA Toolkit 12.8.1… драйвер NVIDIA не будет изменён."
		},
		["web.cuda.waiting_admin"] = new Dictionary<string, string>
		{
			["en"] = "Waiting for administrator password confirmation…",
			["ru"] = "Ожидание подтверждения пароля администратора…"
		},
		["web.cuda.launch_failed"] = new Dictionary<string, string>
		{
			["en"] = "Failed to launch the CUDA Toolkit installer.",
			["ru"] = "Не удалось запустить установщик CUDA Toolkit."
		},
		["web.cuda.official_failed"] = new Dictionary<string, string>
		{
			["en"] = "The official CUDA installer finished with an error.",
			["ru"] = "Официальный установщик CUDA завершился с ошибкой."
		},
		["web.cuda.error"] = new Dictionary<string, string>
		{
			["en"] = "CUDA Toolkit installation error: ",
			["ru"] = "Ошибка установки CUDA Toolkit: "
		},
		["web.cuda.cancelled"] = new Dictionary<string, string>
		{
			["en"] = "The CUDA Toolkit installation was cancelled.",
			["ru"] = "Установка CUDA Toolkit отменена."
		},
		["web.cuda.done"] = new Dictionary<string, string>
		{
			["en"] = "CUDA Toolkit 12.8 is installed. Now click “Install” on the CUDA 12 build.",
			["ru"] = "CUDA Toolkit 12.8 установлен. Теперь нажмите «Установить» у CUDA 12 сборки."
		},
		["web.hf.downloading"] = new Dictionary<string, string>
		{
			["en"] = "Downloading",
			["ru"] = "Скачивание"
		},
		["web.hf.downloading_file"] = new Dictionary<string, string>
		{
			["en"] = "Downloading…",
			["ru"] = "Скачивание..."
		},
		["web.hf.files_count"] = new Dictionary<string, string>
		{
			["en"] = "file(s)",
			["ru"] = "файл(ов)"
		},
		["web.hf.left"] = new Dictionary<string, string>
		{
			["en"] = "remaining",
			["ru"] = "осталось"
		},
		["web.hf.mbs"] = new Dictionary<string, string>
		{
			["en"] = "MB/s",
			["ru"] = "МБ/с"
		},
		["web.hf.mbits"] = new Dictionary<string, string>
		{
			["en"] = "Mbit/s",
			["ru"] = "Мбит/с"
		},
		["web.hf.gib"] = new Dictionary<string, string>
		{
			["en"] = "GB",
			["ru"] = "ГБ"
		},
		["web.hf.mib"] = new Dictionary<string, string>
		{
			["en"] = "MB",
			["ru"] = "МБ"
		},
		["web.hf.kib"] = new Dictionary<string, string>
		{
			["en"] = "KB",
			["ru"] = "КБ"
		},
};

	public static string[] SupportedLanguages => new string[2] { "en", "ru" };

	public string Language
	{
		get
		{
			return _language;
		}
		set
		{
			ChangeLanguage(value);
		}
	}

	public event EventHandler<string>? OnLanguageChanged;

	public string T(string key)
	{
		if (_translations.TryGetValue(key, out Dictionary<string, string> value) && value.TryGetValue(_language, out var value2))
		{
			return value2;
		}
		if (value != null && value.TryGetValue("en", out var value3))
		{
			return value3;
		}
		Console.Error.WriteLine($"[Localization] Missing key: {key} (language: {_language})");
		return "[" + key + "]";
	}

	public void ChangeLanguage(string language)
	{
		_language = language ?? "ru";
		this.OnLanguageChanged?.Invoke(this, _language);
	}
}
