using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Helper script to programmatically create the main menu UI
    /// Attach this to an empty GameObject and run SetupUI from the context menu
    /// </summary>
    public class MenuUISetup : MonoBehaviour
    {
        [Header("Canvas Settings")]
        [SerializeField] private int canvasWidth = 1920;
        [SerializeField] private int canvasHeight = 1080;
        [SerializeField] private float uiScale = 1f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        [SerializeField] private Color buttonColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color buttonHoverColor = new Color(0.3f, 0.5f, 0.9f);
        [SerializeField] private Color textColor = Color.white;

        [ContextMenu("Setup Main Menu UI")]
        public void SetupUI()
        {
            Debug.Log("[MenuUISetup] Creating main menu UI...");

            // Check for EventSystem (required for UI interactions)
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Use InputSystemUIInputModule for new Input System
                // If you get compile error, use StandaloneInputModule instead
                #if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[MenuUISetup] ✓ Created EventSystem with InputSystemUIInputModule");
                #else
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[MenuUISetup] ✓ Created EventSystem with StandaloneInputModule");
                #endif
            }
            else
            {
                Debug.Log("[MenuUISetup] ✓ EventSystem already exists");
            }

            // Create Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(canvasWidth, canvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Main Menu Panel
            GameObject mainMenuPanel = CreatePanel(canvasObj.transform, "MainMenuPanel", backgroundColor);
            CreateMainMenuContent(mainMenuPanel.transform);

            // Create Room Browser Panel
            GameObject roomBrowserPanel = CreatePanel(canvasObj.transform, "RoomBrowserPanel", backgroundColor);
            CreateRoomBrowserContent(roomBrowserPanel.transform);
            roomBrowserPanel.SetActive(false);

            // Create Create Room Panel
            GameObject createRoomPanel = CreatePanel(canvasObj.transform, "CreateRoomPanel", backgroundColor);
            CreateCreateRoomContent(createRoomPanel.transform);
            createRoomPanel.SetActive(false);

            // Create Lobby Panel
            GameObject lobbyPanel = CreatePanel(canvasObj.transform, "LobbyPanel", backgroundColor);
            CreateLobbyContent(lobbyPanel.transform);
            lobbyPanel.SetActive(false);

            // Add RoomUI component
            RoomUI roomUI = canvasObj.AddComponent<RoomUI>();
            AssignRoomUIReferences(roomUI, canvasObj);

            // Add MainMenuManager
            GameObject managerObj = new GameObject("MainMenuManager");
            MainMenuManager manager = managerObj.AddComponent<MainMenuManager>();
            
            // Use reflection to assign references
            var managerType = typeof(MainMenuManager);
            var roomUIField = managerType.GetField("roomUI", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var menuCanvasField = managerType.GetField("menuCanvas", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (roomUIField != null)
                roomUIField.SetValue(manager, roomUI);
            if (menuCanvasField != null)
                menuCanvasField.SetValue(manager, canvasObj);

            // Ensure NetworkManager exists
            if (NetworkManager.Instance == null)
            {
                Debug.Log("[MenuUISetup] Creating NetworkManager...");
                GameObject networkManagerObj = new GameObject("NetworkManager");
                NetworkManager netManager = networkManagerObj.AddComponent<NetworkManager>();
                
                // Set default server URLs
                var netManagerType = typeof(NetworkManager);
                var serverUrlField = netManagerType.GetField("serverUrl",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var wsUrlField = netManagerType.GetField("wsUrl",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (serverUrlField != null)
                    serverUrlField.SetValue(netManager, "http://127.0.0.1:8000");
                if (wsUrlField != null)
                    wsUrlField.SetValue(netManager, "ws://127.0.0.1:8000");
                    
                Debug.Log("[MenuUISetup] ✓ NetworkManager created");
            }
            else
            {
                Debug.Log("[MenuUISetup] ✓ NetworkManager already exists");
            }

            Debug.Log("[MenuUISetup] ✓ Main menu UI created successfully!");
            Debug.Log("[MenuUISetup] Please assign the UI element references in the Inspector if needed.");
        }

        private GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = bgColor;

            return panel;
        }

        private void CreateMainMenuContent(Transform parent)
        {
            // Title
            GameObject title = CreateText(parent, "TitleText", "MULTIPLAYER GAME", 60, TextAlignmentOptions.Center);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.sizeDelta = new Vector2(800, 100);

            // Create Room Button
            GameObject createBtn = CreateButton(parent, "CreateRoomButton", "Create Room", 300, 80);
            RectTransform createRect = createBtn.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0.5f, 0.5f);
            createRect.anchorMax = new Vector2(0.5f, 0.5f);
            createRect.anchoredPosition = new Vector2(0, 50);

            // Join Room Button
            GameObject joinBtn = CreateButton(parent, "JoinRoomButton", "Join Room", 300, 80);
            RectTransform joinRect = joinBtn.GetComponent<RectTransform>();
            joinRect.anchorMin = new Vector2(0.5f, 0.5f);
            joinRect.anchorMax = new Vector2(0.5f, 0.5f);
            joinRect.anchoredPosition = new Vector2(0, -50);
        }

        private void CreateRoomBrowserContent(Transform parent)
        {
            // Title
            GameObject title = CreateText(parent, "BrowserTitle", "Available Rooms", 40, TextAlignmentOptions.Center);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.9f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(600, 60);

            // Room List Container (ScrollView would be better, but simplified here)
            GameObject listContainer = new GameObject("RoomListContainer");
            listContainer.transform.SetParent(parent, false);
            RectTransform listRect = listContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.1f, 0.2f);
            listRect.anchorMax = new Vector2(0.9f, 0.8f);
            listRect.sizeDelta = Vector2.zero;

            // Refresh Button
            GameObject refreshBtn = CreateButton(parent, "RefreshButton", "Refresh", 200, 60);
            RectTransform refreshRect = refreshBtn.GetComponent<RectTransform>();
            refreshRect.anchorMin = new Vector2(0.3f, 0.1f);
            refreshRect.anchorMax = new Vector2(0.3f, 0.1f);
            refreshRect.anchoredPosition = new Vector2(0, 0);

            // Back Button
            GameObject backBtn = CreateButton(parent, "BackToMenuButton", "Back", 200, 60);
            RectTransform backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.7f, 0.1f);
            backRect.anchorMax = new Vector2(0.7f, 0.1f);
            backRect.anchoredPosition = new Vector2(0, 0);
        }

        private void CreateCreateRoomContent(Transform parent)
        {
            // Title
            GameObject title = CreateText(parent, "CreateTitle", "Create New Room", 40, TextAlignmentOptions.Center);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.sizeDelta = new Vector2(600, 60);

            // Room Name Input
            GameObject nameInput = CreateInputField(parent, "RoomNameInput", "Room Name...", 400, 50);
            RectTransform nameRect = nameInput.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.6f);
            nameRect.anchorMax = new Vector2(0.5f, 0.6f);
            nameRect.anchoredPosition = new Vector2(0, 0);

            // Max Players Input
            GameObject maxInput = CreateInputField(parent, "MaxPlayersInput", "Max Players (4)...", 400, 50);
            RectTransform maxRect = maxInput.GetComponent<RectTransform>();
            maxRect.anchorMin = new Vector2(0.5f, 0.5f);
            maxRect.anchorMax = new Vector2(0.5f, 0.5f);
            maxRect.anchoredPosition = new Vector2(0, 0);

            // Confirm Button
            GameObject confirmBtn = CreateButton(parent, "ConfirmCreateButton", "Create", 200, 60);
            RectTransform confirmRect = confirmBtn.GetComponent<RectTransform>();
            confirmRect.anchorMin = new Vector2(0.3f, 0.2f);
            confirmRect.anchorMax = new Vector2(0.3f, 0.2f);
            confirmRect.anchoredPosition = new Vector2(0, 0);

            // Cancel Button
            GameObject cancelBtn = CreateButton(parent, "CancelCreateButton", "Cancel", 200, 60);
            RectTransform cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.7f, 0.2f);
            cancelRect.anchorMax = new Vector2(0.7f, 0.2f);
            cancelRect.anchoredPosition = new Vector2(0, 0);
        }

        private void CreateLobbyContent(Transform parent)
        {
            // Room Name
            GameObject roomName = CreateText(parent, "RoomNameText", "Room Name", 36, TextAlignmentOptions.Center);
            RectTransform roomRect = roomName.GetComponent<RectTransform>();
            roomRect.anchorMin = new Vector2(0.5f, 0.85f);
            roomRect.anchorMax = new Vector2(0.5f, 0.85f);
            roomRect.sizeDelta = new Vector2(600, 50);

            // Player Count
            GameObject playerCount = CreateText(parent, "PlayerCountText", "Players: 0/4", 28, TextAlignmentOptions.Center);
            RectTransform countRect = playerCount.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 0.75f);
            countRect.anchorMax = new Vector2(0.5f, 0.75f);
            countRect.sizeDelta = new Vector2(400, 40);

            // Role Text
            GameObject roleText = CreateText(parent, "RoleText", "Player 1", 24, TextAlignmentOptions.Center);
            RectTransform roleRect = roleText.GetComponent<RectTransform>();
            roleRect.anchorMin = new Vector2(0.5f, 0.65f);
            roleRect.anchorMax = new Vector2(0.5f, 0.65f);
            roleRect.sizeDelta = new Vector2(300, 35);

            // Player List Container
            GameObject listContainer = new GameObject("PlayerListContainer");
            listContainer.transform.SetParent(parent, false);
            RectTransform listRect = listContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.2f, 0.3f);
            listRect.anchorMax = new Vector2(0.8f, 0.6f);
            listRect.sizeDelta = Vector2.zero;

            // Start Game Button
            GameObject startBtn = CreateButton(parent, "StartGameButton", "Start Game", 250, 70);
            RectTransform startRect = startBtn.GetComponent<RectTransform>();
            startRect.anchorMin = new Vector2(0.5f, 0.15f);
            startRect.anchorMax = new Vector2(0.5f, 0.15f);
            startRect.anchoredPosition = new Vector2(0, 0);

            // Leave Button
            GameObject leaveBtn = CreateButton(parent, "LeaveLobbyButton", "Leave", 200, 60);
            RectTransform leaveRect = leaveBtn.GetComponent<RectTransform>();
            leaveRect.anchorMin = new Vector2(0.5f, 0.05f);
            leaveRect.anchorMax = new Vector2(0.5f, 0.05f);
            leaveRect.anchoredPosition = new Vector2(0, 0);
        }

        private GameObject CreateButton(Transform parent, string name, string text, float width, float height)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image image = buttonObj.AddComponent<Image>();
            image.color = buttonColor;

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = buttonHoverColor;
            colors.pressedColor = buttonColor * 0.8f;
            button.colors = colors;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }

        private GameObject CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = alignment;

            return textObj;
        }

        private GameObject CreateInputField(Transform parent, string name, string placeholder, float width, float height)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            Image image = inputObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();

            // Text Area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-20, -10);

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI phText = placeholderObj.AddComponent<TextMeshProUGUI>();
            phText.text = placeholder;
            phText.fontSize = 18;
            phText.color = new Color(0.5f, 0.5f, 0.5f);
            phText.alignment = TextAlignmentOptions.Left;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = "";
            tmpText.fontSize = 18;
            tmpText.color = textColor;
            tmpText.alignment = TextAlignmentOptions.Left;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = tmpText;
            inputField.placeholder = phText;

            return inputObj;
        }

        private void AssignRoomUIReferences(RoomUI roomUI, GameObject canvas)
        {
            var roomUIType = typeof(RoomUI);
            
            // Assign panels
            AssignField(roomUIType, roomUI, "mainMenuPanel", canvas.transform.Find("MainMenuPanel")?.gameObject);
            AssignField(roomUIType, roomUI, "createRoomPanel", canvas.transform.Find("CreateRoomPanel")?.gameObject);
            AssignField(roomUIType, roomUI, "roomBrowserPanel", canvas.transform.Find("RoomBrowserPanel")?.gameObject);
            AssignField(roomUIType, roomUI, "lobbyPanel", canvas.transform.Find("LobbyPanel")?.gameObject);

            // Main Menu
            AssignField(roomUIType, roomUI, "mainMenuCreateButton", 
                canvas.transform.Find("MainMenuPanel/CreateRoomButton")?.GetComponent<Button>());
            AssignField(roomUIType, roomUI, "mainMenuJoinButton", 
                canvas.transform.Find("MainMenuPanel/JoinRoomButton")?.GetComponent<Button>());
            AssignField(roomUIType, roomUI, "titleText", 
                canvas.transform.Find("MainMenuPanel/TitleText")?.GetComponent<TextMeshProUGUI>());

            // Room Browser
            AssignField(roomUIType, roomUI, "roomListContainer", 
                canvas.transform.Find("RoomBrowserPanel/RoomListContainer"));
            AssignField(roomUIType, roomUI, "refreshButton", 
                canvas.transform.Find("RoomBrowserPanel/RefreshButton")?.GetComponent<Button>());
            AssignField(roomUIType, roomUI, "backToMenuButton", 
                canvas.transform.Find("RoomBrowserPanel/BackToMenuButton")?.GetComponent<Button>());

            // Create Room
            AssignField(roomUIType, roomUI, "roomNameInput", 
                canvas.transform.Find("CreateRoomPanel/RoomNameInput")?.GetComponent<TMP_InputField>());
            AssignField(roomUIType, roomUI, "maxPlayersInput", 
                canvas.transform.Find("CreateRoomPanel/MaxPlayersInput")?.GetComponent<TMP_InputField>());
            AssignField(roomUIType, roomUI, "confirmCreateButton", 
                canvas.transform.Find("CreateRoomPanel/ConfirmCreateButton")?.GetComponent<Button>());
            AssignField(roomUIType, roomUI, "cancelCreateButton", 
                canvas.transform.Find("CreateRoomPanel/CancelCreateButton")?.GetComponent<Button>());

            // Lobby
            AssignField(roomUIType, roomUI, "roomNameText", 
                canvas.transform.Find("LobbyPanel/RoomNameText")?.GetComponent<TextMeshProUGUI>());
            AssignField(roomUIType, roomUI, "playerCountText", 
                canvas.transform.Find("LobbyPanel/PlayerCountText")?.GetComponent<TextMeshProUGUI>());
            AssignField(roomUIType, roomUI, "roleText", 
                canvas.transform.Find("LobbyPanel/RoleText")?.GetComponent<TextMeshProUGUI>());
            AssignField(roomUIType, roomUI, "playerListContainer", 
                canvas.transform.Find("LobbyPanel/PlayerListContainer"));
            AssignField(roomUIType, roomUI, "startGameButton", 
                canvas.transform.Find("LobbyPanel/StartGameButton")?.GetComponent<Button>());
            AssignField(roomUIType, roomUI, "leaveLobbyButton", 
                canvas.transform.Find("LobbyPanel/LeaveLobbyButton")?.GetComponent<Button>());
        }

        private void AssignField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null && value != null)
            {
                field.SetValue(instance, value);
                Debug.Log($"[MenuUISetup] Assigned {fieldName}");
            }
            else if (field == null)
            {
                Debug.LogWarning($"[MenuUISetup] Field {fieldName} not found");
            }
            else
            {
                Debug.LogWarning($"[MenuUISetup] Value for {fieldName} is null");
            }
        }
    }
}
