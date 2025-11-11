using Microsoft.AspNetCore.SignalR;

namespace ResponseMonitor;

/// <summary>
/// SignalR Hub для отправки логов в браузер в реальном времени
/// </summary>
public class LogHub : Hub
{
	/// <summary>
	/// Отправляет лог всем подключенным клиентам
	/// </summary>
	public async Task BroadcastLog(LogEntry logEntry)
	{
		await Clients.All.SendAsync("ReceiveLog", logEntry);
	}
}

/// <summary>
/// Модель лога для отправки в браузер
/// </summary>
public class LogEntry
{
	public DateTime Timestamp { get; set; }
	public string Method { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public int StatusCode { get; set; }
	public string ContentType { get; set; } = string.Empty;
	public Dictionary<string, string> Headers { get; set; } = new();
	public string Body { get; set; } = string.Empty;
}
