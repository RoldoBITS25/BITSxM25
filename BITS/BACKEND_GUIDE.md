# Backend Implementation Guide

This guide explains how to extend your FastAPI backend to support the Unity multiplayer game.

## Required Additions

### 1. Player Action Models

Create `app/models/player_actions.py`:

```python
from pydantic import BaseModel
from typing import Optional, Dict, Any
from datetime import datetime

class PlayerAction(BaseModel):
    player_id: str
    action_type: str  # "move", "grab", "cut", "break"
    target_object_id: Optional[str] = None
    position: Optional[Dict[str, float]] = None  # {x, y, z}
    timestamp: str
    data: Optional[Dict[str, Any]] = None

class ObjectState(BaseModel):
    object_id: str
    object_type: str
    position: Dict[str, float]  # {x, y, z}
    rotation: Dict[str, float]  # {x, y, z, w}
    is_grabbed: bool = False
    grabbed_by_player_id: Optional[str] = None

class PlayerState(BaseModel):
    player_id: str
    position: Dict[str, float]
    held_object_id: Optional[str] = None

class GameState(BaseModel):
    room_id: str
    players: list[PlayerState]
    objects: list[ObjectState]
```

### 2. Game State Manager

Create `app/services/game_state.py`:

```python
from typing import Dict, List
from app.models.player_actions import GameState, PlayerState, ObjectState, PlayerAction

class GameStateManager:
    def __init__(self):
        self.game_states: Dict[str, GameState] = {}
        self.player_positions: Dict[str, Dict[str, float]] = {}
        self.object_states: Dict[str, ObjectState] = {}
    
    def get_or_create_game_state(self, room_id: str) -> GameState:
        """Get existing game state or create new one"""
        if room_id not in self.game_states:
            self.game_states[room_id] = GameState(
                room_id=room_id,
                players=[],
                objects=[]
            )
        return self.game_states[room_id]
    
    def update_player_position(self, room_id: str, player_id: str, position: Dict[str, float]):
        """Update player position"""
        state = self.get_or_create_game_state(room_id)
        
        # Find or create player state
        player_state = next((p for p in state.players if p.player_id == player_id), None)
        if player_state:
            player_state.position = position
        else:
            state.players.append(PlayerState(player_id=player_id, position=position))
    
    def handle_grab_action(self, room_id: str, player_id: str, object_id: str):
        """Handle object grab"""
        state = self.get_or_create_game_state(room_id)
        
        # Update object state
        obj = next((o for o in state.objects if o.object_id == object_id), None)
        if obj:
            obj.is_grabbed = True
            obj.grabbed_by_player_id = player_id
        
        # Update player state
        player = next((p for p in state.players if p.player_id == player_id), None)
        if player:
            player.held_object_id = object_id
    
    def handle_break_action(self, room_id: str, object_id: str):
        """Handle object break"""
        state = self.get_or_create_game_state(room_id)
        
        # Remove object from state
        state.objects = [o for o in state.objects if o.object_id != object_id]
    
    def handle_cut_action(self, room_id: str, object_id: str, cut_position: Dict[str, float]):
        """Handle object cut - creates two new objects"""
        state = self.get_or_create_game_state(room_id)
        
        # Find original object
        original = next((o for o in state.objects if o.object_id == object_id), None)
        if not original:
            return
        
        # Remove original
        state.objects = [o for o in state.objects if o.object_id != object_id]
        
        # Create two pieces (simplified - adjust positions based on cut)
        piece1 = ObjectState(
            object_id=f"{object_id}_piece1",
            object_type=original.object_type,
            position=original.position,
            rotation=original.rotation
        )
        piece2 = ObjectState(
            object_id=f"{object_id}_piece2",
            object_type=original.object_type,
            position=cut_position,
            rotation=original.rotation
        )
        
        state.objects.extend([piece1, piece2])
    
    def add_object(self, room_id: str, obj: ObjectState):
        """Add new object to game state"""
        state = self.get_or_create_game_state(room_id)
        state.objects.append(obj)
    
    def remove_player(self, room_id: str, player_id: str):
        """Remove player from game state"""
        if room_id in self.game_states:
            state = self.game_states[room_id]
            state.players = [p for p in state.players if p.player_id != player_id]

# Global instance
game_state_manager = GameStateManager()
```

### 3. WebSocket Endpoint

Create `app/api/websocket.py`:

```python
from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from typing import Dict, Set
import json
from app.services.game_state import game_state_manager
from app.models.player_actions import PlayerAction

router = APIRouter()

# Connection manager
class ConnectionManager:
    def __init__(self):
        self.active_connections: Dict[str, Dict[str, WebSocket]] = {}
    
    async def connect(self, room_id: str, player_id: str, websocket: WebSocket):
        await websocket.accept()
        if room_id not in self.active_connections:
            self.active_connections[room_id] = {}
        self.active_connections[room_id][player_id] = websocket
    
    def disconnect(self, room_id: str, player_id: str):
        if room_id in self.active_connections:
            self.active_connections[room_id].pop(player_id, None)
            if not self.active_connections[room_id]:
                del self.active_connections[room_id]
    
    async def broadcast(self, room_id: str, message: dict, exclude_player: str = None):
        if room_id in self.active_connections:
            for player_id, connection in self.active_connections[room_id].items():
                if player_id != exclude_player:
                    try:
                        await connection.send_json(message)
                    except:
                        pass

manager = ConnectionManager()

@router.websocket("/ws/{room_id}/{player_id}")
async def websocket_endpoint(websocket: WebSocket, room_id: str, player_id: str):
    await manager.connect(room_id, player_id, websocket)
    
    try:
        # Send current game state to new connection
        game_state = game_state_manager.get_or_create_game_state(room_id)
        await websocket.send_json({
            "type": "game_state",
            "data": game_state.dict()
        })
        
        # Notify others of new player
        await manager.broadcast(room_id, {
            "type": "player_joined",
            "data": {"player_id": player_id}
        }, exclude_player=player_id)
        
        # Listen for messages
        while True:
            data = await websocket.receive_text()
            action = PlayerAction.parse_raw(data)
            
            # Handle action
            if action.action_type == "move":
                game_state_manager.update_player_position(
                    room_id, action.player_id, action.position
                )
            elif action.action_type == "grab":
                game_state_manager.handle_grab_action(
                    room_id, action.player_id, action.target_object_id
                )
            elif action.action_type == "cut":
                game_state_manager.handle_cut_action(
                    room_id, action.target_object_id, action.position
                )
            elif action.action_type == "break":
                game_state_manager.handle_break_action(
                    room_id, action.target_object_id
                )
            
            # Broadcast action to all clients
            await manager.broadcast(room_id, {
                "type": "player_action",
                "data": action.dict()
            })
    
    except WebSocketDisconnect:
        manager.disconnect(room_id, player_id)
        game_state_manager.remove_player(room_id, player_id)
        
        # Notify others of player leaving
        await manager.broadcast(room_id, {
            "type": "player_left",
            "data": {"player_id": player_id}
        })
```

### 4. Update Main App

In `app/main.py`, add the WebSocket router:

```python
from app.api import websocket

app.include_router(websocket.router)
```

### 5. Room Model Extension

Update your Room model to track player roles:

```python
class Room(BaseModel):
    # ... existing fields ...
    player_roles: Dict[str, str] = {}  # player_id -> "player1", "player2", "spectator"
    
    def assign_role(self, player_id: str) -> str:
        """Assign role to player based on join order"""
        if len([r for r in self.player_roles.values() if r.startswith("player")]) < 2:
            role = f"player{len([r for r in self.player_roles.values() if r.startswith('player')]) + 1}"
        else:
            role = "spectator"
        self.player_roles[player_id] = role
        return role
```

## Testing

### 1. Test WebSocket Connection:
```bash
# Install wscat
npm install -g wscat

# Connect to WebSocket
wscat -c ws://localhost:8000/ws/test_room/test_player
```

### 2. Test Player Actions:
```json
{
  "player_id": "test_player",
  "action_type": "move",
  "position": {"x": 1.0, "y": 0.0, "z": 1.0},
  "timestamp": "2024-01-01T12:00:00"
}
```

### 3. Run Backend:
```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

## Security Considerations

1. **Authentication**: Add JWT tokens for player authentication
2. **Rate Limiting**: Limit action frequency to prevent spam
3. **Validation**: Validate all action data before processing
4. **Room Passwords**: Hash passwords before storage

## Performance Tips

1. **Batch Updates**: Send game state updates at fixed intervals (e.g., 20 times/second)
2. **Delta Compression**: Only send changed data
3. **Action Validation**: Validate actions server-side to prevent cheating
4. **Connection Limits**: Limit connections per room

## Deployment

For production deployment:
1. Use WSS (WebSocket Secure) instead of WS
2. Add proper CORS configuration
3. Use Redis for game state persistence
4. Add logging and monitoring
5. Implement reconnection logic

## Example Full Implementation

See the complete example in the `backend_example/` directory for a working implementation with all features.
