namespace test_server_app.Models
{
    public class TcpServerStartRequest
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; }
        public string Message { get; set; } = "";
    }

    public class TcpServerChangeAddressRequest
    {
        public string NewHost { get; set; } = "127.0.0.1";
        public int NewPort { get; set; }
    }

    public class TcpServerUpdateMessageRequest
    {
        public string Message { get; set; } = "";
    }
}