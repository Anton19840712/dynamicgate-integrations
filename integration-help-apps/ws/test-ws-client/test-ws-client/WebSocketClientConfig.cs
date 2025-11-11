namespace test_ws_client
{
	public class WebSocketClientConfig
	{
		public string ServerHost { get; set; } = "127.0.0.1";
		public int ServerPort { get; set; } = 6254;
		public int ReconnectDelaySeconds { get; set; } = 5;
		public int BufferSize { get; set; } = 4096;
		public string WebSocketPath { get; set; } = "/ws/";
	}
}