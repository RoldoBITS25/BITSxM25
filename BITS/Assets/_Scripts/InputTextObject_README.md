# InputTextObject - Interactive Text Input System

## Overview
The `InputTextObject` component allows you to create interactable objects that display a text input popup when the player presses the **E key** near them. The popup appears in world space above the object and disappears after the player submits their text.

## Components Created

### 1. **InputText.cs**
A reusable UI component that creates a TMP_InputField with customizable appearance.

**Location:** `Assets/_Scripts/UI/InputText.cs`

### 2. **InputTextObject.cs**
The main interactable component that shows/hides the input popup.

**Location:** `Assets/_Scripts/InputTextObject.cs`

### 3. **InteractionController.cs** (Updated)
Now detects and interacts with `InputTextObject` components.

## How to Use

### Quick Setup (In Unity Editor)

1. **Create or select a GameObject** in your scene (e.g., a cube, sprite, etc.)
2. **Add a Collider** component if it doesn't have one (required for interaction detection)
3. **Add the `InputTextObject` component** to the GameObject
4. **Configure the settings** in the Inspector:
   - **Placeholder Text**: What shows in the empty input field
   - **Input Size**: Width and height of the input box
   - **Popup Offset**: Position offset from the object (default: 2 units up)
   - **Highlight Color**: Color when player is near

### Programmatic Setup

```csharp
// Add InputTextObject to any GameObject
GameObject myObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
InputTextObject inputObj = myObject.AddComponent<InputTextObject>();

// Subscribe to the text submission event
inputObj.OnTextSubmitted.AddListener((text) => {
    Debug.Log($"Player entered: {text}");
    // Do something with the text
});
```

### Handling Text Submission

```csharp
public class MyCustomHandler : MonoBehaviour
{
    private void Start()
    {
        var inputTextObj = GetComponent<InputTextObject>();
        inputTextObj.OnTextSubmitted.AddListener(OnTextReceived);
    }
    
    private void OnTextReceived(string text)
    {
        // Process the submitted text
        Debug.Log($"Received: {text}");
        
        // Examples:
        // - Send to network: NetworkManager.Instance?.SendChatMessage(text);
        // - Execute command: ExecuteCommand(text);
        // - Update game state: UpdateObjectName(text);
    }
}
```

## Player Interaction

1. **Approach the object** (within 2 units by default)
2. **Press E key** to show the input popup
3. **Type your text** in the input field
4. **Press Enter** to submit
5. The popup automatically closes and your `OnTextSubmitted` event fires

## Example Use Cases

See `InputTextObjectExample.cs` for practical examples:

- **Rename objects**: `/name MyNewName`
- **Execute commands**: `/destroy`, `/color red`
- **Chat/messaging**: Any text without `/` prefix
- **Custom game logic**: Parse and process player input however you need

## Customization

### Change Popup Appearance

```csharp
// In InputTextObject.cs, modify these serialized fields:
[SerializeField] private Vector2 inputSize = new Vector2(400, 50);
[SerializeField] private Vector3 popupOffset = new Vector3(0, 2, 0);
```

### Change Input Field Style

The `InputText` component supports:
- Background color
- Text color
- Placeholder color
- Font size
- Character limit
- Content type (Standard, Password, Email, etc.)

## Integration with Existing Systems

The `InputTextObject` works seamlessly with your existing interaction system:
- ✅ Detected by `InteractionController`
- ✅ Works with any weapon type (no weapon restriction)
- ✅ Registered with `GameStateManager`
- ✅ Can be networked (add custom network calls in `OnTextSubmitted`)

## Notes

- The input popup uses **World Space Canvas** rendering
- The popup appears **above the object** by default (configurable via `popupOffset`)
- **Only one popup** can be active per object at a time
- Pressing **E again** while the popup is open will close it
- The input field is **automatically focused** when the popup appears
