using Microsoft.AspNetCore.Mvc;
using test_udp_server_app.Services;
using test_udp_server_app.Models;
using Microsoft.Extensions.Logging;

namespace test_udp_server_app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UdpServerController : ControllerBase
    {
        private readonly DynamicUdpServerManager _serverManager;
        private readonly ILogger<UdpServerController> _logger;

        public UdpServerController(DynamicUdpServerManager serverManager, ILogger<UdpServerController> logger)
        {
            _serverManager = serverManager;
            _logger = logger;
        }

        /// <summary>
        /// Запуск UDP сервера
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] UdpServerStartRequest request)
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
                    message = "UDP сервер запущен успешно",
                    host = request.Host,
                    port = request.Port,
                    status = "started",
                    startedAt = DateTime.UtcNow
                };

                _logger.LogInformation("UDP Server started on {Host}:{Port} via API", request.Host, request.Port);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting UDP server");
                return StatusCode(500, new { message = "Ошибка запуска сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Остановка UDP сервера
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> Stop()
        {
            try
            {
                await _serverManager.StopAsync();

                var result = new
                {
                    message = "UDP сервер остановлен успешно",
                    status = "stopped",
                    stoppedAt = DateTime.UtcNow
                };

                _logger.LogInformation("UDP Server stopped via API");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping UDP server");
                return StatusCode(500, new { message = "Ошибка остановки сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Изменение адреса UDP сервера
        /// </summary>
        [HttpPost("change-address")]
        public async Task<IActionResult> ChangeAddress([FromBody] UdpServerChangeAddressRequest request)
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
                    message = "Адрес UDP сервера изменен успешно",
                    oldHost = oldHost,
                    oldPort = oldPort,
                    newHost = request.NewHost,
                    newPort = request.NewPort,
                    status = "address_changed",
                    changedAt = DateTime.UtcNow
                };

                _logger.LogInformation("UDP Server address changed from {OldHost}:{OldPort} to {NewHost}:{NewPort} via API", 
                    oldHost, oldPort, request.NewHost, request.NewPort);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing UDP server address");
                return StatusCode(500, new { message = "Ошибка изменения адреса сервера", error = ex.Message });
            }
        }

        /// <summary>
        /// Обновление сообщения
        /// </summary>
        [HttpPost("update-message")]
        public IActionResult UpdateMessage([FromBody] UdpServerUpdateMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest("Message is required");

                _serverManager.UpdateMessage(request.Message);

                var result = new
                {
                    message = "Сообщение UDP сервера обновлено успешно",
                    newMessage = request.Message,
                    status = "message_updated",
                    updatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating UDP server message");
                return StatusCode(500, new { message = "Ошибка обновления сообщения", error = ex.Message });
            }
        }

        /// <summary>
        /// Получение статуса UDP сервера
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
                _logger.LogError(ex, "Error getting UDP server status");
                return StatusCode(500, new { message = "Ошибка получения статуса", error = ex.Message });
            }
        }

        /// <summary>
        /// Отправить точное количество сообщений
        /// </summary>
        [HttpPost("send-messages")]
        public async Task<IActionResult> SendMessages([FromBody] UdpServerSendMessagesRequest request)
        {
            try
            {
                if (!_serverManager.IsRunning)
                    return BadRequest("UDP сервер не запущен");

                if (request.Count <= 0 || request.Count > 100)
                    return BadRequest("Количество сообщений должно быть от 1 до 100");

                var result = await _serverManager.SendMessagesAsync(request.Count, request.CustomMessage);

                return Ok(new
                {
                    message = $"Отправлено {result.SentCount} сообщений из {request.Count}",
                    sentCount = result.SentCount,
                    failedCount = result.FailedCount,
                    status = "completed",
                    sentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending messages");
                return StatusCode(500, new { message = "Ошибка отправки сообщений", error = ex.Message });
            }
        }
    }
}