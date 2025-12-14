using UnityEngine;
using UnityEngine.Events;
using MultiplayerGame.UI;

namespace MultiplayerGame
{
    /// <summary>
    /// An interactable object that displays an input text popup when the player interacts with it
    /// </summary>
    public class InputTextObject : MonoBehaviour
    {
        [Header("Object Settings")]
        [SerializeField] private string objectId;
        [SerializeField] private Color highlightColor = Color.cyan;
        
        [Header("Input Popup Settings")]
        [SerializeField] private string placeholderText = "Enter text...";
        [SerializeField] private Vector2 inputSize = new Vector2(400, 50);
        [SerializeField] private Vector3 popupOffset = new Vector3(0, 2, 0); // Offset above the object
        
        [Header("Events")]
        public UnityEvent<string> OnTextSubmitted = new UnityEvent<string>();
        
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private InputText inputTextComponent;
        private GameObject inputPopup;
        private bool isPopupActive = false;
        private Canvas worldCanvas;
        
        public string ObjectId => objectId;
        
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            
            // Generate object ID if not set
            if (string.IsNullOrEmpty(objectId))
            {
                Vector3 pos = transform.position;
                objectId = $"InputTextObject_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
            }
            
            // Register with GameStateManager if available
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RegisterObject(objectId, gameObject);
            }
            this.OnInteract("player1");
        }
        
        /// <summary>
        /// Called when the player interacts with this object
        /// </summary>
        public void OnInteract(string playerId)
        {
            Debug.Log($"[InputTextObject] Player {playerId} interacted with {gameObject.name}");
            
            if (isPopupActive)
            {
                HideInputPopup();
            }
            else
            {
                ShowInputPopup();
            }
        }
        
        /// <summary>
        /// Shows the input text popup
        /// </summary>
        private void ShowInputPopup()
        {
            if (inputPopup != null)
            {
                inputPopup.SetActive(true);
                isPopupActive = true;
                if (inputTextComponent != null)
                {
                    inputTextComponent.Focus();
                }
                return;
            }
            
            // Create world space canvas for the popup
            GameObject canvasObj = new GameObject("InputPopupCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = popupOffset;
            canvasObj.transform.localRotation = Quaternion.identity;
            
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            
            var canvasScaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10;
            
            var graphicRaycaster = canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Set canvas size
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(inputSize.x, inputSize.y);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // Scale down for world space
            
            // Create InputText component
            inputPopup = new GameObject("InputTextPopup");
            inputPopup.transform.SetParent(canvasObj.transform, false);
            
            inputTextComponent = inputPopup.AddComponent<InputText>();
            inputTextComponent.SetPlaceholder(placeholderText);
            inputTextComponent.SetSize(inputSize);
            inputTextComponent.SetBackgroundColor(new Color(0.1f, 0.1f, 0.15f, 0.95f));
            
            // Subscribe to submit event
            inputTextComponent.OnSubmit.AddListener(OnInputSubmitted);
            
            isPopupActive = true;
            
            // Focus the input field
            inputTextComponent.Focus();
            
            Debug.Log($"[InputTextObject] Input popup shown for {gameObject.name}");
        }
        
        /// <summary>
        /// Hides the input text popup
        /// </summary>
        private void HideInputPopup()
        {
            if (inputPopup != null)
            {
                inputPopup.SetActive(false);
            }
            isPopupActive = false;
            
            Debug.Log($"[InputTextObject] Input popup hidden for {gameObject.name}");
        }
        
        /// <summary>
        /// Called when text is submitted in the input field
        /// </summary>
        private void OnInputSubmitted(string text)
        {
            Debug.Log($"[InputTextObject] Text submitted: {text}");
            
            // Invoke the event for custom handling
            OnTextSubmitted?.Invoke(text);
            
            // Clear the input and hide popup
            if (inputTextComponent != null)
            {
                inputTextComponent.Clear();
            }
            HideInputPopup();
            
            // You can send this to the network if needed
            // NetworkManager.Instance?.SendCustomAction(objectId, "text_input", text);
        }
        
        /// <summary>
        /// Sets the highlight state of the object
        /// </summary>
        public void SetHighlight(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : originalColor;
            }
        }
        
        /// <summary>
        /// Sets a custom object ID
        /// </summary>
        public void SetObjectId(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                objectId = id;
                
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.RegisterObject(objectId, gameObject);
                }
            }
        }
        
        private void OnDestroy()
        {
            // Clean up event listeners
            if (inputTextComponent != null)
            {
                inputTextComponent.OnSubmit.RemoveListener(OnInputSubmitted);
            }
        }
    }
}
