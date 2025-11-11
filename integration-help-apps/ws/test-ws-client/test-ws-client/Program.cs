using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using test_ws_client;

public class WebSocketClientService : IHostedService
{
	private readonly ILogger<WebSocketClientService> _logger;
	private readonly WebSocketClientConfig _config;
	private CancellationTokenSource _cts;
	private Task _clientTask;
	private ClientWebSocket _webSocket;

	public WebSocketClientService(ILogger<WebSocketClientService> logger, IOptions<WebSocketClientConfig> config)
	{
		_logger = logger;
		_config = config.Value;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Запуск WebSocket-клиента...");
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_clientTask = Task.Run(() => ConnectToServerAsync(_cts.Token), _cts.Token);
		return Task.CompletedTask;
	}

	private async Task ConnectToServerAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			_webSocket = new ClientWebSocket();
			try
			{
				var uri = new Uri($"ws://{_config.ServerHost}:{_config.ServerPort}{_config.WebSocketPath}");
				_logger.LogInformation($"Подключение к WebSocket серверу {uri}...");
				
				await _webSocket.ConnectAsync(uri, token);

				// Отправляем приветственное сообщение
				string message = "Привет, WebSocket сервер!";
				byte[] sendBytes = Encoding.UTF8.GetBytes(message);
				await _webSocket.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, token);
				_logger.LogInformation($"[WS CLIENT] Отправлено: {message}");

				_logger.LogInformation($"Ожидание ответа от WebSocket сервера на {uri}...");
				bool connected = false;

				while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
				{
					var buffer = new byte[_config.BufferSize];
					var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
					
					if (receiveResult.MessageType == WebSocketMessageType.Close)
					{
						_logger.LogInformation("Сервер закрыл WebSocket соединение");
						break;
					}
					
					if (!connected)
					{
						_logger.LogInformation($"✅ Успешное подключение к WebSocket серверу!");
						connected = true;
					}
					
					string response = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
					_logger.LogInformation($"[WS CLIENT] Получено: {response}");

					// Небольшая пауза
					await Task.Delay(1000, token);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ Ошибка подключения к WebSocket серверу: {ex.Message}");
				_logger.LogInformation($"Повторная попытка через {_config.ReconnectDelaySeconds} секунд...");
				await Task.Delay(_config.ReconnectDelaySeconds * 1000, token);
			}
			finally
			{
				if (_webSocket.State == WebSocketState.Open)
				{
					await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие клиентом", CancellationToken.None);
				}
				_webSocket?.Dispose();
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Остановка WebSocket-клиента...");
		_cts?.Cancel();
		if (_webSocket?.State == WebSocketState.Open)
		{
			_webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Остановка сервиса", CancellationToken.None);
		}
		_webSocket?.Dispose();
		return _clientTask ?? Task.CompletedTask;
	}
}

public class Program
{
	public static async Task Main(string[] args)
	{
		Console.Title = "ws-test-client";
		
		using var host = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddEnvironmentVariables();
				config.AddCommandLine(args);
			})
			.ConfigureServices((context, services) =>
			{
				services.Configure<WebSocketClientConfig>(context.Configuration.GetSection("WebSocketClient"));
				services.AddHostedService<WebSocketClientService>();
			})
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddConsole();
			})
			.Build();

		var config = host.Services.GetRequiredService<IOptions<WebSocketClientConfig>>().Value;
		Console.WriteLine($"WebSocket клиент будет подключаться к ws://{config.ServerHost}:{config.ServerPort}{config.WebSocketPath}");
		Console.WriteLine($"Задержка переподключения: {config.ReconnectDelaySeconds} сек");
		Console.WriteLine($"Размер буфера: {config.BufferSize} байт");
		Console.WriteLine("----------------------------------------");
		Console.WriteLine("Конфигурация через appsettings.json секция 'WebSocketClient'");
		Console.WriteLine("Переопределение через переменные среды: WebSocketClient__ServerHost, WebSocketClient__ServerPort");
		Console.WriteLine("Переопределение через аргументы: --WebSocketClient:ServerHost=X --WebSocketClient:ServerPort=Y");
		Console.WriteLine("----------------------------------------");

		await host.RunAsync();
	}
}
