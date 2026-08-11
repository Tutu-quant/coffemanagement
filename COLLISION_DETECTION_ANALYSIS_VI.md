# 🔍 PHÂN TÍCH ĐỂN CHI TIẾT: XỬ LÝ ĐẶT TRÙNG BÀN

## 📍 VỊ TRÍ ĐOẠN MÃ

### 1. **Repository Layer** - Lớp Kiểm tra Xung Đột
**File**: `Repository/ReservationRepository.cs`

```csharp
// ❌ KIỂM TRA XÓA TRÙNG BÀN (Dòng 102-111)
public async Task<bool> HasConflictAsync(int tableId, DateTime reservationStart, DateTime reservationEnd)
{
	return await _context.Reservations.AnyAsync(r =>
		r.TableID == tableId &&                          // ✓ Cùng bàn
		!r.IsDeleted &&                                  // ✓ Không bị xóa
		r.ReservationStatus != "Cancelled" &&            // ✓ Không bị hủy
		r.ReservationStatus != "Completed" &&            // ✓ Chưa hoàn tất
		r.ReservationDate < reservationEnd &&            // ✓ Bắt đầu trước khi kết thúc
		r.ReservationDate.AddHours(2) > reservationStart // ✓ Kết thúc sau khi bắt đầu
	);
}
```

### 2. **Service Layer** - Gọi Kiểm tra Xung Đột
**File**: `Services/ReservationService.cs`

```csharp
// ✅ GỌI KIỂM TRA XÓA TRÙNG KHI TẠO ĐẶT BÀN (Dòng 93-100)
public async Task<ReservationCreateResult> CreateReservationAsync(
	int customerId,
	int tableId,
	DateTime reservationDate,
	int numberOfGuests,
	string? notes = null)
{
	// ... Các validation khác ...

	// Tính toán khoảng thời gian
	var reservationStart = reservationDate;
	var reservationEnd = reservationDate.AddMinutes(ReservationDurationMinutes); // 120 phút = 2 giờ

	// 🔴 KIỂM TRA XUNG ĐỘT TẠI ĐÂY
	var hasConflict = await _reservationRepository.HasConflictAsync(tableId, reservationStart, reservationEnd);

	if (hasConflict)
		return ReservationCreateResult.FailureResult(
			"Bàn đã được đặt gần khung giờ này.",           // Thông báo lỗi
			"RESERVATION_CONFLICT");                        // Error code

	// ✅ Nếu không xung đột → Tạo đặt bàn mới
	var reservation = new Reservation { ... };
	await _reservationRepository.AddAsync(reservation);
	return ReservationCreateResult.SuccessResult(reservation);
}
```

---

## 🧮 CÔNG THỨC KIỂM TRA XUNG ĐỘT (Collision Detection)

### Thuật Toán: **Interval Overlap Detection**

```
📊 BIỂU DIỄN THỜI GIAN:

Đặt bàn hiện tại (Existing):
━━━━━━━━━━━━━━━━━━━━━━
[18:00] ─────── [20:00]
  ↑                ↑
  Start            End (+ 2 giờ)

Khách muốn đặt lúc (New Request):
					[19:00] ─────── [21:00]
					  ↑                ↑
					  Start            End

					❌ XUNG ĐỘT !
```

### Điều Kiện Xung Đột (4 Conditions phải ALL thỏa)

```csharp
// 📌 ĐIỀU KIỆN 1: Cùng bàn
r.TableID == tableId
//  Bàn 01 == Bàn 01 ✓

// 📌 ĐIỀU KIỆN 2: Không bị xóa
!r.IsDeleted
//  IsDeleted = false ✓

// 📌 ĐIỀU KIỆN 3: Trạng thái hoạt động
r.ReservationStatus != "Cancelled" &&
r.ReservationStatus != "Completed"
//  Status ∈ { "Pending", "Confirmed", "CheckedIn" } ✓

// 📌 ĐIỀU KIỆN 4 + 5: Khoảng thời gian chồng lấn
//  Công thức toán học:
//  
//  existing.Start < new.End  AND  existing.End > new.Start
//
//  r.ReservationDate < reservationEnd  AND
//  r.ReservationDate.AddHours(2) > reservationStart

r.ReservationDate < reservationEnd &&
r.ReservationDate.AddHours(2) > reservationStart
```

---

## 📈 CÁC VÍ DỤ CỤ THỂ

### ✅ Ví Dụ 1: KHÔNG XUNG ĐỘT (Bàn trống)

```
Đặt bàn 1: [18:00 - 20:00]
Khách muốn đặt bàn 1: [20:00 - 22:00]

Kiểm tra:
r.ReservationDate (18:00) < reservationEnd (22:00)  ✓ TRUE
r.ReservationDate.AddHours(2) (20:00) > reservationStart (20:00)  ✗ FALSE (20:00 = 20:00)

Kết quả: FALSE → ✅ Không xung đột → Cho phép đặt
```

### ❌ Ví Dụ 2: XUNG ĐỘT (Bàn bị chiếm)

```
Đặt bàn 1: [18:00 - 20:00]
Khách muốn đặt bàn 1: [19:00 - 21:00]

Kiểm tra:
r.ReservationDate (18:00) < reservationEnd (21:00)  ✓ TRUE
r.ReservationDate.AddHours(2) (20:00) > reservationStart (19:00)  ✓ TRUE

Kết quả: TRUE → ❌ Xung đột → Từ chối đặt
```

### ❌ Ví Dụ 3: XUNG ĐỘT (Khách muốn đặt trước)

```
Đặt bàn 1: [19:00 - 21:00]
Khách muốn đặt bàn 1: [17:00 - 19:00]

Kiểm tra:
r.ReservationDate (19:00) < reservationEnd (19:00)  ✗ FALSE
r.ReservationDate.AddHours(2) (21:00) > reservationStart (17:00)  ✓ TRUE

Kết quả: FALSE → ✅ Không xung đột → Cho phép đặt
```

### ✅ Ví Dụ 4: KHÔNG XUNG ĐỘT (Đủ khoảng cách)

```
Đặt bàn 1: [18:00 - 20:00]
Khách muốn đặt bàn 1: [21:00 - 23:00]

Kiểm tra:
r.ReservationDate (18:00) < reservationEnd (23:00)  ✓ TRUE
r.ReservationDate.AddHours(2) (20:00) > reservationStart (21:00)  ✗ FALSE

Kết quả: FALSE → ✅ Không xung đột → Cho phép đặt
```

---

## 🔗 CÓ 3 METHODS LIÊN QUAN ĐẾN XUNG ĐỘT

### Method 1: `HasConflictAsync()` ⚡ (Dùng nhất)
**Dòng 102-111** | **ReservationRepository.cs**

```csharp
public async Task<bool> HasConflictAsync(
	int tableId, 
	DateTime reservationStart, 
	DateTime reservationEnd)
{
	return await _context.Reservations.AnyAsync(r => /* ... */);
}
```

**Mục đích**: Kiểm tra xem có ĐẶT BÀN nào xung đột không
**Trả về**: 
- `true` → Xung đột
- `false` → Không xung đột

**Gọi từ**: 
- ✅ `ReservationService.CreateReservationAsync()` (dòng 96)
- ✅ `ReservationService.GetAvailableTablesAsync()` (dòng 45)

---

### Method 2: `GetConflictingReservationsAsync()`
**Dòng 113-128** | **ReservationRepository.cs**

```csharp
public async Task<List<Reservation>> GetConflictingReservationsAsync(
	int tableId,
	DateTime reservationStart,
	DateTime reservationEnd)
{
	return await _context.Reservations
		.Where(r => /* điều kiện xung đột */)
		.OrderBy(r => r.ReservationDate)
		.ToListAsync();
}
```

**Mục đích**: Lấy **danh sách** các ĐẶT BÀN xung đột
**Trả về**: List<Reservation> (có thể trống)

**Ứng dụng**: Có thể dùng để xem chi tiết các đặt bàn nào bị xung đột

---

### Method 3: `GetReservationConflictAsync()`
**Dòng 151-165** | **ReservationRepository.cs**

```csharp
public async Task<bool> GetReservationConflictAsync(
	int tableId,
	DateTime reservationDate,
	int durationMinutes = 120)
{
	var reservationEnd = reservationDate.AddMinutes(durationMinutes);
	return await _context.Reservations.AnyAsync(r => /* ... */);
}
```

**Mục đích**: Tương tự `HasConflictAsync()` nhưng tính `reservationEnd` bên trong
**Trả về**: 
- `true` → Xung đột
- `false` → Không xung đột

---

## 🔴 FLOW CHI TIẾT KIỂM TRA XUNG ĐỘT

```
┌─────────────────────────────────────────┐
│ Khách gọi: POST /Customer/Reservations  │
│           Create?TableID=1, Date=18:00  │
└────┬────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────────────────────────┐
│ ReservationsController.Create() (POST)              │
│ (Areas/Customer/Controllers/ReservationsController) │
│ Dòng: 43-63                                         │
└────┬────────────────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────────────────┐
│ ReservationService.CreateReservationAsync()          │
│ (Services/ReservationService.cs)                     │
│ Dòng: 58-118                                         │
│                                                      │
│ Validation sequence:                                 │
│ 1. customerId > 0 ✓                                  │
│ 2. tableId > 0 ✓                                     │
│ 3. numberOfGuests: 1-50 ✓                            │
│ 4. reservationDate > DateTime.UtcNow ✓              │
│ 5. Customer tồn tại ✓                                │
│ 6. Table tồn tại ✓                                   │
│ 7. Table không bảo trì ✓                             │
│ 8. numberOfGuests ≤ table.Capacity ✓                │
│                                                      │
│ ⏰ BƯỚC QUAN TRỌNG:                                  │
│ Dòng 93-100:                                         │
│ ┌──────────────────────────────────────┐            │
│ │ var reservationStart = 18:00         │            │
│ │ var reservationEnd = 20:00 (+ 2h)    │            │
│ │                                      │            │
│ │ var hasConflict =                    │            │
│ │   HasConflictAsync(                  │            │
│ │     tableId=1,                       │            │
│ │     reservationStart=18:00,          │            │
│ │     reservationEnd=20:00             │            │
│ │   )                                  │            │
│ │                                      │            │
│ │ if (hasConflict) → ❌ Từ chối        │            │
│ └──────────────────────────────────────┘            │
│                                                      │
└────┬──────────────────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────────────────┐
│ ReservationRepository.HasConflictAsync()             │
│ (Repository/ReservationRepository.cs)                │
│ Dòng: 102-111                                        │
│                                                      │
│ SQL Query (Entity Framework):                        │
│ ┌──────────────────────────────────────────────────┐│
│ │ SELECT EXISTS (                                  ││
│ │   FROM Reservations                              ││
│ │   WHERE                                          ││
│ │     TableID = 1                    [ĐIỀU KIỆN 1]││
│ │     AND IsDeleted = false           [ĐIỀU KIỆN 2]││
│ │     AND ReservationStatus NOT IN    [ĐIỀU KIỆN 3]││
│ │         ('Cancelled', 'Completed')               ││
│ │     AND ReservationDate < 20:00     [ĐIỀU KIỆN 4]││
│ │     AND ReservationDate.AddHours(2) [ĐIỀU KIỆN 5]││
│ │         > 18:00                                  ││
│ │ )                                                ││
│ └──────────────────────────────────────────────────┘│
│                                                      │
│ Kết quả: true/false                                 │
└────┬──────────────────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────────────────┐
│ Quay lại ReservationService.CreateReservationAsync() │
│                                                      │
│ if (hasConflict)                                     │
│ {                                                    │
│     return FailureResult(                            │
│         "Bàn đã được đặt gần khung giờ này.",      │
│         "RESERVATION_CONFLICT"                      │
│     );                                               │
│ }                                                    │
│                                                      │
│ // Nếu không xung đột:                              │
│ var reservation = new Reservation { ... };         │
│ await _reservationRepository.AddAsync(             │
│     reservation                                     │
│ );                                                   │
│                                                      │
│ return SuccessResult(reservation);                  │
│                                                      │
└────┬──────────────────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────────────┐
│ ReservationsController.Create()         │
│                                         │
│ if (!result.Success)                    │
│ {                                       │
│     // Hiển thị lỗi: "Bàn đã được đặt" │
│     ModelState.AddModelError(           │
│         "", result.Message              │
│     );                                  │
│     return View(model);                 │
│ }                                       │
│                                         │
│ // Thành công: Redirect sang History    │
│ TempData["SuccessMessage"] =            │
│     result.Message;                     │
│ return RedirectToAction(                │
│     nameof(History)                     │
│ );                                      │
│                                         │
└─────────────────────────────────────────┘
```

---

## 📋 BẢNG TÓMS VỊ TRÍ TRONG MÃ

| Vị Trí | File | Dòng | Mục Đích |
|--------|------|------|---------|
| **Method gọi** | Services/ReservationService.cs | 96 | Gọi `HasConflictAsync()` |
| **Method gọi** | Services/ReservationService.cs | 45 | Gọi trong `GetAvailableTablesAsync()` |
| **Method kiểm tra** | Repository/ReservationRepository.cs | 102-111 | `HasConflictAsync()` - Kiểm tra xung đột |
| **Method lấy danh sách** | Repository/ReservationRepository.cs | 113-128 | `GetConflictingReservationsAsync()` |
| **Method thay thế** | Repository/ReservationRepository.cs | 151-165 | `GetReservationConflictAsync()` |

---

## 🎯 LUỒNG KIỂM TRA XÓA TRÙNG TÓM TẮT

```
INPUT: TableID=1, Date=18:00

┌──────────────────────────────────┐
│ 1. Lấy thời gian kết thúc       │
│    End = 18:00 + 120 phút = 20:00│
└──────────────┬───────────────────┘
			   │
			   ▼
┌──────────────────────────────────────────┐
│ 2. Truy vấn DB tìm đặt bàn xung đột      │
│    SELECT * FROM Reservations WHERE:     │
│    - TableID = 1                         │
│    - IsDeleted = false                   │
│    - Status ≠ Cancelled/Completed        │
│    - ReservationDate < 20:00             │
│    - ReservationDate + 2h > 18:00        │
└──────────────┬───────────────────────────┘
			   │
			   ▼
		┌─────────────┐
		│ Có kết quả? │
		└────┬────┬──┘
			 │    │
		✓ CÓ    ✗ KHÔNG
			 │    │
			 ▼    ▼
		 XUNG   TRỐNG
		 ĐỘT    ✅
		 ❌

		 Từ chối          Cho phép
```

---

## 💡 ĐIỂM QUAN TRỌNG

1. **Thời gian mặc định**: 2 giờ (120 phút) cho mỗi lần đặt
   - Defined: `const int ReservationDurationMinutes = 120;` (Dòng 14 - ReservationService.cs)

2. **Chỉ kiểm tra trạng thái hoạt động**:
   - `Status != "Cancelled"` 
   - `Status != "Completed"`
   - → Các đặt bàn bị hủy hoặc hoàn tất KHÔNG ảnh hưởng

3. **Xóa mềm không xóa kiểm tra**:
   - `!r.IsDeleted` 
   - → Những đặt bàn đã xóa KHÔNG tính xung đột

4. **Không có buffer**:
   - `BufferMinutesBefore = 0` (Dòng 15)
   - `BufferMinutesAfter = 0` (Dòng 16)
   - → Có thể đặt lập tức sau lần trước (0 phút chờ)

---

## 🔧 CÓ THỂ ĐIỀU CHỈNH

```csharp
// 📝 FILE: Services/ReservationService.cs (Dòng 14-16)

// Hiện tại:
private const int ReservationDurationMinutes = 120;  // 2 giờ
private const int BufferMinutesBefore = 0;          // Không có chờ trước
private const int BufferMinutesAfter = 0;           // Không có chờ sau

// Có thể thay thành:
private const int ReservationDurationMinutes = 90;   // 1.5 giờ
private const int BufferMinutesBefore = 15;         // Chờ 15 phút trước
private const int BufferMinutesAfter = 15;          // Chờ 15 phút sau
```

---

**✅ Tóm lại: Đoạn mã xử lý xung đột đặt bàn nằm ở:**

1. **Repository**: `ReservationRepository.HasConflictAsync()` (Dòng 102-111)
2. **Service**: `ReservationService.CreateReservationAsync()` (Dòng 93-100 gọi kiểm tra)
3. **Service**: `ReservationService.GetAvailableTablesAsync()` (Dòng 45 gọi kiểm tra)

