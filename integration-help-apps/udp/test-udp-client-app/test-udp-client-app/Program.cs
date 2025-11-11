using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class UdpClientService : IHostedService
{
	private readonly ILogger<UdpClientService> _logger;
	private readonly UdpClientConfig _config;
	private CancellationTokenSource _cts;
	private Task _clientTask;
	private UdpClient _udpClient;

	public UdpClientService(ILogger<UdpClientService> logger, IOptions<UdpClientConfig> config)
	{
		_logger = logger;
		_config = config.Value;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Запуск UDP-клиента...");
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_clientTask = Task.Run(() => ConnectToServerAsync(_cts.Token), _cts.Token);
		return Task.CompletedTask;
	}

	private async Task ConnectToServerAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			_udpClient = new UdpClient();
			try
			{
				_logger.LogInformation($"Подключение к UDP серверу {_config.ServerHost}:{_config.ServerPort}...");
				_udpClient.Connect(_config.ServerHost, _config.ServerPort);

				// Отправляем приветственное сообщение
				string message = "Привет, UDP сервер!";
				byte[] sendBytes = Encoding.UTF8.GetBytes(message);
				await _udpClient.SendAsync(sendBytes, sendBytes.Length);
				_logger.LogInformation($"[UDP CLIENT] Отправлено: {message}");

				_logger.LogInformation($"Ожидание ответа от UDP сервера на {_config.ServerHost}:{_config.ServerPort}...");
				bool connected = false;

				while (!token.IsCancellationRequested)
				{
					var receiveResult = await _udpClient.ReceiveAsync();
					string response = Encoding.UTF8.GetString(receiveResult.Buffer);
					
					if (!connected)
					{
						_logger.LogInformation($"✅ Успешное подключение к UDP серверу!");
						connected = true;
					}
					
					_logger.LogInformation($"[UDP CLIENT] Получено: {response}");

					// Небольшая пауза для зачитывания сообщений:
					await Task.Delay(1000, token);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"❌ Ошибка подключения к UDP серверу: {ex.Message}");
				_logger.LogInformation($"Повторная попытка через {_config.ReconnectDelaySeconds} секунд...");
				await Task.Delay(_config.ReconnectDelaySeconds * 1000, token);
			}
			finally
			{
				_udpClient?.Dispose();
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Остановка UDP-клиента...");
		_cts?.Cancel();
		_udpClient?.Dispose();
		return _clientTask ?? Task.CompletedTask;
	}
}

public class Program
{
	public static async Task Main(string[] args)
	{
		Console.Title = "udp-test-client";
		
		using var host = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddEnvironmentVariables();
				config.AddCommandLine(args);
			})
			.ConfigureServices((context, services) =>
			{
				services.Configure<UdpClientConfig>(context.Configuration.GetSection("UdpClient"));
				services.AddHostedService<UdpClientService>();
			})
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddConsole();
			})
			.Build();

		var config = host.Services.GetRequiredService<IOptions<UdpClientConfig>>().Value;
		Console.WriteLine($"UDP клиент будет подключаться к {config.ServerHost}:{config.ServerPort}");
		Console.WriteLine($"Задержка переподключения: {config.ReconnectDelaySeconds} сек");
		Console.WriteLine($"Размер буфера: {config.BufferSize} байт");
		Console.WriteLine("----------------------------------------");
		Console.WriteLine("Конфигурация через appsettings.json секция 'UdpClient'");
		Console.WriteLine("Переопределение через переменные среды: UdpClient__ServerHost, UdpClient__ServerPort");
		Console.WriteLine("Переопределение через аргументы: --UdpClient:ServerHost=X --UdpClient:ServerPort=Y");
		Console.WriteLine("----------------------------------------");

		await host.RunAsync();
	}
}
