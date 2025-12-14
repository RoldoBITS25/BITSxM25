using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Example script demonstrating how to use the InputTextObject component
    /// This can be attached to any GameObject to make it an interactable input object
    /// </summary>
    public class InputTextObjectExample : MonoBehaviour
    {
        private void Start()
        {
            // Get or add the InputTextObject component
            InputTextObject inputTextObject = GetComponent<InputTextObject>();
            if (inputTextObject == null)
            {
                inputTextObject = gameObject.AddComponent<InputTextObject>();
            }
            
            // Subscribe to the text submission event
            inputTextObject.OnTextSubmitted.AddListener(OnPlayerSubmittedText);
            
            Debug.Log($"[InputTextObjectExample] {gameObject.name} is now an interactable input object!");
        }
        
        /// <summary>
        /// Called when the player submits text through the input popup
        /// </summary>
        private void OnPlayerSubmittedText(string text)
        {
            Debug.Log($"[InputTextObjectExample] Player submitted: '{text}'");
            
            // Example: Use the submitted text for different purposes
            ProcessPlayerInput(text);
        }
        
        /// <summary>
        /// Process the player's input text
        /// </summary>
        private void ProcessPlayerInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("[InputTextObjectExample] Empty input received!");
                return;
            }
            
            // Example use cases:
            
            // 1. Rename this object
            if (input.StartsWith("/name "))
            {
                string newName = input.Substring(6);
                gameObject.name = newName;
                Debug.Log($"[InputTextObjectExample] Object renamed to: {newName}");
            }
            // 2. Execute a command
            else if (input.StartsWith("/"))
            {
                ExecuteCommand(input.Substring(1));
            }
            // 3. Send a message to other players
            else
            {
                BroadcastMessage(input);
            }
        }
        
        /// <summary>
        /// Execute a command from the input
        /// </summary>
        private void ExecuteCommand(string command)
        {
            Debug.Log($"[InputTextObjectExample] Executing command: {command}");
            
            // Example commands
            switch (command.ToLower())
            {
                case "destroy":
                    Debug.Log("[InputTextObjectExample] Destroying object...");
                    Destroy(gameObject);
                    break;
                    
                case "color red":
                    ChangeColor(Color.red);
                    break;
                    
                case "color blue":
                    ChangeColor(Color.blue);
                    break;
                    
                case "color green":
                    ChangeColor(Color.green);
                    break;
                    
                default:
                    Debug.LogWarning($"[InputTextObjectExample] Unknown command: {command}");
                    break;
            }
        }
        
        /// <summary>
        /// Change the color of this object
        /// </summary>
        private void ChangeColor(Color color)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = color;
                Debug.Log($"[InputTextObjectExample] Color changed to {color}");
            }
        }
        
        /// <summary>
        /// Broadcast a message to other players (example)
        /// </summary>
        private void BroadcastMessage(string message)
        {
            Debug.Log($"[InputTextObjectExample] Broadcasting message: {message}");
            
            // In a real implementation, you would send this through NetworkManager
            // NetworkManager.Instance?.SendChatMessage(message);
        }
    }
}
