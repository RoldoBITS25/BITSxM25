# Input System Configuration

This document describes the Input Actions setup for the multiplayer game.

## Creating the Input Actions Asset

1. In Unity, go to `Assets` → `Create` → `Input Actions`
2. Name it `InputSystem_Actions`
3. Double-click to open the Input Actions editor

## Action Maps

### Player Action Map

Create an action map called **"Player"** with the following actions:

#### Move
- **Type**: Value
- **Control Type**: Vector2
- **Bindings**:
  - WASD Composite:
    - Up: W [Keyboard]
    - Down: S [Keyboard]
    - Left: A [Keyboard]
    - Right: D [Keyboard]
  - Arrow Keys Composite:
    - Up: Up Arrow [Keyboard]
    - Down: Down Arrow [Keyboard]
    - Left: Left Arrow [Keyboard]
    - Right: Right Arrow [Keyboard]
  - Left Stick [Gamepad]

#### Grab
- **Type**: Button
- **Bindings**:
  - E [Keyboard]
  - South Button (A/Cross) [Gamepad]

#### Cut
- **Type**: Button
- **Bindings**:
  - C [Keyboard]
  - West Button (X/Square) [Gamepad]

#### Break
- **Type**: Button
- **Bindings**:
  - B [Keyboard]
  - East Button (B/Circle) [Gamepad]

### Spectator Action Map

Create an action map called **"Spectator"** with the following actions:

#### Move
- **Type**: Value
- **Control Type**: Vector2
- **Bindings**:
  - WASD Composite (same as Player)

#### Look
- **Type**: Value
- **Control Type**: Vector2
- **Bindings**:
  - Mouse Delta [Mouse]
  - Right Stick [Gamepad]

#### Zoom
- **Type**: Value
- **Control Type**: Axis
- **Bindings**:
  - Scroll [Mouse/scroll/y]
  - D-Pad Up/Down [Gamepad]

#### FastMove
- **Type**: Button
- **Bindings**:
  - Left Shift [Keyboard]
  - Left Shoulder [Gamepad]

#### ToggleFollow
- **Type**: Button
- **Bindings**:
  - F [Keyboard]
  - Right Shoulder [Gamepad]

## Setup in Scripts

### PlayerController Setup

1. Add `PlayerInput` component to Player prefab
2. Set Actions to `InputSystem_Actions`
3. Set Default Map to `Player`
4. Set Behavior to `Invoke Unity Events` or `Send Messages`

### SpectatorCamera Setup

1. Add `PlayerInput` component to Spectator Camera prefab
2. Set Actions to `InputSystem_Actions`
3. Set Default Map to `Spectator`
4. Set Behavior to `Invoke Unity Events` or `Send Messages`

## Code Integration

The scripts are already set up to use these actions:

```csharp
// In PlayerController.cs
moveAction = playerInput.actions["Move"];
grabAction = playerInput.actions["Grab"];
cutAction = playerInput.actions["Cut"];
breakAction = playerInput.actions["Break"];

// In SpectatorCamera.cs
moveAction = playerInput.actions["Move"];
lookAction = playerInput.actions["Look"];
zoomAction = playerInput.actions["Zoom"];
fastMoveAction = playerInput.actions["FastMove"];
toggleFollowAction = playerInput.actions["ToggleFollow"];
```

## Testing Input

1. Open the Input Debugger: `Window` → `Analysis` → `Input Debugger`
2. Select your Player or Spectator object in the scene
3. Test each action to verify bindings work correctly

## Custom Bindings

Players can rebind controls at runtime by:
1. Accessing the `InputActionAsset`
2. Using `InputActionRebindingExtensions`
3. Saving bindings to PlayerPrefs

Example rebinding code (optional):
```csharp
var rebindOperation = grabAction.PerformInteractiveRebinding()
    .OnComplete(operation => {
        operation.Dispose();
        // Save binding override
    })
    .Start();
```
