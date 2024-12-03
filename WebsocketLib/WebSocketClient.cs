using Newtonsoft.Json;
using SuchByte.MacroDeck.Logging;
using SuchByte.MacroDeck.Plugins;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Linq;

namespace StreamerbotPlugin
{
    internal class WebSocketClient
    {
        private static WebSocketClient _instance;
        private static readonly object _lock = new object();

        private ClientWebSocket ws;
        private CancellationTokenSource cts;

        // Old: private static string _serverUri;
        // NEW: Track server configuration separately
        private string _serverAddress;
        private int _serverPort;
        private string _serverEndpoint;

        private static bool _isConnected;
        private const int MAX_RETRY_DELAY = 30000; // 30 seconds max delay
        private int _currentRetryDelay = 5000; // Start with 5 seconds

        public static event EventHandler WebSocketConnected;
        public static event EventHandler WebSocketDisconnected;
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
            // Improved configuration loading
            LoadConfiguration();

            // Only attempt connection if fully configured
            if (IsValidConfiguration())
            {
                // Use a separate method for initial connection
                InitializeConnectionAsync();
            }
        }

        private void LoadConfiguration()
        {
            // Old configuration retrieval
            /*
            string address = PluginConfiguration.GetValue(PluginInstance.Main, "Address");
            string endpoint = PluginConfiguration.GetValue(PluginInstance.Main, "Endpoint");
            if (int.TryParse(PluginConfiguration.GetValue(PluginInstance.Main, "Port"), out int port))
            {
                UriBuilder uriBuilder = new UriBuilder("ws", address, port, endpoint);
                _ = ConnectAsync(uriBuilder.Uri.ToString());
            }
            */

            // New: Separate configuration loading
            _serverAddress = PluginConfiguration.GetValue(PluginInstance.Main, "Address");
            _serverEndpoint = PluginConfiguration.GetValue(PluginInstance.Main, "Endpoint");
            _serverPort = int.TryParse(
                PluginConfiguration.GetValue(PluginInstance.Main, "Port"),
                out int port) ? port : 0;
        }

        private bool IsValidConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_serverAddress) &&
                   !string.IsNullOrWhiteSpace(_serverEndpoint) &&
                   _serverPort > 0;
        }

        private void InitializeConnectionAsync()
        {
            // Start connection in background to avoid blocking constructor
            Task.Run(async () =>
            {
                try
                {
                    UriBuilder uriBuilder = new UriBuilder("ws", _serverAddress, _serverPort, _serverEndpoint);
                    await ConnectAsync(uriBuilder.Uri.ToString());
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(PluginInstance.Main, $"Initial connection failed: {ex.Message}");
                }
            });
        }

        public async Task ConnectAsync(string serverUri)
        {
            // Prevent multiple simultaneous connection attempts
            if (_isConnected || ws?.State == WebSocketState.Connecting)
            {
                MacroDeckLogger.Info(PluginInstance.Main, "Connection in progress or already connected.");
                return;
            }

            // Reset retry delay on new connection attempt
            _currentRetryDelay = 5000;

            ws = new ClientWebSocket();
            cts = new CancellationTokenSource();

            try
            {
                await ws.ConnectAsync(new Uri(serverUri), cts.Token);
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket Connected");

                _isConnected = true;
                WebSocketConnected?.Invoke(this, EventArgs.Empty);

                // Start listening for messages
                _ = ReceiveMessagesAsync();

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
                WebSocketDisconnected?.Invoke(this, EventArgs.Empty);

                // Trigger reconnection with exponential backoff
                await HandleReconnectionAsync(serverUri);
            }
        }

        private async Task HandleReconnectionAsync(string serverUri)
        {
            while (!_isConnected)
            {
                try
                {
                    // Wait before retrying
                    await Task.Delay(_currentRetryDelay);

                    // Exponential backoff with max limit
                    _currentRetryDelay = Math.Min(
                        _currentRetryDelay * 2,  // Double delay
                        MAX_RETRY_DELAY         // But not more than max
                    );

                    MacroDeckLogger.Info(PluginInstance.Main, $"Attempting reconnection. Next attempt in {_currentRetryDelay / 1000} seconds.");

                    await ConnectAsync(serverUri);
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(PluginInstance.Main, $"Reconnection attempt failed: {ex.Message}");
                }
            }
        }
        public void ConfirmConnection(string message)
        {

            MacroDeckLogger.Info(PluginInstance.Main, $"WebSocket Send Message: {JsonConvert.SerializeObject(message)}");
            var buffer = Encoding.UTF8.GetBytes(message);
            ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[102400];
            var messageBuffer = new List<byte>();

            while (ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    messageBuffer.AddRange(buffer.Take(result.Count));
                    if (result.EndOfMessage)
                    {
                        string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        HandleReceivedMessages(message);
                        messageBuffer.Clear();
                    }
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
            // if (message.Contains("\"request\":\"Hello\""))
            // {
            //     return;
            // }
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
                WebSocketDisconnected?.Invoke(this, EventArgs.Empty);
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket Closed.");
            }
        }
    }
}
