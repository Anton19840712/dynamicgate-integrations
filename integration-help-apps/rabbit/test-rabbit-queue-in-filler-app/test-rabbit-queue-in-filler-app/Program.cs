using System.Text;
using RabbitMQ.Client;

class Program
{
	static void Main()
	{
		var factory = new ConnectionFactory()
		{
			HostName = "localhost",
			UserName = "user1",
			Password = "pass1"
		};

		using var connection = factory.CreateConnection();
		using var channel = connection.CreateModel();

		string[] queues =
		{
			"channel_out"
		};

		//		string[] queues =
		//{
		//			"corporation_out",
		//			"epam_out",
		//			"protei_out"
		//		};

		foreach (var queue in queues)
		{
			channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

			for (int i = 1; i <= 10; i++)
			{
				string message = $"Message {i} for {queue}";
				var body = Encoding.UTF8.GetBytes(message);

				channel.BasicPublish(exchange: "",
									 routingKey: queue,
									 basicProperties: null,
									 body: body);

				Console.WriteLine($"[x] Sent '{message}'");
			}
		}

		Console.WriteLine("All messages sent.");
	}
}
