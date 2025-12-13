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

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                PlayerId = GeneratePlayerId();
            }
            else
            {
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
            StartCoroutine(CreateRoomCoroutine(roomName, maxPlayers, isPrivate, password));
        }

        private IEnumerator CreateRoomCoroutine(string roomName, int maxPlayers, bool isPrivate, string password)
        {
            var roomData = new RoomCreate
            {
                name = roomName,
                max_players = maxPlayers,
                is_private = isPrivate,
                password = password
            };

            string json = JsonUtility.ToJson(roomData);
            string url = $"{serverUrl}/api/rooms/?host_player_id={PlayerId}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Room room = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                    CurrentRoomId = room.room_id;
                    IsPlayer = true;
                    PlayerNumber = 1; // Host is always player 1
                    OnRoomCreated?.Invoke(room);
                    
                    // Connect to WebSocket for real-time updates
                    ConnectWebSocket();
                }
                else
                {
                    OnError?.Invoke($"Failed to create room: {request.error}");
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

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    RoomList roomList = JsonUtility.FromJson<RoomList>("{\"rooms\":" + json + "}");
                    OnRoomListUpdated?.Invoke(roomList.rooms);
                }
                else
                {
                    OnError?.Invoke($"Failed to get room list: {request.error}");
                }
            }
        }

        /// <summary>
        /// Join an existing room
        /// </summary>
        public void JoinRoom(string roomId, string password = null)
        {
            StartCoroutine(JoinRoomCoroutine(roomId, password));
        }

        private IEnumerator JoinRoomCoroutine(string roomId, string password)
        {
            string url = $"{serverUrl}/api/rooms/{roomId}/join?player_id={PlayerId}";
            if (!string.IsNullOrEmpty(password))
            {
                url += $"&password={password}";
            }

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Get room details to determine role
                    yield return GetRoomDetails(roomId);
                }
                else
                {
                    OnError?.Invoke($"Failed to join room: {request.error}");
                }
            }
        }

        private IEnumerator GetRoomDetails(string roomId)
        {
            string url = $"{serverUrl}/api/rooms/{roomId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Room room = JsonUtility.FromJson<Room>(request.downloadHandler.text);
                    CurrentRoomId = room.room_id;
                    
                    // Determine player role (first 2 are players, rest are spectators)
                    int playerIndex = room.current_players.IndexOf(PlayerId);
                    if (playerIndex < 2)
                    {
                        IsPlayer = true;
                        PlayerNumber = playerIndex + 1;
                    }
                    else
                    {
                        IsPlayer = false;
                        PlayerNumber = 0;
                    }

                    OnRoomJoined?.Invoke(room);
                    
                    // Connect to WebSocket for real-time updates
                    ConnectWebSocket();
                }
                else
                {
                    OnError?.Invoke($"Failed to get room details: {request.error}");
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

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                DisconnectWebSocket();
                CurrentRoomId = null;
                IsPlayer = false;
                PlayerNumber = 0;
                OnRoomLeft?.Invoke();
            }
        }

        #endregion

        #region WebSocket Communication

        private void ConnectWebSocket()
        {
            if (webSocket != null)
            {
                DisconnectWebSocket();
            }

            string wsEndpoint = $"{wsUrl}/ws/{CurrentRoomId}/{PlayerId}";
            webSocket = new WebSocketClient(wsEndpoint);
            webSocket.OnMessage += HandleWebSocketMessage;
            webSocket.OnError += (error) => OnError?.Invoke($"WebSocket error: {error}");
            webSocket.Connect();
            isConnected = true;
        }

        private void DisconnectWebSocket()
        {
            if (webSocket != null)
            {
                webSocket.Disconnect();
                webSocket = null;
            }
            isConnected = false;
        }

        private void HandleWebSocketMessage(string message)
        {
            try
            {
                // Parse message type
                var msgData = JsonUtility.FromJson<WebSocketMessage>(message);
                
                switch (msgData.type)
                {
                    case "player_action":
                        var action = JsonUtility.FromJson<PlayerAction>(msgData.data);
                        OnPlayerActionReceived?.Invoke(action);
                        break;
                    
                    case "game_state":
                        var state = JsonUtility.FromJson<GameState>(msgData.data);
                        OnGameStateUpdated?.Invoke(state);
                        break;
                    
                    case "player_joined":
                    case "player_left":
                        // Refresh room details
                        StartCoroutine(GetRoomDetails(CurrentRoomId));
                        break;
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to parse WebSocket message: {e.Message}");
            }
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Send a player action to the server
        /// </summary>
        public void SendPlayerAction(string actionType, string targetObjectId = null, Vector3? position = null, Dictionary<string, object> data = null)
        {
            if (!IsPlayer || !isConnected)
            {
                Debug.LogWarning("Cannot send action: not a player or not connected");
                return;
            }

            var action = new PlayerAction
            {
                player_id = PlayerId,
                action_type = actionType,
                target_object_id = targetObjectId,
                position = position ?? Vector3.zero,
                timestamp = DateTime.UtcNow.ToString("o"),
                data = data
            };

            string json = JsonUtility.ToJson(action);
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
            DisconnectWebSocket();
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
