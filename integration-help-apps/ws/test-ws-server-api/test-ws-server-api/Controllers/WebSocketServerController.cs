using Microsoft.AspNetCore.Mvc;

namespace test_ws_server_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebSocketServerController : ControllerBase
{
    private readonly ILogger<WebSocketServerController> _logger;

    public WebSocketServerController(ILogger<WebSocketServerController> logger)
    {
        _logger = logger;
    }

    [HttpPost("start")]
    public IActionResult StartServer([FromBody] WebSocketServerRequest request)
    {
        try
        {
            _logger.LogInformation("Запрос на запуск WebSocket сервера. Host: {Host}, Port: {Port}", 
                request.Host, request.Port);

            // Имитация успешного запуска сервера
            // В реальной реализации здесь бы запускался фактический WebSocket сервер
            var response = new
            {
                Status = "Started",
                Host = request.Host,
                Port = request.Port,
                Message = $"WebSocket сервер запущен на {request.Host}:{request.Port}",
                TestMessage = request.Message,
                StartedAt = DateTime.UtcNow
            };

            _logger.LogInformation("WebSocket сервер успешно 'запущен': {Response}", response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при запуске WebSocket сервера");
            return StatusCode(500, new { Error = "Ошибка запуска сервера", Details = ex.Message });
        }
    }

    [HttpPost("stop")]
    public IActionResult StopServer()
    {
        try
        {
            _logger.LogInformation("Запрос на остановку WebSocket сервера");

            var response = new
            {
                Status = "Stopped",
                Message = "WebSocket сервер остановлен",
                StoppedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при остановке WebSocket сервера");
            return StatusCode(500, new { Error = "Ошибка остановки сервера", Details = ex.Message });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var response = new
        {
            Status = "Running",
            Message = "WebSocket сервер API работает",
            CheckedAt = DateTime.UtcNow,
            ServerInfo = new
            {
                ApiPort = 8003,
                WebSocketPort = 5019,
                Protocol = "WebSocket"
            }
        };

        return Ok(response);
    }
}

public class WebSocketServerRequest
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5019;
    public string Message { get; set; } = "Default WebSocket message";
}