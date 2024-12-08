using Newtonsoft.Json;
using SuchByte.MacroDeck.Logging;
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
        private const int MAX_RETRY_DELAY = 30000;
        private const int INITIAL_RETRY_DELAY = 1000;
        private int _currentRetryDelay;
        private static WebSocketClient _instance;
        Configuration config = Configuration.Instance;
        private ClientWebSocket ws;
        private CancellationTokenSource cts;
        private static bool _isConnected;
        public static bool IsConnected => _isConnected;
        public bool _isIntentionallyClosed = false;
        private static readonly object _lock = new object();

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
        public WebSocketClient()
        {
            MacroDeckLogger.Info(PluginInstance.Main, "Initializing WebSocket..");
            InitializeConnectionAsync();
        }
        public void InitializeConnectionAsync()
        {

            Task.Run(async () =>
            {
                await Instance.ConnectAsync();
            });
        }

        public async Task ConnectAsync(bool intentional = false)//сделать lock для работы в 1 экземпляре, может начаться хаос.
        {

            if (_isConnected.Equals(true))
            {
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket is already connected.");
                return;
            }
            else if (ws?.State == WebSocketState.Connecting)
            {
                MacroDeckLogger.Info(PluginInstance.Main, "WebSocket is already connecting.");
                return;
            }
            _isIntentionallyClosed = intentional;
            while (_isIntentionallyClosed.Equals(false))
            {
                _currentRetryDelay = INITIAL_RETRY_DELAY;

                for (; _isConnected.Equals(false) && _isIntentionallyClosed.Equals(false);)
                {
                    try
                    {
                        ws = new ClientWebSocket();
                        cts = new CancellationTokenSource();
                        await ws.ConnectAsync(config.uri, cts.Token);
                        _isConnected = true;
                    }
                    // catch (WebSocketException ex)
                    // {
                    //     MacroDeckLogger.Error(PluginInstance.Main, $"UnexpectedError: {ex.Message}");
                    // }
                    catch (Exception ex)
                    {
                        MacroDeckLogger.Warning(PluginInstance.Main, $"WebSocket connection failed: {ex.Message}");
                        MacroDeckLogger.Info(PluginInstance.Main, $"Reconnection trying in {_currentRetryDelay / 1000} seconds...");
                        await Task.Delay(_currentRetryDelay);
                        _currentRetryDelay = Math.Min(_currentRetryDelay * 2, MAX_RETRY_DELAY);
                        await CleanupWebSocketAsync();
                    }
                }
                if (_isIntentionallyClosed.Equals(true) || cts.Token.IsCancellationRequested)
                    return;
                try
                {
                    MacroDeckLogger.Info(PluginInstance.Main, "WebSocket Connected");
                    await Instance.ReceiveMessagesAsync();
                }
                catch (WebSocketException ex)
                {
                    MacroDeckLogger.Info(PluginInstance.Main, $"WebSocketException Error: {ex.Message}");
                    await CleanupWebSocketAsync();
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Trace(PluginInstance.Main, $"Exception Error: {ex.Message}");
                    await CleanupWebSocketAsync();
                }
            }
        }
        public async Task ReceiveMessagesAsync()
        {
            WebSocketConnected?.Invoke(this, EventArgs.Empty);
            var buffer = new byte[102400];
            var messageBuffer = new List<byte>();

            while (ws != null && ws.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new WebSocketException("WebSocket Server Closed Connection");
                    }
                    messageBuffer.AddRange(buffer.Take(result.Count));
                    if (result.EndOfMessage)
                    {
                        string message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        HandleReceivedMessages(message);
                        messageBuffer.Clear();
                    }
                }
                catch (WebSocketException ex)
                {
                    throw new WebSocketException($"Try to Recconect {ex.Message}");
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Trace(PluginInstance.Main, $"Error Receive Messages: {ex.Message}");
                }

            }
        }

        public async Task CleanupWebSocketAsync()
        {
            try
            {
                if (ws != null)
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cleanup", CancellationToken.None);
                    }
                    ws?.Dispose();
                }
                cts?.Cancel();
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Error(PluginInstance.Main, $"Cleanup failed: {ex.Message}");
            }
            finally
            {
                ws = null;
                cts = null;
                _isConnected = false;
                WebSocketDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }
        public async Task CloseAsync(bool intentional = false)
        {
            _isIntentionallyClosed = intentional;
            await CleanupWebSocketAsync();
        }
        public async Task SendMessageAsync(string message)
        {

            if (ws == null || ws.State != WebSocketState.Open)
            {
                MacroDeckLogger.Error(PluginInstance.Main, "Cannot send message: WebSocket not connected.");
                return;
            }
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);
                MacroDeckLogger.Info(PluginInstance.Main, $"Message sent: {message}");
            }
            catch (Exception ex)
            {
                MacroDeckLogger.Error(PluginInstance.Main, $"Failed to send message: {ex.Message}");
            }
        }

        public void HandleReceivedMessages(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (message.Contains("\"source\":\"websocketServer\""))
                SubscribeToCustom();
            else if (message.Contains("\"actions\":[{\"id\""))
                WebSocketOnMessageRecieved_actions?.Invoke(this, message);
            else if (message.Contains("\"type\":\"Custom\""))
                WebSocketOnMessageRecieved_globals?.Invoke(this, message);
            else
                MacroDeckLogger.Info(PluginInstance.Main, $"Message: {message}");
        }

        public async void SubscribeToCustom()
        {
            string requestId = Guid.NewGuid().ToString();
            string jsonString = $@"{{ ""request"": ""Subscribe"", ""id"": ""{requestId}"", ""events"": {{ ""General"": [""Custom""] }} }}";

            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    await SendMessageAsync(jsonString);
                    break;
                }
                catch (Exception ex)
                {
                    MacroDeckLogger.Error(PluginInstance.Main, $"Failed to subscribe: {ex.Message}");
                    await Task.Delay(5000);
                }
            }
        }
    }
}