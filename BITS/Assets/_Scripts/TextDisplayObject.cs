using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that display parametrized text when interacted with
    /// Implements IReadable for consistent interaction handling
    /// Reading interactions are local-only and do not send data to the backend
    /// </summary>
    public class TextDisplayObject : MonoBehaviour, IReadable
    {
        [Header("Text Display Settings")]
        [SerializeField] [TextArea(3, 10)] private string displayText = "Hello, World!";
        [SerializeField] private Color highlightColor = Color.green;
        [SerializeField] private float displayDuration = 3f; // How long to show the text (0 = until dismissed)
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 textBoxSize = new Vector2(600, 400);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 16;

        public bool isNote = false;
        public int bookId = 0;
        private SpriteRenderer spriteRenderer;
        private MeshRenderer meshRenderer;
        private Color originalColor;
        private string objectId;
        private bool isShowingText = false;
        
        // UI References
        private static GameObject textDisplayCanvas;
        private static GameObject textPanel;
        private static TextMeshProUGUI textComponent;

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

            // Wait for enigma data to be loaded, then load text
            StartCoroutine(WaitAndLoadText());
            
            // this.OnInteract("player1");
        }

        private IEnumerator WaitAndLoadText()
        {
            Debug.Log($"[TextDisplayObject] {gameObject.name} waiting for enigma data...");
            
            // Wait until EnigmaManager has loaded the enigma data
            while (EnigmaManager.Instance.GetCurrentEnigma() == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"[TextDisplayObject] {gameObject.name} enigma data loaded, loading text now...");
            
            // Now load the text
            LoadTextFromEnigmaManager();
        }

        /// <summary>
        /// Load the appropriate text from EnigmaManager based on isNote and bookId
        /// </summary>
        public void LoadTextFromEnigmaManager()
        {
            Debug.Log($"[TextDisplayObject] LoadTextFromEnigmaManager called for {gameObject.name} (isNote={isNote}, bookId={bookId})");
            
            if (EnigmaManager.Instance == null)
            {
                Debug.LogWarning($"[TextDisplayObject] EnigmaManager not found for {gameObject.name}");
                return;
            }

            if (isNote)
            {
                // This is the note/enigma
                string enigmaText = EnigmaManager.Instance.GetEnigmaText();
                Debug.Log($"[TextDisplayObject] Retrieved enigma text: '{enigmaText}'");
                
                if (!string.IsNullOrEmpty(enigmaText))
                {
                    displayText = enigmaText;
                    Debug.Log($"[TextDisplayObject] ✓ Set displayText for Note to: '{displayText}'");
                }
                else
                {
                    Debug.LogWarning($"[TextDisplayObject] No enigma text available for Note");
                }
            }
            else
            {
                // This is a book
                string bookTitle = EnigmaManager.Instance.GetBookTitle(bookId);
                string bookContent = EnigmaManager.Instance.GetBookContent(bookId);
                
                Debug.Log($"[TextDisplayObject] Retrieved book {bookId} - Title: '{bookTitle}', Content length: {bookContent?.Length ?? 0}");
                
                if (!string.IsNullOrEmpty(bookTitle) && !string.IsNullOrEmpty(bookContent))
                {
                    displayText = $"<b>{bookTitle}</b>\n\n{bookContent}";
                    Debug.Log($"[TextDisplayObject] ✓ Set displayText for Book {bookId} to: '{displayText.Substring(0, Math.Min(100, displayText.Length))}...'");
                }
                else
                {
                    Debug.LogWarning($"[TextDisplayObject] No content available for Book {bookId}");
                }
            }
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
        /// Display text with an optional color
        /// </summary>
        public void DisplayText(string text, Color color)
        {
            displayText = text;
            if (textComponent != null)
            {
                textComponent.color = color;
            }
            ShowText();
        }

        /// <summary>
        /// Display text with default color
        /// </summary>
        public void DisplayText(string text)
        {
            DisplayText(text, textColor);
        }

        /// <summary>
        /// Show the text display UI
        /// </summary>
        private void ShowText()
        {
            if (textDisplayCanvas == null) return;

            isShowingText = true;
            textDisplayCanvas.SetActive(true);
            
            Debug.Log($"[TextDisplayObject] ShowText() called for {gameObject.name}, displayText = '{displayText}'");
            
            if (textComponent != null)
            {
                textComponent.text = displayText;
                Debug.Log($"[TextDisplayObject] Set textComponent.text to: '{textComponent.text}'");
            }
            else
            {
                Debug.LogWarning($"[TextDisplayObject] textComponent is null!");
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
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = "Sample Text";
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.TopLeft;
            textComponent.enableWordWrapping = true;

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
