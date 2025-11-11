using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using test_server_app.Services;

Console.Title = "Dynamic TCP Server";

var builder = WebApplication.CreateBuilder(args);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DynamicTcpServerManager>();

var app = builder.Build();

// Настройка pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Автозапуск TCP сервера с настройками из конфигурации
var tcpManager = app.Services.GetRequiredService<DynamicTcpServerManager>();
var message = app.Configuration["TcpSettings:Message"];
var initialHost = app.Configuration["TcpSettings:Host"] ?? "127.0.0.1";
var initialPort = int.TryParse(app.Configuration["TcpSettings:Port"], out var port) ? port : 5018;

if (!string.IsNullOrWhiteSpace(message))
{
    try
    {
        await tcpManager.StartAsync(initialHost, initialPort, message);
        Log.Information("TCP сервер автоматически запущен на {Host}:{Port}", initialHost, initialPort);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ошибка автозапуска TCP сервера");
    }
}
else
{
    Log.Warning("Сообщение не найдено в конфигурации. TCP сервер не запущен автоматически.");
    Log.Information("Используйте API для запуска: POST /api/TcpServer/start");
}

Log.Information("Web API запущен. Swagger: http://localhost:{Port}/swagger", app.Configuration["urls"] ?? "5000");

await app.RunAsync();
