using UnityEngine;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Example script demonstrating how to use the InputText component
    /// </summary>
    public class InputTextExample : MonoBehaviour
    {
        private InputText inputText;
        
        private void Start()
        {
            // Create a new GameObject with InputText component
            GameObject inputObj = new GameObject("MyInputText");
            inputObj.transform.SetParent(transform, false);
            
            // Add the InputText component
            inputText = inputObj.AddComponent<InputText>();
            
            // Configure the input text
            inputText.SetPlaceholder("Type something and press Enter...");
            inputText.SetSize(new Vector2(400, 50));
            inputText.SetBackgroundColor(new Color(0.2f, 0.2f, 0.3f, 0.9f));
            
            // Subscribe to the OnSubmit event
            inputText.OnSubmit.AddListener(OnTextSubmitted);
            
            // Subscribe to the OnValueChanged event (optional)
            inputText.OnValueChanged.AddListener(OnTextChanged);
        }
        
        /// <summary>
        /// Called when the user presses Enter in the input field
        /// </summary>
        private void OnTextSubmitted(string text)
        {
            Debug.Log($"Text submitted: {text}");
            
            // Do something with the submitted text
            ProcessInput(text);
            
            // Clear the input field after submission
            inputText.Clear();
        }
        
        /// <summary>
        /// Called whenever the text changes
        /// </summary>
        private void OnTextChanged(string text)
        {
            Debug.Log($"Text changed: {text}");
        }
        
        /// <summary>
        /// Process the submitted input
        /// </summary>
        private void ProcessInput(string input)
        {
            // Example: Send a chat message, execute a command, etc.
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("Input is empty!");
                return;
            }
            
            // Your custom logic here
            Debug.Log($"Processing input: {input}");
        }
    }
}
