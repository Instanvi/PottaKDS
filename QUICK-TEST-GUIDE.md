# PottaKDS Quick Test Guide

## Test 1: Settings Window Singleton

**Goal**: Verify only one Settings window can be open

1. Launch PottaKDS
2. Click Settings gear icon
3. Settings window opens
4. Click Settings gear icon again
5. **Expected**: Same window comes to front, no duplicate
6. Minimize Settings window
7. Click Settings gear icon
8. **Expected**: Window restores and focuses
9. Close Settings window
10. Click Settings gear icon
11. **Expected**: New window opens correctly

**Status**: Pass / Fail

---

## Test 2: Auto-Scan Network

**Goal**: Verify auto-scan finds server and tests connection automatically

1. Make sure PottaAPI is running on local network
2. Launch PottaKDS
3. Open Settings
4. Click "Auto-Scan LAN" button
5. **Expected**: Status shows "Scanning network..."
6. **Expected**: Discovers server and updates IP field
7. **Expected**: Automatically tests connection
8. **Expected**: Green success box appears: "Connection successful to http://[IP]:5001"

**Status**: Pass / Fail

---

## Test 3: Manual Test Connection - Success

**Goal**: Verify manual test shows clear success feedback

1. Open Settings
2. Enter valid server IP (e.g., localhost or 127.0.0.1)
3. Enter port 5001
4. Click "Test Connection"
5. **Expected**: Button shows "Testing connection..." briefly
6. **Expected**: Green box appears with checkmark icon
7. **Expected**: Message: "Connection successful to http://localhost:5001"

**Status**: Pass / Fail

---

## Test 4: Manual Test Connection - Failure

**Goal**: Verify manual test shows clear failure feedback with reason

1. Open Settings
2. Enter invalid IP (e.g., 192.168.1.999)
3. Enter port 5001
4. Click "Test Connection"
5. **Expected**: Button shows "Testing connection..." briefly
6. **Expected**: Red box appears with X icon
7. **Expected**: Clear error message like:
   - "Connection failed: Connection timed out. Host did not respond."
   - "Connection failed: Host unreachable. Verify computers are on the same network."
   - "Connection failed: Connection refused. Potta POS server is not running on this IP/port."

**Status**: Pass / Fail

---

## Test 5: Discovered Servers Selection

**Goal**: Verify clicking discovered server updates IP and tests

1. Open Settings
2. Click "Auto-Scan LAN"
3. Wait for scan to complete
4. Multiple servers appear in list
5. Click on a different server from the list
6. **Expected**: IP and Port fields update
7. **Expected**: Connection test runs automatically
8. **Expected**: Success or failure message appears

**Status**: Pass / Fail

---

## Common Issues

### Test Connection shows nothing
- **Check**: Is PottaAPI running?
- **Check**: Are you on the same network?
- **Check**: Is Windows Firewall blocking port 5001?

### Auto-Scan finds no servers
- **Check**: PottaAPI must be running
- **Check**: Computers must be on same LAN
- **Check**: Port 5001 must be accessible

### Settings window opens multiple times
- **This is the bug we fixed** - Should not happen anymore
- If it does, report as regression

---

## Expected Results Summary

All tests should **PASS** with:
1. Only one Settings window at a time
2. Auto-scan automatically tests connection
3. Test Connection shows clear green success or red failure
4. Error messages are specific and helpful
5. No duplicate windows
6. Clean, professional UX

---

## Build Info
- Build Status: Successful
- Target: .NET 8.0 Windows
- Platform: win-x64
