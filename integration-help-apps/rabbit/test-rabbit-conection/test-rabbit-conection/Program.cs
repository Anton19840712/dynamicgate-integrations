using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using Serilog.Events;

namespace RabbitMqTest
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console(LogEventLevel.Verbose, "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}", null, null, null, null, false, null).CreateLogger();
			Log.Information("Application is starting...");

			ConnectionFactory factory = new ConnectionFactory
			{
				Uri = new Uri("AMQP://admin:admin@172.16.211.18/termidesk")
							//{amqp://admin:admin@172.16.211.18/termidesk}
			};
			try
			{
				using (IConnection connection = factory.CreateConnection())
				{
					using (IModel channel = connection.CreateModel())
					{
						Log.Information("Подключение к RabbitMQ установлено.");
						string queueName = "TestQueue";
						channel.QueueDeclare(queueName, false, false, false, null);
						string message = "Hello RabbitMQ!";
						byte[] body = Encoding.UTF8.GetBytes(message);
						channel.BasicPublish("", queueName, null, body);
						Log.Information("Сообщение отправлено: " + message);
					}
				}
			}
			catch (BrokerUnreachableException ex)
			{
				Log.Error("Ошибка подключения к RabbitMQ: " + ex.Message);
			}
			catch (Exception ex2)
			{
				Log.Error("Произошла ошибка: " + ex2.Message);
			}
			finally
			{
				Log.Information("Приложение завершает работу.");
				Log.CloseAndFlush();
			}
		}
	}
}