using System.Text;
using RabbitMQ.Client;

/// <summary>
/// Main program class to send test messages to RabbitMQ queues.
/// </summary>
class Program
{
	static void Main()
	{
		var factory = new ConnectionFactory()
		{
			HostName = "localhost",
			UserName = "service",
			Password = "A1qwert"
		};

		using var connection = factory.CreateConnection();
		using var channel = connection.CreateModel();

		string[] queues =
		{
			"dev_channel_out"
		};

		//		string[] queues =
		//{
		//			"corporation_out",
		//			"epam_out",
		//			"protei_out"
		//		};

		foreach (var queue in queues)
		{
			// Queue already exists with DLX configuration - no need to declare
			// channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

			// Тестовые сообщения для разных сценариев
			string[] testMessages =
			{
				// Сценарий 1: JSON с cardNumber (для LogField)
				"{\"cardNumber\":\"1234-5678-9012-3456\",\"globalId\":\"card-001\",\"temperature\":25,\"humidity\":60,\"status\":\"active\"}",

				// Сценарий 2: JSON без cardNumber
				"{\"deviceId\":\"sensor-123\",\"timestamp\":\"2025-11-12T10:30:00Z\",\"value\":42.5}",

				// Сценарий 3: JSON с вложенными объектами
				"{\"cardNumber\":\"9876-5432-1098-7654\",\"data\":{\"location\":\"warehouse-A\",\"items\":[{\"id\":1,\"name\":\"item1\"},{\"id\":2,\"name\":\"item2\"}]}}",

				// Сценарий 4: XML сообщение
				"<message><cardNumber>5555-6666-7777-8888</cardNumber><type>alert</type><priority>high</priority></message>",

				// Сценарий 5: Простой текст
				"Simple text message without structure",

				// Сценарий 6: JSON с разными типами данных
				"{\"cardNumber\":\"1111-2222-3333-4444\",\"active\":true,\"count\":100,\"price\":29.99,\"tags\":[\"urgent\",\"important\"],\"metadata\":null}",

				// Сценарий 7: Большой JSON (для тестирования производительности)
				"{\"cardNumber\":\"7777-8888-9999-0000\",\"description\":\"" + new string('A', 500) + "\",\"longArray\":[1,2,3,4,5,6,7,8,9,10]}",

				// Сценарий 8: JSON с кириллицей
				"{\"cardNumber\":\"0000-1111-2222-3333\",\"userName\":\"Иванов Иван\",\"city\":\"Москва\",\"comment\":\"Тестовое сообщение на русском языке\"}"
			};

			for (int i = 0; i < testMessages.Length; i++)
			{
				string message = testMessages[i];
				var body = Encoding.UTF8.GetBytes(message);

				channel.BasicPublish(exchange: "",
									 routingKey: queue,
									 basicProperties: null,
									 body: body);

				Console.WriteLine($"[{i + 1}/{testMessages.Length}] Sent to '{queue}':");
				Console.WriteLine($"  {(message.Length > 100 ? message.Substring(0, 100) + "..." : message)}");
				Console.WriteLine();
			}
		}

		Console.WriteLine("All messages sent.");
	}
}
