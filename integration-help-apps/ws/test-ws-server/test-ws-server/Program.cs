using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;

class Program
{
	private static IConfiguration _config;
	private static ILogger _logger;
	private static string _responseMessage;
	private static int _port;
	private static string _host;

	static async Task Main(string[] args)
	{
		_config = BuildConfig();
		_logger = new LoggerConfiguration()
			.ReadFrom.Configuration(_config)
			.Enrich.FromLogContext()
			.CreateLogger();

		_host = _config["WebSocketSettings:Host"] ?? "127.0.0.1";
		_port = int.Parse(_config["WebSocketSettings:Port"] ?? "5018");
		_responseMessage = _config["WebSocketSettings:Message"] ?? "Сообщение от сервера";

		var listener = new HttpListener();
		listener.Prefixes.Add($"http://{_host}:{_port}/");
		listener.Start();

		Console.Title = "WebSocket server";
		_logger.Information("WebSocket-сервер запущен на {Host}:{Port}", _host, _port);

		while (true)
		{
			try
			{
				HttpListenerContext context = await listener.GetContextAsync();
				if (context.Request.IsWebSocketRequest)
				{
					_logger.Information("Получен новый WebSocket-запрос");
					HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
					_ = HandleConnectionAsync(wsContext.WebSocket);
				}
				else
				{
					context.Response.StatusCode = 400;
					context.Response.Close();
				}
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Ошибка при обработке WebSocket-запроса");
			}
		}
	}

	private static async Task HandleConnectionAsync(WebSocket webSocket)
	{
		byte[] buffer = new byte[1024];
		_logger.Information("Клиент подключен");

		try
		{
			var sendTask = Task.Run(async () =>
			{
				while (webSocket.State == WebSocketState.Open)
				{
					try
					{
						byte[] messageBytes = Encoding.UTF8.GetBytes(_responseMessage);
						await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
						_logger.Information("Отправлено клиенту: {Message}", _responseMessage);
					}
					catch (Exception ex)
					{
						_logger.Warning("Ошибка при отправке сообщения клиенту: {Error}", ex.Message);
					}
					await Task.Delay(3000);
				}
			});

			while (webSocket.State == WebSocketState.Open)
			{
				var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
				if (result.MessageType == WebSocketMessageType.Close)
				{
					_logger.Information("Клиент закрыл соединение");
					await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие клиентом", CancellationToken.None);
					break;
				}

				string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
				_logger.Information("Получено сообщение от клиента: {Message}", message);
			}
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Ошибка в WebSocket-соединении");
		}
		finally
		{
			if (webSocket.State != WebSocketState.Closed)
			{
				await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Сервер завершает соединение", CancellationToken.None);
			}
			_logger.Information("WebSocket-соединение закрыто");
		}
	}

	private static IConfiguration BuildConfig()
	{
		var builder = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.AddEnvironmentVariables();

		return builder.Build();
	}
}
