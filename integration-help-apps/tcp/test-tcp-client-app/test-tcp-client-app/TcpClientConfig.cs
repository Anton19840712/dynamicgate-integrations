public class TcpClientConfig
{
    public string ServerHost { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 8888;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int BufferSize { get; set; } = 1024;
}