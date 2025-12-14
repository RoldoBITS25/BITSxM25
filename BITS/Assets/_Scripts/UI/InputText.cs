using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// A reusable input text component that can be interacted with and triggers an action on submit
    /// </summary>
    public class InputText : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Vector2 size = new Vector2(300, 40);
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color placeholderColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private int fontSize = 20;
        
        [Header("Input Settings")]
        [SerializeField] private string placeholderText = "Enter text...";
        [SerializeField] private TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
        [SerializeField] private int characterLimit = 0; // 0 = no limit
        
        [Header("Submit Settings")]
        [SerializeField] private bool submitOnEnter = true;
        
        [Header("Events")]
        public UnityEvent<string> OnSubmit = new UnityEvent<string>();
        public UnityEvent<string> OnValueChanged = new UnityEvent<string>();
        
        private TMP_InputField inputField;
        private GameObject inputObject;
        
        /// <summary>
        /// Gets or sets the current text value
        /// </summary>
        public string Text
        {
            get => inputField != null ? inputField.text : "";
            set
            {
                if (inputField != null)
                    inputField.text = value;
            }
        }
        
        /// <summary>
        /// Gets the TMP_InputField component
        /// </summary>
        public TMP_InputField InputField => inputField;
        
        private void Awake()
        {
            CreateInputField();
        }
        
        /// <summary>
        /// Creates the input field UI at runtime
        /// </summary>
        private void CreateInputField()
        {
            // Create main input object
            inputObject = new GameObject("InputField");
            inputObject.transform.SetParent(transform, false);
            
            RectTransform rect = inputObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            
            // Background
            Image bg = inputObject.AddComponent<Image>();
            bg.color = backgroundColor;
            
            // Input Field Component
            inputField = inputObject.AddComponent<TMP_InputField>();
            inputField.contentType = contentType;
            inputField.characterLimit = characterLimit;
            
            // Text Area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObject.transform, false);
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
            placeholderRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.text = placeholderText;
            placeholder.fontSize = fontSize;
            placeholder.color = placeholderColor;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Left;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            
            // Assign to input field
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.fontAsset = placeholder.font;
            
            // Setup event listeners
            if (submitOnEnter)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);
            }
            
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }
        
        /// <summary>
        /// Called when the input field is submitted (Enter key pressed)
        /// </summary>
        private void OnInputSubmit(string value)
        {
            OnSubmit?.Invoke(value);
        }
        
        /// <summary>
        /// Called when the input value changes
        /// </summary>
        private void OnInputValueChanged(string value)
        {
            OnValueChanged?.Invoke(value);
        }
        
        /// <summary>
        /// Clears the input field
        /// </summary>
        public void Clear()
        {
            if (inputField != null)
                inputField.text = "";
        }
        
        /// <summary>
        /// Sets focus to the input field
        /// </summary>
        public void Focus()
        {
            if (inputField != null)
                inputField.Select();
        }
        
        /// <summary>
        /// Sets the placeholder text
        /// </summary>
        public void SetPlaceholder(string text)
        {
            placeholderText = text;
            if (inputField != null && inputField.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.text = text;
            }
        }
        
        /// <summary>
        /// Sets the size of the input field
        /// </summary>
        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            if (inputObject != null)
            {
                RectTransform rect = inputObject.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = newSize;
            }
        }
        
        /// <summary>
        /// Sets the background color
        /// </summary>
        public void SetBackgroundColor(Color color)
        {
            backgroundColor = color;
            if (inputObject != null)
            {
                Image bg = inputObject.GetComponent<Image>();
                if (bg != null)
                    bg.color = color;
            }
        }
    }
}
