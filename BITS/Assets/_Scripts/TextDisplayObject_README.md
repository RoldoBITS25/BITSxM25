# TextDisplayObject - Usage Guide

## Overview
`TextDisplayObject` is a component that displays parametrized text in a popup UI when a player interacts with it.

## Features
- ✅ Parametrized text display
- ✅ Customizable UI appearance (colors, size, font)
- ✅ Auto-dismiss after duration (optional)
- ✅ Object highlighting when nearby
- ✅ Works with any weapon equipped
- ✅ Network-ready (can be extended for multiplayer)

## How to Use

### 1. Add Component to GameObject
1. Create or select a GameObject in your scene (e.g., a cube, sign, or NPC)
2. Add the `TextDisplayObject` component to it
3. Ensure the object has a Collider (for interaction detection)

### 2. Configure Settings

#### Text Display Settings
- **Display Text**: The text to show when interacted with (supports multi-line)
- **Highlight Color**: Color to highlight the object when player is nearby
- **Display Duration**: How long to show the text (0 = until manually closed)

#### UI Settings
- **Text Box Size**: Size of the popup window (default: 400x200)
- **Background Color**: Color of the popup background
- **Text Color**: Color of the displayed text
- **Font Size**: Size of the text font

### 3. Interaction
- Player walks near the object
- Press **E** to interact
- Text popup appears on screen
- Click "Close" button or wait for auto-dismiss

## Example Setup

### Simple Sign
```csharp
// In Unity Inspector:
Display Text: "Welcome to the game!\nPress SPACE to change weapons."
Highlight Color: Yellow
Display Duration: 3
```

### Quest Giver NPC
```csharp
// In Unity Inspector:
Display Text: "Greetings, traveler!\n\nI need your help collecting 5 crystals.\nWill you accept this quest?"
Display Duration: 0 (manual close)
Background Color: Dark Blue
Text Color: Gold
```

### Programmatic Usage
```csharp
// Get the component
TextDisplayObject textDisplay = GetComponent<TextDisplayObject>();

// Set custom text at runtime
textDisplay.SetDisplayText("Dynamic text based on game state!");

// Manually trigger interaction
textDisplay.OnInteract(playerId);

// Manually hide the popup
textDisplay.HideText();
```

## Integration with Existing Systems

The `TextDisplayObject` automatically integrates with:
- **InteractionController**: Detects 'E' key press when player is nearby
- **GameStateManager**: Registers object for state management
- **NetworkManager**: Ready for multiplayer synchronization (extend as needed)

## Notes
- The text display UI is created once and shared across all TextDisplayObject instances
- The UI persists across scene loads (DontDestroyOnLoad)
- TextDisplayObject interaction takes priority over weapon-based interactions
- Objects can have both TextDisplayObject and other interaction components (GrabbableObject, etc.)
