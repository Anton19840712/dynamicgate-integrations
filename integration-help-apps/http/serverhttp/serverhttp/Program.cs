using System.Net;
using System.Text;
using Serilog;

var builder = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Build();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(configuration)
	.Enrich.FromLogContext()
	.WriteTo.Console()
	.CreateLogger();

Log.Logger.Information("SSE Server starting");

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((context, services) =>
	{
		services.AddHostedService<SseBackgroundService>();
	})
	.UseSerilog()
	.Build();

await host.RunAsync();

// Класс фоновой службы для обработки SSE
public class SseBackgroundService : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var listener = new HttpListener();
		listener.Prefixes.Add("http://localhost:52799/sse/");
		listener.Start();
		Log.Information("SSE Server started at http://localhost:52799/sse/");

		while (!stoppingToken.IsCancellationRequested)
		{
			var context = await listener.GetContextAsync();
			var response = context.Response;

			// Добавление CORS заголовка
			response.Headers.Add("Access-Control-Allow-Origin", "*");
			response.Headers.Add("Content-Type", "text/event-stream");
			response.Headers.Add("Cache-Control", "no-cache");

			Log.Information("New SSE connection established");

			try
			{
				while (!stoppingToken.IsCancellationRequested)
				{
					var data = $"data: Server time is {DateTime.Now}\n\n"; // Изменено сообщение
					var buffer = Encoding.UTF8.GetBytes(data);

					await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, stoppingToken);
					await response.OutputStream.FlushAsync(stoppingToken);

					Log.Information($"Sent data: {data.Trim()}");
					await Task.Delay(1000, stoppingToken); // Задержка между сообщениями
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Error sending data: {ex.Message}");
			}
			finally
			{
				response.Close();
				Log.Information("SSE connection closed");
			}
		}
	}
}
