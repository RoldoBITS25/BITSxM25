using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplayerGame
{
    /// <summary>
    /// WebSocket client for real-time communication
    /// Note: This is a simplified implementation. For production, consider using a library like WebSocketSharp
    /// </summary>
    public class WebSocketClient
    {
        public event Action<string> OnMessage;
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private string url;
        private bool isRunning = false;

        public WebSocketClient(string url)
        {
            this.url = url;
        }

        public async void Connect()
        {
            try
            {
                Debug.Log($"[WebSocketClient] ========== Connecting to WebSocket ==========");
                Debug.Log($"[WebSocketClient] URL: {url}");
                Debug.Log($"[WebSocketClient] Validating URL format...");
                
                // Validate URL format
                Uri uri;
                try
                {
                    uri = new Uri(url);
                    Debug.Log($"[WebSocketClient] ✓ URL is valid");
                    Debug.Log($"[WebSocketClient]   Scheme: {uri.Scheme}");
                    Debug.Log($"[WebSocketClient]   Host: {uri.Host}");
                    Debug.Log($"[WebSocketClient]   Port: {uri.Port}");
                    Debug.Log($"[WebSocketClient]   Path: {uri.AbsolutePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[WebSocketClient] ✗ Invalid URL format: {ex.Message}");
                    OnError?.Invoke($"Invalid WebSocket URL: {ex.Message}");
                    return;
                }
                
                Debug.Log($"[WebSocketClient] Creating WebSocket client...");
                webSocket = new ClientWebSocket();
                
                // Configure WebSocket options for better compatibility
                webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                Debug.Log($"[WebSocketClient] ✓ Keep-alive interval set to 20 seconds");
                
                cancellationTokenSource = new CancellationTokenSource();
                
                Debug.Log($"[WebSocketClient] Attempting connection...");
                Debug.Log($"[WebSocketClient] This may take a few seconds...");
                
                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);
                
                Debug.Log($"[WebSocketClient] ✓ Connection established!");
                Debug.Log($"[WebSocketClient] WebSocket State: {webSocket.State}");
                
                isRunning = true;
                OnConnected?.Invoke();
                
                Debug.Log($"[WebSocketClient] Starting receive loop...");
                // Start receiving messages
                _ = ReceiveLoop();
                
                Debug.Log($"[WebSocketClient] ========== WebSocket Connected Successfully ==========");
            }
            catch (WebSocketException wsEx)
            {
                string errorDetails = $"WebSocket connection failed: {wsEx.Message}";
                Debug.LogError($"[WebSocketClient] ✗ {errorDetails}");
                Debug.LogError($"[WebSocketClient] WebSocketErrorCode: {wsEx.WebSocketErrorCode}");
                Debug.LogError($"[WebSocketClient] NativeErrorCode: {wsEx.NativeErrorCode}");
                Debug.LogError($"[WebSocketClient] Stack Trace: {wsEx.StackTrace}");
                
                // Provide user-friendly error messages
                string userMessage = wsEx.WebSocketErrorCode switch
                {
                    WebSocketError.ConnectionClosedPrematurely => "Connection closed unexpectedly. The server may be offline or unreachable.",
                    WebSocketError.Faulted => "Connection faulted. Check if the server is running and accessible from this network.",
                    WebSocketError.HeaderError => "Invalid WebSocket headers. The server may not support WebSocket connections.",
                    WebSocketError.InvalidState => "Invalid connection state. Try restarting the application.",
                    _ => $"Connection failed: {wsEx.Message}. Ensure the server is running and accessible from this PC."
                };
                
                OnError?.Invoke(userMessage);
            }
            catch (TaskCanceledException)
            {
                string errorMsg = "Connection timeout. The server may be unreachable from this network.";
                Debug.LogError($"[WebSocketClient] ✗ {errorMsg}");
                Debug.LogError($"[WebSocketClient] Possible causes:");
                Debug.LogError($"[WebSocketClient]   - Server is not running");
                Debug.LogError($"[WebSocketClient]   - Firewall blocking connection");
                Debug.LogError($"[WebSocketClient]   - Server not bound to 0.0.0.0 (only listening on localhost)");
                Debug.LogError($"[WebSocketClient]   - Network connectivity issues");
                OnError?.Invoke(errorMsg);
            }
            catch (Exception e)
            {
                string errorMsg = $"Connection failed: {e.Message}";
                Debug.LogError($"[WebSocketClient] ✗ {errorMsg}");
                Debug.LogError($"[WebSocketClient] Exception Type: {e.GetType().Name}");
                Debug.LogError($"[WebSocketClient] Stack Trace: {e.StackTrace}");
                OnError?.Invoke(errorMsg);
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            Debug.Log($"[WebSocketClient] Receive loop started");
            
            while (isRunning && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.LogWarning($"[WebSocketClient] Server initiated close. Status: {result.CloseStatus}, Reason: {result.CloseStatusDescription}");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        OnDisconnected?.Invoke();
                        break;
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log($"[WebSocketClient] ← Received {result.Count} bytes");
                        OnMessage?.Invoke(message);
                    }
                }
                catch (WebSocketException wsEx)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"[WebSocketClient] WebSocket receive error: {wsEx.Message}");
                        Debug.LogError($"[WebSocketClient] WebSocketErrorCode: {wsEx.WebSocketErrorCode}");
                        OnError?.Invoke($"Connection lost: {wsEx.Message}");
                    }
                    break;
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        Debug.LogError($"[WebSocketClient] Receive error: {e.Message}");
                        OnError?.Invoke($"Receive error: {e.Message}");
                    }
                    break;
                }
            }
            
            Debug.Log($"[WebSocketClient] Receive loop ended. State: {webSocket?.State}, isRunning: {isRunning}");
        }

        public async void Send(string message)
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    OnError?.Invoke($"Send error: {e.Message}");
                }
            }
        }

        public async void Disconnect()
        {
            isRunning = false;
            
            if (webSocket != null)
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                    }
                    webSocket.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Disconnect error: {e.Message}");
                }
            }
            
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}
