using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using LlamaStudio.Core.Interfaces;

namespace LlamaStudio.ViewModels;

public class ApiTestViewModel : ObservableObject
{
	private readonly ISettings _settings;

	private readonly ILogService _log;

	private readonly ILocalizationService _loc;

	private readonly HttpClient _httpClient;

	[ObservableProperty]
	private string _host = "localhost";

	[ObservableProperty]
	private int _port = 8080;

	[ObservableProperty]
	private string _apiKey = "";

	[ObservableProperty]
	private int _timeoutSeconds = 30;

	[ObservableProperty]
	private string _healthResult = "";

	[ObservableProperty]
	private string _modelsResult = "";

	[ObservableProperty]
	private string _chatResult = "";

	[ObservableProperty]
	private string _completionResult = "";

	[ObservableProperty]
	private string _customResult = "";

	[ObservableProperty]
	private string _customMethod = "GET";

	[ObservableProperty]
	private string _customPath = "";

	[ObservableProperty]
	private string _customBody = "";

	[ObservableProperty]
	private bool _isTesting;

	[ObservableProperty]
	private string _lastError = "";

	[ObservableProperty]
	private DateTime? _lastTestTime;

	[ObservableProperty]
	private string _chatPrompt = "Hello, who are you?";

	[ObservableProperty]
	private string _completionPrompt = "Once upon a time";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? testHealthCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? testModelsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? testChatCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? testCompletionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? testCustomCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? runAllTestsCommand;

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

	private string BaseUrl => $"http://{Host}:{Port}";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return _host;
		}
		[MemberNotNull("_host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_host, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Host);
				_host = value;
				OnHostChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_port, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.Port);
				_port = value;
				OnPortChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ApiKey
	{
		get
		{
			return _apiKey;
		}
		[MemberNotNull("_apiKey")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_apiKey, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ApiKey);
				_apiKey = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ApiKey);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int TimeoutSeconds
	{
		get
		{
			return _timeoutSeconds;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_timeoutSeconds, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.TimeoutSeconds);
				_timeoutSeconds = value;
				OnTimeoutSecondsChanged(value);
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.TimeoutSeconds);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string HealthResult
	{
		get
		{
			return _healthResult;
		}
		[MemberNotNull("_healthResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_healthResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.HealthResult);
				_healthResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.HealthResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModelsResult
	{
		get
		{
			return _modelsResult;
		}
		[MemberNotNull("_modelsResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modelsResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ModelsResult);
				_modelsResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ModelsResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ChatResult
	{
		get
		{
			return _chatResult;
		}
		[MemberNotNull("_chatResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_chatResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ChatResult);
				_chatResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ChatResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CompletionResult
	{
		get
		{
			return _completionResult;
		}
		[MemberNotNull("_completionResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_completionResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CompletionResult);
				_completionResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CompletionResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CustomResult
	{
		get
		{
			return _customResult;
		}
		[MemberNotNull("_customResult")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_customResult, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CustomResult);
				_customResult = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CustomResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CustomMethod
	{
		get
		{
			return _customMethod;
		}
		[MemberNotNull("_customMethod")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_customMethod, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CustomMethod);
				_customMethod = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CustomMethod);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CustomPath
	{
		get
		{
			return _customPath;
		}
		[MemberNotNull("_customPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_customPath, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CustomPath);
				_customPath = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CustomPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CustomBody
	{
		get
		{
			return _customBody;
		}
		[MemberNotNull("_customBody")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_customBody, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CustomBody);
				_customBody = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CustomBody);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsTesting
	{
		get
		{
			return _isTesting;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isTesting, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.IsTesting);
				_isTesting = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.IsTesting);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LastError
	{
		get
		{
			return _lastError;
		}
		[MemberNotNull("_lastError")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_lastError, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LastError);
				_lastError = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LastError);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public DateTime? LastTestTime
	{
		get
		{
			return _lastTestTime;
		}
		set
		{
			if (!EqualityComparer<DateTime?>.Default.Equals(_lastTestTime, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.LastTestTime);
				_lastTestTime = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.LastTestTime);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string ChatPrompt
	{
		get
		{
			return _chatPrompt;
		}
		[MemberNotNull("_chatPrompt")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_chatPrompt, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.ChatPrompt);
				_chatPrompt = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.ChatPrompt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string CompletionPrompt
	{
		get
		{
			return _completionPrompt;
		}
		[MemberNotNull("_completionPrompt")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_completionPrompt, value))
			{
				OnPropertyChanging(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangingArgs.CompletionPrompt);
				_completionPrompt = value;
				OnPropertyChanged(CommunityToolkit.Mvvm.ComponentModel.__Internals.__KnownINotifyPropertyChangedArgs.CompletionPrompt);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TestHealthCommand => testHealthCommand ?? (testHealthCommand = new AsyncRelayCommand(TestHealthAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TestModelsCommand => testModelsCommand ?? (testModelsCommand = new AsyncRelayCommand(TestModelsAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TestChatCommand => testChatCommand ?? (testChatCommand = new AsyncRelayCommand(TestChatAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TestCompletionCommand => testCompletionCommand ?? (testCompletionCommand = new AsyncRelayCommand(TestCompletionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand TestCustomCommand => testCustomCommand ?? (testCustomCommand = new AsyncRelayCommand(TestCustomAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RunAllTestsCommand => runAllTestsCommand ?? (runAllTestsCommand = new AsyncRelayCommand(RunAllTestsAsync));

	public ApiTestViewModel(ISettings settings, ILogService log, ILocalizationService loc)
	{
		_settings = settings;
		_log = log;
		_loc = loc;
		_httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
		};
		string text = settings.DefaultHost ?? "localhost";
		if (text == "0.0.0.0" || text == "::")
		{
			text = "127.0.0.1";
		}
		Host = text;
		Port = ((settings.DefaultPort != 0) ? settings.DefaultPort : 8080);
		_loc.OnLanguageChanged += delegate
		{
			UpdateLocalizedProperties();
		};
	}

	private HttpClient ConfigureClient(HttpClient client)
	{
		if (!string.IsNullOrWhiteSpace(ApiKey))
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
		}
		else
		{
			client.DefaultRequestHeaders.Authorization = null;
		}
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		return client;
	}

	[RelayCommand]
	private async Task TestHealthAsync()
	{
		try
		{
			IsTesting = true;
			LastError = "";
			HealthResult = _loc.T("apitest.status_checking");
			using HttpClient client = ConfigureClient(new HttpClient
			{
				Timeout = _httpClient.Timeout
			});
			HttpResponseMessage response = await client.GetAsync(BaseUrl + "/health");
			string json = await response.Content.ReadAsStringAsync();
			HealthResult = $"Status: {response.StatusCode}\n\n{FormatJson(json)}";
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

	[RelayCommand]
	private async Task TestModelsAsync()
	{
		try
		{
			IsTesting = true;
			LastError = "";
			ModelsResult = _loc.T("apitest.status_fetching");
			using HttpClient client = ConfigureClient(new HttpClient
			{
				Timeout = _httpClient.Timeout
			});
			HttpResponseMessage response = await client.GetAsync(BaseUrl + "/v1/models");
			string json = await response.Content.ReadAsStringAsync();
			ModelsResult = $"Status: {response.StatusCode}\n\n{FormatJson(json)}";
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

	[RelayCommand]
	private async Task TestChatAsync()
	{
		try
		{
			IsTesting = true;
			LastError = "";
			ChatResult = _loc.T("apitest.status_sending_chat");
			var value = new
			{
				model = "local-model",
				messages = new[]
				{
					new
					{
						role = "user",
						content = ChatPrompt
					}
				},
				temperature = 0.7,
				max_tokens = 256
			};
			using HttpClient client = ConfigureClient(new HttpClient
			{
				Timeout = _httpClient.Timeout
			});
			HttpResponseMessage response = await client.PostAsJsonAsync(BaseUrl + "/v1/chat/completions", value);
			string json = await response.Content.ReadAsStringAsync();
			ChatResult = $"Status: {response.StatusCode}\n\n{FormatJson(json)}";
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

	[RelayCommand]
	private async Task TestCompletionAsync()
	{
		try
		{
			IsTesting = true;
			LastError = "";
			CompletionResult = _loc.T("apitest.status_sending_completion");
			var value = new
			{
				model = "local-model",
				prompt = CompletionPrompt,
				temperature = 0.7,
				max_tokens = 128
			};
			using HttpClient client = ConfigureClient(new HttpClient
			{
				Timeout = _httpClient.Timeout
			});
			HttpResponseMessage response = await client.PostAsJsonAsync(BaseUrl + "/v1/completions", value);
			string json = await response.Content.ReadAsStringAsync();
			CompletionResult = $"Status: {response.StatusCode}\n\n{FormatJson(json)}";
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

	[RelayCommand]
	private async Task TestCustomAsync()
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
			using HttpClient client = ConfigureClient(new HttpClient
			{
				Timeout = _httpClient.Timeout
			});
			string method = CustomMethod.ToUpper();
			string url = (CustomPath.StartsWith("http") ? CustomPath : (BaseUrl + CustomPath));
			HttpResponseMessage response;
			switch (method)
			{
			case "GET":
				response = await client.GetAsync(url);
				break;
			case "POST":
			{
				StringContent content = (string.IsNullOrWhiteSpace(CustomBody) ? new StringContent("") : new StringContent(CustomBody, Encoding.UTF8, "application/json"));
				response = await client.PostAsync(url, content);
				break;
			}
			case "PUT":
			{
				StringContent content = (string.IsNullOrWhiteSpace(CustomBody) ? new StringContent("") : new StringContent(CustomBody, Encoding.UTF8, "application/json"));
				response = await client.PutAsync(url, content);
				break;
			}
			case "DELETE":
				response = await client.DeleteAsync(url);
				break;
			default:
				CustomResult = string.Format(_loc.T("apitest.method_unsupported"), method);
				return;
			}
			string json = await response.Content.ReadAsStringAsync();
			CustomResult = $"Status: {response.StatusCode}\n\n{FormatJson(json)}";
			LastTestTime = DateTime.Now;
			_log.Information($"Custom {method} {url}: {response.StatusCode}", "ApiTest");
		}
		catch (Exception ex)
		{
			CustomResult = string.Format(_loc.T("apitest.result_error"), ex.Message);
			LastError = ex.Message;
			_log.Error(ex, "Custom request failed", "ApiTest");
		}
		finally
		{
			IsTesting = false;
		}
	}

	[RelayCommand]
	private async Task RunAllTestsAsync()
	{
		try
		{
			IsTesting = true;
			LastError = "";
			StringBuilder results = new StringBuilder();
			results.AppendLine(_loc.T("apitest.test_suite"));
			results.AppendLine(string.Format(_loc.T("apitest.base_url"), BaseUrl));
			results.AppendLine(string.Format(_loc.T("apitest.time"), DateTime.Now));
			results.AppendLine();
			results.AppendLine(_loc.T("apitest.health_section"));
			await TestHealthAsync();
			results.AppendLine(HealthResult);
			results.AppendLine();
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

	private string FormatJson(string json)
	{
		try
		{
			using JsonDocument value = JsonDocument.Parse(json);
			return JsonSerializer.Serialize(value, new JsonSerializerOptions
			{
				WriteIndented = true
			});
		}
		catch
		{
			return json;
		}
	}

	private void UpdateLocalizedProperties()
	{
		OnPropertyChanged("Title");
		OnPropertyChanged("HealthLabel");
		OnPropertyChanged("ModelsLabel");
		OnPropertyChanged("ChatLabel");
		OnPropertyChanged("CompletionLabel");
		OnPropertyChanged("CustomLabel");
		OnPropertyChanged("TestBtn");
		OnPropertyChanged("RunAllBtn");
		OnPropertyChanged("ConnectionSettingsLabel");
		OnPropertyChanged("HostLabel");
		OnPropertyChanged("PortLabel");
		OnPropertyChanged("ApiKeyLabel");
		OnPropertyChanged("ApiKeyOptionalLabel");
		OnPropertyChanged("TimeoutLabel");
		OnPropertyChanged("MethodLabel");
		OnPropertyChanged("PathLabel");
		OnPropertyChanged("PathWatermark");
		OnPropertyChanged("BodyLabel");
		OnPropertyChanged("BodyWatermark");
		OnPropertyChanged("SendBtn");
		OnPropertyChanged("ChatWatermark");
		OnPropertyChanged("CompletionWatermark");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnHostChanged(string value)
	{
		string defaultHost = ((value == "0.0.0.0" || value == "::") ? "127.0.0.1" : value);
		_settings.DefaultHost = defaultHost;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnPortChanged(int value)
	{
		_settings.DefaultPort = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnTimeoutSecondsChanged(int value)
	{
		_httpClient.Timeout = TimeSpan.FromSeconds(value);
	}
}
