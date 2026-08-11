# Vietnamese Timezone - Quick Reference

## 🇻🇳 System Uses Vietnam Timezone (UTC+7)

All times in the system automatically use **Asia/Ho_Chi_Minh** timezone.

## ⏰ Quick Code Snippets

### C# Backend

```csharp
// Get current Vietnam time
var now = BusinessClock.Now;
// Result: 2025-11-08 14:30:45

// Get today's date
var today = BusinessClock.Today;
// Result: 2025-11-08 00:00:00

// Convert UTC from database to Vietnam time for display
var vietnamTime = BusinessClock.FromUtc(utcDateTime);

// Convert Vietnam local time to UTC for storage
var utcForDb = BusinessClock.ToUtc(vietnamLocalTime);
```

### Razor View (HTML)

```html
<!-- Display reservation time in Vietnam timezone -->
<span>@BusinessClock.FromUtc(reservation.ReservationDate).ToString("HH:mm")</span>

<!-- Pass UTC time for JavaScript countdown timer -->
<p data-reservation="@BusinessClock.ToUtc(reservation.ReservationDate).ToString("o")">
	Còn 15 phút
</p>
```

### JavaScript Frontend

```javascript
// Format UTC ISO string as Vietnam time
VietnamTimeUtil.formatTimeDisplay("2025-11-08T10:30:00Z")
// Output: "10:30, 08/11/2025"

// Get countdown text
VietnamTimeUtil.formatCountdown("2025-11-08T10:30:00Z")
// Output: "Còn 15 phút" or "Đã quá giờ 5 phút"

// Initialize datetime-local input
VietnamTimeUtil.initDatetimeInput('ReservationDate', 30);
// Sets to current Vietnam time + 30 minutes
```

## 📊 Data Flow

```
Customer → Browser (datetime-local) → API → Server
										↓
								   BusinessClock.ToUtc()
										↓
								   Database (UTC)
										↓
								   BusinessClock.FromUtc()
										↓
						   Cashier Display (Vietnam time)
```

## 🔔 Notifications

Cashier dashboard shows auto-updating notifications:

| Notification | Emoji | Color | When |
|---|---|---|---|
| Pending Payment | 💳 | Red | Customer at payment stage |
| Arriving Soon | ⏰ | Yellow | Next 15 minutes |
| **Overdue** | 🔴 | Red | Past reservation time (< 30 min) |
| Over-time Table | ⏱️ | Red | Table used > 90 minutes |
| No Empty Tables | 📊 | Blue | All tables occupied |

## 🤖 Auto-Cancellation Rules

```
Reservation Time: 14:30 Vietnam

14:30 → 14:59 : Status = "Pending" or "Confirmed" ✓
14:35 : Show "Bàn Quá Giờ - Quá giờ 5 phút" ⚠️
15:00 : Auto-cancel reservation ✓
	   Log: "Auto-cancelled reservation #123"
	   Runs every 5 minutes
```

## 🧪 Quick Test

1. **Create Reservation**
   - Set time to 30 minutes from now (e.g., 14:30)
   - Verify displayed as 14:30 in database

2. **Test Auto-Cancel**
   - Create reservation for now (14:00)
   - Wait ~5 minutes for background service
   - Verify status = "Cancelled"

3. **Test Notification**
   - Create reservation for 14:20
   - At 14:05, go to cashier dashboard
   - Should see "⏰ Khách Sắp Đến - Còn 15 phút"

## ⚙️ Configuration

**Auto-cancellation interval:** 5 minutes  
**Location:** `Services/ReservationAutoCleanupService.cs:12`

```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
```

**Change to 2 minutes (for testing):**
```csharp
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);
```

**Timezone IDs used:**
1. "Asia/Ho_Chi_Minh" (Linux/Mac)
2. "SE Asia Standard Time" (Windows)

## 🆘 Quick Troubleshooting

| Problem | Solution |
|---|---|
| Times are UTC (wrong by 7 hours) | Check server timezone. Should be UTC+7 |
| Datetime picker shows wrong time | Call `VietnamTimeUtil.initDatetimeInput()` after page load |
| Auto-cancel not working | Restart app. Verify service registered in Program.cs |
| Countdown timer not updating | Ensure `data-reservation` attribute has UTC ISO format |
| Database shows wrong UTC times | Ensure code uses `BusinessClock.ToUtc()` before saving |

## 📞 Common Tasks

### Display reservation time
```html
@BusinessClock.FromUtc(reservation.ReservationDate).ToString("HH:mm")
<!-- Output: 14:30 -->
```

### Query today's reservations
```csharp
var today = BusinessClock.Today;
var tomorrow = today.AddDays(1);
var todayReservations = context.Reservations
	.Where(r => r.ReservationDate >= BusinessClock.ToUtc(today) && 
			   r.ReservationDate < BusinessClock.ToUtc(tomorrow))
	.ToList();
```

### Check if reservation is overdue
```csharp
var now = BusinessClock.Now;
var isOverdue = reservation.ReservationDate <= now;
var minutesOverdue = (now - reservation.ReservationDate).TotalMinutes;
```

### Schedule something for specific time
```csharp
var target = BusinessClock.Now.AddHours(2);
var targetUtc = BusinessClock.ToUtc(target);
// Store targetUtc in database
```

## 📚 Full Documentation

See: `VIETNAM_TIMEZONE_IMPLEMENTATION.md`

---

**Last Updated:** Nov 2025  
**Timezone:** Asia/Ho_Chi_Minh (UTC+7)  
**Status:** ✅ Active
