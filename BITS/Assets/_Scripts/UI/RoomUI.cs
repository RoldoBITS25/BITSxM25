using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Manages the room browser and lobby UI
    /// </summary>
    public class RoomUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject createRoomPanel;
        [SerializeField] private GameObject roomBrowserPanel;
        [SerializeField] private GameObject lobbyPanel;

        [Header("Room Browser")]
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createRoomButton;

        [Header("Create Room")]
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private TMP_InputField maxPlayersInput;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button confirmCreateButton;
        [SerializeField] private Button cancelCreateButton;

        [Header("Lobby")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private TextMeshProUGUI roleText;

        private List<GameObject> roomListItems = new List<GameObject>();
        private List<GameObject> playerListItems = new List<GameObject>();

        private void Start()
        {
            // Setup button listeners
            if (refreshButton != null)
                refreshButton.onClick.AddListener(OnRefreshRooms);

            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(OnShowCreateRoom);

            if (confirmCreateButton != null)
                confirmCreateButton.onClick.AddListener(OnConfirmCreateRoom);

            if (cancelCreateButton != null)
                cancelCreateButton.onClick.AddListener(OnCancelCreateRoom);

            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGame);

            if (leaveLobbyButton != null)
                leaveLobbyButton.onClick.AddListener(OnLeaveLobby);

            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomListUpdated += OnRoomListUpdated;
                NetworkManager.Instance.OnRoomCreated += OnRoomCreated;
                NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft += OnRoomLeft;
                NetworkManager.Instance.OnError += OnNetworkError;
            }

            // Show main menu
            ShowPanel(mainMenuPanel);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomListUpdated -= OnRoomListUpdated;
                NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
                NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
                NetworkManager.Instance.OnError -= OnNetworkError;
            }
        }

        #region Button Handlers

        private void OnRefreshRooms()
        {
            NetworkManager.Instance?.GetRoomList(false);
        }

        private void OnShowCreateRoom()
        {
            ShowPanel(createRoomPanel);
        }

        private void OnConfirmCreateRoom()
        {
            string roomName = roomNameInput.text;
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("Room name cannot be empty");
                return;
            }

            int maxPlayers = 4;
            if (!string.IsNullOrEmpty(maxPlayersInput.text))
            {
                int.TryParse(maxPlayersInput.text, out maxPlayers);
            }

            bool isPrivate = privateToggle.isOn;
            string password = isPrivate ? passwordInput.text : null;

            NetworkManager.Instance?.CreateRoom(roomName, maxPlayers, isPrivate, password);
        }

        private void OnCancelCreateRoom()
        {
            ShowPanel(roomBrowserPanel);
        }

        private void OnStartGame()
        {
            // Load game scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        private void OnLeaveLobby()
        {
            NetworkManager.Instance?.LeaveRoom();
        }

        #endregion

        #region Network Event Handlers

        private void OnRoomListUpdated(List<Room> rooms)
        {
            // Clear existing list
            foreach (var item in roomListItems)
            {
                Destroy(item);
            }
            roomListItems.Clear();

            // Create new list items
            foreach (var room in rooms)
            {
                if (roomListItemPrefab != null && roomListContainer != null)
                {
                    GameObject item = Instantiate(roomListItemPrefab, roomListContainer);
                    
                    // Setup room item
                    var nameText = item.transform.Find("RoomName")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                        nameText.text = room.name;

                    var playerCountText = item.transform.Find("PlayerCount")?.GetComponent<TextMeshProUGUI>();
                    if (playerCountText != null)
                        playerCountText.text = $"{room.current_players.Count}/{room.max_players}";

                    var joinButton = item.transform.Find("JoinButton")?.GetComponent<Button>();
                    if (joinButton != null)
                    {
                        string roomId = room.room_id;
                        joinButton.onClick.AddListener(() => OnJoinRoom(roomId, room.is_private));
                    }

                    roomListItems.Add(item);
                }
            }

            ShowPanel(roomBrowserPanel);
        }

        private void OnJoinRoom(string roomId, bool isPrivate)
        {
            if (isPrivate)
            {
                // Show password dialog (simplified - you'd want a proper dialog)
                string password = ""; // In a real implementation, show a password input dialog
                NetworkManager.Instance?.JoinRoom(roomId, password);
            }
            else
            {
                NetworkManager.Instance?.JoinRoom(roomId);
            }
        }

        private void OnRoomCreated(Room room)
        {
            ShowLobby(room);
        }

        private void OnRoomJoined(Room room)
        {
            ShowLobby(room);
        }

        private void OnRoomLeft()
        {
            ShowPanel(roomBrowserPanel);
            OnRefreshRooms();
        }

        private void OnNetworkError(string error)
        {
            Debug.LogError($"Network Error: {error}");
            // In a real implementation, show an error dialog
        }

        #endregion

        #region Lobby Display

        private void ShowLobby(Room room)
        {
            if (roomNameText != null)
                roomNameText.text = room.name;

            if (playerCountText != null)
                playerCountText.text = $"Players: {room.current_players.Count}/{room.max_players}";

            // Update role text
            if (roleText != null)
            {
                if (NetworkManager.Instance.IsPlayer)
                {
                    roleText.text = $"Player {NetworkManager.Instance.PlayerNumber}";
                    roleText.color = NetworkManager.Instance.PlayerNumber == 1 ? Color.blue : Color.red;
                }
                else
                {
                    roleText.text = "Spectator";
                    roleText.color = Color.gray;
                }
            }

            // Update player list
            UpdatePlayerList(room);

            // Show/hide start button (only host can start)
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(room.host_player_id == NetworkManager.Instance.PlayerId);
            }

            ShowPanel(lobbyPanel);
        }

        private void UpdatePlayerList(Room room)
        {
            // Clear existing list
            foreach (var item in playerListItems)
            {
                Destroy(item);
            }
            playerListItems.Clear();

            // Create player list items
            for (int i = 0; i < room.current_players.Count; i++)
            {
                string playerId = room.current_players[i];
                
                if (playerListItemPrefab != null && playerListContainer != null)
                {
                    GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
                    
                    var playerText = item.GetComponent<TextMeshProUGUI>();
                    if (playerText != null)
                    {
                        string role = i < 2 ? $"Player {i + 1}" : "Spectator";
                        playerText.text = $"{playerId} ({role})";
                        
                        // Color code
                        if (i == 0)
                            playerText.color = Color.blue;
                        else if (i == 1)
                            playerText.color = Color.red;
                        else
                            playerText.color = Color.gray;
                    }

                    playerListItems.Add(item);
                }
            }
        }

        #endregion

        private void ShowPanel(GameObject panel)
        {
            mainMenuPanel?.SetActive(panel == mainMenuPanel);
            createRoomPanel?.SetActive(panel == createRoomPanel);
            roomBrowserPanel?.SetActive(panel == roomBrowserPanel);
            lobbyPanel?.SetActive(panel == lobbyPanel);
        }
    }
}
