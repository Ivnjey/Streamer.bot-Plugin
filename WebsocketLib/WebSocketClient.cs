using Newtonsoft.Json;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;

namespace StreamerbotPlugin
{
    internal class WebSocketClient
    {
        private static WebSocketClient _instance;
        private static readonly object _lock = new object();

        private ClientWebSocket ws;
        private CancellationTokenSource cts;

        private static string _serverUri;
        private static bool _isConnected;

        public static event EventHandler WebSocketConnected;
        public static event EventHandler<WebSocketCloseStatus?> WebSocketDisconnected;
        public static event EventHandler<string> WebSocketOnMessageRecieved_actions;
        public static event EventHandler<string> WebSocketOnMessageRecieved_globals;

         public static WebSocketClient Instance
        {
            get
            {
                lock (_lock)
                {
                    return _instance ??= new WebSocketClient();
                }
            }
        }

        public static bool IsConnected
        {
            get { return _isConnected; }
        }

        private WebSocketClient()
        {
            if (PluginConfiguration.GetValue(PluginInstance.Main, "Configured") == "True")
            {
                string address = PluginConfiguration.GetValue(PluginInstance.Main, "Address");
                string endpoint = PluginConfiguration.GetValue(PluginInstance.Main, "Endpoint");
                if (int.TryParse(PluginConfiguration.GetValue(PluginInstance.Main, "Port"), out int port))
                {
                    UriBuilder uriBuilder = new UriBuilder("ws", address, port, endpoint);
                    ConnectAsync(uriBuilder.Uri.ToString());
                }
            }
        }

        public async Task ConnectAsync(string serverUri)
        {
            _serverUri = serverUri;

            if (ws != null && ws.State == WebSocketState.Open)
            {
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket already connected.");
                return;
            }

            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();

            try
            {
                await ws.ConnectAsync(new Uri(serverUri), cts.Token);
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket Connected");

                _isConnected = true;
                WebSocketConnected?.Invoke(this, EventArgs.Empty);

                _ = ReceiveMessagesAsync(); // Start listening for messages

                ConfirmConnection(@"
                {
                    ""request"": ""GetInfo"",
                    ""id"": ""MacroDeck Connecting""
                }");
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Error(PluginInstance.Main, $"WebSocket connection failed: {ex.Message}");
                _isConnected = false;
                RetryConnect(serverUri);
            }
        }
        public void ConfirmConnection(string message)
        {

            MacroDeckLogger.Info(PluginInstance.Main, $"WebSocket Send Message: {JsonConvert.SerializeObject(message)}");
            var buffer = Encoding.UTF8.GetBytes(message);
            ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
        }

        public void RetryConnect(string serverUri)
        {
            Task.Run(async () =>
            {
                while (!_isConnected)
                {
                    MacroDeckLogger.Info(PluginInstance.Main, "Retrying WebSocket connection...");
                    try
                    {
                        await ConnectAsync(serverUri);
                    }
                    catch (Exception ex)
                    {
                        MacroDeckLogger.Error(PluginInstance.Main, $"Retry failed: {ex.Message}");
                        await Task.Delay(5000);
                    }
                }
            });
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[8192];
            while (ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        MacroDeckLogger.Info(PluginInstance.Main, "WebSocket closed by server.");
                        WebSocketDisconnected?.Invoke(this, result.CloseStatus);
                        _isConnected = false;
                        break;
                    }
                    await Task.Delay(100);
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleReceivedMessages(message);
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(PluginInstance.Main, $"Error receiving WebSocket message: {ex.Message}");
                    _isConnected = false;
                }
            }
        }

        private void HandleReceivedMessages(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MacroDeckLogger.Info(PluginInstance.Main, $"WebSocket Message Received: {message}");

            if (message.Contains("\"source\":\"websocketServer\""))
            {
                SubscribeToCustom();
            }
            else if (message.Contains("\"actions\":[{\"id\""))
            {
                WebSocketOnMessageRecieved_actions?.Invoke(this, message);
            }
            else if (message.Contains("\"type\":\"Custom\""))
            {
                WebSocketOnMessageRecieved_globals?.Invoke(this, message);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                MacroDeckLogger.Error(PluginInstance.Main, "WebSocket is not connected.");
                return;
            }

            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
                MacroDeckLogger.Info(PluginInstance.Main, $"WebSocket Sent Message: {message}");
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Error(PluginInstance.Main, $"Failed to send message: {ex.Message}");
            }
        }

        private async void SubscribeToCustom()
        {
            string requestId = Guid.NewGuid().ToString();
            string jsonString = $@"
            {{
                ""request"": ""Subscribe"",
                ""id"": ""{requestId}"",
                ""events"": {{
                    ""General"": [""Custom""]
                }}
            }}";

            int retryCount = 5;
            while (retryCount > 0)
            {
                try
                {
                    await SendMessageAsync(jsonString);
                    break;
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(PluginInstance.Main, $"Failed to send subscription: {ex.Message}");
                    retryCount--;
                    if (retryCount > 0)
                    {
                        await Task.Delay(5000);
                    }
                }
            }
        }

        public async Task CloseAsync()
        {
            if (ws != null)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cts.Token);
                _isConnected = false;
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket Closed.");
            }
        }
    }
}
