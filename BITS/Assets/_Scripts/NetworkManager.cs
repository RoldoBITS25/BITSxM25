using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MultiplayerGame
{
    /// <summary>
    /// Manages network communication with the FastAPI backend
    /// Handles HTTP REST API calls and WebSocket connections
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Server Configuration")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:8000";
        [SerializeField] private string wsUrl = "ws://127.0.0.1:8000";

        [Header("Player Info")]
        public string PlayerId { get; private set; }
        public string CurrentRoomId { get; private set; }
        public bool IsPlayer { get; private set; } // true = player, false = spectator
        public int PlayerNumber { get; private set; } // 1 or 2 for players, 0 for spectators
        public bool IsReadyForRoomOperations => isPlayerRegistered; // Check if connected to backend

        // Events
        public event Action<Room> OnRoomCreated;
        public event Action<Room> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<List<Room>> OnRoomListUpdated;
        public event Action<PlayerAction> OnPlayerActionReceived;
        public event Action<GameState> OnGameStateUpdated;
        public event Action<string> OnError;

        private WebSocketClient webSocket;
        private bool isConnected = false;
        private bool isPlayerRegistered = false;

        private void Awake()
        {
            Debug.Log("[NetworkManager] ========== NetworkManager Initializing ==========");
            
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                PlayerId = GeneratePlayerId();
                Debug.Log($"[NetworkManager] ✓ Initialized as singleton");
                Debug.Log($"[NetworkManager] Player ID: {PlayerId}");
                Debug.Log($"[NetworkManager] Server URL: {serverUrl}");
                Debug.Log($"[NetworkManager] WebSocket URL: {wsUrl}");
                
                // Connect to WebSocket immediately to register player with backend
                Debug.Log($"[NetworkManager] Connecting to WebSocket to register player...");
                ConnectWebSocketForRegistration();
            }
            else
            {
                Debug.LogWarning("[NetworkManager] Duplicate instance detected, destroying");
                Destroy(gameObject);
            }
        }

        private string GeneratePlayerId()
        {
            return $"player_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        #region Room Management

        /// <summary>
        /// Create a new game room
        /// </summary>
        public void CreateRoom(string roomName, int maxPlayers = 4, bool isPrivate = false, string password = null)
        {
            if (!isPlayerRegistered)
            {
                Debug.LogError("[NetworkManager] Cannot create room: Player not registered with backend yet. Please wait for WebSocket connection.");
                OnError?.Invoke("Not connected to server. Please wait...");
                return;
            }
            
            StartCoroutine(CreateRoomCoroutine(roomName, maxPlayers, isPrivate, password));
        }

        private IEnumerator CreateRoomCoroutine(string roomName, int maxPlayers, bool isPrivate, string password)
        {
            Debug.Log($"[NetworkManager] Creating room: {roomName}, MaxPlayers: {maxPlayers}, Private: {isPrivate}");
            
            var roomData = new RoomCreate
            {
                name = roomName,
                max_players = maxPlayers,
                is_private = isPrivate,
                password = password
            };

            string json = JsonUtility.ToJson(roomData);
            string url = $"{serverUrl}/api/rooms/?host_player_id={PlayerId}";
            
            Debug.Log($"[NetworkManager] POST {url}");
            Debug.Log($"[NetworkManager] Request body: {json}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                Debug.Log($"[NetworkManager] Response: {request.downloadHandler.text}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[NetworkManager] ✓ Room created successfully!");
                    Room room = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                    CurrentRoomId = room.room_id;
                    IsPlayer = true;
                    PlayerNumber = 1; // Host is always player 1
                    Debug.Log($"[NetworkManager] Room ID: {room.room_id}, Player Number: {PlayerNumber}");
                    OnRoomCreated?.Invoke(room);
                    
                    // Connect to WebSocket for real-time updates
                    ConnectWebSocket();
                }
                else
                {
                    string errorMsg = $"Failed to create room: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response body: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Get list of available rooms
        /// </summary>
        public void GetRoomList(bool includePrivate = false)
        {
            StartCoroutine(GetRoomListCoroutine(includePrivate));
        }

        private IEnumerator GetRoomListCoroutine(bool includePrivate)
        {
            string url = $"{serverUrl}/api/rooms/?include_private={includePrivate}";
            Debug.Log($"[NetworkManager] GET {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    Debug.Log($"[NetworkManager] ✓ Room list received: {json}");
                    RoomList roomList = JsonUtility.FromJson<RoomList>("{\"rooms\":" + json + "}");
                    Debug.Log($"[NetworkManager] Found {roomList.rooms.Count} rooms");
                    OnRoomListUpdated?.Invoke(roomList.rooms);
                }
                else
                {
                    string errorMsg = $"Failed to get room list: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Join an existing room
        /// </summary>
        public void JoinRoom(string roomId, string password = null)
        {
            if (!isPlayerRegistered)
            {
                Debug.LogError("[NetworkManager] Cannot join room: Player not registered with backend yet. Please wait for WebSocket connection.");
                OnError?.Invoke("Not connected to server. Please wait...");
                return;
            }
            
            StartCoroutine(JoinRoomCoroutine(roomId, password));
        }

        private IEnumerator JoinRoomCoroutine(string roomId, string password)
        {
            string url = $"{serverUrl}/api/rooms/{roomId}/join?player_id={PlayerId}";
            if (!string.IsNullOrEmpty(password))
            {
                url += $"&password={password}";
            }
            
            Debug.Log($"[NetworkManager] Joining room: {roomId}");
            Debug.Log($"[NetworkManager] POST {url}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                Debug.Log($"[NetworkManager] Response: {request.downloadHandler.text}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[NetworkManager] ✓ Successfully joined room {roomId}");
                    // Get room details to determine role
                    yield return GetRoomDetails(roomId);
                }
                else
                {
                    string errorMsg = $"Failed to join room: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        private IEnumerator GetRoomDetails(string roomId)
        {
            string url = $"{serverUrl}/api/rooms/{roomId}";
            Debug.Log($"[NetworkManager] GET {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                Debug.Log($"[NetworkManager] Response: {request.downloadHandler.text}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Room room = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                    CurrentRoomId = room.room_id;
                    
                    // Determine player role (first 2 are players, rest are spectators)
                    int playerIndex = room.current_players.IndexOf(PlayerId);
                    Debug.Log($"[NetworkManager] Player index in room: {playerIndex} / {room.current_players.Count}");
                    
                    if (playerIndex < 2)
                    {
                        IsPlayer = true;
                        PlayerNumber = playerIndex + 1;
                        Debug.Log($"[NetworkManager] ✓ Assigned as Player {PlayerNumber}");
                    }
                    else
                    {
                        IsPlayer = false;
                        PlayerNumber = 0;
                        Debug.Log($"[NetworkManager] ✓ Assigned as Spectator");
                    }

                    OnRoomJoined?.Invoke(room);
                    
                    // Connect to WebSocket for real-time updates
                    ConnectWebSocket();
                }
                else
                {
                    string errorMsg = $"Failed to get room details: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        /// <summary>
        /// Leave the current room
        /// </summary>
        public void LeaveRoom()
        {
            if (string.IsNullOrEmpty(CurrentRoomId))
                return;

            StartCoroutine(LeaveRoomCoroutine());
        }

        private IEnumerator LeaveRoomCoroutine()
        {
            string url = $"{serverUrl}/api/rooms/{CurrentRoomId}/leave?player_id={PlayerId}";
            Debug.Log($"[NetworkManager] Leaving room: {CurrentRoomId}");
            Debug.Log($"[NetworkManager] POST {url}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[NetworkManager] ✓ Left room successfully");
                }
                else
                {
                    Debug.LogWarning($"[NetworkManager] Leave room failed: {request.error} (Code: {request.responseCode})");
                }

                DisconnectWebSocket();
                CurrentRoomId = null;
                IsPlayer = false;
                PlayerNumber = 0;
                OnRoomLeft?.Invoke();
            }
        }

        #endregion

        #region WebSocket Communication

        /// <summary>
        /// Connect to WebSocket on startup to register player with backend
        /// This must happen before any room operations
        /// </summary>
        private void ConnectWebSocketForRegistration()
        {
            // Backend expects: ws://host:port/ws/{player_id}
            string wsEndpoint = $"{wsUrl}/ws/{PlayerId}";
            Debug.Log($"[NetworkManager] Connecting to WebSocket for registration: {wsEndpoint}");
            
            webSocket = new WebSocketClient(wsEndpoint);
            
            webSocket.OnConnected += () => {
                Debug.Log($"[NetworkManager] ✓ WebSocket connected - Player registered with backend");
                isConnected = true;
                isPlayerRegistered = true;
            };
            
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += (error) => {
                Debug.LogError($"[NetworkManager] WebSocket registration error: {error}");
                Debug.LogError($"[NetworkManager] Cannot create/join rooms without backend connection");
                OnError?.Invoke($"Failed to connect to server: {error}");
                isPlayerRegistered = false;
            };
            
            webSocket.OnDisconnected += () => {
                Debug.LogWarning($"[NetworkManager] WebSocket disconnected!");
                isConnected = false;
                // Don't set isPlayerRegistered to false here - we can reconnect
            };
            
            webSocket.Connect();
        }

        private void ConnectWebSocket()
        {
            // If already connected from registration, just send JOIN_ROOM
            if (webSocket != null && isConnected)
            {
                Debug.Log($"[NetworkManager] WebSocket already connected, sending JOIN_ROOM message");
                SendJoinRoomMessage();
                return;
            }

            // Otherwise establish new connection (shouldn't normally happen)
            Debug.LogWarning($"[NetworkManager] WebSocket not connected, establishing new connection");
            
            // Backend expects: ws://host:port/ws/{player_id}
            string wsEndpoint = $"{wsUrl}/ws/{PlayerId}";
            Debug.Log($"[NetworkManager] Connecting to WebSocket: {wsEndpoint}");
            
            webSocket = new WebSocketClient(wsEndpoint);
            
            webSocket.OnConnected += () => {
                Debug.Log($"[NetworkManager] ✓ WebSocket connected, sending JOIN_ROOM message");
                isConnected = true;
                // Send JOIN_ROOM message after connection
                SendJoinRoomMessage();
            };
            
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += (error) => {
                Debug.LogError($"[NetworkManager] WebSocket error: {error}");
                OnError?.Invoke($"WebSocket error: {error}");
            };
            
            webSocket.OnDisconnected += () => {
                Debug.LogWarning($"[NetworkManager] WebSocket disconnected during game!");
                isConnected = false;
            };
            
            webSocket.Connect();
        }

        private void SendJoinRoomMessage()
        {
            if (string.IsNullOrEmpty(CurrentRoomId))
            {
                Debug.LogWarning("[NetworkManager] Cannot send JOIN_ROOM: CurrentRoomId is null");
                return;
            }

            var joinMessage = new
            {
                type = "JOIN_ROOM",
                data = new
                {
                    room_id = CurrentRoomId,
                    username = PlayerId
                }
            };

            string json = JsonUtility.ToJson(joinMessage);
            Debug.Log($"[NetworkManager] Sending JOIN_ROOM: {json}");
            webSocket?.Send(json);
        }

        private void DisconnectWebSocket()
        {
            if (webSocket != null)
            {
                Debug.Log($"[NetworkManager] Disconnecting WebSocket");
                webSocket.Disconnect();
                webSocket = null;
            }
            isConnected = false;
            Debug.Log($"[NetworkManager] ✓ WebSocket disconnected");
        }

        private void HandleWebSocketMessage(string message)
        {
            Debug.Log($"[NetworkManager] WebSocket message received: {message}");
            
            try
            {
                // Parse message - backend sends: { "type": "MESSAGE_TYPE", "data": {...} }
                var msgWrapper = JsonUtility.FromJson<WebSocketMessageWrapper>(message);
                Debug.Log($"[NetworkManager] Message type: {msgWrapper.type}");
                
                switch (msgWrapper.type)
                {
                    case "CONNECT":
                        Debug.Log($"[NetworkManager] ✓ Connection confirmed by server");
                        break;
                    
                    case "ROOM_UPDATE":
                        Debug.Log($"[NetworkManager] Room update received");
                        // Parse the room update data
                        var roomUpdate = JsonUtility.FromJson<RoomUpdateData>(msgWrapper.data);
                        Debug.Log($"[NetworkManager] Room update action: {roomUpdate.action}, player: {roomUpdate.player_id}");
                        
                        if (roomUpdate.action == "player_joined")
                        {
                            Debug.Log($"[NetworkManager] Player {roomUpdate.username} joined the room");
                            // Refresh room details to update player list
                            StartCoroutine(GetRoomDetails(CurrentRoomId));
                        }
                        else if (roomUpdate.action == "player_left")
                        {
                            Debug.Log($"[NetworkManager] Player {roomUpdate.username} left the room");
                            StartCoroutine(GetRoomDetails(CurrentRoomId));
                        }
                        break;
                    
                    case "PLAYER_ACTION":
                        Debug.Log($"[NetworkManager] Player action received");
                        var actionData = JsonUtility.FromJson<PlayerActionData>(msgWrapper.data);
                        Debug.Log($"[NetworkManager] Action type: {actionData.action_type}");
                        
                        // Convert to PlayerAction format
                        var action = new PlayerAction
                        {
                            player_id = msgWrapper.sender_id,
                            action_type = actionData.action_type,
                            // Parse action_data based on type
                        };
                        
                        OnPlayerActionReceived?.Invoke(action);
                        break;
                    
                    case "STATE_UPDATE":
                        Debug.Log($"[NetworkManager] State update received");
                        var state = JsonUtility.FromJson<GameState>(msgWrapper.data);
                        OnGameStateUpdated?.Invoke(state);
                        break;
                    
                    case "ERROR":
                        var errorData = JsonUtility.FromJson<ErrorData>(msgWrapper.data);
                        Debug.LogError($"[NetworkManager] Server error: {errorData.error}");
                        OnError?.Invoke(errorData.error);
                        break;
                    
                    case "HEARTBEAT":
                        // Respond to heartbeat
                        SendHeartbeat();
                        break;
                    
                    default:
                        Debug.LogWarning($"[NetworkManager] Unknown message type: {msgWrapper.type}");
                        break;
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"Failed to parse WebSocket message: {e.Message}";
                Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                Debug.LogError($"[NetworkManager] Message was: {message}");
                Debug.LogError($"[NetworkManager] Stack trace: {e.StackTrace}");
                OnError?.Invoke(errorMsg);
            }
        }

        private void SendHeartbeat()
        {
            var heartbeat = new
            {
                type = "HEARTBEAT",
                data = new { }
            };
            string json = JsonUtility.ToJson(heartbeat);
            webSocket?.Send(json);
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Send a player action to the server
        /// </summary>
        public void SendPlayerAction(string actionType, string targetObjectId = null, Vector3? position = null, Dictionary<string, object> extraData = null)
        {
            if (!IsPlayer || !isConnected)
            {
                Debug.LogWarning($"[NetworkManager] Cannot send action: IsPlayer={IsPlayer}, isConnected={isConnected}");
                return;
            }

            // Build action_data based on action type
            var actionData = new Dictionary<string, object>();
            
            if (position.HasValue)
            {
                actionData["position"] = new { x = position.Value.x, y = position.Value.y, z = position.Value.z };
            }
            
            if (!string.IsNullOrEmpty(targetObjectId))
            {
                actionData["target_object_id"] = targetObjectId;
            }
            
            // Add extra data if provided
            if (extraData != null)
            {
                foreach (var kvp in extraData)
                {
                    actionData[kvp.Key] = kvp.Value;
                }
            }

            var message = new
            {
                type = "PLAYER_ACTION",
                data = new
                {
                    action_type = actionType,
                    action_data = actionData
                }
            };

            string json = JsonUtility.ToJson(message);
            Debug.Log($"[NetworkManager] Sending {actionType} action");
            Debug.Log($"[NetworkManager] Action JSON: {json}");
            webSocket?.Send(json);
        }

        /// <summary>
        /// Send move action
        /// </summary>
        public void SendMoveAction(Vector3 position)
        {
            SendPlayerAction("move", null, position);
        }

        /// <summary>
        /// Send grab action
        /// </summary>
        public void SendGrabAction(string objectId)
        {
            SendPlayerAction("grab", objectId);
        }

        /// <summary>
        /// Send cut action
        /// </summary>
        public void SendCutAction(string objectId, Vector3 cutPosition)
        {
            SendPlayerAction("cut", objectId, cutPosition);
        }

        /// <summary>
        /// Send break action
        /// </summary>
        public void SendBreakAction(string objectId)
        {
            SendPlayerAction("break", objectId);
        }

        #endregion

        private void OnDestroy()
        {
            // Only disconnect if this is the actual singleton instance
            // Duplicates should not disconnect the WebSocket
            if (Instance == this)
            {
                Debug.Log("[NetworkManager] Singleton instance being destroyed, disconnecting WebSocket");
                DisconnectWebSocket();
            }
            else
            {
                Debug.Log("[NetworkManager] Duplicate instance being destroyed, keeping WebSocket connected");
            }
        }

        private void OnApplicationQuit()
        {
            LeaveRoom();
        }
    }

    #region Data Models

    [Serializable]
    public class RoomCreate
    {
        public string name;
        public int max_players = 4;
        public bool is_private = false;
        public string password;
    }

    [Serializable]
    public class Room
    {
        public string room_id;
        public string name;
        public string host_player_id;
        public int max_players;
        public List<string> current_players = new List<string>();
        public bool is_private;
        public bool is_game_started;
    }

    [Serializable]
    public class RoomList
    {
        public List<Room> rooms;
    }

    [Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string data;
    }

    [Serializable]
    public class WebSocketMessageWrapper
    {
        public string type;
        public string data;
        public string sender_id;
    }

    [Serializable]
    public class RoomUpdateData
    {
        public string action; // "player_joined" or "player_left"
        public string player_id;
        public string username;
        public Room room;
    }

    [Serializable]
    public class PlayerActionData
    {
        public string action_type;
        public string action_data; // JSON string containing action-specific data
    }

    [Serializable]
    public class ErrorData
    {
        public string error;
    }

    [Serializable]
    public class PlayerAction
    {
        public string player_id;
        public string action_type; // "move", "grab", "cut", "break"
        public string target_object_id;
        public Vector3 position;
        public string timestamp;
        public Dictionary<string, object> data;
    }

    [Serializable]
    public class GameState
    {
        public string room_id;
        public List<PlayerState> players;
        public List<ObjectState> objects;
    }

    [Serializable]
    public class PlayerState
    {
        public string player_id;
        public Vector3 position;
        public string held_object_id;
    }

    [Serializable]
    public class ObjectState
    {
        public string object_id;
        public string object_type;
        public Vector3 position;
        public Quaternion rotation;
        public bool is_grabbed;
        public string grabbed_by_player_id;
    }

    #endregion
}
