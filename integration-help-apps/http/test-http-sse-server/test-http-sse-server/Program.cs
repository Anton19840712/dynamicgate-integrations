using System.Net;
using System.Text;

class Program
{
	static async Task Main()
	{
		var listener = new HttpListener();
		listener.Prefixes.Add("http://localhost:5000/sse/");
		listener.Start();
		Console.WriteLine("SSE сервер запущен на http://localhost:5000/sse/");

		while (true)
		{
			var context = await listener.GetContextAsync();
			_ = Task.Run(() => HandleClient(context));
		}
	}

	private static async Task HandleClient(HttpListenerContext context)
	{
		try
		{
			Console.WriteLine("Новый клиент подключен");

			var response = context.Response;
			response.ContentType = "text/event-stream";
			response.Headers["Cache-Control"] = "no-cache";
			response.Headers["Connection"] = "keep-alive";
			response.ContentEncoding = Encoding.UTF8;

			using var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
			while (true)
			{
				var message = $"data: Сообщение в {DateTime.UtcNow:O}\n\n";
				await writer.WriteAsync(message);
				await writer.FlushAsync();
				Console.WriteLine($"Отправлено: {message}");

				await Task.Delay(5000);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
		}
	}
}
