using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace test_udp_server_app.Services
{
    public class DynamicUdpServerManager
    {
        private readonly ILogger<DynamicUdpServerManager> _logger;
        private UdpClient? _udpServer;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;
        private string _message = "";
        private string _host = "127.0.0.1";
        private int _port = 5018;
        private readonly HashSet<IPEndPoint> _clients = new();

        public DynamicUdpServerManager(ILogger<DynamicUdpServerManager> logger)
        {
            _logger = logger;
        }

        public string CurrentHost => _host;
        public int CurrentPort => _port;
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested && _udpServer != null;

        public async Task StartAsync(string host, int port, string message)
        {
            if (IsRunning)
            {
                _logger.LogWarning("UDP сервер уже запущен на {Host}:{Port}", _host, _port);
                return;
            }

            _host = host;
            _port = port;
            _message = message;

            _cts = new CancellationTokenSource();
            _udpServer = new UdpClient(new IPEndPoint(IPAddress.Parse(host), port));
            
            try
            {
                _logger.LogInformation("UDP сервер запущен на {Host}:{Port}", host, port);
                
                _serverTask = RunServerAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска UDP сервера на {Host}:{Port}", host, port);
                await StopAsync();
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!IsRunning && _cts == null) return;

            _logger.LogInformation("Останавливаем UDP сервер...");

            _cts?.Cancel();

            if (_udpServer != null)
            {
                _udpServer.Close();
                _udpServer = null;
            }

            if (_serverTask != null)
            {
                try
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Тайм-аут при остановке UDP сервера");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("UDP сервер остановлен корректно");
                }
            }

            _cts?.Dispose();
            _cts = null;
            _serverTask = null;
            _clients.Clear();

            _logger.LogInformation("UDP сервер остановлен");
        }

        public async Task ChangeAddressAsync(string newHost, int newPort)
        {
            if (!IsRunning)
            {
                _logger.LogWarning("UDP сервер не запущен, нельзя изменить адрес");
                throw new InvalidOperationException("Сервер не запущен");
            }

            _logger.LogInformation("Изменяем адрес UDP сервера с {OldHost}:{OldPort} на {NewHost}:{NewPort}", 
                _host, _port, newHost, newPort);

            var currentMessage = _message;
            
            // Останавливаем текущий сервер
            await StopAsync();
            
            // Небольшая задержка для освобождения ресурсов
            await Task.Delay(500);
            
            // Запускаем на новом адресе
            await StartAsync(newHost, newPort, currentMessage);
        }

        public void UpdateMessage(string newMessage)
        {
            _message = newMessage;
            _logger.LogInformation("Сообщение UDP сервера обновлено");
        }

        public async Task<(int SentCount, int FailedCount)> SendMessagesAsync(int count, string? customMessage = null)
        {
            if (!IsRunning || _udpServer == null)
            {
                _logger.LogWarning("UDP сервер не запущен - невозможно отправить сообщения");
                return (0, count);
            }

            var messageToSend = customMessage ?? _message;
            var sentCount = 0;
            var failedCount = 0;

            _logger.LogInformation("Начинаем отправку {Count} сообщений всем клиентам", count);

            for (int i = 0; i < count; i++)
            {
                var numberedMessage = $"[{i + 1}/{count}] {messageToSend}";
                var clientsCopy = _clients.ToList();
                
                if (!clientsCopy.Any())
                {
                    _logger.LogWarning("Нет подключенных клиентов для отправки сообщения {MessageNumber}", i + 1);
                    failedCount++;
                    continue;
                }

                foreach (var client in clientsCopy)
                {
                    try
                    {
                        byte[] data = Encoding.UTF8.GetBytes(numberedMessage);
                        await _udpServer.SendAsync(data, data.Length, client);
                        _logger.LogInformation("Отправлено сообщение {MessageNumber} клиенту {Client}: {Message}", 
                            i + 1, client, numberedMessage);
                        sentCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка отправки сообщения {MessageNumber} клиенту {Client}", i + 1, client);
                        _clients.Remove(client);
                        failedCount++;
                    }
                }

                // Небольшая задержка между сообщениями
                await Task.Delay(100);
            }

            _logger.LogInformation("Завершена отправка сообщений. Успешно: {SentCount}, Ошибок: {FailedCount}", 
                sentCount, failedCount);

            return (sentCount, failedCount);
        }

        private async Task RunServerAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _udpServer != null)
                {
                    try
                    {
                        var result = await _udpServer.ReceiveAsync();
                        
                        string received = Encoding.UTF8.GetString(result.Buffer);
                        var clientEndPoint = result.RemoteEndPoint;

                        _logger.LogInformation("Получено сообщение от клиента {Client}: {Message}", clientEndPoint, received);

                        if (_clients.Add(clientEndPoint))
                        {
                            _logger.LogInformation("Добавлен новый клиент: {Client}", clientEndPoint);
                        }

                        // Отправляем сообщение всем зарегистрированным клиентам
                        foreach (var client in _clients.ToList())
                        {
                            try
                            {
                                byte[] data = Encoding.UTF8.GetBytes(_message);
                                await _udpServer.SendAsync(data, data.Length, client);
                                _logger.LogInformation("Отправлено сообщение клиенту {Client} на {Host}:{Port}", client, _host, _port);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка отправки сообщения клиенту {Client}", client);
                                _clients.Remove(client);
                            }
                        }

                        await Task.Delay(3000, token);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Сервер остановлен
                        break;
                    }
                    catch (Exception ex) when (token.IsCancellationRequested)
                    {
                        _logger.LogDebug("UDP сервер остановлен: {Message}", ex.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке UDP сообщения");
                        await Task.Delay(1000, token); // Небольшая пауза перед повторной попыткой
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UDP сервер остановлен по запросу");
            }
            finally
            {
                _logger.LogInformation("UDP сервер завершил работу");
            }
        }
    }
}