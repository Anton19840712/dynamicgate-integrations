# TCP Test Client

TCP клиент для тестирования интеграции с dynamic gateway.

## Конфигурация

### appsettings.json
```json
{
  "TcpClient": {
    "ServerHost": "127.0.0.1",
    "ServerPort": 8888,
    "ReconnectDelaySeconds": 5,
    "BufferSize": 1024
  }
}
```

## Способы запуска

### 1. Стандартный запуск (из appsettings.json)
```bash
dotnet run
```

### 2. Переопределение через переменные среды
```bash
set TcpClient__ServerHost=192.168.1.100
set TcpClient__ServerPort=9999
dotnet run
```

### 3. Переопределение через аргументы командной строки
```bash
dotnet run --TcpClient:ServerHost=localhost --TcpClient:ServerPort=7777
```

### 4. Разные профили
```bash
# Development (порт 8888, больше логов)
dotnet run --environment=Development

# Production (порт 6254, меньше логов)
dotnet run --environment=Production
```

## Функциональность

- ✅ Автоматическое переподключение при разрыве соединения
- ✅ Конфигурируемые параметры через appsettings.json
- ✅ Поддержка переменных среды и аргументов командной строки
- ✅ Разные профили конфигурации для Development/Production
- ✅ Подробное логирование всех операций

## Интеграция с Dynamic Gateway

Клиент предназначен для тестирования late binding серверов:

1. **Early Binding**: Запустить с `--TcpClient:ServerPort=6254` для подключения к статическому серверу
2. **Late Binding**: Запустить с `--TcpClient:ServerPort=8888` для подключения к динамическому серверу

Клиент автоматически получает сообщения из RabbitMQ очереди `channel_out` через TCP сервер.