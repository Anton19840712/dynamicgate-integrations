using Microsoft.Extensions.Configuration;
using Serilog;
using test_udp_server_app.Services;

Console.Title = "Dynamic UDP Server";

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
builder.Services.AddSingleton<DynamicUdpServerManager>();

var app = builder.Build();

// Настройка pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

// Автозапуск UDP сервера с настройками из конфигурации
var udpManager = app.Services.GetRequiredService<DynamicUdpServerManager>();
var message = app.Configuration["UdpSettings:Message"];
var initialHost = app.Configuration["UdpSettings:Host"] ?? "127.0.0.1";
var initialPort = int.TryParse(app.Configuration["UdpSettings:Port"], out var port) ? port : 5019;

if (!string.IsNullOrWhiteSpace(message))
{
    try
    {
        await udpManager.StartAsync(initialHost, initialPort, message);
        Log.Information("UDP сервер автоматически запущен на {Host}:{Port}", initialHost, initialPort);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Ошибка автозапуска UDP сервера");
    }
}
else
{
    Log.Warning("Сообщение не найдено в конфигурации. UDP сервер не запущен автоматически.");
    Log.Information("Используйте API для запуска: POST /api/UdpServer/start");
}

Log.Information("Web API запущен. Swagger: http://localhost:{Port}/swagger", app.Configuration["urls"] ?? "5101");

await app.RunAsync();