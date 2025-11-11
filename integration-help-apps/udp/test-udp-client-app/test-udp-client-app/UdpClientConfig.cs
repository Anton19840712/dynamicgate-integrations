public class UdpClientConfig
{
    public string ServerHost { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 6254;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int BufferSize { get; set; } = 1024;
}