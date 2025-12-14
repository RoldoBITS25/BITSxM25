using UnityEngine;
using TMPro;

namespace MultiplayerGame
{
    /// <summary>
    /// Debug UI for multiplayer connection and player information
    /// Displays connection status, player ID, room info, and player count
    /// </summary>
    public class MultiplayerDebugUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerInfoText;
        [SerializeField] private TextMeshProUGUI roomInfoText;
        [SerializeField] private TextMeshProUGUI fpsText;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private bool showFPS = true;
        
        private bool isVisible;
        private float deltaTime;

        private void Start()
        {
            isVisible = showOnStart;
            if (debugPanel != null)
            {
                debugPanel.SetActive(isVisible);
            }
            
            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft += OnRoomLeft;
                NetworkManager.Instance.OnError += OnError;
            }
        }

        private void Update()
        {
            // Toggle visibility
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
                if (debugPanel != null)
                {
                    debugPanel.SetActive(isVisible);
                }
            }

            if (!isVisible) return;

            // Update FPS
            if (showFPS)
            {
                deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
                UpdateFPS();
            }

            // Update info
            UpdateStatusText();
            UpdatePlayerInfo();
            UpdateRoomInfo();
        }

        private void UpdateStatusText()
        {
            if (statusText == null || NetworkManager.Instance == null) return;

            string status = "🔴 Disconnected";
            Color statusColor = Color.red;

            if (!string.IsNullOrEmpty(NetworkManager.Instance.CurrentRoomId))
            {
                status = "🟢 Connected";
                statusColor = Color.green;
            }

            statusText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>{status}</color>";
        }

        private void UpdatePlayerInfo()
        {
            if (playerInfoText == null || NetworkManager.Instance == null) return;

            string role = NetworkManager.Instance.IsPlayer ? 
                $"Player {NetworkManager.Instance.PlayerNumber}" : 
                "Spectator";
            
            Color roleColor = NetworkManager.Instance.IsPlayer ?
                (NetworkManager.Instance.PlayerNumber == 1 ? Color.blue : Color.red) :
                Color.gray;

            playerInfoText.text = $"<b>Player ID:</b> {NetworkManager.Instance.PlayerId}\n" +
                                 $"<b>Role:</b> <color=#{ColorUtility.ToHtmlStringRGB(roleColor)}>{role}</color>";
        }

        private void UpdateRoomInfo()
        {
            if (roomInfoText == null || NetworkManager.Instance == null) return;

            if (string.IsNullOrEmpty(NetworkManager.Instance.CurrentRoomId))
            {
                roomInfoText.text = "<b>Room:</b> Not in a room\n" +
                                   "<b>Players:</b> -";
            }
            else
            {
                roomInfoText.text = $"<b>Room ID:</b> {NetworkManager.Instance.CurrentRoomId}\n" +
                                   $"<b>Players:</b> Syncing...";
            }
        }

        private void UpdateFPS()
        {
            if (fpsText == null) return;

            float fps = 1.0f / deltaTime;
            Color fpsColor = fps >= 60 ? Color.green : fps >= 30 ? Color.yellow : Color.red;
            
            fpsText.text = $"<b>FPS:</b> <color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{fps:0.}</color>";
        }

        private void OnRoomJoined(Room room)
        {
            if (roomInfoText != null)
            {
                roomInfoText.text = $"<b>Room:</b> {room.name}\n" +
                                   $"<b>Room ID:</b> {room.join_code}\n" +
                                   $"<b>Players:</b> {room.current_players.Count}/{room.max_players}";
            }
        }

        private void OnRoomLeft()
        {
            if (roomInfoText != null)
            {
                roomInfoText.text = "<b>Room:</b> Not in a room\n" +
                                   "<b>Players:</b> -";
            }
        }

        private void OnError(string error)
        {
            Debug.LogError($"Network Error: {error}");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
                NetworkManager.Instance.OnError -= OnError;
            }
        }

        /// <summary>
        /// Create a simple debug UI programmatically if none exists
        /// </summary>
        [ContextMenu("Create Debug UI")]
        public void CreateDebugUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("DebugCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create Panel
            GameObject panel = new GameObject("DebugPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 150);

            // Create Status Text
            CreateTextElement("StatusText", panel.transform, new Vector2(10, -10), "Status: Disconnected");
            
            // Create Player Info Text
            CreateTextElement("PlayerInfoText", panel.transform, new Vector2(10, -40), "Player Info");
            
            // Create Room Info Text
            CreateTextElement("RoomInfoText", panel.transform, new Vector2(10, -80), "Room Info");
            
            // Create FPS Text
            CreateTextElement("FPSText", panel.transform, new Vector2(10, -120), "FPS: 60");

            debugPanel = panel;
            
            Debug.Log("Debug UI created! Assign text references in Inspector.");
        }

        private GameObject CreateTextElement(string name, Transform parent, Vector2 position, string defaultText)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 14;
            text.color = Color.white;
            
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, 30);
            
            return textObj;
        }
    }
}
