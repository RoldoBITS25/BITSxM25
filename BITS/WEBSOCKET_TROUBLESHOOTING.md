# WebSocket Connection Troubleshooting Guide

## Quick Diagnosis

When connecting from another PC fails, check the Unity console for specific error messages. The enhanced logging will show:

### Connection Attempt Logs
```
[WebSocketClient] ========== Connecting to WebSocket ==========
[WebSocketClient] URL: wss://aims-rangers-theorem-association.trycloudflare.com/ws/player_xxx
[WebSocketClient] Validating URL format...
[WebSocketClient] ✓ URL is valid
[WebSocketClient]   Scheme: wss
[WebSocketClient]   Host: aims-rangers-theorem-association.trycloudflare.com
[WebSocketClient]   Port: 443
[WebSocketClient]   Path: /ws/player_xxx
```

### Common Error Messages

#### 1. "Connection timeout. The server may be unreachable from this network."

**Cause:** The WebSocket server is not accessible from the client's network.

**Solutions:**
- Verify the backend server is running
- Check that the server is bound to `0.0.0.0`, not `127.0.0.1`
- Ensure Cloudflare tunnel is active
- Check firewall settings on the server

**Backend Fix:**
```bash
# Start backend with correct binding
cd /path/to/backend
uvicorn main:app --host 0.0.0.0 --port 8000 --ws-ping-interval 20
```

#### 2. "Connection faulted. Check if the server is running and accessible from this network."

**Cause:** Server is offline or network path is blocked.

**Solutions:**
- Verify backend server is running
- Test HTTP endpoint first: `https://aims-rangers-theorem-association.trycloudflare.com/api/rooms/`
- Check Cloudflare tunnel status
- Verify firewall rules

#### 3. "Invalid WebSocket headers. The server may not support WebSocket connections."

**Cause:** Backend not configured for WebSocket upgrade.

**Solutions:**
- Ensure FastAPI WebSocket routes are properly configured
- Check that Cloudflare tunnel supports WebSocket (it should by default)
- Verify no proxy is interfering with WebSocket upgrade

---

## Backend Configuration Checklist

### ✅ Server Binding
Ensure the backend is accessible from all network interfaces:

```bash
# ❌ Wrong - only accessible from localhost
uvicorn main:app --host 127.0.0.1 --port 8000

# ✅ Correct - accessible from all interfaces
uvicorn main:app --host 0.0.0.0 --port 8000
```

### ✅ Cloudflare Tunnel
Verify the tunnel is running and accessible:

```bash
# Check if tunnel is running
ps aux | grep cloudflared

# Test HTTP endpoint from remote PC
curl https://aims-rangers-theorem-association.trycloudflare.com/api/rooms/

# Start tunnel if not running
cloudflared tunnel --url http://localhost:8000
```

### ✅ Firewall Configuration

**macOS Server:**
```bash
# Check firewall status
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate

# Allow Python (for uvicorn)
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --add $(which python3)
sudo /usr/libexec/ApplicationFirewall/socketfilterfw --unblockapp $(which python3)
```

### ✅ CORS Configuration
Ensure the backend has proper CORS settings:

```python
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify actual origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
```

---

## Diagnostic Script

Run the diagnostic script from the remote PC to test connectivity:

```bash
cd /Users/ton/git/TON/BxM25/game/BITS
./diagnose_websocket.sh
```

This will test:
1. DNS resolution
2. HTTP endpoint accessibility
3. WebSocket connection (if wscat is installed)
4. Network connectivity
5. Local firewall status

---

## Testing WebSocket Connection

### Using wscat (Recommended)

Install wscat:
```bash
npm install -g wscat
```

Test the WebSocket connection:
```bash
wscat -c wss://aims-rangers-theorem-association.trycloudflare.com/ws/test_player_123
```

**Expected output if working:**
```
Connected (press CTRL+C to quit)
< {"type":"CONNECT","data":{...}}
```

### Using Browser Console

Open browser console and run:
```javascript
const ws = new WebSocket('wss://aims-rangers-theorem-association.trycloudflare.com/ws/test_player_123');
ws.onopen = () => console.log('✓ Connected');
ws.onerror = (e) => console.error('✗ Error:', e);
ws.onmessage = (m) => console.log('Message:', m.data);
```

---

## Network Architecture

```
[Unity Client on PC A]
        ↓
[Internet/Local Network]
        ↓
[Cloudflare Tunnel]
        ↓
[FastAPI Backend on PC B]
    (listening on 0.0.0.0:8000)
```

**Critical Points:**
- Backend MUST bind to `0.0.0.0` to accept external connections
- Cloudflare tunnel MUST be running and forwarding to backend
- No firewall should block the connection at any point

---

## Step-by-Step Troubleshooting

### Step 1: Verify Backend is Running
On the server PC:
```bash
# Check if backend is running
ps aux | grep uvicorn

# Check what ports are listening
lsof -i :8000
```

### Step 2: Test Local Connection
On the server PC:
```bash
# Test local HTTP
curl http://localhost:8000/api/rooms/

# Test local WebSocket (if wscat installed)
wscat -c ws://localhost:8000/ws/test_123
```

### Step 3: Test Cloudflare Tunnel
From ANY PC (including remote):
```bash
# Test HTTP through tunnel
curl https://aims-rangers-theorem-association.trycloudflare.com/api/rooms/
```

If this fails, the tunnel is not working.

### Step 4: Test WebSocket Through Tunnel
From the remote PC:
```bash
# Test WebSocket through tunnel
wscat -c wss://aims-rangers-theorem-association.trycloudflare.com/ws/test_123
```

### Step 5: Check Unity Logs
Run the Unity build on the remote PC and check the console logs for detailed error messages.

---

## Common Solutions Summary

| Problem | Solution |
|---------|----------|
| Connection timeout | Check server is running and bound to 0.0.0.0 |
| HTTP works, WS fails | Verify WebSocket support in tunnel/proxy |
| Both HTTP and WS fail | Check tunnel is running and accessible |
| Works locally, fails remotely | Check firewall and server binding |
| Intermittent disconnections | Add keep-alive pings (already configured) |

---

## Getting Help

If issues persist, gather the following information:

1. **Unity Console Logs** - Full error messages from WebSocketClient
2. **Backend Logs** - Output from uvicorn showing connection attempts
3. **Network Test Results** - Output from diagnostic script
4. **Tunnel Status** - Is cloudflared running? What's the tunnel URL?
5. **Firewall Status** - Are there any blocking rules?

With this information, you can identify exactly where the connection is failing.
