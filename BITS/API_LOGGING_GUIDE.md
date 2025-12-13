# API Logging Guide

## Overview

I've added comprehensive logging to the NetworkManager to help you debug API and connection issues. All logs are prefixed with `[NetworkManager]` for easy filtering.

## Log Symbols

- ✓ (checkmark) = Success
- ✗ (X) = Error
- ⚠ (warning) = Warning

## What to Look For

### 1. Initialization Logs

When Unity starts, you should see:

```
[NetworkManager] ========== NetworkManager Initializing ==========
[NetworkManager] ✓ Initialized as singleton
[NetworkManager] Player ID: player_abc12345
[NetworkManager] Server URL: http://127.0.0.1:8000
[NetworkManager] WebSocket URL: ws://127.0.0.1:8000
```

**If you see:**
- "Duplicate instance detected" → You have multiple NetworkManager objects in scene (remove extras)
- Nothing → NetworkManager is not in the scene or disabled

### 2. Creating a Room

```
[NetworkManager] Creating room: MyRoom, MaxPlayers: 4, Private: False
[NetworkManager] POST http://127.0.0.1:8000/api/rooms/?host_player_id=player_abc12345
[NetworkManager] Request body: {"name":"MyRoom","max_players":4,"is_private":false,"password":null}
[NetworkManager] Response Code: 200
[NetworkManager] Response: {"room_id":"room_xyz","name":"MyRoom",...}
[NetworkManager] ✓ Room created successfully!
[NetworkManager] Room ID: room_xyz, Player Number: 1
[NetworkManager] Connecting to WebSocket: ws://127.0.0.1:8000/ws/room_xyz/player_abc12345
[NetworkManager] ✓ WebSocket connected
```

**Common Issues:**

#### Response Code: 0
```
[NetworkManager] Response Code: 0
[NetworkManager] ✗ Failed to create room: ... (Code: 0)
```
**Problem:** Cannot reach the server
**Solutions:**
- Check if backend is running: `uvicorn app.main:app --reload`
- Verify server URL is correct (http://127.0.0.1:8000)
- Check firewall settings
- Try pinging the server

#### Response Code: 404
```
[NetworkManager] Response Code: 404
[NetworkManager] ✗ Failed to create room: ... (Code: 404)
```
**Problem:** Endpoint not found
**Solutions:**
- Backend may not have the `/api/rooms/` endpoint
- Check backend implementation
- Verify API routes are registered

#### Response Code: 500
```
[NetworkManager] Response Code: 500
[NetworkManager] Response body: {"detail":"Internal server error"}
```
**Problem:** Server error
**Solutions:**
- Check backend logs for errors
- Verify database is running (if used)
- Check backend dependencies

### 3. Getting Room List

```
[NetworkManager] GET http://127.0.0.1:8000/api/rooms/?include_private=False
[NetworkManager] Response Code: 200
[NetworkManager] ✓ Room list received: [{"room_id":"room_xyz",...}]
[NetworkManager] Found 1 rooms
```

**If no rooms found:**
```
[NetworkManager] Found 0 rooms
```
This is normal if no rooms have been created yet.

### 4. Joining a Room

```
[NetworkManager] Joining room: room_xyz
[NetworkManager] POST http://127.0.0.1:8000/api/rooms/room_xyz/join?player_id=player_abc12345
[NetworkManager] Response Code: 200
[NetworkManager] Response: {"message":"Joined successfully"}
[NetworkManager] ✓ Successfully joined room room_xyz
[NetworkManager] GET http://127.0.0.1:8000/api/rooms/room_xyz
[NetworkManager] Response Code: 200
[NetworkManager] Response: {"room_id":"room_xyz","current_players":["player_1","player_abc12345"],...}
[NetworkManager] Player index in room: 1 / 2
[NetworkManager] ✓ Assigned as Player 2
[NetworkManager] Connecting to WebSocket: ws://127.0.0.1:8000/ws/room_xyz/player_abc12345
[NetworkManager] ✓ WebSocket connected
```

**Player Assignment:**
- Player index 0 → Player 1
- Player index 1 → Player 2
- Player index 2+ → Spectator

### 5. WebSocket Messages

```
[NetworkManager] WebSocket message received: {"type":"player_action","data":"{...}"}
[NetworkManager] Message type: player_action
[NetworkManager] Player action: move from player_def67890
```

**Message Types:**
- `player_action` → Another player did something (move, grab, cut, break)
- `game_state` → Full game state update
- `player_joined` → Someone joined the room
- `player_left` → Someone left the room

### 6. Sending Actions

```
[NetworkManager] Sending action: move, Object: null
[NetworkManager] Action JSON: {"player_id":"player_abc12345","action_type":"move",...}
```

**If you see:**
```
[NetworkManager] Cannot send action: IsPlayer=False, isConnected=True
```
**Problem:** You're a spectator, not a player
**Solution:** Join as one of the first 2 players

```
[NetworkManager] Cannot send action: IsPlayer=True, isConnected=False
```
**Problem:** Not connected to WebSocket
**Solution:** Make sure you joined a room successfully

### 7. Leaving a Room

```
[NetworkManager] Leaving room: room_xyz
[NetworkManager] POST http://127.0.0.1:8000/api/rooms/room_xyz/leave?player_id=player_abc12345
[NetworkManager] Response Code: 200
[NetworkManager] ✓ Left room successfully
[NetworkManager] Disconnecting WebSocket
[NetworkManager] ✓ WebSocket disconnected
```

## Filtering Logs in Unity

To see only NetworkManager logs:

1. Open Console window
2. Click the search box
3. Type: `[NetworkManager]`
4. Only NetworkManager logs will show

## Common Error Patterns

### Backend Not Running

```
[NetworkManager] Response Code: 0
[NetworkManager] ✗ Failed to create room: ... (Code: 0)
```

**Fix:** Start your backend:
```bash
cd /path/to/backend
uvicorn app.main:app --reload
```

### Wrong Server URL

```
[NetworkManager] Server URL: http://localhost:8000
[NetworkManager] Response Code: 0
```

**Fix:** Change to `http://127.0.0.1:8000` in NetworkManager Inspector

### CORS Issues (WebGL builds)

```
[NetworkManager] Response Code: 0
Access to XMLHttpRequest blocked by CORS policy
```

**Fix:** Add CORS middleware to your FastAPI backend

### WebSocket Connection Failed

```
[NetworkManager] WebSocket error: Connection refused
```

**Fix:** 
- Check backend WebSocket endpoint is running
- Verify WebSocket URL is correct
- Check firewall allows WebSocket connections

## Testing Checklist

Use this checklist to verify everything is working:

### Backend Check
- [ ] Backend is running (`uvicorn app.main:app --reload`)
- [ ] Can access http://127.0.0.1:8000/docs in browser
- [ ] API endpoints are visible in Swagger docs

### Unity Check
- [ ] NetworkManager is in scene
- [ ] Server URL is `http://127.0.0.1:8000`
- [ ] WebSocket URL is `ws://127.0.0.1:8000`
- [ ] No duplicate NetworkManager instances

### Connection Test
- [ ] See initialization logs on Play
- [ ] Can create room (Response Code: 200)
- [ ] WebSocket connects successfully
- [ ] Can see room in room list

### Multiplayer Test
- [ ] First player becomes Player 1
- [ ] Second player becomes Player 2
- [ ] Third player becomes Spectator
- [ ] Actions are sent and received

## Advanced Debugging

### Enable Verbose WebSocket Logs

Check the WebSocketClient.cs file and ensure it has logging enabled.

### Network Traffic Monitoring

Use tools like:
- **Wireshark** - Monitor all network traffic
- **Fiddler** - HTTP/HTTPS proxy debugger
- **Browser DevTools** - For WebGL builds

### Backend Logs

Check your FastAPI backend logs:
```bash
# Terminal running uvicorn
INFO:     127.0.0.1:xxxxx - "POST /api/rooms/ HTTP/1.1" 200 OK
```

## Quick Diagnosis

| Symptom | Likely Cause | Quick Fix |
|---------|-------------|-----------|
| Response Code: 0 | Backend not running | Start backend |
| Response Code: 404 | Wrong endpoint | Check backend routes |
| Response Code: 500 | Server error | Check backend logs |
| No initialization logs | NetworkManager missing | Add to scene |
| Duplicate instance warning | Multiple NetworkManagers | Remove extras |
| Cannot send action (IsPlayer=False) | You're a spectator | Join as first 2 players |
| Cannot send action (isConnected=False) | Not in room | Join a room first |
| WebSocket error | Backend WebSocket issue | Check backend WebSocket setup |

## Example: Successful Connection Flow

Here's what a successful connection looks like:

```
[NetworkManager] ========== NetworkManager Initializing ==========
[NetworkManager] ✓ Initialized as singleton
[NetworkManager] Player ID: player_abc12345
[NetworkManager] Server URL: http://127.0.0.1:8000
[NetworkManager] WebSocket URL: ws://127.0.0.1:8000

[NetworkManager] Creating room: TestRoom, MaxPlayers: 4, Private: False
[NetworkManager] POST http://127.0.0.1:8000/api/rooms/?host_player_id=player_abc12345
[NetworkManager] Request body: {"name":"TestRoom","max_players":4,"is_private":false,"password":null}
[NetworkManager] Response Code: 200
[NetworkManager] Response: {"room_id":"room_xyz123","name":"TestRoom","host_player_id":"player_abc12345","max_players":4,"current_players":["player_abc12345"],"is_private":false,"is_game_started":false}
[NetworkManager] ✓ Room created successfully!
[NetworkManager] Room ID: room_xyz123, Player Number: 1
[NetworkManager] Connecting to WebSocket: ws://127.0.0.1:8000/ws/room_xyz123/player_abc12345
[NetworkManager] ✓ WebSocket connected

[NetworkManager] Sending action: move, Object: null
[NetworkManager] Action JSON: {"player_id":"player_abc12345","action_type":"move","target_object_id":null,"position":{"x":5.0,"y":0.5,"z":3.2},"timestamp":"2025-12-13T14:18:22.123Z","data":null}

[NetworkManager] WebSocket message received: {"type":"player_joined","data":"{}"}
[NetworkManager] Message type: player_joined
[NetworkManager] Player joined notification
[NetworkManager] GET http://127.0.0.1:8000/api/rooms/room_xyz123
[NetworkManager] Response Code: 200
[NetworkManager] Response: {"room_id":"room_xyz123",...,"current_players":["player_abc12345","player_def67890"]}
[NetworkManager] Player index in room: 0 / 2
[NetworkManager] ✓ Assigned as Player 1
```

## Need More Help?

If you're still having issues:

1. **Copy all Console logs** (especially [NetworkManager] ones)
2. **Check backend terminal** for errors
3. **Verify URLs** match between Unity and backend
4. **Test backend** directly using Swagger UI (http://127.0.0.1:8000/docs)

The logs will tell you exactly where the connection is failing! 🔍
