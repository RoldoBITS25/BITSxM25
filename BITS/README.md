# Multiplayer Game - Unity Client

A Unity multiplayer game supporting 2 active players and spectators with real-time synchronization via FastAPI backend.

## Features

- **2 Player + Spectator Support**: First 2 players to join are active players, additional players become spectators
- **Player Actions**:
  - **Move**: WASD movement with network synchronization
  - **Grab**: Pick up and hold objects (E key)
  - **Cut**: Split objects into pieces (C key)
  - **Break**: Destroy objects with effects (B key)
- **Spectator Mode**: Free camera movement to observe the game
- **Real-time Synchronization**: WebSocket-based multiplayer updates
- **Room System**: Create/join rooms with optional passwords

## Project Structure

```
Assets/
├── _Scripts/
│   ├── NetworkManager.cs          # API & WebSocket communication
│   ├── WebSocketClient.cs         # WebSocket client implementation
│   ├── GameStateManager.cs        # Game state synchronization
│   ├── PlayerController.cs        # Player movement & actions
│   ├── InteractableObject.cs      # Interactable object base class
│   ├── SpectatorCamera.cs         # Spectator camera controller
│   └── UI/
│       ├── RoomUI.cs              # Room browser & lobby UI
│       └── PlayerActionUI.cs      # In-game action UI
├── Scenes/
│   ├── MainMenu.unity             # Room selection scene
│   └── GameScene.unity            # Main gameplay scene
└── Resources/
    ├── Player.prefab              # Player prefab
    └── InteractableBox.prefab     # Example interactable object
```

## Setup Instructions

### 1. Unity Configuration

1. **Unity Version**: 2021.3 or later
2. **Required Packages**:
   - Input System (com.unity.inputsystem)
   - TextMeshPro (com.unity.textmeshpro)

### 2. Input System Setup

Create an Input Actions asset with the following actions:

**Player Actions:**
- `Move` (Vector2) - WASD keys
- `Grab` (Button) - E key
- `Cut` (Button) - C key
- `Break` (Button) - B key

**Spectator Actions:**
- `Move` (Vector2) - WASD keys
- `Look` (Vector2) - Mouse Delta
- `Zoom` (Float) - Mouse Scroll
- `FastMove` (Button) - Left Shift
- `ToggleFollow` (Button) - F key

### 3. Scene Setup

#### MainMenu Scene:
1. Create a Canvas with RoomUI component
2. Add UI elements:
   - Room browser panel with room list
   - Create room panel with input fields
   - Lobby panel with player list

#### GameScene Scene:
1. Add NetworkManager GameObject
2. Add GameStateManager GameObject with:
   - Player prefab reference
   - Spectator camera prefab reference
   - Player spawn points
3. Add PlayerActionUI Canvas

### 4. Backend Configuration

Update the server URL in NetworkManager:
```csharp
[SerializeField] private string serverUrl = "http://127.0.0.1:8000";
[SerializeField] private string wsUrl = "ws://127.0.0.1:8000";
```

## Backend Requirements

The Unity client expects the following API endpoints:

### REST API:
- `POST /api/rooms/` - Create room
- `GET /api/rooms/` - List rooms
- `GET /api/rooms/{room_id}` - Get room details
- `POST /api/rooms/{room_id}/join` - Join room
- `POST /api/rooms/{room_id}/leave` - Leave room

### WebSocket:
- `WS /ws/{room_id}/{player_id}` - Real-time game updates

**Message Types:**
- `player_action` - Player action events (move, grab, cut, break)
- `game_state` - Full game state sync
- `player_joined` - Player joined notification
- `player_left` - Player left notification

See `BACKEND_GUIDE.md` for implementation details.

## Usage

### Creating a Room:
1. Launch the game
2. Click "Create Room"
3. Enter room name and settings
4. Click "Create" to host

### Joining a Room:
1. Click "Refresh" to see available rooms
2. Click "Join" on desired room
3. Enter password if private

### Playing:
- **Players 1 & 2**: Use WASD to move, E to grab, C to cut, B to break objects
- **Spectators**: Use WASD to fly, Mouse to look, F to follow players

## Player Roles

- **Player 1** (Blue): First player to join/create room
- **Player 2** (Red): Second player to join
- **Spectators** (Gray): All subsequent players

## Networking

### Action Synchronization:
All player actions are sent to the server and broadcast to other clients:
```csharp
NetworkManager.Instance.SendGrabAction(objectId);
NetworkManager.Instance.SendCutAction(objectId, cutPosition);
NetworkManager.Instance.SendBreakAction(objectId);
```

### Position Updates:
Player positions are sent every 10 frames to reduce bandwidth.

## Troubleshooting

### WebSocket Connection Issues:
- Ensure backend WebSocket endpoint is running
- Check firewall settings
- Verify URL configuration in NetworkManager

### Player Not Spawning:
- Check GameStateManager has player prefab assigned
- Verify spawn points are set
- Check NetworkManager is in the scene

### Actions Not Working:
- Verify Input System is properly configured
- Check InteractableObject has correct layer
- Ensure player is not a spectator

## Development Notes

### Adding New Actions:
1. Add action to `PlayerController.cs`
2. Add network method to `NetworkManager.cs`
3. Add handler to `GameStateManager.cs`
4. Update backend to handle new action type

### Creating Custom Interactables:
Inherit from `InteractableObject` and override action methods:
```csharp
public class CustomObject : InteractableObject
{
    public override void OnCut(Vector3 cutPosition)
    {
        // Custom cut behavior
        base.OnCut(cutPosition);
    }
}
```

## License

MIT License - See LICENSE file for details
