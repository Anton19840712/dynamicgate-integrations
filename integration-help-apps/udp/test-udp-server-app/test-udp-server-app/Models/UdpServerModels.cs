namespace test_udp_server_app.Models
{
    public class UdpServerStartRequest
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; }
        public string Message { get; set; } = "";
    }

    public class UdpServerChangeAddressRequest
    {
        public string NewHost { get; set; } = "127.0.0.1";
        public int NewPort { get; set; }
    }

    public class UdpServerUpdateMessageRequest
    {
        public string Message { get; set; } = "";
    }

    public class UdpServerSendMessagesRequest
    {
        public int Count { get; set; }
        public string? CustomMessage { get; set; }
    }
}