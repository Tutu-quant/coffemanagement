# 🎯 HƯỚNG DẪN CHỨC NĂNG ĐẶT BÀN - HỆ THỐNG QUẢN LÝ QUÁN CAFE

## 📋 MỤC LỤC
1. [Tổng quan chức năng](#tổng-quan)
2. [Kiến trúc hệ thống](#kiến-trúc)
3. [Luồng hoạt động chi tiết](#luồng-hoạt-động)
4. [Các thành phần chính](#các-thành-phần)
5. [Trạng thái đặt bàn](#trạng-thái)
6. [Quy tắc kinh doanh](#quy-tắc-kinh-doanh)

---

## 🎭 Tổng Quan

### Định Nghĩa
Chức năng **Đặt Bàn (Reservations)** cho phép khách hàng đặt bàn tại quán cafe trước thời gian đến quán.

### Tính Năng Chính
- ✅ **Tìm bàn còn trống** dựa trên thời gian và số khách
- ✅ **Đặt bàn** với các thông tin cơ bản (ngày giờ, số khách, ghi chú)
- ✅ **Xem lịch sử đặt bàn** và trạng thái các lần đặt
- ✅ **Hủy đặt bàn** (nếu còn có thể)
- ✅ **Xem chi tiết** từng lần đặt bàn

### Đối Tượng Sử Dụng
- **Khách hàng đã đăng nhập** vào hệ thống

### Truy Cập
- **Route**: `/Customer/Reservations`
- **Menu**: "Đặt bàn" trong sidebar khách hàng

---

## 🏗️ Kiến Trúc Hệ Thống

### Cấu Trúc Tầng

```
┌─────────────────────────────────────────┐
│     CONTROLLER LAYER                    │
│  ReservationsController                 │
│  - Create, Cancel, History, Details     │
│  - SearchAvailableTables                │
└──────────────────┬──────────────────────┘
				   │
┌──────────────────▼──────────────────────┐
│     SERVICE LAYER                       │
│  ReservationService                     │
│  - CreateReservationAsync               │
│  - GetAvailableTablesAsync              │
│  - CancelReservationAsync               │
│  - ConfirmReservationAsync              │
└──────────────────┬──────────────────────┘
				   │
┌──────────────────▼──────────────────────┐
│     REPOSITORY LAYER                    │
│  ReservationRepository                  │
│  - CRUD Operations                      │
│  - HasConflictAsync (Kiểm tra xung đột) │
└──────────────────┬──────────────────────┘
				   │
┌──────────────────▼──────────────────────┐
│     DATA LAYER                          │
│  Entity Framework Core + SQLite DB      │
│  - Reservation Entity                   │
│  - RestaurantTable Entity               │
│  - Customer Entity                      │
└─────────────────────────────────────────┘
```

### Diagram Mối Quan Hệ Entity

```
┌─────────────────┐      1───────────N    ┌──────────────────┐
│   Customer      │◄────────────────────┤  Reservation      │
│                 │                       │                  │
│ - CustomerID    │                       │ - ReservationID  │
│ - Name          │                       │ - CustomerID (FK)│
│ - Email         │                       │ - TableID (FK)   │
│ - Phone         │                       │ - ReservationDate│
│ - Points        │                       │ - NumberOfGuests │
│ - IsDeleted     │                       │ - Status         │
└─────────────────┘                       │ - Notes          │
										  │ - CreatedAt      │
										  │ - IsDeleted      │
										  └────────┬─────────┘
												   │
										1────────N │
												   │
										  ┌────────▼─────────┐
										  │ RestaurantTable   │
										  │                  │
										  │ - TableID        │
										  │ - TableNumber    │
										  │ - Capacity       │
										  │ - Location       │
										  │ - Status         │
										  │ - IsDeleted      │
										  └──────────────────┘
```

---

## 🔄 Luồng Hoạt Động Chi Tiết

### 1️⃣ LUỒNG TẠO ĐẶT BÀN (Trang Create)

#### Flow Đơn Giản
```
┌─────────┐
│ Start   │
└────┬────┘
	 │
	 ▼
┌────────────────────────────┐
│ Khách hàng truy cập trang  │
│ /Customer/Reservations/    │
│ Create                     │
└────┬───────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ Giao diện hiển thị:                      │
│ 1. Chọn Ngày & Giờ                       │
│ 2. Chọn Số Khách (1-50)                  │
│ 3. Nút "Tìm Bàn Còn Trống"               │
│ 4. Danh sách bàn (chưa hiển thị)         │
│ 5. Ghi chú (tùy chọn)                    │
│ 6. Tóm tắt đặt bàn (sidebar phải)        │
└──────┬───────────────────────────────────┘
	   │
	   ▼
┌─────────────────────────────────────────┐
│ Khách chọn Ngày/Giờ & Số Khách          │
│ Nhấn "Tìm Bàn Còn Trống"                │
└────┬────────────────────────────────────┘
	 │
	 ▼ (Gửi AJAX POST)
┌────────────────────────────────────────────┐
│ ReservationsController.                    │
│ SearchAvailableTables()                    │
│                                            │
│ Request Body:                              │
│ {                                          │
│   reservationDate: "2024-01-15T18:00",     │
│   numberOfGuests: 4,                       │
│   durationMinutes: 120                     │
│ }                                          │
└────┬───────────────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────────────┐
│ ReservationService.                        │
│ GetAvailableTablesAsync()                  │
│                                            │
│ 1. Lấy tất cả bàn từ DB                    │
│ 2. Lọc: Capacity >= numberOfGuests         │
│ 3. Lọc: TableStatus != "Maintenance"       │
│ 4. Kiểm tra xung đột với các đặt bàn       │
│    khác bằng hasConflictAsync()            │
│                                            │
│ Công thức xung đột (2 giờ):                │
│ IF reservationTime < endTime AND           │
│    reservationTime + 2h > startTime        │
│    THEN Conflict!                          │
│                                            │
│ 5. Trả về danh sách bàn còn trống         │
└────┬───────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────────────┐
│ JavaScript (reservation.js)              │
│ renderAvailableTables()                  │
│                                          │
│ Hiển thị từng bàn:                       │
│ ┌─────────────────────────────────┐    │
│ │ Bàn 01                          │    │
│ │ 🪑 4 khách                      │    │
│ │ 🏢 Tầng 1                       │    │
│ │ Status: ✓ Còn trống             │    │
│ │ [Chọn]                          │    │
│ └─────────────────────────────────┘    │
└────┬────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────┐
│ Khách chọn bàn                  │
│ (Click vào card bàn)            │
└────┬────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ JavaScript selectTable()                 │
│ 1. Lưu TableID vào input hidden          │
│ 2. Đánh dấu bàn được chọn (Active)       │
│ 3. Cập nhật Tóm Tắt                     │
│ 4. Enable nút "Xác Nhận Đặt Bàn"        │
└────┬───────────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────────────┐
│ Tóm Tắt Đặt Bàn (Sidebar Phải):           │
│ ┌────────────────────────────────────┐   │
│ │ Bàn: Bàn 01                        │   │
│ │ Sức chứa: 4 khách                  │   │
│ │ Tầng: Tầng 1                       │   │
│ │ Ngày: 15/01/2024                   │   │
│ │ Giờ: 18:00                         │   │
│ │ Khách: 4                           │   │
│ │ [✓ Xác Nhận Đặt Bàn]               │   │
│ └────────────────────────────────────┘   │
└────┬───────────────────────────────────────┘
	 │
	 ▼ (Nhấn Xác Nhận)
┌────────────────────────────────────────────┐
│ Form Submit (POST)                         │
│ /Customer/Reservations/Create              │
│                                            │
│ Form Data:                                 │
│ {                                          │
│   TableID: 1,                              │
│   ReservationDate: "2024-01-15T18:00",     │
│   NumberOfGuests: 4,                       │
│   Notes: "Yêu cầu bàn gần cửa"            │
│ }                                          │
└────┬───────────────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────────────┐
│ ReservationsController.Create()            │
│ (POST - Server Side Validation)            │
│                                            │
│ 1. Kiểm tra đăng nhập (IsLoggedIn)        │
│ 2. Lấy thông tin khách từ Session         │
│ 3. Gọi ReservationService.                │
│    CreateReservationAsync()                │
└────┬───────────────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────────────┐
│ ReservationService.                        │
│ CreateReservationAsync()                   │
│                                            │
│ Validation:                                │
│ ✓ customerId > 0                           │
│ ✓ tableId > 0                              │
│ ✓ numberOfGuests: 1-50                     │
│ ✓ reservationDate > DateTime.UtcNow        │
│ ✓ Customer tồn tại và không bị xóa        │
│ ✓ Table tồn tại và không bị xóa            │
│ ✓ Table != "Maintenance"                   │
│ ✓ numberOfGuests <= table.Capacity         │
│ ✓ Không xung đột với đặt bàn khác         │
│                                            │
│ Nếu có lỗi:                                │
│ → Trả về ErrorResult với Message           │
│                                            │
│ Nếu thành công:                            │
│ → Tạo Reservation object:                  │
│   {                                        │
│     CustomerID: customerId,                │
│     TableID: tableId,                      │
│     ReservationDate: reservationDate,      │
│     NumberOfGuests: numberOfGuests,        │
│     ReservationStatus: "Pending",          │
│     Notes: notes,                          │
│     CreatedAt: DateTime.UtcNow,            │
│     UpdatedAt: DateTime.UtcNow,            │
│     IsDeleted: false                       │
│   }                                        │
│                                            │
│ → Lưu vào DB                               │
│ → Trả về SuccessResult                     │
└────┬───────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────┐
│ Nếu lỗi:                        │
│ → Hiển thị thông báo lỗi        │
│ → Giữ lại form (kèm dữ liệu)   │
│                                 │
│ Nếu thành công:                 │
│ → Lưu thông báo vào TempData    │
│ → Redirect sang History         │
└────┬────────────────────────────┘
	 │
	 ▼
┌──────────────────────┐
│ Hiển thị lịch sử     │
│ với đặt bàn mới      │
└──────────────────────┘
```

---

### 2️⃣ LUỒNG XEM LỊCH SỬ ĐẶT BÀN (History)

```
┌────────────────────────────────────┐
│ Khách hàng truy cập:               │
│ /Customer/Reservations/History     │
└────┬───────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────┐
│ ReservationsController.History()   │
│ (GET)                              │
│                                    │
│ 1. Kiểm tra đăng nhập              │
│ 2. Lấy CustomerID từ Session       │
│ 3. Gọi ReservationService.         │
│    GetCustomerReservationsAsync()  │
└────┬───────────────────────────────┘
	 │
	 ▼
┌────────────────────────────────────┐
│ ReservationRepository.             │
│ GetByCustomerAsync()               │
│                                    │
│ SQL Query:                         │
│ SELECT * FROM Reservations         │
│ WHERE CustomerID = @customerId     │
│   AND IsDeleted = false            │
│ ORDER BY ReservationDate DESC      │
│                                    │
│ Include:                           │
│ - Customer info                    │
│ - Table info                       │
└────┬───────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────────────┐
│ Hiển thị Bảng:                          │
│                                         │
│ │ # │ Bàn │ Thời gian │ Số khách │ Trạng thái │ Thao tác │
│ ├────────────────────────────────────────────────────┤
│ │ 1 │ 01  │ 15/01... │ 4       │ 🟡 Chờ xác nhận │ Chi tiết│
│ │ 2 │ 03  │ 10/01... │ 2       │ 🟢 Đã xác nhận  │ Chi tiết│
│ │ 3 │ 02  │ 05/01... │ 6       │ ✓ Hoàn tất    │ Chi tiết│
│ │ 4 │ 04  │ 01/01... │ 4       │ ✗ Đã hủy      │ Chi tiết│
│                                         │
└─────────────────────────────────────────┘
```

---

### 3️⃣ LUỒNG XEM CHI TIẾT ĐẶT BÀN (Details)

```
┌──────────────────────────────────────────┐
│ Khách truy cập:                          │
│ /Customer/Reservations/Details/{id}      │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ ReservationsController.Details()         │
│ (GET - id = ReservationID)               │
│                                          │
│ 1. Kiểm tra đăng nhập                    │
│ 2. Lấy CustomerID từ Session             │
│ 3. Gọi ReservationService.               │
│    GetReservationDetailsAsync(id, cusId) │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ ReservationService.                      │
│ GetReservationDetailsAsync()             │
│                                          │
│ Kiểm tra:                                │
│ ✓ reservationId > 0                      │
│ ✓ customerId > 0                         │
│ ✓ Tồn tại reservation                    │
│ ✓ Chủ sở hữu = customerId (bảo mật!)    │
│                                          │
│ Nếu OK → Trả về Reservation              │
│ Nếu không → Trả về null                  │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────┐
│ Hiển thị Chi Tiết:       │
│ ┌────────────────────┐  │
│ │ Đặt bàn #123      │  │
│ │ 🟡 Chờ xác nhận   │  │
│ ├────────────────────┤  │
│ │ Bàn: 01           │  │
│ │ Số khách: 4       │  │
│ │ Ngày giờ: ...     │  │
│ │ Vị trí: Tầng 1    │  │
│ │ Ghi chú: ...      │  │
│ │                   │  │
│ │ [Hủy đặt bàn]    │  │
│ └────────────────────┘  │
└──────────────────────────┘
```

---

### 4️⃣ LUỒNG HỦY ĐẶT BÀN (Cancel)

```
┌─────────────────────────────────┐
│ Khách nhấn nút "Hủy đặt bàn"    │
│ (từ Details hoặc History)       │
└────┬──────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────┐
│ Confirm Dialog:                 │
│ "Bạn chắc chắn muốn hủy?"       │
│ [Hủy] [Xác nhận]                │
└────┬──────────────────────────────┘
	 │
	 ▼ (Chọn Xác nhận)
┌──────────────────────────────────────────┐
│ Form Submit (POST)                       │
│ /Customer/Reservations/Cancel/{id}       │
│ Gửi Anti-Forgery Token                   │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ ReservationsController.Cancel()          │
│ (POST)                                   │
│                                          │
│ 1. Kiểm tra đăng nhập                    │
│ 2. Lấy CustomerID từ Session             │
│ 3. Gọi ReservationService.               │
│    CancelReservationAsync(id, cusId)     │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ ReservationService.                      │
│ CancelReservationAsync()                 │
│                                          │
│ Validation:                              │
│ ✓ reservationId > 0                      │
│ ✓ Reservation tồn tại                    │
│ ✓ Chủ sở hữu = customerId (bảo mật!)    │
│ ✓ Status = "Pending" hoặc "Confirmed"    │
│   (không được hủy: "Completed", ...)     │
│                                          │
│ Nếu OK:                                  │
│ → Set ReservationStatus = "Cancelled"    │
│ → Set UpdatedAt = DateTime.UtcNow        │
│ → Lưu vào DB                             │
│ → Trả về SuccessResult                   │
│                                          │
│ Nếu lỗi:                                 │
│ → Trả về FailureResult (Message + Code)  │
└────┬─────────────────────────────────────┘
	 │
	 ▼
┌─────────────────────────────────┐
│ Nếu lỗi:                        │
│ → TempData["ErrorMessage"]      │
│ → Redirect sang History         │
│                                 │
│ Nếu thành công:                 │
│ → TempData["SuccessMessage"]    │
│ → Redirect sang History         │
└────┬────────────────────────────┘
	 │
	 ▼
┌──────────────────────────────────────────┐
│ Hiển thị History với thông báo           │
│ Đặt bàn đã được đánh dấu: ✗ Đã hủy      │
└──────────────────────────────────────────┘
```

---

## 🧩 Các Thành Phần Chính

### 1. CONTROLLER: `ReservationsController`
**File**: `Areas/Customer/Controllers/ReservationsController.cs`

#### Các Action (Hành động):

| Action | Method | URL | Mục đích |
|--------|--------|-----|---------|
| `Create` | GET | `/Customer/Reservations/Create` | Hiển thị form đặt bàn |
| `Create` | POST | `/Customer/Reservations/Create` | Xử lý tạo đặt bàn mới |
| `History` | GET | `/Customer/Reservations/History` | Danh sách đặt bàn của khách |
| `Details` | GET | `/Customer/Reservations/Details/{id}` | Chi tiết 1 lần đặt bàn |
| `Cancel` | POST | `/Customer/Reservations/Cancel/{id}` | Hủy đặt bàn |
| `SearchAvailableTables` | POST | `/Customer/Reservations/SearchAvailableTables` | Tìm bàn còn trống (AJAX) |

---

### 2. SERVICE: `ReservationService`
**File**: `Services/ReservationService.cs`

#### Các Method Chính:

```csharp
// Tạo đặt bàn mới
Task<ReservationCreateResult> CreateReservationAsync(
	int customerId,
	int tableId,
	DateTime reservationDate,
	int numberOfGuests,
	string? notes = null);

// Lấy danh sách bàn còn trống
Task<List<RestaurantTable>> GetAvailableTablesAsync(
	DateTime reservationDate,
	int numberOfGuests,
	int durationMinutes = 120);

// Hủy đặt bàn
Task<ReservationOperationResult> CancelReservationAsync(
	int reservationId,
	int customerId);

// Xác nhận đặt bàn (dùng cho Admin)
Task<ReservationOperationResult> ConfirmReservationAsync(int reservationId);

// Lấy lịch sử đặt bàn của khách
Task<List<Reservation>> GetCustomerReservationsAsync(int customerId);

// Lấy chi tiết đặt bàn
Task<Reservation?> GetReservationDetailsAsync(int reservationId, int customerId);

// Kiểm tra xung đột
Task<bool> HasReservationConflictAsync(
	int tableId,
	DateTime reservationDate,
	int durationMinutes = 120);
```

#### Quy Tắc Kinh Doanh (Business Logic):
- **Thời gian mặc định**: 2 giờ (120 phút) cho mỗi lần đặt
- **Xung đột**: Nếu có 2 đặt bàn trong khung giờ 2 giờ → Xung đột
- **Buffer**: Không có buffer trước/sau (có thể điều chỉnh)
- **Sức chứa**: Số khách ≤ sức chứa bàn

---

### 3. REPOSITORY: `ReservationRepository`
**File**: `Repository/ReservationRepository.cs`

#### Các Method CRUD:

```csharp
// Lấy 1 đặt bàn
Task<Reservation?> GetByIdAsync(int id);

// Lấy tất cả (không xóa)
Task<List<Reservation>> GetAllAsync();

// Lấy theo khách hàng
Task<List<Reservation>> GetByCustomerAsync(int customerId);

// Lấy theo bàn
Task<List<Reservation>> GetByTableAsync(int tableId);

// Lấy các đặt sắp tới (7 ngày)
Task<List<Reservation>> GetUpcomingAsync(int days = 7);

// Thêm mới
Task AddAsync(Reservation reservation);

// Cập nhật
Task UpdateAsync(Reservation reservation);

// Xóa mềm (soft delete)
Task DeleteAsync(int id);

// Kiểm tra xung đột
Task<bool> HasConflictAsync(
	int tableId,
	DateTime reservationStart,
	DateTime reservationEnd);

// Cập nhật trạng thái
Task UpdateStatusAsync(int reservationId, string newStatus);

// Lấy theo ngày
Task<List<Reservation>> GetReservationsByDateAsync(DateTime reservationDate);
```

---

### 4. ENTITY: `Reservation`
**File**: `Models/Entities/Reservation.cs`

```csharp
public class Reservation
{
	public int ReservationID { get; set; }              // ID duy nhất
	public int CustomerID { get; set; }                  // FK: Khách hàng
	public int TableID { get; set; }                     // FK: Bàn
	public DateTime ReservationDate { get; set; }        // Ngày giờ đặt
	public DateTime ReservationTime { get; set; }        // Thời gian đặt
	public DateTime? CheckinTime { get; set; }           // Thời gian nhận bàn
	public DateTime? CheckoutTime { get; set; }          // Thời gian rời bàn
	public int NumberOfGuests { get; set; }              // Số khách
	public string ReservationStatus { get; set; }        // Trạng thái
	public string? Notes { get; set; }                   // Ghi chú
	public DateTime CreatedAt { get; set; }              // Thời gian tạo
	public DateTime? UpdatedAt { get; set; }             // Lần sửa cuối
	public bool IsDeleted { get; set; }                  // Xóa mềm

	// Navigation Properties
	public virtual Customer? Customer { get; set; }
	public virtual RestaurantTable? Table { get; set; }
}
```

---

### 5. VIEW MODELS

#### `ReservationViewModel`
- Dùng cho **Form Đặt Bàn**
- Chứa: TableID, ReservationDate, NumberOfGuests, Notes
- Kèm danh sách bàn và bàn có sẵn

#### `AvailableTableViewModel`
- Dùng để **Hiển thị Bàn Còn Trống**
- Chứa: TableID, TableNumber, Capacity, Location, IsSelected

#### `ReservationSummaryViewModel`
- Dùng để **Hiển thị Tóm Tắt**
- Chứa: SelectedTable, ReservationDate, NumberOfGuests, Status

#### `ReservationHistoryViewModel`
- Dùng để **Hiển thị Lịch Sử**
- Chứa: ReservationID, TableNumber, Date, Guests, Status

---

### 6. VIEWS (Razor Pages)

#### `Create.cshtml`
- Layout gồm 2 phần:
  - **Trái**: Form nhập (ngày giờ, số khách, tìm bàn, ghi chú)
  - **Phải**: Tóm tắt (sticky)
- Bàn hiển thị dưới dạng Grid Card
- Hỗ trợ AJAX tìm kiếm

#### `History.cshtml`
- Bảng liệt kê các đặt bàn
- Hiển thị: #, Bàn, Thời gian, Số khách, Trạng thái, Thao tác

#### `Details.cshtml`
- Hiển thị chi tiết từng lần đặt bàn
- Nút hủy (nếu có thể)

---

### 7. JAVASCRIPT: `reservation.js`

#### Main Object: `ReservationManager`

```javascript
const ReservationManager = {
	selectedTableId: null,
	selectedTableNumber: null,
	selectedTableCapacity: 0,

	// Khởi tạo event listeners
	init(),

	// Validate form (ngày, khách, bàn)
	validateForm(),

	// Tìm bàn còn trống via AJAX
	async searchAvailableTables(),

	// Render danh sách bàn
	renderAvailableTables(tables),

	// Xử lý khi khách chọn bàn
	selectTable(cardElement),

	// Cập nhật tóm tắt
	updateSummary()
};
```

#### Key Features:
- **Tìm kiếm AJAX**: Không reload trang
- **Validation**: Real-time validate
- **Summary Update**: Tự động cập nhật tóm tắt
- **Button State**: Enable/disable nút theo trạng thái

---

## 📊 Trạng Thái Đặt Bàn

```
┌─────────────────────────────────────────────────────────────────────┐
│ TRẠNG THÁI ĐẶT BÀN                                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  1. PENDING (🟡 Chờ xác nhận)                                        │
│     ├─ Khách vừa tạo đặt bàn                                        │
│     ├─ Admin chưa xác nhận                                          │
│     └─ Có thể Hủy                                                   │
│                                                                      │
│  2. CONFIRMED (🟢 Đã xác nhận)                                       │
│     ├─ Admin đã xác nhận đặt bàn                                    │
│     ├─ Bàn được giữ lại                                             │
│     └─ Có thể Hủy                                                   │
│                                                                      │
│  3. CHECKED-IN (👤 Đã nhận bàn)                                      │
│     ├─ Khách đã đến quán và nhận bàn                                │
│     ├─ Hệ thống ghi lại CheckinTime                                │
│     └─ Không thể Hủy                                                │
│                                                                      │
│  4. COMPLETED (✓ Hoàn tất)                                           │
│     ├─ Khách đã rời bàn                                             │
│     ├─ Hệ thống ghi lại CheckoutTime                               │
│     └─ Không thể Hủy                                                │
│                                                                      │
│  5. CANCELLED (✗ Đã hủy)                                             │
│     ├─ Khách hoặc Admin hủy đặt bàn                                 │
│     ├─ Bàn được giải phóng                                          │
│     └─ Không thể quay lại                                           │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘

CHUYỂN TIẾP TRẠNG THÁI:

							  ┌─────────────┐
							  │   PENDING   │
							  └──────┬──────┘
									 │
					┌────────────────┼────────────────┐
					│                │                │
					▼                ▼                ▼
			┌──────────────┐  ┌────────────┐  ┌──────────────┐
			│ CONFIRMED    │  │ CANCELLED  │  │ (Hủy)        │
			└──────┬───────┘  └────────────┘  │              │
				   │                          └──────────────┘
				   │
				   ▼
			┌──────────────┐
			│  CHECKED-IN  │
			└──────┬───────┘
				   │
				   ▼
			┌──────────────┐
			│  COMPLETED   │
			└──────────────┘
```

---

## ⚙️ Quy Tắc Kinh Doanh (Business Rules)

### 1. Quy Tắc Đặt Bàn

| Quy Tắc | Chi Tiết |
|---------|---------|
| **Số khách** | 1 - 50 người |
| **Thời gian** | Phải sau hiện tại ≥ 0 phút |
| **Thời lượng** | Mặc định 2 giờ (120 phút) |
| **Sức chứa** | Số khách ≤ Sức chứa bàn |
| **Xung đột** | Cách nhau ≥ 2 giờ trên cùng bàn |

### 2. Quy Tắc Xung Đột (Conflict Detection)

```
Giả sử:
- Bàn A được đặt từ 18:00 - 20:00 (2 giờ)
- Người dùng muốn đặt Bàn A lúc 19:00

SQL Logic:
WHERE ReservationDate < endTime (20:00)
  AND ReservationDate.AddHours(2) > startTime (19:00)
  AND Status NOT IN ("Cancelled", "Completed")

Kết quả: XÁC ĐỊNH Xung đột → Không cho phép

─────────────────────────────────────────────────────
Giả sử:
- Bàn A được đặt từ 18:00 - 20:00
- Người dùng muốn đặt Bàn A lúc 20:00

SQL Logic:
WHERE ReservationDate < 22:00
  AND ReservationDate.AddHours(2) > 20:00

Kết quả: KHÔNG xung đột (20:00 + 2h = 22:00, không < 22:00) 
		 → Cho phép đặt
```

### 3. Quy Tắc Bảo Mật

- ✅ Khách **chỉ được xem/sửa** đặt bàn của chính mình
- ✅ Kiểm tra `reservation.CustomerID == currentCustomerId` trước mọi thao tác
- ✅ Yêu cầu đăng nhập để truy cập bất kỳ chức năng nào
- ✅ Anti-Forgery Token cho tất cả POST/DELETE request

### 4. Quy Tắc Xóa

- ❌ **Hard Delete**: KHÔNG bao giờ xóa dữ liệu
- ✅ **Soft Delete**: Đánh dấu `IsDeleted = true`
- ✅ Query luôn filter: `WHERE IsDeleted = false`

---

## 🔍 Quy Trình Tìm Bàn Chi Tiết

### Thuật Toán: `GetAvailableTablesAsync()`

```csharp
// Input:
// - reservationDate: Thời gian khách muốn đặt
// - numberOfGuests: Số khách
// - durationMinutes: Thời lượng (default 120)

// Bước 1: Validate input
if (numberOfGuests <= 0 || reservationDate <= DateTime.UtcNow)
	return [];  // Empty list

// Bước 2: Lấy tất cả bàn từ DB
var allTables = await tableRepository.GetAvailableTablesAsync(numberOfGuests);
// Query: WHERE Capacity >= numberOfGuests
//        AND TableStatus != "Maintenance"
//        AND IsDeleted = false

// Bước 3: Loop qua từng bàn
var availableTables = new List<RestaurantTable>();
foreach (var table in allTables)
{
	// Bước 4: Kiểm tra xung đột
	var hasConflict = await reservationRepository.HasConflictAsync(
		table.TableID,
		reservationDate,
		reservationDate.AddMinutes(durationMinutes));

	// SQL:
	// SELECT EXISTS (
	//   WHERE TableID = table.TableID
	//     AND IsDeleted = false
	//     AND ReservationStatus NOT IN ("Cancelled", "Completed")
	//     AND ReservationDate < endTime
	//     AND ReservationDate.AddHours(2) > reservationDate
	// )

	// Bước 5: Nếu không xung đột → Thêm vào danh sách
	if (!hasConflict)
	{
		availableTables.Add(table);
	}
}

// Bước 6: Sắp xếp theo số bàn
return availableTables.OrderBy(t => t.TableNumber).ToList();
```

---

## 📱 Giao Diện Người Dùng (UI/UX)

### Trang Đặt Bàn (Create.cshtml)

```
┌─────────────────────────────────────────────────────────────┐
│  🏠 Dashboard > 📅 Đặt Bàn                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  LAYOUT: 2 COLUMNS                                          │
│  ┌────────────────────────────┐  ┌─────────────────────┐  │
│  │ LEFT: FORM                 │  │ RIGHT: SUMMARY      │  │
│  │                            │  │ (STICKY)            │  │
│  │ ┌─────────────────────┐    │  │ ┌─────────────────┐ │  │
│  │ │ NGÀY & GIỜ, SỐ KHÁCH│    │  │ │ TÓM TẮT ĐẶT BÀN │ │  │
│  │ ├─────────────────────┤    │  │ │                 │ │  │
│  │ │ Ngày & Giờ: [date] │    │  │ │ Bàn: ...        │ │  │
│  │ │ Số khách: [1] [+][-]│    │  │ │ Sức chứa: ...   │ │  │
│  │ │                     │    │  │ │ Tầng: ...       │ │  │
│  │ │ [🔍 TÌM BÀN]        │    │  │ │ Ngày: ...       │ │  │
│  │ └─────────────────────┘    │  │ │ Giờ: ...        │ │  │
│  │                            │  │ │ Khách: ...      │ │  │
│  │ ┌─────────────────────┐    │  │ │                 │ │  │
│  │ │ DANH SÁCH BÀN       │    │  │ │ [✓ XÁC NHẬN]   │ │  │
│  │ │ ┌─────────────┐     │    │  │ │ (disabled)      │ │  │
│  │ │ │ Bàn 01      │     │    │  │ └─────────────────┘ │  │
│  │ │ │ 🪑 4 khách  │     │    │  │                     │  │
│  │ │ │ 🏢 Tầng 1   │     │    │  │                     │  │
│  │ │ │ [Chọn]      │     │    │  │                     │  │
│  │ │ └─────────────┘     │    │  │                     │  │
│  │ │ ┌─────────────┐     │    │  │                     │  │
│  │ │ │ Bàn 02      │     │    │  │                     │  │
│  │ │ │ ✓ Đã chọn   │     │    │  │                     │  │
│  │ │ └─────────────┘     │    │  │                     │  │
│  │ └─────────────────────┘    │  │                     │  │
│  │                            │  │                     │  │
│  │ ┌─────────────────────┐    │  │                     │  │
│  │ │ GHI CHÚ (TỲY CHỌN)  │    │  │                     │  │
│  │ │ [Textarea: 500 ký]  │    │  │                     │  │
│  │ └─────────────────────┘    │  │                     │  │
│  │                            │  │                     │  │
│  └────────────────────────────┘  └─────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Trang Lịch Sử (History.cshtml)

```
┌────────────────────────────────────────────────────────────┐
│ Lịch Sử Đặt Bàn  [+ Đặt Bàn Mới]                           │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ │ # │ Bàn │ Thời gian   │ Khách │ Trạng thái │ Xem  │ │
│  │ ├──────────────────────────────────────────────────┤ │
│  │ │ 1 │ 01  │ 15/01 18:00 │ 4     │ 🟡 Chờ    │ Chi  │ │
│  │ │ 2 │ 03  │ 10/01 19:30 │ 2     │ 🟢 Đã xác │ Chi  │ │
│  │ │ 3 │ 02  │ 05/01 12:00 │ 6     │ ✓ Hoàn    │ Chi  │ │
│  │ │ 4 │ 04  │ 01/01 20:00 │ 4     │ ✗ Hủy    │ Chi  │ │
│  │ └──────────────────────────────────────────────────┘ │
│  │                                                      │ │
│  │ Không có lượt đặt bàn nào.                          │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 🔐 Bảo Mật

### Session Management
```csharp
// Kiểm tra đăng nhập
if (!IsLoggedIn()) 
	return RedirectToLogin();

// Lấy CustomerID từ Session
private bool IsLoggedIn() 
	=> (HttpContext.Session.GetInt32("UserId") ?? 0) > 0;

private IActionResult RedirectToLogin() 
	=> RedirectToAction("Login", "Account", new { area = "" });
```

### Anti-Forgery Protection
```csharp
// POST actions bắt buộc có Anti-Forgery Token
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ReservationViewModel model)
{
	// Form phải có: @Html.AntiForgeryToken()
}

// AJAX requests
[HttpPost, ValidateAntiForgeryTokenFromHeader]
public async Task<IActionResult> SearchAvailableTables(
	[FromBody] SearchAvailableTablesRequest request)
{
	// AJAX gửi token trong header: X-CSRF-TOKEN
}
```

### Ownership Verification
```csharp
// Verify khách hàng chỉ thao tác với đặt bàn của mình
public async Task<Reservation?> GetReservationDetailsAsync(
	int reservationId, 
	int customerId)
{
	var reservation = await _reservationRepository.GetByIdAsync(reservationId);

	// ❌ CHỈ TRẢ VỀ NẾU CHỦNG SỞ HỮU MATCH
	if (reservation == null || reservation.CustomerID != customerId)
		return null;

	return reservation;
}
```

---

## 📈 Performance Considerations

### Database Queries
- ✅ Dùng `.Include()` để eager load Customer & Table
- ✅ Có index trên `CustomerID`, `TableID`, `ReservationDate`
- ✅ Filter `IsDeleted = false` trong mọi query

### Caching Opportunity
- Có thể cache danh sách bàn có sẵn (cache 5 phút)
- Invalidate cache khi có đặt bàn mới

### AJAX Search
- ❌ KHÔNG reload page
- ✅ Real-time validation
- ✅ Spinners cho loading state

---

## 🎯 Key Takeaways

1. **Luồng chính**: Create → Search → Select → Confirm → History → Details → Cancel
2. **Trạng thái**: Pending → Confirmed → CheckedIn → Completed hoặc Cancelled
3. **Xung đột**: Phát hiện via SQL query kiểm tra khung giờ 2 giờ
4. **Bảo mật**: Session + Anti-Forgery + Ownership Verification
5. **Soft Delete**: Không xóa dữ liệu, chỉ đánh dấu IsDeleted
6. **Responsive**: UI hỗ trợ desktop/tablet/mobile

---

## 📚 Tham Khảo File

| File | Mô Tả |
|------|-------|
| `Areas/Customer/Controllers/ReservationsController.cs` | Logic xử lý API |
| `Services/ReservationService.cs` | Business Logic |
| `Repository/ReservationRepository.cs` | Data Access |
| `Models/Entities/Reservation.cs` | Entity Model |
| `Areas/Customer/Views/Reservations/*.cshtml` | UI Views |
| `wwwroot/js/reservation.js` | JavaScript logic |
| `Services/Interfaces/IReservationService.cs` | Service interface |
| `Repository/Interfaces/IReservationRepository.cs` | Repository interface |

---

**Tài liệu này cung cấp cái nhìn toàn diện về chức năng Đặt Bàn trong hệ thống Quản Lý Quán Cafe. Mọi chi tiết kỹ thuật và quy trình kinh doanh đều được ghi chép chi tiết.**

📝 *Tổng hợp từ mã nguồn: `Quản lý quán cafe` v1.0*
