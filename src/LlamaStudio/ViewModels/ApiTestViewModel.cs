using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LlamaStudio.ViewModels;

/// <summary>
/// ViewModel для тестирования API endpoints llama.cpp сервера.
/// Поддерживает тесты: /health, /v1/models, /v1/chat/completions, /v1/completions.
/// </summary>
public partial class ApiTestViewModel : ObservableObject
{
    readonly ISettings _settings;
    readonly ILogService _log;
    readonly ILocalizationService _loc;
    readonly HttpClient _httpClient;

    // Connection settings
    [ObservableProperty] string _host = "localhost";
    [ObservableProperty] int _port = 8080;
    [ObservableProperty] string _apiKey = "";
    [ObservableProperty] int _timeoutSeconds = 30;

    // Test results
    [ObservableProperty] string _healthResult = "";
    [ObservableProperty] string _modelsResult = "";
    [ObservableProperty] string _chatResult = "";
    [ObservableProperty] string _completionResult = "";
    [ObservableProperty] string _customResult = "";

    // Custom request
    [ObservableProperty] string _customMethod = "GET";
    [ObservableProperty] string _customPath = "";
    [ObservableProperty] string _customBody = "";

    // State
    [ObservableProperty] bool _isTesting = false;
    [ObservableProperty] string _lastError = "";
    [ObservableProperty] DateTime? _lastTestTime;

    // Translated strings
    public string Title => _loc.T("api_test.title") ?? "API Test";
    public string HealthLabel => _loc.T("api_test.health") ?? "Health Check";
    public string ModelsLabel => _loc.T("api_test.models") ?? "Models List";
    public string ChatLabel => _loc.T("api_test.chat") ?? "Chat Completion";
    public string CompletionLabel => _loc.T("api_test.completion") ?? "Completion";
    public string CustomLabel => _loc.T("api_test.custom") ?? "Custom Request";
    public string TestBtn => _loc.T("api_test.test") ?? "Test";
    public string RunAllBtn => _loc.T("api_test.run_all") ?? "Run All Tests";
    public string ConnectionSettingsLabel => _loc.T("api_test.connection_settings") ?? "Connection Settings";
    public string HostLabel => _loc.T("api_test.host") ?? "Host:";
    public string PortLabel => _loc.T("api_test.port") ?? "Port:";
    public string ApiKeyLabel => _loc.T("api_test.api_key") ?? "API Key:";
    public string ApiKeyOptionalLabel => _loc.T("api_test.api_key_optional") ?? "Optional";
    public string TimeoutLabel => _loc.T("api_test.timeout") ?? "Timeout (s):";
    public string MethodLabel => _loc.T("api_test.method") ?? "Method:";
    public string PathLabel => _loc.T("api_test.path") ?? "Path:";
    public string PathWatermark => _loc.T("api_test.path_watermark") ?? "/v1/models or full URL";
    public string BodyLabel => _loc.T("api_test.body") ?? "Body:";
    public string BodyWatermark => _loc.T("api_test.body_watermark") ?? "JSON body optional";
    public string SendBtn => _loc.T("api_test.send") ?? "Send";
    public string ChatWatermark => _loc.T("api_test.chat_watermark") ?? "Enter a message to send...";
    public string CompletionWatermark => _loc.T("api_test.completion_watermark") ?? "Enter text to complete...";

    public ApiTestViewModel(ISettings settings, ILogService log, ILocalizationService loc)
    {
        _settings = settings;
        _log = log;
        _loc = loc;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(_timeoutSeconds) };

        // Load settings
        var host = settings.DefaultHost ?? "localhost";
        if (host == "0.0.0.0" || host == "::")
            host = "127.0.0.1";
        Host = host;
        Port = settings.DefaultPort != 0 ? settings.DefaultPort : 8080;

        _loc.OnLanguageChanged += (_, _) => UpdateLocalizedProperties();
    }

    partial void OnHostChanged(string value)
        {
            var normalized = (value == "0.0.0.0" || value == "::") ? "127.0.0.1" : value;
            _settings.DefaultHost = normalized;
        }
    partial void OnPortChanged(int value) => _settings.DefaultPort = value;
    partial void OnTimeoutSecondsChanged(int value) => _httpClient.Timeout = TimeSpan.FromSeconds(value);

    string BaseUrl => $"http://{Host}:{Port}";

    HttpClient ConfigureClient(HttpClient client)
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        else
            client.DefaultRequestHeaders.Authorization = null;

        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    #region Health Check

    [RelayCommand]
    async Task TestHealthAsync()
    {
        try
        {
            IsTesting = true;
            LastError = "";
            HealthResult = _loc.T("apitest.status_checking");

            using var client = ConfigureClient(new HttpClient { Timeout = _httpClient.Timeout });
            var response = await client.GetAsync($"{BaseUrl}/health");

            var content = await response.Content.ReadAsStringAsync();
            HealthResult = $"Status: {response.StatusCode}\n\n{FormatJson(content)}";
            LastTestTime = DateTime.Now;

            _log.Information($"Health check: {response.StatusCode}", "ApiTest");
        }
        catch (Exception ex)
        {
            HealthResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
            LastError = ex.Message;
            _log.Error(ex, "Health check failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Models List

    [RelayCommand]
    async Task TestModelsAsync()
    {
        try
        {
            IsTesting = true;
            LastError = "";
            ModelsResult = _loc.T("apitest.status_fetching");

            using var client = ConfigureClient(new HttpClient { Timeout = _httpClient.Timeout });
            var response = await client.GetAsync($"{BaseUrl}/v1/models");

            var content = await response.Content.ReadAsStringAsync();
            ModelsResult = $"Status: {response.StatusCode}\n\n{FormatJson(content)}";
            LastTestTime = DateTime.Now;

            _log.Information($"Models list: {response.StatusCode}", "ApiTest");
        }
        catch (Exception ex)
        {
            ModelsResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
            LastError = ex.Message;
            _log.Error(ex, "Models list failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Chat Completion

    [ObservableProperty] string _chatPrompt = "Hello, who are you?";

    [RelayCommand]
    async Task TestChatAsync()
    {
        try
        {
            IsTesting = true;
            LastError = "";
            ChatResult = _loc.T("apitest.status_sending_chat");

            var request = new
            {
                model = "local-model",
                messages = new[] {
                    new { role = "user", content = ChatPrompt }
                },
                temperature = 0.7,
                max_tokens = 256
            };

            using var client = ConfigureClient(new HttpClient { Timeout = _httpClient.Timeout });
            var response = await client.PostAsJsonAsync($"{BaseUrl}/v1/chat/completions", request);

            var content = await response.Content.ReadAsStringAsync();
            ChatResult = $"Status: {response.StatusCode}\n\n{FormatJson(content)}";
            LastTestTime = DateTime.Now;

            _log.Information($"Chat completion: {response.StatusCode}", "ApiTest");
        }
        catch (Exception ex)
        {
            ChatResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
            LastError = ex.Message;
            _log.Error(ex, "Chat completion failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Completion

    [ObservableProperty] string _completionPrompt = "Once upon a time";

    [RelayCommand]
    async Task TestCompletionAsync()
    {
        try
        {
            IsTesting = true;
            LastError = "";
            CompletionResult = _loc.T("apitest.status_sending_completion");

            var request = new
            {
                model = "local-model",
                prompt = CompletionPrompt,
                temperature = 0.7,
                max_tokens = 128
            };

            using var client = ConfigureClient(new HttpClient { Timeout = _httpClient.Timeout });
            var response = await client.PostAsJsonAsync($"{BaseUrl}/v1/completions", request);

            var content = await response.Content.ReadAsStringAsync();
            CompletionResult = $"Status: {response.StatusCode}\n\n{FormatJson(content)}";
            LastTestTime = DateTime.Now;

            _log.Information($"Completion: {response.StatusCode}", "ApiTest");
        }
        catch (Exception ex)
        {
            CompletionResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
            LastError = ex.Message;
            _log.Error(ex, "Completion failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Custom Request

    [RelayCommand]
    async Task TestCustomAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomPath))
        {
            CustomResult = _loc.T("apitest.path_empty");
            return;
        }

        try
        {
            IsTesting = true;
            LastError = "";
            CustomResult = _loc.T("apitest.status_sending_custom");

            using var client = ConfigureClient(new HttpClient { Timeout = _httpClient.Timeout });
            HttpResponseMessage response;

            var method = CustomMethod.ToUpper();
            var url = CustomPath.StartsWith("http") ? CustomPath : $"{BaseUrl}{CustomPath}";

            switch (method)
            {
                case "GET":
                    response = await client.GetAsync(url);
                    break;
                case "POST":
                    var content = string.IsNullOrWhiteSpace(CustomBody)
                        ? new StringContent("")
                        : new StringContent(CustomBody, System.Text.Encoding.UTF8, "application/json");
                    response = await client.PostAsync(url, content);
                    break;
                case "PUT":
                    content = string.IsNullOrWhiteSpace(CustomBody)
                        ? new StringContent("")
                        : new StringContent(CustomBody, System.Text.Encoding.UTF8, "application/json");
                    response = await client.PutAsync(url, content);
                    break;
                case "DELETE":
                    response = await client.DeleteAsync(url);
                    break;
                default:
                    CustomResult = string.Format(_loc.T("apitest.method_unsupported"), method);
                    return;
            }

            var resultContent = await response.Content.ReadAsStringAsync();
            CustomResult = $"Status: {response.StatusCode}\n\n{FormatJson(resultContent)}";
            LastTestTime = DateTime.Now;

            _log.Information($"Custom {method} {url}: {response.StatusCode}", "ApiTest");
        }
        catch (Exception ex)
        {
            CustomResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
            LastError = ex.Message;
            _log.Error(ex, $"Custom request failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Run All

    [RelayCommand]
    async Task RunAllTestsAsync()
    {
        try
        {
            IsTesting = true;
            LastError = "";

            var results = new System.Text.StringBuilder();
            results.AppendLine(_loc.T("apitest.test_suite"));
            results.AppendLine(string.Format(_loc.T("apitest.base_url"), BaseUrl));
            results.AppendLine(string.Format(_loc.T("apitest.time"), DateTime.Now));
            results.AppendLine();

            // Health
            results.AppendLine(_loc.T("apitest.health_section"));
            await TestHealthAsync();
            results.AppendLine(HealthResult);
            results.AppendLine();

            // Models
            results.AppendLine(_loc.T("apitest.models_section"));
            await TestModelsAsync();
            results.AppendLine(ModelsResult);
            results.AppendLine();

            LastTestTime = DateTime.Now;
            _log.Information("All API tests completed", "ApiTest");
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _log.Error(ex, "Run all tests failed", "ApiTest");
        }
        finally
        {
            IsTesting = false;
        }
    }

    #endregion

    #region Helpers

    string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    void UpdateLocalizedProperties()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(HealthLabel));
        OnPropertyChanged(nameof(ModelsLabel));
        OnPropertyChanged(nameof(ChatLabel));
        OnPropertyChanged(nameof(CompletionLabel));
        OnPropertyChanged(nameof(CustomLabel));
        OnPropertyChanged(nameof(TestBtn));
        OnPropertyChanged(nameof(RunAllBtn));
        OnPropertyChanged(nameof(ConnectionSettingsLabel));
        OnPropertyChanged(nameof(HostLabel));
        OnPropertyChanged(nameof(PortLabel));
        OnPropertyChanged(nameof(ApiKeyLabel));
        OnPropertyChanged(nameof(ApiKeyOptionalLabel));
        OnPropertyChanged(nameof(TimeoutLabel));
        OnPropertyChanged(nameof(MethodLabel));
        OnPropertyChanged(nameof(PathLabel));
        OnPropertyChanged(nameof(PathWatermark));
        OnPropertyChanged(nameof(BodyLabel));
        OnPropertyChanged(nameof(BodyWatermark));
        OnPropertyChanged(nameof(SendBtn));
        OnPropertyChanged(nameof(ChatWatermark));
        OnPropertyChanged(nameof(CompletionWatermark));
    }

    #endregion
}