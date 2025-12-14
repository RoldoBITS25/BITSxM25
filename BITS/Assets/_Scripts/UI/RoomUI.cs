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

        [Header("Main Menu")]
        [SerializeField] private Button mainMenuCreateButton;
        [SerializeField] private Button mainMenuJoinButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Room Browser")]
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private TextMeshProUGUI noRoomsText;
        [SerializeField] private TMP_InputField usernameInput;

        [Header("Join Room")]
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button joinWithCodeButton;

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
            Debug.Log("[RoomUI] ========== RoomUI Initializing ==========");
            
            // Debug: Check panel references
            Debug.Log($"[RoomUI] Main Menu Panel: {(mainMenuPanel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"[RoomUI] Create Room Panel: {(createRoomPanel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"[RoomUI] Room Browser Panel: {(roomBrowserPanel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"[RoomUI] Room Browser Panel: {(roomBrowserPanel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"[RoomUI] Lobby Panel: {(lobbyPanel != null ? "✓" : "✗ NULL")}");
            
            // // RUNTIME FIX: Create username input if missing
            // if (usernameInput == null && mainMenuPanel != null)
            // {
            //     Debug.LogWarning("[RoomUI] ⚠ usernameInput is NULL! Creating runtime input in Main Menu...");
            //     usernameInput = CreateRuntimeUsernameInput();
            //     Debug.Log("[RoomUI] ✓ Runtime username input created in Main Menu");
            // }
            
            // RUNTIME FIX: Create prefabs if they're missing
            if (roomListItemPrefab == null)
            {
                // We don't need room list item prefab for the new flow, but keeping it to avoid errors if referenced
                Debug.LogWarning("[RoomUI] ⚠ roomListItemPrefab is NULL! Creating runtime prefab...");
                roomListItemPrefab = CreateRoomListItemPrefab();
            }
            
            if (playerListItemPrefab == null)
            {
                Debug.LogWarning("[RoomUI] ⚠ playerListItemPrefab is NULL! Creating runtime prefab...");
                playerListItemPrefab = CreatePlayerListItemPrefab();
                Debug.Log("[RoomUI] ✓ Runtime player list item prefab created");
            }

            // SETUP JOIN ROOM UI
            // SetupJoinRoomUI call removed (handled by MenuUISetup)
            
            // Setup button listeners - Main Menu
            if (mainMenuCreateButton != null)
            {
                mainMenuCreateButton.onClick.AddListener(OnShowCreateRoom);
                Debug.Log("[RoomUI] ✓ Main Menu Create Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Main Menu Create Button is NULL!");

            if (mainMenuJoinButton != null)
            {
                mainMenuJoinButton.onClick.AddListener(OnShowRoomBrowser);
                Debug.Log("[RoomUI] ✓ Main Menu Join Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Main Menu Join Button is NULL!");

            // Room Browser
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(OnRefreshRooms);
                Debug.Log("[RoomUI] ✓ Refresh Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Refresh Button is NULL!");

            if (createRoomButton != null)
            {
                createRoomButton.onClick.AddListener(OnShowCreateRoom);
                Debug.Log("[RoomUI] ✓ Create Room Button (browser) listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Create Room Button (browser) is NULL!");

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMainMenu);
                Debug.Log("[RoomUI] ✓ Back To Menu Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Back To Menu Button is NULL!");

            if (joinWithCodeButton != null)
            {
                joinWithCodeButton.onClick.AddListener(OnJoinRoomByCode);
                Debug.Log("[RoomUI] ✓ Join With Code Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Join With Code Button is NULL!");

            // Create Room
            if (confirmCreateButton != null)
            {
                confirmCreateButton.onClick.AddListener(OnConfirmCreateRoom);
                Debug.Log("[RoomUI] ✓ Confirm Create Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Confirm Create Button is NULL!");

            if (cancelCreateButton != null)
            {
                cancelCreateButton.onClick.AddListener(OnCancelCreateRoom);
                Debug.Log("[RoomUI] ✓ Cancel Create Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Cancel Create Button is NULL!");

            // Lobby
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGame);
                Debug.Log("[RoomUI] ✓ Start Game Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Start Game Button is NULL!");

            if (leaveLobbyButton != null)
            {
                leaveLobbyButton.onClick.AddListener(OnLeaveLobby);
                Debug.Log("[RoomUI] ✓ Leave Lobby Button listener added");
            }
            else
                Debug.LogWarning("[RoomUI] ✗ Leave Lobby Button is NULL!");

            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomListUpdated += OnRoomListUpdated;
                NetworkManager.Instance.OnRoomCreated += OnRoomCreated;
                NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft += OnRoomLeft;

                NetworkManager.Instance.OnGameStarted += OnGameStartedFromNetwork;
                NetworkManager.Instance.OnPlayerNamesUpdated += OnPlayerNamesUpdated;
                NetworkManager.Instance.OnError += OnNetworkError;
                Debug.Log("[RoomUI] ✓ Subscribed to NetworkManager events");
            }
            else
            {
                Debug.LogError("[RoomUI] ✗ NetworkManager.Instance is NULL! Network events won't work.");
            }

            // Show main menu
            Debug.Log("[RoomUI] Showing main menu panel");
            ShowPanel(mainMenuPanel);
            Debug.Log("[RoomUI] ========== RoomUI Initialization Complete ==========");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomListUpdated -= OnRoomListUpdated;
                NetworkManager.Instance.OnRoomCreated -= OnRoomCreated;
                NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
                NetworkManager.Instance.OnGameStarted -= OnGameStartedFromNetwork;
                NetworkManager.Instance.OnPlayerNamesUpdated -= OnPlayerNamesUpdated;
                NetworkManager.Instance.OnError -= OnNetworkError;
            }
        }

        #region Button Handlers

        private void OnShowRoomBrowser()
        {
            Debug.Log("[RoomUI] OnShowRoomBrowser called (Showing Join by Code)");
            ShowPanel(roomBrowserPanel);
            // No longer fetching room list
        }

        private void OnBackToMainMenu()
        {
            Debug.Log("[RoomUI] OnBackToMainMenu called");
            ShowPanel(mainMenuPanel);
        }

        private void OnRefreshRooms()
        {
            // Deprecated for Room Code flow
            // NetworkManager.Instance?.GetRoomList(false);
        }

        private void OnShowCreateRoom()
        {
            Debug.Log("[RoomUI] OnShowCreateRoom called");
            ShowPanel(createRoomPanel);
        }

        private void OnConfirmCreateRoom()
        {
            Debug.Log("[RoomUI] OnConfirmCreateRoom called");
            
            if (roomNameInput == null)
            {
                Debug.LogError("[RoomUI] roomNameInput is NULL!");
                return;
            }
            
            string roomName = roomNameInput.text;
            if (string.IsNullOrEmpty(roomName))
            {
                Debug.LogWarning("[RoomUI] Room name cannot be empty");
                return;
            }

            int maxPlayers = 4;
            if (maxPlayersInput != null && !string.IsNullOrEmpty(maxPlayersInput.text))
            {
                int.TryParse(maxPlayersInput.text, out maxPlayers);
            }

            bool isPrivate = privateToggle != null && privateToggle.isOn;


            string password = isPrivate && passwordInput != null ? passwordInput.text : null;
            string username = usernameInput != null ? usernameInput.text : "";

            Debug.Log($"[RoomUI] Creating room: {roomName}, MaxPlayers: {maxPlayers}, Private: {isPrivate}, User: {username}");
            NetworkManager.Instance?.CreateRoom(roomName, maxPlayers, isPrivate, password, username);
        }

        private void OnCancelCreateRoom()
        {
            ShowPanel(mainMenuPanel);
        }

        private void OnStartGame()
        {
            Debug.Log("[RoomUI] Requesting Game Start...");
            // Send start game request to server - we will load scene when we receive the START_GAME event
            NetworkManager.Instance?.StartGame();
        }

        private System.Collections.IEnumerator LoadGameSceneAsync()
        {
            Debug.Log("[RoomUI] Loading GameScene...");
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameScene");
            
            // Wait until the scene is fully loaded
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[RoomUI] Loading progress: {asyncLoad.progress * 100}%");
                yield return null;
            }
            
            Debug.Log("[RoomUI] GameScene loaded successfully");
        }

        private void OnLeaveLobby()
        {
            NetworkManager.Instance?.LeaveRoom();
        }

        #endregion

        #region Network Event Handlers

        private void OnRoomListUpdated(List<Room> rooms)
        {
            // Deprecated - we don't display the list anymore
            return;
            Debug.Log($"[RoomUI] ========== OnRoomListUpdated called ==========");
            Debug.Log($"[RoomUI] Received {rooms?.Count ?? 0} rooms");
            Debug.Log($"[RoomUI] Room List Container: {(roomListContainer != null ? "✓" : "✗ NULL")}");
            Debug.Log($"[RoomUI] Room List Item Prefab: {(roomListItemPrefab != null ? "✓" : "✗ NULL")}");
            
            // Clear existing list
            Debug.Log($"[RoomUI] Clearing {roomListItems.Count} existing room items");
            foreach (var item in roomListItems)
            {
                Destroy(item);
            }
            roomListItems.Clear();

            // Create new list items
            if (rooms == null || rooms.Count == 0)
            {
                Debug.LogWarning("[RoomUI] ⚠ No rooms to display!");
                if (noRoomsText != null)
                {
                    noRoomsText.gameObject.SetActive(true);
                    noRoomsText.text = "No rooms available.\\nClick 'Create Room' to start a new game!";
                    Debug.Log("[RoomUI] Showing 'no rooms' message");
                }
                ShowPanel(roomBrowserPanel);
                return;
            }
            
            // Hide "no rooms" message when we have rooms
            if (noRoomsText != null)
            {
                noRoomsText.gameObject.SetActive(false);
            }
            
            // Ensure container has layout component
            if (roomListContainer != null)
            {
                var layout = roomListContainer.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    Debug.LogWarning("[RoomUI] Adding VerticalLayoutGroup to room list container");
                    layout = roomListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                    layout.spacing = 10;
                    layout.childAlignment = TextAnchor.UpperCenter;
                    layout.childControlWidth = true;
                    layout.childControlHeight = false;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                    layout.padding = new RectOffset(10, 10, 10, 10);
                }
                
                Debug.Log($"[RoomUI] Container active: {roomListContainer.gameObject.activeInHierarchy}");
            }
            
            int itemsCreated = 0;
            foreach (var room in rooms)
            {
                Debug.Log($"[RoomUI] Processing room: {room.name} (ID: {room.room_id}, Players: {room.current_players.Count}/{room.max_players})");
                
                if (roomListItemPrefab != null && roomListContainer != null)
                {
                    GameObject item = Instantiate(roomListItemPrefab, roomListContainer);
                    item.SetActive(true); // CRITICAL: Ensure item is active!
                    Debug.Log($"[RoomUI] ✓ Created room list item for '{room.name}' (Active: {item.activeInHierarchy})");
                    
                    // Setup room item
                    var nameText = item.transform.Find("RoomName")?.GetComponent<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        nameText.text = room.name;
                        Debug.Log($"[RoomUI]   - Set room name: {room.name}");
                    }
                    else
                        Debug.LogWarning("[RoomUI]   - ✗ RoomName text component not found!");

                    var playerCountText = item.transform.Find("PlayerCount")?.GetComponent<TextMeshProUGUI>();
                    if (playerCountText != null)
                    {
                        playerCountText.text = $"{room.current_players.Count}/{room.max_players}";
                        Debug.Log($"[RoomUI]   - Set player count: {room.current_players.Count}/{room.max_players}");
                    }
                    else
                        Debug.LogWarning("[RoomUI]   - ✗ PlayerCount text component not found!");

                    var joinButton = item.transform.Find("JoinButton")?.GetComponent<Button>();
                    if (joinButton != null)
                    {
                        string roomId = room.join_code;
                        joinButton.onClick.AddListener(() => OnJoinRoom(roomId, room.is_private));
                        Debug.Log($"[RoomUI]   - ✓ Join button configured");
                    }
                    else
                        Debug.LogWarning("[RoomUI]   - ✗ JoinButton component not found!");

                    roomListItems.Add(item);
                    itemsCreated++;
                }
                else
                {
                    Debug.LogError($"[RoomUI] ✗ Cannot create room item - Prefab: {(roomListItemPrefab != null ? "OK" : "NULL")}, Container: {(roomListContainer != null ? "OK" : "NULL")}");
                }
            }

            Debug.Log($"[RoomUI] ✓ Created {itemsCreated} room list items");
            Debug.Log($"[RoomUI] Total items in list: {roomListItems.Count}");
            Debug.Log($"[RoomUI] Showing room browser panel");
            ShowPanel(roomBrowserPanel);
            Debug.Log($"[RoomUI] ========== OnRoomListUpdated complete ==========");
        }

        private void OnJoinRoom(string roomId, bool isPrivate)
        {
            string username = usernameInput != null ? usernameInput.text : "";
            
            if (isPrivate)
            {
                // Show password dialog (simplified - you'd want a proper dialog)
                string password = ""; // In a real implementation, show a password input dialog
                NetworkManager.Instance?.JoinRoom(roomId, password, username);
            }
            else
            {
                NetworkManager.Instance?.JoinRoom(roomId, null, username);
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
            ShowPanel(mainMenuPanel);
        }

        private void OnGameStartedFromNetwork()
        {
            Debug.Log("[RoomUI] Received Game Start signal from Network.");
            StartCoroutine(LoadGameSceneAsync());
        }

        private void OnPlayerNamesUpdated()
        {
            if (NetworkManager.Instance.CurrentRoom != null)
            {
                ShowLobby(NetworkManager.Instance.CurrentRoom);
            }
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
                roomNameText.text = $"{room.name}\n<size=60%>Code: {room.join_code}</size>"; // Display Room Code

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
                        string playerName = NetworkManager.Instance.GetPlayerName(playerId);
                        playerText.text = $"{playerName} ({role})";
                        
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
            
            // Ensure room list container is visible when showing room browser
            if (panel == roomBrowserPanel && roomListContainer != null)
            {
                roomListContainer.gameObject.SetActive(true);
                Debug.Log($"[RoomUI] Room browser panel shown, container active: {roomListContainer.gameObject.activeInHierarchy}");
            }
            
            Debug.Log($"[RoomUI] Panel visibility - Main: {mainMenuPanel?.activeSelf}, Browser: {roomBrowserPanel?.activeSelf}, Create: {createRoomPanel?.activeSelf}, Lobby: {lobbyPanel?.activeSelf}");
        }

        #region Runtime Prefab Creation

        /// <summary>
        /// Creates a room list item prefab at runtime if one wasn't assigned in the Inspector
        /// </summary>
        private GameObject CreateRoomListItemPrefab()
        {
            GameObject prefab = new GameObject("RoomListItemPrefab");
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(700, 80);

            // Background
            Image bg = prefab.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f);

            // Room Name Text
            GameObject nameTextObj = new GameObject("RoomName");
            nameTextObj.transform.SetParent(prefab.transform, false);
            RectTransform nameRect = nameTextObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(20, 0);
            nameRect.sizeDelta = new Vector2(400, 40);
            
            TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Room Name";
            nameText.fontSize = 24;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.Left;

            // Player Count Text
            GameObject countTextObj = new GameObject("PlayerCount");
            countTextObj.transform.SetParent(prefab.transform, false);
            RectTransform countRect = countTextObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.6f, 0.5f);
            countRect.anchorMax = new Vector2(0.6f, 0.5f);
            countRect.anchoredPosition = new Vector2(0, 0);
            countRect.sizeDelta = new Vector2(100, 30);
            
            TextMeshProUGUI countText = countTextObj.AddComponent<TextMeshProUGUI>();
            countText.text = "0/4";
            countText.fontSize = 20;
            countText.color = Color.white;
            countText.alignment = TextAlignmentOptions.Center;

            // Join Button
            GameObject joinBtnObj = new GameObject("JoinButton");
            joinBtnObj.transform.SetParent(prefab.transform, false);
            RectTransform joinRect = joinBtnObj.AddComponent<RectTransform>();
            joinRect.anchorMin = new Vector2(1, 0.5f);
            joinRect.anchorMax = new Vector2(1, 0.5f);
            joinRect.anchoredPosition = new Vector2(-70, 0);
            joinRect.sizeDelta = new Vector2(120, 50);
            
            Image joinBg = joinBtnObj.AddComponent<Image>();
            joinBg.color = new Color(0.2f, 0.4f, 0.8f);
            
            Button joinButton = joinBtnObj.AddComponent<Button>();
            ColorBlock colors = joinButton.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.8f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f);
            colors.pressedColor = new Color(0.16f, 0.32f, 0.64f);
            joinButton.colors = colors;
            
            GameObject joinTextObj = new GameObject("Text");
            joinTextObj.transform.SetParent(joinBtnObj.transform, false);
            RectTransform joinTextRect = joinTextObj.AddComponent<RectTransform>();
            joinTextRect.anchorMin = Vector2.zero;
            joinTextRect.anchorMax = Vector2.one;
            joinTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI joinText = joinTextObj.AddComponent<TextMeshProUGUI>();
            joinText.text = "Join";
            joinText.fontSize = 24;
            joinText.color = Color.white;
            joinText.alignment = TextAlignmentOptions.Center;

            prefab.SetActive(false); // Prefabs should be inactive
            return prefab;
        }

        /// <summary>
        /// Creates a player list item prefab at runtime if one wasn't assigned in the Inspector
        /// </summary>
        private GameObject CreatePlayerListItemPrefab()
        {
            GameObject prefab = new GameObject("PlayerListItemPrefab");
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 40);

            TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
            text.text = "Player Name (Role)";
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            prefab.SetActive(false); // Prefabs should be inactive
            return prefab;
        }

        /// <summary>
        /// Creates the username input field at runtime
        /// </summary>
        private TMP_InputField CreateRuntimeUsernameInput()
        {
            GameObject jsonObj = new GameObject("UsernameInput");
            if (mainMenuPanel != null)
                jsonObj.transform.SetParent(mainMenuPanel.transform, false);
            
            RectTransform rect = jsonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 100); // Above buttons (assuming buttons are around 0 or below)
            rect.sizeDelta = new Vector2(300, 40);

            Image bg = jsonObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            TMP_InputField input = jsonObj.AddComponent<TMP_InputField>();
            
            // Text Area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(jsonObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 0);
            textAreaRect.offsetMax = new Vector2(-10, 0);

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter Username...";
            placeholder.fontSize = 20;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Left;
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            input.textViewport = textAreaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = placeholder.font; // Ensure font matches

            return input;
        }

        #endregion

        #region Join Room UI Setup

        // UI Setup is now handled by MenuUISetup.cs

        private void OnJoinRoomByCode()
        {
            if (roomCodeInput == null) return;
            
            string code = roomCodeInput.text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                Debug.LogWarning("Room code is empty");
                return;
            }

            string username = usernameInput != null ? usernameInput.text : "";
            Debug.Log($"[RoomUI] Joining room with code: {code}");
            NetworkManager.Instance?.JoinRoom(code, null, username);
        }

        #endregion
    }
}
