using System.Text;

class Program
{
	private static async Task Main(string[] args)
	{

		// Убедитесь, что адрес соответствует вашему SSE серверу
		var serverUrl = "http://localhost:52799/sse";

		// Создание HttpClient для подключения к серверу
		using var client = new HttpClient();

		// Запрос для получения потока SSE
		var stream = await client.GetStreamAsync(serverUrl);

		// Чтение данных из потока
		using var reader = new StreamReader(stream, Encoding.UTF8);

		Console.WriteLine("Connected to SSE server...");

		while (true)
		{
			try
			{
				// Чтение строки из потока
				var line = await reader.ReadLineAsync();

				if (line == null)
				{
					// Если поток закрыт
					break;
				}

				// Вывод полученной строки (сообщения)
				if (line.StartsWith("data: "))
				{
					var message = line.Substring(6); // Убираем префикс "data: "
					Console.WriteLine($"Received message: {message}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error receiving message: {ex.Message}");
				break;
			}
		}

		Console.WriteLine("Disconnected from SSE server.");
	}
}
