using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;

class Program
{
	static async Task Main(string[] args)
	{
		var config = BuildConfig();

		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(config)
			.Enrich.FromLogContext()
			.CreateLogger();

		using var httpClient = new HttpClient();
		httpClient.DefaultRequestHeaders.Accept.Clear();
		httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


		// Добавляем заголовки (можно сделать более гибкими через конфиг)
		httpClient.DefaultRequestHeaders.Add("X-Custom-Header", "value");

		string url = config["HttpSettings:Url"] ?? "http://127.0.0.1/api/httpprotocol/push";
		string messageToSend = config["HttpSettings:Message"];

		if (string.IsNullOrWhiteSpace(messageToSend))
		{
			Log.Error("❌ Сообщение для отправки не задано в конфигурации.");
			return;
		}

		while (true)
		{
			try
			{
				Log.Information("📤 Отправка запроса...");

				var parsedJson = System.Text.Json.JsonDocument.Parse(messageToSend).RootElement;
				var content = new StringContent(parsedJson.GetRawText(), Encoding.UTF8, "application/json");

				await httpClient.PostAsync(url, content);
			}
			catch (Exception ex)
			{
				Log.Error("❌ Ошибка при отправке: {Error}", ex.Message);
			}

			await Task.Delay(int.Parse(config["HttpSettings:IntervalMs"] ?? "3000"));
		}
	}

	static IConfiguration BuildConfig() =>
		new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddEnvironmentVariables()
			.Build();
}
