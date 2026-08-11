# Vietnamese Timezone Implementation Guide

## Overview

This document describes the complete timezone handling system for the café management application. All timestamps in the system use **Asia/Ho_Chi_Minh (Vietnam/Saigon) timezone**, which is UTC+7.

## Key Components

### 1. Backend - BusinessClock Class (`Models/BusinessClock.cs`)

The `BusinessClock` class provides timezone-aware time utilities:

```csharp
// Get current Vietnam time
var now = BusinessClock.Now;

// Get today's date in Vietnam timezone
var today = BusinessClock.Today;

// Convert UTC to Vietnam time
var localTime = BusinessClock.FromUtc(utcDateTime);

// Convert Vietnam local time to UTC for storage
var utcTime = BusinessClock.ToUtc(localDateTime);

// Get UTC boundaries for today
var todayStart = BusinessClock.StartOfTodayUtc;  // 2025-11-07T17:00:00Z (midnight Vietnam time)
var tomorrowStart = BusinessClock.StartOfTomorrowUtc;
```

**Key Rules:**
- All times stored in the database are UTC
- All business logic uses local Vietnam time via `BusinessClock.Now`
- Conversion happens transparently at boundaries

### 2. Reservation Policy (`Models/ReservationPolicy.cs`)

Defines reservation timeout rules:

```csharp
public const int DurationMinutes = 120;  // Reservation duration
public const int HoldBeforeMinutes = 30; // Auto-cancel after 30 minutes no-show
```

**Auto-Cancellation Logic:**
- Reservations are auto-cancelled if customer is 30+ minutes late
- Runs every 5 minutes via `ReservationAutoCleanupService`
- Only cancels "Pending" or "Confirmed" reservations
- Logs every auto-cancellation action

### 3. Reservation Status Service (`Services/ReservationStatusService.cs`)

Handles reservation lifecycle:

```csharp
// Auto-cancel overdue reservations (runs in background)
await reservationStatusService.AutoCancelOverdueReservationsAsync();

// Get reservations that are currently overdue (not yet 30 min late)
var overdue = await reservationStatusService.GetOverdueReservationsAsync();

// Get reservations arriving soon (next 15 minutes)
var upcoming = await reservationStatusService.GetUpcomingReservationsAsync();
```

### 4. Background Auto-Cleanup Service (`Services/ReservationAutoCleanupService.cs`)

- Runs every 5 minutes
- Automatically cancels reservations 30+ minutes overdue
- Logs all actions for audit trail
- Registered in `Program.cs` as hosted service

## Frontend - JavaScript Timezone Utilities

### VietnamTimeUtil API (`wwwroot/js/vietnam-timezone.js`)

Use these functions for all client-side time handling:

```javascript
// Display current Vietnam time (updates every second if in countdown)
VietnamTimeUtil.formatTimeDisplay("2025-11-08T10:30:00Z")
// Returns: "10:30, 08/11/2025"

// Get current Vietnam time
const now = VietnamTimeUtil.now();

// Convert UTC ISO string to Vietnam Date
const vietnamDate = VietnamTimeUtil.toVietnamTime("2025-11-08T10:30:00Z");

// Calculate minutes until an event
const minutes = VietnamTimeUtil.minutesUntil("2025-11-08T10:30:00Z");

// Format countdown text
VietnamTimeUtil.formatCountdown("2025-11-08T10:30:00Z")
// Returns: "Còn 15 phút" or "Đã quá giờ 5 phút"

// Format "time ago" (for notifications)
VietnamTimeUtil.formatTimeAgo("2025-11-08T10:30:00Z")
// Returns: "Vừa xong", "5m", "2h", etc.

// Initialize datetime-local input with current time
VietnamTimeUtil.initDatetimeInput('ReservationDate', 30);  // 30 minutes from now

// Start live countdown timer
const intervalId = VietnamTimeUtil.startLiveUpdate('[data-reservation]', utcTimeString, 'countdown');
```

### Auto-Update Elements

Elements with `data-reservation` or `data-order-start` attributes are automatically updated:

```html
<!-- Countdown timer - updates every 30 seconds -->
<p class="table-countdown" data-reservation="2025-11-08T10:30:00Z">Còn 15 phút</p>

<!-- Order duration - updates every minute -->
<p class="table-order-info" data-order-start="2025-11-08T10:00:00Z">30 phút</p>
```

## Implementation Details

### How Reservation Times Work

1. **Customer Creates Reservation** (Frontend)
   ```html
   <!-- datetime-local input returns local Vietnam time -->
   <input type="datetime-local" id="ReservationDate" value="2025-11-08T14:30">
   ```

2. **JavaScript Initialization** (Frontend)
   ```javascript
   // Initialize with current Vietnam time + 30 minutes
   VietnamTimeUtil.initDatetimeInput('ReservationDate', 30);
   ```

3. **POST to Server** (Frontend → Backend)
   ```javascript
   // datetime-local value is local Vietnam time
   // Send as-is to server
   const reservationData = {
	   reservationDate: document.getElementById('ReservationDate').value,
	   numberOfGuests: 4
   };
   ```

4. **Store in Database** (Backend)
   ```csharp
   var localTime = DateTime.Parse(reservationData.reservationDate);

   // Convert to UTC before storing
   var utcTime = BusinessClock.ToUtc(localTime);

   reservation.ReservationDate = utcTime;
   ```

5. **Display to Cashier** (Backend → Frontend)
   ```csharp
   // Retrieve from database (in UTC)
   var reservation = await context.Reservations.FindAsync(id);

   // Convert to local for view
   var localTime = BusinessClock.FromUtc(reservation.ReservationDate);
   ```

   ```html
   <!-- Pass UTC time to view -->
   <p data-reservation="@BusinessClock.ToUtc(reservation.ReservationDate).ToString("o")">
	   @reservation.ReservationDate.ToString("HH:mm")
   </p>
   ```

### Cashier Dashboard - Notifications

The dashboard shows four types of notifications with proper Vietnam timezone:

1. **Chờ Thanh Toán** (Pending Payment)
   - Tables waiting for payment
   - Red/danger style

2. **Khách Sắp Đến** (Customer Arriving Soon)
   - Reservations within next 15 minutes
   - Yellow/warning style
   - Shows minutes remaining

3. **Bàn Quá Giờ** (Reservation Overdue)
   - Customers more than 0 minutes late but less than 30 minutes
   - Red/danger style
   - Shows how many minutes late
   - Auto-cancelled after 30 minutes

4. **Bàn Sử Dụng Quá Lâu** (Table Over-time)
   - Tables serving for more than 90 minutes
   - Red/danger style
   - Shows total minutes used

**All times in notifications automatically update and use Vietnam timezone.**

## Database Query Examples

### Query Reservations for Today (Vietnam Timezone)

```csharp
var localToday = BusinessClock.Today;
var localTomorrow = localToday.AddDays(1);

// Convert to UTC for database query
var utcTodayStart = BusinessClock.ToUtc(localToday);
var utcTodayEnd = BusinessClock.ToUtc(localTomorrow);

var todayReservations = await context.Reservations
	.Where(r => r.ReservationDate >= utcTodayStart && 
			   r.ReservationDate < utcTodayEnd)
	.ToListAsync();

// Convert back to local for display
foreach (var res in todayReservations)
{
	var localTime = BusinessClock.FromUtc(res.ReservationDate);
	Console.WriteLine($"Reservation at {localTime:HH:mm}");
}
```

### Query Overdue Reservations

```csharp
var now = BusinessClock.Now;
var overdueThreshold = now.AddMinutes(-ReservationPolicy.HoldBeforeMinutes);

var overdueReservations = await context.Reservations
	.Where(r => !r.IsDeleted &&
			   (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed") &&
			   r.ReservationDate <= overdueThreshold)
	.ToListAsync();
```

## Testing Timezone Handling

### Manual Test Cases

1. **Create Reservation at 14:30 Vietnam time**
   - Customer creates reservation for 14:30 Vietnam time
   - Database should store as 07:30 UTC
   - Display to cashier should show 14:30

2. **Auto-Cancel Reservation at 30 Minutes Late**
   - Create reservation for 14:30 Vietnam time
   - Wait until 15:00+ Vietnam time (auto-cancel runs every 5 minutes)
   - Reservation status should change to "Cancelled"
   - Check logs for auto-cancellation message

3. **Display Countdown Timer**
   - Reservation scheduled for 14:30 Vietnam time
   - At 14:15, cashier views dashboard
   - Should show "Còn 15 phút"
   - Timer updates every 30 seconds
   - At 14:31, should show "Đã quá giờ 1 phút"

## Configuration

### Vietnam Timezone Detection

The `BusinessClock` attempts to use these timezone IDs in order:
1. "Asia/Ho_Chi_Minh" (Linux/Mac)
2. "SE Asia Standard Time" (Windows)

If neither is found, application throws `InvalidOperationException`.

### Change Timezone

To use a different timezone, modify `BusinessClock.cs`:

```csharp
private static TimeZoneInfo ResolveZone()
{
	foreach (var id in new[] { "Your/Timezone", "Fallback/Timezone" })
	{
		try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
		catch { }
	}
	throw new InvalidOperationException("Timezone unavailable");
}
```

## Troubleshooting

### Issue: Times are off by X hours

**Solution:** Verify server timezone settings:
```bash
# Windows
tzutil /l  # List all timezones
tzutil /g  # Get current timezone

# Linux
timedatectl
```

### Issue: Datetime picker shows wrong time

**Solution:** Ensure `VietnamTimeUtil.initDatetimeInput()` is called after page load:
```javascript
document.addEventListener('DOMContentLoaded', function() {
	VietnamTimeUtil.initDatetimeInput('ReservationDate', 30);
});
```

### Issue: Database has UTC times but views show wrong time

**Solution:** Ensure all display uses `BusinessClock.FromUtc()`:
```csharp
// WRONG - will show UTC time
<span>@reservation.ReservationDate.ToString("HH:mm")</span>

// CORRECT - will show Vietnam time
<span>@BusinessClock.FromUtc(reservation.ReservationDate).ToString("HH:mm")</span>
```

## Summary

- ✅ All database times stored in UTC
- ✅ All business logic uses Vietnam timezone
- ✅ All display converts UTC → Vietnam local time
- ✅ Datetime inputs use Vietnam time
- ✅ Auto-cancellation runs every 5 minutes
- ✅ Notifications show proper countdown/elapsed time
- ✅ Dashboard updates in real-time with Vietnam time
