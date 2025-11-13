using System.Text;
using RabbitMQ.Client;

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

		string queue = "dev_channel_out";
		// Queue already exists with DLX configuration - no need to declare
		// channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

		// Выбор режима нагрузки
		Console.WriteLine("Выберите режим нагрузочного тестирования:");
		Console.WriteLine();
		Console.WriteLine("1 - Низкая нагрузка (10 msg/sec, 100 сообщений)");
		Console.WriteLine("2 - Средняя нагрузка (50 msg/sec, 500 сообщений)");
		Console.WriteLine("3 - Высокая нагрузка (100 msg/sec, 1000 сообщений)");
		Console.WriteLine("4 - Экстремальная нагрузка (500 msg/sec, 5000 сообщений)");
		Console.WriteLine("5 - Burst тест (1000 msg мгновенно)");
		Console.WriteLine();
		Console.Write("Режим: ");

		var choice = Console.ReadLine();

		int messagesPerSecond = 10;
		int totalMessages = 100;
		bool burstMode = false;

		switch (choice)
		{
			case "1":
				messagesPerSecond = 10;
				totalMessages = 100;
				break;
			case "2":
				messagesPerSecond = 50;
				totalMessages = 500;
				break;
			case "3":
				messagesPerSecond = 100;
				totalMessages = 1000;
				break;
			case "4":
				messagesPerSecond = 500;
				totalMessages = 5000;
				break;
			case "5":
				burstMode = true;
				totalMessages = 1000;
				break;
			default:
				Console.WriteLine("Неверный выбор, используется режим 1");
				break;
		}

		Console.WriteLine();
		Console.WriteLine($"Режим: {(burstMode ? "Burst" : messagesPerSecond + " msg/sec")}");
		Console.WriteLine($"Всего сообщений: {totalMessages}");
		Console.WriteLine();
		Console.WriteLine("Нажмите Enter для начала...");
		Console.ReadLine();

		var startTime = DateTime.Now;
		int sentCount = 0;

		if (burstMode)
		{
			// Burst режим - отправляем все сразу
			Console.WriteLine("Отправка burst...");
			for (int i = 1; i <= totalMessages; i++)
			{
				SendMessage(channel, queue, i);
				sentCount++;

				if (i % 100 == 0)
				{
					Console.WriteLine($"Отправлено: {i}/{totalMessages}");
				}
			}
		}
		else
		{
			// Контролируемая нагрузка
			int delayMs = 1000 / messagesPerSecond;
			Console.WriteLine($"Задержка между сообщениями: {delayMs} ms");
			Console.WriteLine();

			for (int i = 1; i <= totalMessages; i++)
			{
				var iterationStart = DateTime.Now;

				SendMessage(channel, queue, i);
				sentCount++;

				if (i % messagesPerSecond == 0)
				{
					var elapsed = (DateTime.Now - startTime).TotalSeconds;
					var currentRate = sentCount / elapsed;
					Console.WriteLine($"[{elapsed:F1}s] Отправлено: {sentCount}/{totalMessages} | Скорость: {currentRate:F1} msg/sec");
				}

				// Контроль скорости отправки
				var iterationTime = (DateTime.Now - iterationStart).TotalMilliseconds;
				if (iterationTime < delayMs)
				{
					Thread.Sleep((int)(delayMs - iterationTime));
				}
			}
		}

		var endTime = DateTime.Now;
		var duration = (endTime - startTime).TotalSeconds;
		var actualRate = sentCount / duration;

		Console.WriteLine();
		Console.WriteLine("====================================================");
		Console.WriteLine("РЕЗУЛЬТАТЫ:");
		Console.WriteLine($"Отправлено: {sentCount} сообщений");
		Console.WriteLine($"Время: {duration:F2} секунд");
		Console.WriteLine($"Средняя скорость: {actualRate:F2} msg/sec");
	}

	static void SendMessage(IModel channel, string queue, int index)
	{
		// Чередуем разные типы сообщений
		string message = (index % 3) switch
		{
			0 => $"{{\"cardNumber\":\"LOAD-{index:D6}\",\"type\":\"load-test\",\"index\":{index},\"timestamp\":\"{DateTime.UtcNow:O}\"}}",
			1 => $"{{\"cardNumber\":\"PERF-{index:D6}\",\"data\":{{\"value\":{index},\"random\":{Random.Shared.Next(1000)}}},\"timestamp\":\"{DateTime.UtcNow:O}\"}}",
			_ => $"{{\"cardNumber\":\"TEST-{index:D6}\",\"message\":\"Load testing message #{index}\",\"timestamp\":\"{DateTime.UtcNow:O}\"}}"
		};

		var body = Encoding.UTF8.GetBytes(message);

		channel.BasicPublish(
			exchange: "",
			routingKey: queue,
			basicProperties: null,
			body: body
		);
	}
}
