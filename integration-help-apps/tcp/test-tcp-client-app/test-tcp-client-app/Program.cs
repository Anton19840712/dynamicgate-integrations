using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class TcpClientService : IHostedService
{
	private readonly ILogger<TcpClientService> _logger;
	private readonly TcpClientConfig _config;
	private CancellationTokenSource _cts;
	private Task _clientTask;

	public TcpClientService(ILogger<TcpClientService> logger, IOptions<TcpClientConfig> config)
	{
		_logger = logger;
		_config = config.Value;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Запуск TCP-клиента...");
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_clientTask = Task.Run(() => ConnectToServerAsync(_cts.Token), _cts.Token);
		return Task.CompletedTask;
	}

	private async Task ConnectToServerAsync(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			using var client = new System.Net.Sockets.TcpClient();

			try
			{
				_logger.LogInformation($"Подключение к {_config.ServerHost}:{_config.ServerPort}...");
				await client.ConnectAsync(_config.ServerHost, _config.ServerPort);

				_logger.LogInformation("Успешное подключение!");

				using var stream = client.GetStream();
				byte[] buffer = new byte[_config.BufferSize];

				while (!token.IsCancellationRequested)
				{
					int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
					if (bytesRead == 0)
					{
						_logger.LogWarning("Сервер закрыл соединение.");
						break;
					}

					string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					foreach (var line in message.Split('\n', StringSplitOptions.RemoveEmptyEntries))
					{
						_logger.LogInformation($"[CLIENT] Получено: {line}");
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Ошибка: {ex.Message}");
				await Task.Delay(_config.ReconnectDelaySeconds * 1000, token); // Ожидание перед повторным подключением
			}
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Остановка TCP-клиента...");
		_cts?.Cancel();
		return _clientTask ?? Task.CompletedTask;
	}
}

public class Program
{
	public static async Task Main(string[] args)
	{
		Console.Title = "tcp-test-client";
		
		using var host = Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
				config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
				config.AddEnvironmentVariables();
				config.AddCommandLine(args);
			})
			.ConfigureServices((context, services) =>
			{
				// Конфигурация TCP клиента
				services.Configure<TcpClientConfig>(context.Configuration.GetSection("TcpClient"));
				services.AddHostedService<TcpClientService>();
			})
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.AddConsole();
			})
			.Build();

		// Показать конфигурацию при запуске
		var config = host.Services.GetRequiredService<IOptions<TcpClientConfig>>().Value;
		Console.WriteLine($"TCP клиент будет подключаться к {config.ServerHost}:{config.ServerPort}");
		Console.WriteLine($"Задержка переподключения: {config.ReconnectDelaySeconds} сек");
		Console.WriteLine($"Размер буфера: {config.BufferSize} байт");
		Console.WriteLine("----------------------------------------");
		Console.WriteLine("Конфигурация через appsettings.json секция 'TcpClient'");
		Console.WriteLine("Переопределение через переменные среды: TcpClient__ServerHost, TcpClient__ServerPort");
		Console.WriteLine("Переопределение через аргументы: --TcpClient:ServerHost=X --TcpClient:ServerPort=Y");
		Console.WriteLine("----------------------------------------");

		await host.RunAsync();
	}
}

