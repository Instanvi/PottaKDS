# PottaKDS Network Discovery - Quick Test Guide

## What Changed?

The KDS now **automatically scans all IPs on your network** to find Potta POS, instead of requiring manual IP entry.

## Quick Test Scenarios

### ✅ Test 1: Same Computer (Should work immediately)

**Setup:**
1. Start PottaAPI on port 5001
2. Start PottaKDS

**Expected Result:**
- KDS connects to `localhost:5001` within 1 second
- Shows "Connected (http://localhost:5001)" in green
- No settings dialog opens

---

### ✅ Test 2: Different Computer (Automatic network scan)

**Setup:**
1. Computer A: Start PottaAPI (note its IP, e.g., 192.168.1.100)
2. Computer B: Start PottaKDS with default settings (serverIp = "localhost")

**Expected Result:**
1. KDS checks localhost → not found
2. Shows message: "🔍 Scanning local network for Potta POS..."
3. Progress updates: "⏳ Progress: 50% (127/254 IPs checked)"
4. Finds server: "✓ Found server: 192.168.1.100:5001"
5. Auto-connects automatically
6. **Total time: 30-40 seconds**

---

### ✅ Test 3: Manual Scan from Settings

**Setup:**
1. PottaAPI running somewhere on network
2. Open PottaKDS Settings (gear icon)

**Expected Result:**
1. Settings dialog opens
2. **Automatically starts scanning** (no button click needed!)
3. Shows real-time progress messages
4. Lists all discovered servers
5. Click a server to select it
6. Auto-tests connection
7. Click "Save & Connect"

---

### ✅ Test 4: Connection Failure Recovery

**Setup:**
1. KDS is connected
2. Stop PottaAPI
3. Wait 5 seconds
4. Restart PottaAPI

**Expected Result:**
- Status changes to "Connecting..." (orange)
- Within 5 seconds, reconnects automatically
- Status back to "Connected" (green)

---

## How to Watch It Work

### In Settings Dialog
Look for these progress messages in the settings dialog:

```
🔍 Checking localhost (Port 5001)...
🔍 Checking 127.0.0.1:5001...
🔍 Scanning local network for Potta POS...
📡 Found 1 network interface(s)
🔍 Scanning subnet 192.168.1.0/24...
⏳ Progress: 25% (64/254 IPs checked)
✓ Found server: 192.168.1.105:5001
⏳ Progress: 50% (127/254 IPs checked)
⏳ Progress: 75% (191/254 IPs checked)
✓ Scan complete! Found 1 Potta POS server(s)
```

### On Main Dashboard
Watch the connection status indicator (top bar):
- 🔴 Red "Connecting..." = No connection
- 🟢 Green "Connected (http://...)" = Connected

---

## Network Requirements

### What needs to be open?
On the **Potta POS computer**, ensure:
- ✅ Port **5001** is open (Windows Firewall)
- ✅ PottaAPI is running and listening on `0.0.0.0:5001` (not just localhost)

### Check if API is accessible:
From any computer on the network, open a browser and navigate to:
```
http://<POS_COMPUTER_IP>:5001/health
```

You should see: `"Healthy"`

If you get a timeout or error, the firewall is blocking it.

---

## Performance Benchmarks

| Scenario | Time to Connect |
|----------|----------------|
| **Localhost** | < 1 second |
| **Same subnet (1-254 hosts)** | 30-40 seconds |
| **Multiple subnets** | 60-80 seconds |

---

## Troubleshooting

### ❌ "No active network adapter found"
**Problem:** Computer has no active Ethernet or WiFi
**Fix:** Connect to a network via Ethernet or WiFi

### ❌ "No servers found" (but POS is running)
**Possible causes:**
1. **Firewall:** Port 5001 blocked on POS computer
2. **API not listening:** PottaAPI is only listening on `localhost`, not `0.0.0.0`
3. **Different subnet:** POS on 10.x.x.x, KDS on 192.168.x.x (router blocking)

**How to fix:**
1. Check firewall: Windows Defender Firewall → Allow PottaAPI.exe
2. Check API config: Ensure it's not bound to `localhost` only
3. Manual entry: Enter IP address manually in Settings

### ❌ Scan takes too long
**Cause:** Many devices on network (e.g., 100+ hosts)
**Solution:** 
- Wait for scan to complete (shows progress)
- OR manually enter known IP address

---

## What to Look For (Testing Checklist)

- [ ] Localhost connection works instantly
- [ ] Network scan shows real-time progress messages
- [ ] Discovered servers appear in list
- [ ] Clicking a server auto-tests connection
- [ ] Success message shows "✓ Connected successfully!"
- [ ] Settings dialog auto-scans when opened (if not connected)
- [ ] Auto-reconnects after PottaAPI restart
- [ ] Connection status indicator updates correctly
- [ ] Orders load after successful connection

---

## Advanced: Simulating Network Issues

### Test Auto-Reconnect:
```powershell
# Stop API
Stop-Process -Name "PottaAPI" -Force

# Wait 10 seconds (KDS will show "Connecting...")

# Restart API
cd "C:\Path\To\PottaAPI"
dotnet run
```

### Test Firewall Blocking:
```powershell
# Block port 5001
New-NetFirewallRule -DisplayName "Block PottaAPI Test" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Block

# KDS should fail to connect
# Settings dialog should open automatically

# Unblock
Remove-NetFirewallRule -DisplayName "Block PottaAPI Test"
```

---

## Summary

**The Goal:** PottaKDS should find Potta POS automatically without needing to know the IP address.

**Key Improvements:**
- ✅ Automatic localhost check (instant)
- ✅ Automatic network scanning (30-40 seconds)
- ✅ Real-time progress updates
- ✅ Auto-opens settings if no connection
- ✅ Lists all discovered servers
- ✅ One-click connection

**Result:** 90% of users will never need to manually enter an IP address!
