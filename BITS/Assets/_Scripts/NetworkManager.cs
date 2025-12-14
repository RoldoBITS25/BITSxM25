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
        [SerializeField] private string wsUrl = "ws://127.0.0.1:8000/ws";
        // [SerializeField] private string serverUrl = "https://rankings-adapted-modeling-rich.trycloudflare.com";
        // [SerializeField] private string wsUrl = "wss://rankings-adapted-modeling-rich.trycloudflare.com/ws";

        [Header("Player Info")]
        public string PlayerId { get; private set; }
        public string CurrentRoomId { get; private set; }
        public Room CurrentRoom { get; private set; } // Cache the full room object
        public bool IsPlayer { get; private set; } // true = player, false = spectator
        public int PlayerNumber { get; private set; } // 1 or 2 for players, 0 for spectators

        public bool IsReadyForRoomOperations => isPlayerRegistered; // Check if connected to backend

        // Username Handling
        public string Username { get; private set; }
        public Dictionary<string, string> PlayerNames { get; private set; } = new Dictionary<string, string>();

        // Events
        public event Action<Room> OnRoomCreated;
        public event Action<Room> OnRoomJoined;
        public event Action OnRoomLeft;
        public event Action<List<Room>> OnRoomListUpdated;
        public event Action<PlayerAction> OnPlayerActionReceived;
        public event Action<GameState> OnGameStateUpdated;
        public event Action OnGameStarted;

        public event Action<string> OnError;
        public event Action OnPlayerNamesUpdated;

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
            return $"player_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public string GetPlayerName(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "Unknown";
            
            // Prefer dictionary lookup for authoritative name
            if (PlayerNames.TryGetValue(playerId, out string name))
            {
                return name;
            }
            
            // Fallback to local Username for our own player if not yet synced/cached
            if (playerId == PlayerId && !string.IsNullOrEmpty(Username))
            {
                return Username;
            }

            return playerId; // Fallback to ID
        }

        #region Room Management

        /// <summary>
        /// Create a new game room
        /// </summary>
        public void CreateRoom(string roomName, int maxPlayers = 4, bool isPrivate = false, string password = null, string username = null)
        {
            this.Username = !string.IsNullOrEmpty(username) ? username : PlayerId;
            Debug.Log($"[NetworkManager] Set local username (CreateRoom) to: {this.Username}");

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
                    CurrentRoom = room; // Cache room
                    IsPlayer = true;
                    PlayerNumber = 1; // Host is always player 1
                    Debug.Log($"[NetworkManager] Room ID: {room.room_id}, Player Number: {PlayerNumber}");
                    OnRoomCreated?.Invoke(room);
                    
                    // Connect to WebSocket for real-time updates
                    ConnectWebSocket();
                    
                    // Start polling for lobby updates (failsafe for WebSocket issues)
                    StartLobbyPolling();
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

        // ... GetRoomList (omitted for brevity, assume unchanged or use view_file to integrity check if needed) ...
        // Note: replace_file_content expects a CONTIGUOUS block. I must include GetRoomList if I span across it? 
        // Actually I'll just skip GetRoomList in this replacement chunk if I can.
        // But GetRoomDetails is further down.
        // Let's just do CurrentRoomId property and CreateRoom first. 
        // Wait, I can't skip deeply. I'll split this or just assume I need to handle GetRoomDetails in a separate chunk using multi_replace?
        // Ah, current tool is replace_file_content (single block).
        // I should probably use multi_replace for NetworkManager to touch CreateRoom, properties, GetRoomDetails, LeaveRoom.


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
            Debug.Log($"[NetworkManager] ========== Getting Room List ==========");
            Debug.Log($"[NetworkManager] GET {url}");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                Debug.Log($"[NetworkManager] Response Body: {request.downloadHandler.text}");

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    Debug.Log($"[NetworkManager] ✓ Room list received successfully");
                    Debug.Log($"[NetworkManager] Raw JSON: {json}");
                    
                    // Wrap the array in an object for Unity's JsonUtility
                    string wrappedJson = "{\"rooms\":" + json + "}";
                    Debug.Log($"[NetworkManager] Wrapped JSON: {wrappedJson}");
                    
                    RoomList roomList = JsonUtility.FromJson<RoomList>(wrappedJson);
                    
                    if (roomList == null)
                    {
                        Debug.LogError("[NetworkManager] ✗ Failed to parse room list - roomList is null!");
                        OnRoomListUpdated?.Invoke(new List<Room>());
                    }
                    else if (roomList.rooms == null)
                    {
                        Debug.LogError("[NetworkManager] ✗ Failed to parse room list - rooms array is null!");
                        OnRoomListUpdated?.Invoke(new List<Room>());
                    }
                    else
                    {
                        Debug.Log($"[NetworkManager] ✓ Parsed {roomList.rooms.Count} rooms");
                        for (int i = 0; i < roomList.rooms.Count; i++)
                        {
                            var room = roomList.rooms[i];
                            Debug.Log($"[NetworkManager]   Room {i + 1}: '{room.name}' (ID: {room.room_id}, Players: {room.current_players?.Count ?? 0}/{room.max_players})");
                        }
                        OnRoomListUpdated?.Invoke(roomList.rooms);
                    }
                }
                else
                {
                    string errorMsg = $"Failed to get room list: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                    // Invoke with empty list so UI knows request completed
                    OnRoomListUpdated?.Invoke(new List<Room>());
                }
                
                Debug.Log($"[NetworkManager] ========== Room List Request Complete ==========");
            }
        }

        /// <summary>
        /// Join an existing room
        /// </summary>
        /// <summary>
        /// Join an existing room
        /// </summary>
        public void JoinRoom(string roomId, string password = null, string username = null)
        {
            this.Username = !string.IsNullOrEmpty(username) ? username : PlayerId;
            Debug.Log($"[NetworkManager] Set local username to: {this.Username}");

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
            string url = $"{serverUrl}/api/rooms/join";
            
            Debug.Log($"[NetworkManager] Joining room: {roomId}");
            Debug.Log($"[NetworkManager] POST {url}");

            var joinRequest = new RoomJoinRequest
            {
                join_code = roomId,
                player_id = PlayerId,
                password = password
            };

            string json = JsonUtility.ToJson(joinRequest);
            Debug.Log($"[NetworkManager] Request Body: {json}");

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
                    Debug.Log($"[NetworkManager] ✓ Successfully joined room {roomId}");
                    // Get room details to determine role (connect WS explicitly)
                    yield return GetRoomDetails(roomId, true);
                    
                    // Start polling for lobby updates
                    StartLobbyPolling();
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

        private IEnumerator GetRoomDetails(string roomId, bool connectWebSocket = true)
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
                    CurrentRoomId = room.join_code;
                    CurrentRoom = room; // Cache room
                    
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
                    
                    // Connect to WebSocket for real-time updates ONLY if requested and not connected
                    if (connectWebSocket && !isConnected)
                    {
                        ConnectWebSocket();
                    }
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
                CurrentRoom = null; // Clear cached room
                IsPlayer = false;
                PlayerNumber = 0;
                OnRoomLeft?.Invoke();
                StopLobbyPolling();
            }
        }

        /// <summary>
        /// Start the game for the current room
        /// </summary>
        public void StartGame()
        {
            if (string.IsNullOrEmpty(CurrentRoomId))
                return;

            StartCoroutine(StartGameCoroutine());
        }

        private IEnumerator StartGameCoroutine()
        {
            string url = $"{serverUrl}/api/rooms/{CurrentRoomId}/start?player_id={PlayerId}";
            Debug.Log($"[NetworkManager] Starting game: {CurrentRoomId}");
            Debug.Log($"[NetworkManager] POST {url}");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                Debug.Log($"[NetworkManager] Response Code: {request.responseCode}");
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[NetworkManager] ✓ Game started successfully, waiting for WebSocket notification...");
                    StopLobbyPolling();
                }
                else
                {
                    string errorMsg = $"Failed to start game: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[NetworkManager] ✗ {errorMsg}");
                    Debug.LogError($"[NetworkManager] Response: {request.downloadHandler.text}");
                    OnError?.Invoke(errorMsg);
                }
            }
        }

        private Coroutine lobbyPollingCoroutine;

        private void StartLobbyPolling()
        {
            if (lobbyPollingCoroutine != null) StopCoroutine(lobbyPollingCoroutine);
            lobbyPollingCoroutine = StartCoroutine(PollLobbyDetails());
        }

        private void StopLobbyPolling()
        {
            if (lobbyPollingCoroutine != null)
            {
                StopCoroutine(lobbyPollingCoroutine);
                lobbyPollingCoroutine = null;
            }
        }

        private IEnumerator PollLobbyDetails()
        {
            Debug.Log("[NetworkManager] Starting lobby polling (failsafe)");
            while (!string.IsNullOrEmpty(CurrentRoomId) && CurrentRoom != null && !CurrentRoom.is_game_started)
            {
                yield return new WaitForSeconds(3.0f);
                if (!string.IsNullOrEmpty(CurrentRoomId))
                {
                    // Refresh details without reconnecting WS
                    yield return GetRoomDetails(CurrentRoomId, false);
                }
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

            var joinData = new WSJoinData
            {
                room_id = CurrentRoomId,
                username = !string.IsNullOrEmpty(this.Username) ? this.Username : PlayerId
            };

            var joinMessage = new WSJoinMessage
            {
                data = joinData
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
                    
                    case "room_update":
                    case "ROOM_UPDATE":
                        Debug.Log($"[NetworkManager] Room update received");
                        // Parse the room update data
                        var roomUpdate = JsonUtility.FromJson<RoomUpdateData>(msgWrapper.data);
                        Debug.Log($"[NetworkManager] Room update action: {roomUpdate.action}, player: {roomUpdate.player_id}");
                        
                        // Update name cache if provided
                        if (!string.IsNullOrEmpty(roomUpdate.player_id) && !string.IsNullOrEmpty(roomUpdate.username))
                        {
                            PlayerNames[roomUpdate.player_id] = roomUpdate.username;
                            OnPlayerNamesUpdated?.Invoke();
                            Debug.Log($"[NetworkManager] Updated name for {roomUpdate.player_id}: {roomUpdate.username}");
                        }
                        
                        if (roomUpdate.action == "player_joined")
                        {
                            Debug.Log($"[NetworkManager] Player {roomUpdate.username} joined the room");
                            
                            if (roomUpdate.player != null && !string.IsNullOrEmpty(roomUpdate.player.player_id))
                            {
                                // Use the provided player object to spawn immediately
                                Debug.Log($"[NetworkManager] Spawning joined player from update data: {roomUpdate.player.player_id}");
                                GameStateManager.Instance?.SpawnRemotePlayer(
                                    roomUpdate.player.player_id, 
                                    roomUpdate.player.position.ToVector3(), 
                                    roomUpdate.player.rotation.ToQuaternion()
                                );

                                // UPDATE LOCAL ROOM STATE FOR UI
                                if (CurrentRoom != null)
                                {
                                    if (!CurrentRoom.current_players.Contains(roomUpdate.player.player_id))
                                    {
                                        CurrentRoom.current_players.Add(roomUpdate.player.player_id);
                                        Debug.Log($"[NetworkManager] Added {roomUpdate.player.player_id} to local room player list. Count: {CurrentRoom.current_players.Count}");
                                        // Trigger UI update using OnRoomJoined (since RoomUI listens to this)
                                        OnRoomJoined?.Invoke(CurrentRoom);
                                    }
                                }
                            }
                            else
                            {
                                // Fallback to fetching room details if player object is missing
                                Debug.LogWarning("[NetworkManager] Player object missing in room_update, fetching details...");
                                StartCoroutine(GetRoomDetails(CurrentRoomId, false));
                            }
                        }
                        else if (roomUpdate.action == "player_left")
                        {
                            Debug.Log($"[NetworkManager] Player {roomUpdate.username} left the room");
                            StartCoroutine(GetRoomDetails(CurrentRoomId, false));
                        }
                        break;

                    case "join_room":
                        Debug.Log($"[NetworkManager] Join room data received via WebSocket");
                        var joinData = JsonUtility.FromJson<JoinRoomData>(msgWrapper.data);
                        
                        if (joinData.players != null)
                        {
                            Debug.Log($"[NetworkManager] Processing {joinData.players.Count} existing players...");
                            foreach (var p in joinData.players)
                            {
                                // Cache Names
                                if (!string.IsNullOrEmpty(p.player_id) && !string.IsNullOrEmpty(p.username))
                                {
                                    PlayerNames[p.player_id] = p.username;
                                    Debug.Log($"[NetworkManager] Cached name for {p.player_id}: {p.username}");
                                }
                                else
                                {
                                    Debug.LogWarning($"[NetworkManager] Missing name info for {p.player_id} (Username: '{p.username}')");
                                }

                                // Don't spawn ourselves
                                if (p.player_id != PlayerId)
                                {
                                    GameStateManager.Instance?.SpawnRemotePlayer(
                                        p.player_id, 
                                        p.position.ToVector3(), 
                                        p.rotation.ToQuaternion()
                                    );
                                }
                            }
                            OnPlayerNamesUpdated?.Invoke();
                        }
                        break;
                    
                    case "PLAYER_ACTION":
                    case "player_action": // Handle lowercase type as per requirements
                        // Debug.Log($"[NetworkManager] Player action received");
                        
                        // We need to parse specifically for this type because 'data' is an object, not a string
                        var detailedMsg = JsonUtility.FromJson<ReplicationMessage>(message);
                        
                        if (detailedMsg != null && detailedMsg.data != null)
                        {
                            // Convert to internal PlayerAction format
                            var action = new PlayerAction
                            {
                                player_id = detailedMsg.data.player_id,
                                action_type = detailedMsg.data.action_type,
                                // Provide default/empty values if null
                                position = detailedMsg.data.action_data?.position?.ToVector3() ?? Vector3.zero,
                                rotation = detailedMsg.data.action_data?.rotation?.ToQuaternion() ?? Quaternion.identity,
                                target_object_id = detailedMsg.data.action_data?.target_object_id
                            };
                            
                            OnPlayerActionReceived?.Invoke(action);
                        }
                        else 
                        {
                            // Fallback for flat format if necessary, or just log warning
                            Debug.LogWarning("[NetworkManager] Failed to parse detailed player_action");
                        }
                        break;
                    
                    case "STATE_UPDATE":
                        Debug.Log($"[NetworkManager] State update received");
                        var state = JsonUtility.FromJson<GameState>(msgWrapper.data);
                        OnGameStateUpdated?.Invoke(state);
                        break;
                    
                    case "ERROR":
                        var errorData = JsonUtility.FromJson<ErrorData>(msgWrapper.data);
                        
                        // Treat "Player already in room" as success/warning since we likely joined via HTTP first
                        if (!string.IsNullOrEmpty(errorData.error) && errorData.error.Contains("Player already in room"))
                        {
                            Debug.Log($"[NetworkManager] Server reported '{errorData.error}' - ignoring as player is already joined via REST API");
                        }
                        else
                        {
                            Debug.LogError($"[NetworkManager] Server error: {errorData.error}");
                            OnError?.Invoke(errorData.error);
                        }
                        break;
                    
                    case "game_start":
                    case "START_GAME":
                        Debug.Log($"[NetworkManager] Game start notification received!");
                        OnGameStarted?.Invoke();
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
            var heartbeat = new WSHeartbeat();
            string json = JsonUtility.ToJson(heartbeat);
            webSocket?.Send(json);
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Send a player action to the server
        /// </summary>
        /// <summary>
        /// Send a player action to the server
        /// </summary>
        public void SendPlayerAction(string actionType, string targetObjectId = null, Vector3? position = null, Quaternion? rotation = null, Dictionary<string, object> extraData = null)
        {
            if (!IsPlayer || !isConnected)
            {
                Debug.LogWarning($"[NetworkManager] Cannot send action: IsPlayer={IsPlayer}, isConnected={isConnected}");
                return;
            }

            // Build payload
            var payload = new WSPlayerActionPayload();
            
            if (position.HasValue)
            {
                payload.position = new PacketVector3(position.Value);
            }
            if (rotation.HasValue)
            {
                payload.rotation = new PacketVector3(rotation.Value.eulerAngles);
            }
            
            if (!string.IsNullOrEmpty(targetObjectId))
            {
                payload.target_object_id = targetObjectId;
            }
            
            // Note: extraData is ignored here because JsonUtility doesn't support dictionaries
            // If we need extra data, we should add fields to WSPlayerActionPayload

            var request = new WSPlayerActionRequest
            {
                action_type = actionType,
                action_data = payload
            };

            var message = new WSPlayerActionMessage
            {
                data = request
            };

            string json = JsonUtility.ToJson(message);
            Debug.Log($"[NetworkManager] Sending {actionType} action");
            // Debug.Log($"[NetworkManager] Action JSON: {json}"); // noisy
            webSocket?.Send(json);
        }

        /// <summary>
        /// Send move action
        /// </summary>
        public void SendMoveAction(Vector3 position, Quaternion rotation)
        {
            SendPlayerAction("move", null, position, rotation);
        }

        public void SendMoveAction(Vector3 position)
        {
            SendMoveAction(position, Quaternion.identity);
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
    public class RoomJoinRequest
    {
        public string join_code;
        public string player_id;
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
        public string join_code;
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
        public PlayerInfo player;
    }

    [Serializable]
    public class JoinRoomData
    {
        public bool success;
        public Room room;
        public List<PlayerInfo> players;
    }

    [Serializable]
    public class PlayerInfo
    {
        public string player_id;
        public string username;
        public PacketVector3 position;
        public PacketVector3 rotation;
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
        public Quaternion rotation;
        public string timestamp;
    }

    [Serializable]
    public class WSJoinMessage
    {
        public string type = "JOIN_ROOM";
        public WSJoinData data;
    }

    [Serializable]
    public class WSJoinData
    {
        public string room_id;
        public string username;
    }

    [Serializable]
    public class WSPlayerActionMessage
    {
        public string type = "PLAYER_ACTION";
        public WSPlayerActionRequest data;
    }

    [Serializable]
    public class WSPlayerActionRequest
    {
        public string action_type;
        public WSPlayerActionPayload action_data;
    }

    [Serializable]
    public class WSPlayerActionPayload
    {
        public PacketVector3 position;
        public PacketVector3 rotation;
        public string target_object_id;
    }

    [Serializable]
    public class PacketVector3
    {
        public float x;
        public float y;
        public float z;
        
        public PacketVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public Quaternion ToQuaternion()
        {
            return Quaternion.Euler(x, y, z);
        }
    }

    [Serializable]
    public class WSHeartbeat
    {
        public string type = "HEARTBEAT";
        // Empty data object for JsonUtility is tricky, so we'll just not send data if not needed, 
        // or we can send a dummy string. 
        // Backend probably ignores data for heartbeat.
        // Let's try sending an empty string for data if that structure is required.
        // Or if backend expects specific structure.
        // Previous code sent `data = new {}` which results in "data": {}
        // To get "data": {} with JsonUtility, we need a class.
        public HeartbeatData data = new HeartbeatData();
    }

    [Serializable]
    public class HeartbeatData
    {
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

    [Serializable]
    public class ReplicationMessage
    {
        public string type;
        public ReplicationData data;
    }

    [Serializable]
    public class ReplicationData
    {
        public string player_id;
        public string action_type;
        public ReplicationActionData action_data;
    }

    [Serializable]
    public class ReplicationActionData
    {
        public PacketVector3 position;
        public PacketVector3 rotation;
        public string target_object_id;
        public bool is_running; // from requirements example
    }

    #endregion
}
