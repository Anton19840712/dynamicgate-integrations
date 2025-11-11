using Serilog;

class Program
{
	static async Task Main(string[] args)
	{
		// Настройка логирования
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.CreateLogger();

		Log.Information("SSE Client starting...");

		using var httpClient = new HttpClient
		{
			Timeout = Timeout.InfiniteTimeSpan // Отключаем таймаут, так как соединение должно быть долгим
		};

		var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:52799/sse/");
		request.Headers.Accept.Clear();
		request.Headers.Accept.ParseAdd("text/event-stream");

		try
		{
			using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();

			using var stream = await response.Content.ReadAsStreamAsync();
			using var reader = new StreamReader(stream);

			Log.Information("Connected to SSE server.");

			while (!reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync();
				if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
				{
					var message = line.Substring(6).Trim();
					Log.Information($"Received message: {message}");
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error: {ex.Message}");
		}
	}
}
