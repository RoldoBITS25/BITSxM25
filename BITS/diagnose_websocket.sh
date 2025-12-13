#!/bin/bash

# WebSocket Connection Diagnostic Script
# This script helps diagnose WebSocket connection issues from remote PCs

echo "=========================================="
echo "WebSocket Connection Diagnostic Tool"
echo "=========================================="
echo ""

# Configuration
SERVER_HOST="aims-rangers-theorem-association.trycloudflare.com"
HTTP_ENDPOINT="https://${SERVER_HOST}/api/rooms/"
WS_ENDPOINT="wss://${SERVER_HOST}/ws/test_diagnostic_123"

echo "Testing connection to: ${SERVER_HOST}"
echo ""

# Test 1: DNS Resolution
echo "[1/5] Testing DNS resolution..."
if host ${SERVER_HOST} > /dev/null 2>&1; then
    echo "✓ DNS resolution successful"
    host ${SERVER_HOST}
else
    echo "✗ DNS resolution failed"
    echo "  The domain cannot be resolved. Check your internet connection."
fi
echo ""

# Test 2: HTTP Endpoint
echo "[2/5] Testing HTTP endpoint..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${HTTP_ENDPOINT} 2>/dev/null)
if [ "$HTTP_STATUS" = "200" ] || [ "$HTTP_STATUS" = "404" ]; then
    echo "✓ HTTP endpoint is reachable (Status: ${HTTP_STATUS})"
else
    echo "✗ HTTP endpoint failed (Status: ${HTTP_STATUS})"
    echo "  The server may be offline or unreachable from this network."
fi
echo ""

# Test 3: WebSocket Connection (requires wscat)
echo "[3/5] Testing WebSocket connection..."
if command -v wscat &> /dev/null; then
    echo "Attempting WebSocket connection (will timeout after 5 seconds)..."
    timeout 5 wscat -c ${WS_ENDPOINT} --no-check 2>&1 | head -n 5 || true
    echo "✓ wscat test completed"
else
    echo "⚠ wscat not installed. Install with: npm install -g wscat"
    echo "  Skipping WebSocket connection test."
fi
echo ""

# Test 4: Network Connectivity
echo "[4/5] Testing network connectivity..."
if ping -c 3 ${SERVER_HOST} > /dev/null 2>&1; then
    echo "✓ Network connectivity OK"
else
    echo "⚠ Ping failed (this may be normal if ICMP is blocked)"
fi
echo ""

# Test 5: Local Firewall Check (macOS)
echo "[5/5] Checking local firewall status..."
if [[ "$OSTYPE" == "darwin"* ]]; then
    FIREWALL_STATUS=$(sudo /usr/libexec/ApplicationFirewall/socketfilterfw --getglobalstate 2>/dev/null)
    echo "Firewall status: ${FIREWALL_STATUS}"
else
    echo "⚠ Not macOS, skipping firewall check"
fi
echo ""

# Summary
echo "=========================================="
echo "Diagnostic Summary"
echo "=========================================="
echo ""
echo "If HTTP endpoint is reachable but WebSocket fails:"
echo "  → The server may not be configured for WebSocket connections"
echo "  → Check backend is running with: uvicorn main:app --host 0.0.0.0"
echo ""
echo "If both HTTP and WebSocket fail:"
echo "  → Server may be offline"
echo "  → Cloudflare tunnel may not be running"
echo "  → Network firewall may be blocking connections"
echo ""
echo "Next steps:"
echo "  1. Check Unity console logs for specific error messages"
echo "  2. Verify backend server is running"
echo "  3. Ensure Cloudflare tunnel is active"
echo "  4. Check firewall settings on both client and server"
echo ""
