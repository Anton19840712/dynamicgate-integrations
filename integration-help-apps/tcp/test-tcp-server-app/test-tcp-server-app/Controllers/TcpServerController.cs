using Microsoft.AspNetCore.Mvc;
using test_server_app.Services;
using test_server_app.Models;
using Microsoft.Extensions.Logging;

namespace test_server_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TcpServerController : ControllerBase
    {
        private readonly DynamicTcpServerManager _serverManager;
        private readonly ILogger<TcpServerController> _logger;

        public TcpServerController(DynamicTcpServerManager serverManager, ILogger<TcpServerController> logger)
        {
            _serverManager = serverManager;
            _logger = logger;
        }

        /// <summary>
        /// Запуск TCP сервера
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] TcpServerStartRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Host))
                    return BadRequest("Host is required");

                if (request.Port <= 0 || request.Port > 65535)
                    return BadRequest("Port must be between 1 and 65535");

                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message is required");

                await _serverManager.StartAsync(request.Host, request.Port, request.Message);

                var result = new
                {
                    message = "TCP сервер запущен успешно",
                    host = request.Host,
                    port = request.Port,
                    status = "started",
                    startedAt = DateTime.UtcNow
                };

                _logger.LogInformation("TCP Server started on {Host}:{Port} via API", request.Host, request.Port);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting TCP server");
                return StatusCode(500, new { message = "Ошибка запуска сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Остановка TCP сервера
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            try
            {
                await _serverManager.StopAsync();

                var result = new
                {
                    message = "TCP сервер остановлен успешно",
                    status = "stopped",
                    stoppedAt = DateTime.UtcNow
                };

                _logger.LogInformation("TCP Server stopped via API");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping TCP server");
                return StatusCode(500, new { message = "Ошибка остановки сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Изменение адреса TCP сервера
        /// </summary>
        [HttpPost("change-address")]
        public async Task<IActionResult> ChangeAddress([FromBody] TcpServerChangeAddressRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NewHost))
                    return BadRequest("NewHost is required");

                if (request.NewPort <= 0 || request.NewPort > 65535)
                    return BadRequest("NewPort must be between 1 and 65535");

                var oldHost = _serverManager.CurrentHost;
                var oldPort = _serverManager.CurrentPort;

                await _serverManager.ChangeAddressAsync(request.NewHost, request.NewPort);

                var result = new
                {
                    message = "Адрес TCP сервера изменен успешно",
                    oldHost = oldHost,
                    oldPort = oldPort,
                    newHost = request.NewHost,
                    newPort = request.NewPort,
                    status = "address_changed",
                    changedAt = DateTime.UtcNow
                };

                _logger.LogInformation("TCP Server address changed from {OldHost}:{OldPort} to {NewHost}:{NewPort} via API", 
                    oldHost, oldPort, request.NewHost, request.NewPort);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing TCP server address");
                return StatusCode(500, new { message = "Ошибка изменения адреса сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Обновление сообщения
        /// </summary>
        [HttpPost("update-message")]
        public IActionResult UpdateMessage([FromBody] TcpServerUpdateMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message is required");

                _serverManager.UpdateMessage(request.Message);

                var result = new
                {
                    message = "Сообщение TCP сервера обновлено успешно",
                    newMessage = request.Message,
                    status = "message_updated",
                    updatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating TCP server message");
                return StatusCode(500, new { message = "Ошибка обновления сообщения", error = ex.Message });
            }
        }

        /// <summary>
        /// Получение статуса TCP сервера
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            try
            {
                var result = new
                {
                    isRunning = _serverManager.IsRunning,
                    host = _serverManager.CurrentHost,
                    port = _serverManager.CurrentPort,
                    status = _serverManager.IsRunning ? "running" : "stopped",
                    checkedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TCP server status");
                return StatusCode(500, new { message = "Ошибка получения статуса", error = ex.Message });
            }
        }
    }
}