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
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();
                
                await webSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                isRunning = true;
                OnConnected?.Invoke();
                
                // Start receiving messages
                _ = ReceiveLoop();
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Connection failed: {e.Message}");
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            
            while (isRunning && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        OnDisconnected?.Invoke();
                        break;
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        OnMessage?.Invoke(message);
                    }
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        OnError?.Invoke($"Receive error: {e.Message}");
                    }
                    break;
                }
            }
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
