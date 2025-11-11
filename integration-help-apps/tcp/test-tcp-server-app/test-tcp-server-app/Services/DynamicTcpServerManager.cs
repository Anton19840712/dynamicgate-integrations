using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace test_server_app.Services
{
    public class DynamicTcpServerManager
    {
        private readonly ILogger<DynamicTcpServerManager> _logger;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private Task? _serverTask;
        private string _message = "";
        private string _host = "127.0.0.1";
        private int _port = 5018;

        public DynamicTcpServerManager(ILogger<DynamicTcpServerManager> logger)
        {
            _logger = logger;
        }

        public string CurrentHost => _host;
        public int CurrentPort => _port;
        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested && _listener?.Server?.IsBound == true;

        public async Task StartAsync(string host, int port, string message)
        {
            if (IsRunning)
            {
                _logger.LogWarning("TCP сервер уже запущен на {Host}:{Port}", _host, _port);
                return;
            }

            _host = host;
            _port = port;
            _message = message;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Parse(host), port);
            
            try
            {
                _listener.Start();
                _logger.LogInformation("TCP сервер запущен на {Host}:{Port}", host, port);
                
                _serverTask = RunServerAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска TCP сервера на {Host}:{Port}", host, port);
                await StopAsync();
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!IsRunning && _cts == null) return;

            _logger.LogInformation("Останавливаем TCP сервер...");

            _cts?.Cancel();

            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }

            if (_serverTask != null)
            {
                try
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("Тайм-аут при остановке сервера");
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Сервер остановлен корректно");
                }
            }

            _cts?.Dispose();
            _cts = null;
            _serverTask = null;

            _logger.LogInformation("TCP сервер остановлен");
        }

        public async Task ChangeAddressAsync(string newHost, int newPort)
        {
            if (!IsRunning)
            {
                _logger.LogWarning("Сервер не запущен, нельзя изменить адрес");
                throw new InvalidOperationException("Сервер не запущен");
            }

            _logger.LogInformation("Изменяем адрес TCP сервера с {OldHost}:{OldPort} на {NewHost}:{NewPort}", 
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
            _logger.LogInformation("Сообщение TCP сервера обновлено");
        }

        private async Task RunServerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _logger.LogInformation("Клиент подключен");
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
                catch (ObjectDisposedException)
                {
                    // Сервер остановлен
                    break;
                }
                catch (Exception ex) when (token.IsCancellationRequested)
                {
                    _logger.LogDebug("Сервер остановлен: {Message}", ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при принятии подключения");
                    await Task.Delay(1000, token); // Небольшая пауза перед повторной попыткой
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    int messageCount = 1;
                    byte[] buffer = new byte[1024];

                    while (client.Connected && !token.IsCancellationRequested)
                    {
                        // Читаем входящие сообщения от клиента
                        if (stream.DataAvailable)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            if (bytesRead == 0) break;
                            _logger.LogInformation("Получено сообщение: {Data}", Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }

                        // Отправляем сообщение клиенту
                        byte[] payload = Encoding.UTF8.GetBytes(_message);
                        byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);
                        if (!BitConverter.IsLittleEndian)
                            Array.Reverse(lengthPrefix);

                        await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length, token);
                        await stream.WriteAsync(payload, 0, payload.Length, token);

                        _logger.LogInformation("Отправлено сообщение номер {Count} на {Host}:{Port}", messageCount, _host, _port);

                        messageCount++;
                        await Task.Delay(3000, token);
                    }
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                _logger.LogDebug("Обработка клиента прервана");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке клиента");
            }
            finally
            {
                _logger.LogInformation("Клиент отключился");
            }
        }
    }
}