# KDS Kitchen Order Card - Complete Testing Guide

## 🎯 Overview
This guide helps you verify that the Kitchen Display System (KDS) order cards work correctly with the PottaAPI backend.

---

## ✅ Features Implemented

### 1. **Ready Button** ✔️
- **Action**: Marks order as "Ready" and completes ALL items
- **API Call**: `PUT /api/orders/waiting/{transactionId}/status` with `status: "Ready"`
- **Additional**: Updates all item `IsCompleted` flags to `true` via items endpoint
- **Prevention**: Cannot mark the same order as Ready twice (shows info message)
- **UI Feedback**: Button temporarily disabled for 1 second after click

### 2. **Delayed Button** ⏱️
- **Action**: Marks order as "Delayed"
- **API Call**: `PUT /api/orders/waiting/{transactionId}/status` with `status: "Delayed"`
- **Prevention**: Cannot mark the same order as Delayed twice (shows info message)
- **UI Feedback**: Button temporarily disabled for 1 second after click

### 3. **Item Checkboxes** ☑️
- **Action**: Toggles individual item completion status
- **API Call**: `PUT /api/orders/waiting/{transactionId}/items` with updated items array
- **Auto-Ready**: If ALL items are checked and status is "Pending", automatically changes to "Ready"
- **Click Areas**: Both checkbox AND item row are clickable

---

## 🧪 Test Scenarios

### Test 1: Ready Button
**Steps:**
1. Open KDS application
2. Wait for orders to load (status: "Pending")
3. Click "Ready" button on any order
4. Confirm the dialog
5. Verify:
   - ✅ Order status changes to "Ready" on the card
   - ✅ All item checkboxes are now checked
   - ✅ API receives the update (check console logs)
   - ✅ Clicking "Ready" again shows "Already Ready" message

**Expected Result:** Order marked as Ready with all items completed

---

### Test 2: Delayed Button
**Steps:**
1. Open KDS application
2. Click "Delayed" button on a Pending order
3. Confirm the warning dialog
4. Verify:
   - ✅ Order status changes to "Delayed" on the card
   - ✅ Visual indicators show delayed state (red/orange)
   - ✅ API receives the update
   - ✅ Clicking "Delayed" again shows "Already Delayed" message

**Expected Result:** Order marked as Delayed

---

### Test 3: Individual Item Completion
**Steps:**
1. Find an order with multiple items
2. Click checkbox on first item
3. Verify:
   - ✅ Checkbox is checked
   - ✅ Item text may show strikethrough
   - ✅ API call updates the items array
4. Click checkbox on remaining items one by one
5. When last item is checked:
   - ✅ Order status automatically changes to "Ready"

**Expected Result:** Items can be individually marked, auto-Ready when all completed

---

### Test 4: Item Row Click
**Steps:**
1. Find an order with items
2. Click anywhere on the item row (not just the checkbox)
3. Verify:
   - ✅ Checkbox toggles
   - ✅ Item completion status updates
   - ✅ API receives update

**Expected Result:** Entire row is clickable for better UX

---

### Test 5: Double-Click Prevention
**Steps:**
1. Click "Ready" button rapidly multiple times
2. Verify:
   - ✅ Button becomes disabled after first click
   - ✅ Only ONE API call is made
   - ✅ Button re-enables after 1 second

**Expected Result:** Prevents duplicate API calls

---

### Test 6: Status-Based Validation
**Steps:**
1. Mark an order as "Ready"
2. Try to click "Ready" button again
3. Verify:
   - ✅ Shows information dialog: "Already marked as Ready"
   - ✅ No API call is made
4. Same test for "Delayed" button

**Expected Result:** Prevents redundant status updates

---

## 🔌 API Endpoint Verification

### Check API Logs
When testing, the PottaAPI console should show:

```
✅ Transaction status updated to 'Ready' (ID: TXN_123456). Rows affected: 1
✅ Transaction items updated (ID: TXN_123456). Rows affected: 1
```

### Verify Database
After marking Ready, check `WaitingTransactions` table:
```sql
SELECT TransactionId, Status, CartItems 
FROM WaitingTransactions 
WHERE TransactionId = 'TXN_123456';
```

**Expected:**
- `Status` = "Ready"
- `CartItems` JSON has all items with `"IsCompleted": true`

---

## 🐛 Troubleshooting

### Issue: Buttons Don't Work
**Check:**
1. Is KDS connected to API? (Check connection status indicator)
2. Is PottaAPI running? (Should be on http://localhost:5000)
3. Check browser console / KDS debug output for errors

### Issue: Checkboxes Don't Update
**Check:**
1. Verify `ToggleItemCompletionAsync` is being called (add breakpoint)
2. Check API logs for `/items` endpoint calls
3. Verify items have valid `ProductId` and `Name`

### Issue: Status Not Persisting
**Check:**
1. Database connection string in PottaAPI
2. SQLite database file exists and is writable
3. Transaction exists in `WaitingTransactions` table

---

## 📝 Code Flow Summary

```
┌─────────────────┐
│ User clicks     │
│ "Ready" button  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ DoneButton_Click│
│ - Check status  │
│ - Show confirm  │
│ - Disable btn   │
└────────┬────────┘
         │
         ▼
┌──────────────────────┐
│ MarkOrderReadyCommand│
│ - Set Status="Ready" │
│ - Mark all items ✔   │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────────┐
│ UpdateOrderStatusAsync   │
│ PUT /api/orders/waiting/ │
│ {transactionId}/status   │
└────────┬─────────────────┘
         │
         ▼
┌──────────────────────────┐
│ PushItemsStatusToApi     │
│ PUT /api/orders/waiting/ │
│ {transactionId}/items    │
└────────┬─────────────────┘
         │
         ▼
┌──────────────────────────┐
│ OrderService.UpdateStatus│
│ - Updates DB record      │
│ - Serializes items JSON  │
└──────────────────────────┘
```

---

## ✨ Key Implementation Details

### 1. **Status Updates** (`OrderService.cs`)
```csharp
// When status = "Ready" or "Completed", all items are marked complete
if (status == "Completed")
{
    foreach (var item in transaction.Items)
    {
        item.IsCompleted = true;
    }
}
```

### 2. **Item Updates** (`KdsApiService.cs`)
```csharp
PUT /api/orders/waiting/{transactionId}/items
Body: {
    "items": [ { "isCompleted": true, ... } ],
    "staffId": null
}
```

### 3. **Auto-Ready Logic** (`KdsDashboardViewModel.cs`)
```csharp
// If all items completed and status is Pending → auto Ready
if (order.AllItemsCompleted && order.Status == "Pending")
{
    order.Status = "Ready";
    await _apiService.UpdateOrderStatusAsync(order.TransactionId, "Ready");
}
```

---

## 🎉 Success Criteria

All tests pass when:
- ✅ Ready button marks order and all items as complete
- ✅ Delayed button changes status correctly
- ✅ Checkboxes toggle individual items
- ✅ Auto-Ready works when last item is checked
- ✅ Double-click prevention works
- ✅ Duplicate status updates are prevented
- ✅ API receives all updates correctly
- ✅ Database reflects the changes

---

## 📞 Support

If you encounter any issues:
1. Check the troubleshooting section above
2. Verify API logs in PottaAPI console
3. Check KDS debug output
4. Verify database state directly

**Last Updated**: 2026-09-28
