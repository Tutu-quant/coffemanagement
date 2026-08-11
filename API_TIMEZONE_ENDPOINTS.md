# API Endpoints - Timezone Considerations

## Reservation Endpoints

All reservation endpoints now properly handle Vietnam timezone.

### POST /Customer/Reservations/Create

**Request Body:**
```json
{
	"reservationDate": "2025-11-08T14:30",  // datetime-local format (Vietnam time)
	"numberOfGuests": 4,
	"tableId": 5,
	"notes": "Near window please"
}
```

**Processing:**
1. Frontend sends datetime-local value (local Vietnam time)
2. Server receives as local time via binding
3. `ReservationService.CreateReservationAsync()` treats as local time
4. `BusinessClock.ToUtc()` converts to UTC for storage

**Database Storage:**
```sql
ReservationDate: "2025-11-08T07:30:00Z"  -- UTC stored
```

**Success Response:**
```json
{
	"success": true,
	"data": {
		"reservationId": 123,
		"reservationDate": "2025-11-08T07:30:00Z",  // UTC returned
		"numberOfGuests": 4,
		"status": "Pending"
	}
}
```

### GET /Cashier/Reservations

**Response:**
```json
{
	"reservations": [
		{
			"reservationId": 123,
			"reservationDate": "2025-11-08T07:30:00Z",  // UTC in JSON
			"customerName": "Nguyễn Văn A",
			"tableNumber": "T02",
			"numberOfGuests": 4,
			"status": "Pending"
		}
	]
}
```

**Client-side Display (in Razor view):**
```html
<!-- Convert UTC to Vietnam time for display -->
<span>@BusinessClock.FromUtc(reservation.ReservationDate).ToString("HH:mm")</span>
<!-- Output: 14:30 -->
```

### GET /Cashier/Dashboard

Returns dashboard with notifications. All times in notifications are Vietnam local.

**Response includes:**
```json
{
	"notifications": [
		{
			"title": "⏰ Khách Sắp Đến",
			"message": "Bàn T02 - Nguyễn Văn A (4 người) - Còn 15 phút",
			"type": "warning",
			"createdAt": "2025-11-08T07:20:00Z",  // UTC
			"timeAgo": "2m"
		},
		{
			"title": "🔴 Bàn Quá Giờ",
			"message": "Bàn T03 - Nguyễn Thị B - Quá giờ 5 phút",
			"type": "danger",
			"createdAt": "2025-11-08T07:25:00Z",  // UTC
			"timeAgo": "1m"
		}
	]
}
```

**Time Display Logic:**
- Backend calculates times using `BusinessClock.Now` (Vietnam time)
- Stores UTC times in response
- Frontend converts to display format using `VietnamTimeUtil`

## Reservation Query Endpoints (Async APIs)

### GET /api/Reservations/Available

**Query Parameters:**
```
?date=2025-11-08T14:30&guests=4
```

**Processing:**
1. `date` parameter is datetime-local (Vietnam time)
2. Converted to UTC for database query: `BusinessClock.ToUtc(date)`
3. Queries available tables for that UTC time slot

**Response:**
```json
{
	"availableTables": [
		{
			"tableId": 1,
			"tableNumber": "T01",
			"capacity": 4,
			"availability": "Available for 14:30-16:30"
		}
	]
}
```

### GET /api/Reservations/UpcomingReservations

**Response:**
```json
{
	"reservations": [
		{
			"reservationId": 123,
			"reservationTime": "2025-11-08T07:30:00Z",  // UTC
			"tableNumber": "T02",
			"customerName": "Nguyễn Văn A",
			"minutesUntilArrival": 15
		}
	]
}
```

**Frontend Processing:**
```javascript
// Convert to Vietnam countdown
const countdown = VietnamTimeUtil.formatCountdown(reservation.reservationTime);
// Result: "Còn 15 phút"
```

### GET /api/Reservations/OverdueReservations

**Response:**
```json
{
	"reservations": [
		{
			"reservationId": 124,
			"reservationTime": "2025-11-08T07:25:00Z",  // UTC
			"tableNumber": "T03",
			"customerName": "Nguyễn Thị B",
			"minutesOverdue": 5,
			"status": "Pending"
		}
	]
}
```

**Auto-Cancellation Trigger:**
- Background service queries this endpoint
- Auto-cancels if `minutesOverdue >= 30`
- Updates status to "Cancelled"
- Logs action

## SignalR Real-time Notifications

When reservation status changes, SignalR broadcasts update:

```javascript
// Client-side listener
connection.on("ReservationStatusChanged", function (data) {
	// data.reservationId: 123
	// data.oldStatus: "Pending"
	// data.newStatus: "Cancelled"
	// data.reason: "Auto-cancelled: 30+ minutes late"
	// data.timestamp: "2025-11-08T08:00:00Z"  // UTC

	const localTime = VietnamTimeUtil.toVietnamTime(data.timestamp);
	console.log(`Reservation cancelled at ${localTime.toLocaleTimeString('vi-VN')}`);
});
```

## Error Handling with Timezones

### Invalid Reservation Time

**Request:**
```json
{
	"reservationDate": "2025-11-08T14:30"  // In the past
}
```

**Response:**
```json
{
	"success": false,
	"error": "Thời gian đặt phải ở tương lai.",
	"code": "INVALID_DATE",
	"currentVietnamTime": "2025-11-08T14:45:00Z"  // For debugging
}
```

### Business Hours Check

If implementing business hours:
```csharp
var localTime = BusinessClock.FromUtc(reservationDate);
var hour = localTime.Hour;  // 0-23 in Vietnam time

if (hour < 10 || hour > 22)  // Outside 10am-10pm Vietnam time
	return error("Ngoài giờ hoạt động.");
```

## Mobile App Integration

### For Mobile Developers

1. **Send times as datetime-local to API:**
   ```javascript
   const localTime = document.getElementById('datePicker').value;
   // Format: "2025-11-08T14:30" (no timezone info)
   fetch('/api/reservations/create', {
	   body: JSON.stringify({ reservationDate: localTime })
   });
   ```

2. **Receive UTC times from API:**
   ```json
   { "reservationDate": "2025-11-08T07:30:00Z" }
   ```

3. **Convert to display on mobile:**
   ```javascript
   // Use moment.js or similar
   const vietnamTime = moment.utc(data.reservationDate).tz('Asia/Ho_Chi_Minh');
   console.log(vietnamTime.format('HH:mm')); // Output: 14:30
   ```

## Testing API Endpoints with Timezone

### Using cURL

```bash
# Create reservation for 30 minutes from now (Vietnam time)
# Current time: 14:00 Vietnam (07:00 UTC)
curl -X POST http://localhost:5000/Customer/Reservations/Create \
  -H "Content-Type: application/json" \
  -d '{
	"reservationDate": "2025-11-08T14:30",
	"numberOfGuests": 4,
	"tableId": 1,
	"notes": "Test"
  }'

# Query available tables at 14:30 Vietnam time
curl "http://localhost:5000/api/Reservations/Available?date=2025-11-08T14:30&guests=4"

# Get overdue reservations
curl "http://localhost:5000/api/Reservations/OverdueReservations"
```

### Using Postman

1. **Set timestamp in pre-request script:**
   ```javascript
   const now = new Date();
   const vietnamTime = new Date(now.toLocaleString('en-US', { timeZone: 'Asia/Ho_Chi_Minh' }));
   vietnamTime.setMinutes(vietnamTime.getMinutes() + 30);

   // Format as datetime-local
   const year = vietnamTime.getFullYear();
   const month = String(vietnamTime.getMonth() + 1).padStart(2, '0');
   const day = String(vietnamTime.getDate()).padStart(2, '0');
   const hours = String(vietnamTime.getHours()).padStart(2, '0');
   const minutes = String(vietnamTime.getMinutes()).padStart(2, '0');

   pm.environment.set("reservationDate", `${year}-${month}-${day}T${hours}:${minutes}`);
   ```

2. **Use in request body:**
   ```json
   {
	   "reservationDate": "{{reservationDate}}",
	   "numberOfGuests": 4
   }
   ```

## Debugging Timeline

### Scenario: "Why is my 14:30 reservation showing as 7:30 in database?"

**Answer:** This is correct! The system stores all times in UTC:
- **User Time:** 14:30 (Vietnam, UTC+7)
- **Database:** 07:30 (UTC)
- **Calculation:** 14:30 - 7 hours = 07:30 UTC ✓

**Verify in database:**
```sql
SELECT ReservationID, ReservationDate, ReservationStatus
FROM Reservations
WHERE ReservationID = 123;

-- Result:
-- ReservationID: 123
-- ReservationDate: 2025-11-08 07:30:00.0000000
-- ReservationStatus: Pending
```

**Verify in display (use BusinessClock):**
```csharp
var reservation = await context.Reservations.FindAsync(123);
var displayTime = BusinessClock.FromUtc(reservation.ReservationDate);
Console.WriteLine(displayTime);  // 2025-11-08 14:30:00
```

---

## Summary

| Layer | Time Format | Timezone | Example |
|---|---|---|---|
| **Database** | DateTime UTC | UTC | 2025-11-08T07:30:00Z |
| **API JSON** | ISO 8601 UTC | UTC | "2025-11-08T07:30:00Z" |
| **API Parameters** | datetime-local | Vietnam | "2025-11-08T14:30" |
| **Display (HTML)** | HH:mm | Vietnam | "14:30" |
| **JavaScript** | ISO 8601 UTC | UTC | "2025-11-08T07:30:00Z" |
| **Business Logic** | Local object | Vietnam | BusinessClock.Now |

---

**Version:** 1.0  
**Last Updated:** November 2025  
**Status:** ✅ Complete
