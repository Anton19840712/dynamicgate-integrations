using Microsoft.AspNetCore.SignalR;
using ResponseMonitor;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

var app = builder.Build();
app.UseCors();

// Главная страница с консолью
app.MapGet("/", async context =>
{
	var html = @"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Response Monitor</title>
    <script src=""https://cdn.jsdelivr.net/npm/@microsoft/signalr@8.0.0/dist/browser/signalr.min.js""></script>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
            background: #fafafa;
            color: #333;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 12px 16px;
            background: #fff;
            border-bottom: 1px solid #e0e0e0;
        }
        .status {
            display: flex;
            align-items: center;
            font-size: 13px;
            color: #666;
        }
        .status-indicator {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #999;
            margin-right: 8px;
        }
        .status.connected .status-indicator { background: #666; }
        button {
            background: #fff;
            color: #333;
            border: 1px solid #999;
            padding: 6px 12px;
            cursor: pointer;
            font-size: 12px;
            font-family: inherit;
        }
        button:hover { background: #666; color: #ddd; border-color: #666; }
        .message {
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #999;
            font-size: 13px;
        }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""status"" id=""status"">
            <span class=""status-indicator""></span>
            <span id=""status-text"">CONNECTING...</span>
        </div>
        <button onclick=""clearConsole()"">Clear</button>
    </div>

    <div class=""message"">Open DevTools (F12) to view incoming messages in Console</div>

    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(""/loghub"")
            .withAutomaticReconnect()
            .build();

        connection.on(""ReceiveLog"", function (log) {
            addLog(log);
        });

        connection.start()
            .then(() => {
                document.getElementById('status').classList.add('connected');
                document.getElementById('status-text').textContent = 'CONNECTED';
                console.log('%cResponse Monitor Connected', 'color: #666; font-weight: 600');
                console.log('Waiting for incoming requests...');
            })
            .catch(err => {
                document.getElementById('status-text').textContent = 'ERROR';
                console.error('Connection error:', err.message);
            });

        connection.onreconnecting(() => {
            document.getElementById('status').classList.remove('connected');
            document.getElementById('status-text').textContent = 'RECONNECTING...';
            console.log('Reconnecting...');
        });

        connection.onreconnected(() => {
            document.getElementById('status').classList.add('connected');
            document.getElementById('status-text').textContent = 'CONNECTED';
            console.log('Reconnected');
        });

        function addLog(log) {
            const timestamp = new Date(log.timestamp).toLocaleString('ru-RU');

            console.log('---');
            console.log(`[${timestamp}] ${log.method} ${log.path}`);
            console.log(`Status: ${log.statusCode} | Content-Type: ${log.contentType}`);

            if (Object.keys(log.headers).length > 0) {
                console.log('Headers:');
                for (const [key, value] of Object.entries(log.headers)) {
                    console.log(`  ${key}: ${value}`);
                }
            }

            if (log.body) {
                console.log('Body:');
                console.log(log.body);
            }
        }

        function clearConsole() {
            console.clear();
            console.log('Console cleared');
        }
    </script>
</body>
</html>";

	context.Response.ContentType = "text/html; charset=utf-8";
	await context.Response.WriteAsync(html);
});

// Endpoint для приёма запросов
app.MapPost("/api/receive", async (HttpContext context, IHubContext<LogHub> hubContext) =>
{
	try
	{
		using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
		var body = await reader.ReadToEndAsync();

		var headers = new Dictionary<string, string>();
		foreach (var header in context.Request.Headers)
		{
			headers[header.Key] = header.Value.ToString();
		}

		var logEntry = new LogEntry
		{
			Timestamp = DateTime.Now,
			Method = context.Request.Method,
			Path = context.Request.Path,
			StatusCode = 200,
			ContentType = context.Request.ContentType ?? "unknown",
			Headers = headers,
			Body = body
		};

		await hubContext.Clients.All.SendAsync("ReceiveLog", logEntry);

		Console.WriteLine($"[{logEntry.Timestamp:HH:mm:ss}] {logEntry.Method} {logEntry.Path}");
		Console.WriteLine($"Content-Type: {logEntry.ContentType} | Body: {body.Length} bytes");

		return Results.Ok(new { success = true, message = "Received" });
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
		return Results.BadRequest(new { success = false, error = ex.Message });
	}
});

app.MapHub<LogHub>("/loghub");

Console.WriteLine("Response Monitor - Dynamic Gateway");
Console.WriteLine("===================================");
Console.WriteLine();
Console.WriteLine("Веб-интерфейс:");
Console.WriteLine("http://localhost:5000");
Console.WriteLine();
Console.WriteLine("Endpoint для приёма:");
Console.WriteLine("http://localhost:5000/api/receive");
Console.WriteLine();
Console.WriteLine("Проверяю браузер...");

var url = "http://localhost:5000";
try
{
	// Проверяем, не открыт ли уже браузер на этом порту
	using var httpClient = new HttpClient();
	httpClient.Timeout = TimeSpan.FromMilliseconds(500);

	bool alreadyOpen = false;
	try
	{
		var response = await httpClient.GetAsync(url);
		alreadyOpen = true;
		Console.WriteLine("Браузер уже открыт - страница будет обновлена автоматически через SignalR");
	}
	catch
	{
		// Порт не отвечает - значит это первый запуск
	}

	if (!alreadyOpen)
	{
		System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});
		Console.WriteLine("Браузер запущен");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Не удалось открыть браузер: {ex.Message}");
	Console.WriteLine($"Откройте вручную: {url}");
}

Console.WriteLine();
Console.WriteLine("Ожидание запросов...");
Console.WriteLine();

app.Run("http://localhost:5000");
