using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that display parametrized text when interacted with
    /// </summary>
    public class TextDisplayObject : MonoBehaviour
    {
        [Header("Text Display Settings")]
        [SerializeField] [TextArea(3, 10)] private string displayText = "Hello, World!";
        [SerializeField] private Color highlightColor = Color.green;
        [SerializeField] private float displayDuration = 3f; // How long to show the text (0 = until dismissed)
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 textBoxSize = new Vector2(400, 200);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 24;

        private SpriteRenderer spriteRenderer;
        private MeshRenderer meshRenderer;
        private Color originalColor;
        private string objectId;
        private bool isShowingText = false;
        
        // UI References
        private static GameObject textDisplayCanvas;
        private static GameObject textPanel;
        private static TextMeshProUGUI textComponent;
        private static Button closeButton;

        private void Awake()
        {
            // Try to get renderer for highlighting
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            else
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    originalColor = meshRenderer.material.color;
                }
            }

            // Generate deterministic ID based on position
            Vector3 pos = transform.position;
            objectId = $"TextDisplay_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
        }

        private void Start()
        {
            // Initialize UI if not already created
            if (textDisplayCanvas == null)
            {
                CreateTextDisplayUI();
            }

            // Register with GameStateManager if available
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RegisterObject(objectId, gameObject);
            }
            // this.OnInteract("player1");
        }

        /// <summary>
        /// Set custom text to display
        /// </summary>
        public void SetDisplayText(string text)
        {
            displayText = text;
        }

        /// <summary>
        /// Get the current display text
        /// </summary>
        public string GetDisplayText()
        {
            return displayText;
        }

        /// <summary>
        /// Called when player interacts with this object
        /// </summary>
        public void OnInteract(string playerId)
        {
            if (isShowingText) return;

            Debug.Log($"TextDisplayObject {objectId} interacted by player {playerId}");
            ShowText();

            // Send interaction to network if needed
            if (NetworkManager.Instance != null)
            {
                // You can add a custom network action here if needed
                // NetworkManager.Instance.SendCustomAction("text_display", objectId);
            }
        }

        /// <summary>
        /// Show the text display UI
        /// </summary>
        private void ShowText()
        {
            if (textDisplayCanvas == null) return;

            isShowingText = true;
            textDisplayCanvas.SetActive(true);
            
            if (textComponent != null)
            {
                textComponent.text = displayText;
            }

            // Auto-hide after duration if set
            if (displayDuration > 0)
            {
                Invoke(nameof(HideText), displayDuration);
            }
        }

        /// <summary>
        /// Hide the text display UI
        /// </summary>
        public void HideText()
        {
            if (textDisplayCanvas != null)
            {
                textDisplayCanvas.SetActive(false);
            }
            isShowingText = false;
        }

        /// <summary>
        /// Highlight the object when player is nearby
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : originalColor;
            }
            else if (meshRenderer != null)
            {
                meshRenderer.material.color = highlighted ? highlightColor : originalColor;
            }
        }

        /// <summary>
        /// Create the UI canvas for displaying text
        /// </summary>
        private void CreateTextDisplayUI()
        {
            // Ensure EventSystem exists
            EnsureEventSystem();

            // Create Canvas
            textDisplayCanvas = new GameObject("TextDisplayCanvas");
            Canvas canvas = textDisplayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High sorting order to appear on top
            
            CanvasScaler scaler = textDisplayCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            textDisplayCanvas.AddComponent<GraphicRaycaster>();

            // Create Panel (background)
            textPanel = new GameObject("TextPanel");
            textPanel.transform.SetParent(textDisplayCanvas.transform, false);
            
            RectTransform panelRect = textPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = textBoxSize;
            
            Image panelImage = textPanel.AddComponent<Image>();
            panelImage.color = backgroundColor;

            // Create Text
            GameObject textObject = new GameObject("DisplayText");
            textObject.transform.SetParent(textPanel.transform, false);
            
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.2f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Sample Text";
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.enableWordWrapping = true;

            // Create Close Button
            GameObject buttonObject = new GameObject("CloseButton");
            buttonObject.transform.SetParent(textPanel.transform, false);
            
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.1f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(120, 40);
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            closeButton = buttonObject.AddComponent<Button>();
            closeButton.onClick.AddListener(() => {
                HideText();
            });

            // Button Text
            GameObject buttonTextObject = new GameObject("ButtonText");
            buttonTextObject.transform.SetParent(buttonObject.transform, false);
            
            RectTransform buttonTextRect = buttonTextObject.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Close";
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            // Start hidden
            textDisplayCanvas.SetActive(false);
            
            // Make it persist across scenes (optional)
            DontDestroyOnLoad(textDisplayCanvas);
        }

        /// <summary>
        /// Ensure an EventSystem exists for UI interactions
        /// </summary>
        private void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            
            if (eventSystem == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
                
                #if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                #else
                eventSystemObj.AddComponent<StandaloneInputModule>();
                #endif
                
                DontDestroyOnLoad(eventSystemObj);
                Debug.Log("[TextDisplayObject] Created EventSystem for UI interactions");
            }
        }

        public string GetObjectId()
        {
            return objectId;
        }

        private void OnDestroy()
        {
            // Clean up
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.UnregisterObject(objectId);
            }
        }
    }
}
