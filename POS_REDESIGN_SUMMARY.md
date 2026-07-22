# POS Module Redesign - Hoàn Thành

## 📋 Tóm tắt công việc

Đã thiết kế lại toàn bộ module POS của Area Cashier để bám sát thiết kế Figma 3 cột (Danh sách bàn, Chi tiết đơn hàng, Thanh toán), đồng thời duy trì kiến trúc hiện tại của dự án ASP.NET Core MVC.

---

## ✅ Các thay đổi chính

### 1. **ViewModels** (`Areas/Cashier/ViewModels/POSViewModel.cs`)
- ✨ **POSViewModel**: ViewModel chính cho trang POS, chứa danh sách bàn mở, bàn hiện tại, danh sách món, thông tin khách hàng, tính toán thanh toán
- ✨ **POSTableViewModel**: Thông tin từng bàn (ID, số bàn, mã đơn, số lượng món, tổng tiền, trạng thái)
- ✨ **POSOrderItemViewModel**: Thông tin từng món (tên, size, giá, số lượng, ghi chú)
- ✨ **POSCustomerViewModel**: Thông tin khách hàng (ID, SĐT, tên, điểm tích lũy, hạng thành viên)

**Lợi ích**: Tách biệt dữ liệu View khỏi Entity, dễ mở rộng và bảo trì

### 2. **Controller** (`Areas/Cashier/Controllers/POSController.cs`)

#### Endpoints được tạo:

| Endpoint | Phương thức | Mô tả | Trạng thái |
|----------|-----------|-------|-----------|
| `/Cashier/POS` | GET | Tải danh sách bàn mở và UI ban đầu | ✅ Hoàn thành |
| `/Cashier/POS/SelectTable` | GET | Chọn bàn, trả về thông tin bàn và danh sách món | ✅ Skeleton |
| `/Cashier/POS/AddItem` | POST | Thêm món vào đơn hàng | ✅ Skeleton |
| `/Cashier/POS/UpdateItem` | POST | Cập nhật số lượng món | ✅ Skeleton |
| `/Cashier/POS/RemoveItem` | POST | Xóa món khỏi đơn hàng | ✅ Skeleton |
| `/Cashier/POS/SearchCustomer` | GET | Tìm khách hàng theo SĐT, hiển thị điểm tích lũy | ✅ Hoàn thành |
| `/Cashier/POS/Checkout` | POST | Xử lý thanh toán | ✅ Skeleton |

**Ghi chú**: Các endpoint trả về JSON để hỗ trợ AJAX, TODO markers chỉ vị trí cần implement DB logic

### 3. **Views - Bố cục 3 cột**

#### a. **Index.cshtml** - View chính
```
┌─────────────────────────────────────────────────┐
│  Left (280px)  │  Middle (1fr)  │  Right (380px) │
│  Danh sách bàn │  Chi tiết đơn   │  Thanh toán    │
└─────────────────────────────────────────────────┘
```

**Tính năng**:
- ✅ Responsive Grid layout
- ✅ CSS được tách vào file external (`wwwroot/css/pos.css`)
- ✅ Client-side JS hooks cho tương tác

#### b. **_TableList.cshtml** - Cột trái
```
┌─────────────────────┐
│ Logo BrewPoint POS  │
├─────────────────────┤
│ [🔍 Tìm bàn..]     │
├─────────────────────┤
│ □ Bàn 1 - 2 món     │
│ □ Bàn 3 - 4 món     │ (Danh sách)
│ □ Bàn 4 - 3 món     │
│ 🔔 THANH TOÁN       │ (Status badge)
├─────────────────────┤
│ ← Nhân viên         │
└─────────────────────┘
```

**Tính năng**:
- ✅ Hiển thị danh sách bàn đang mở (từ RestaurantTables, status = Occupied/WaitingPayment)
- ✅ Search box để tìm kiếm bàn
- ✅ Click vào bàn để chọn (gọi hàm `selectTable()`)
- ✅ Badge "THANH TOÁN" cho bàn đang chờ

#### c. **_OrderDetails.cshtml** - Cột giữa
```
┌──────────────────────────┐
│ Bàn 1 — #2407     [×]   │
│ 2 món                    │
├──────────────────────────┤
│ MÓN         │ SL  │ GIÁ  │
│ Caramel Mac│ −1+ │65.000│
│ Cỡ L       │      │      │
│ Trà sữa    │ −2+ │110.00│
│ Cỡ M       │      │      │
├──────────────────────────┤
│ Thông tin khách hàng     │
│ ⭐ Member               │
│ ☎ [Tìm khách...]       │
├──────────────────────────┤
│ Ghi chú: [Textarea]      │
├──────────────────────────┤
│ Giảm giá:                │
│ [Không] [%] [Số tiền]    │
│ [Nhập giá trị............]│
└──────────────────────────┘
```

**Tính năng**:
- ✅ Hiển thị tên bàn, mã đơn
- ✅ Danh sách món với size, số lượng (tăng/giảm), đơn giá, thành tiền
- ✅ Nút xóa từng món
- ✅ Tìm khách hàng theo SĐT (AJAX)
- ✅ Ghi chú cho đơn hàng
- ✅ Chọn loại giảm giá (%, số tiền, không)

#### d. **_PaymentSummary.cshtml** - Cột phải
```
┌────────────────────────┐
│ Tổng kết               │
├────────────────────────┤
│ Tạm tính:   175.000đ   │
│ Giảm giá:   0đ         │
│ ═══════════════════    │
│ Tổng cộng:  175.000đ   │
├────────────────────────┤
│ ⭐ Khách tích +17 điểm │
├────────────────────────┤
│ PHƯƠNG THỨC THANH TOÁN │
│ [💰 Tiền mặt] [📱 QR] │
├────────────────────────┤
│ Tiền khách đưa (ngàn)  │
│ [VD: 200..................]│
├────────────────────────┤
│ [💳 Xác nhận thanh toán]│
│ [🖨 In hóa đơn]        │
└────────────────────────┘
```

**Tính năng**:
- ✅ Tính toán tạm tính, giảm giá, tổng cộng
- ✅ Hiển thị điểm tích lũy (nếu có khách)
- ✅ Chọn phương thức thanh toán (Tiền mặt / QR Pay)
- ✅ Nhập số tiền khách đưa
- ✅ Nút xác nhận thanh toán
- ✅ Placeholder cho chức năng in hóa đơn

### 4. **CSS** (`wwwroot/css/pos.css`)

- ✅ **Grid 3 cột**: Responsive layout với breakpoint 1400px, 1024px, 768px
- ✅ **Color scheme**: Sử dụng CSS variables từ theme Admin
  - `--coffee-dark`: Màu nền sidebar
  - `--coffee-brown`: Màu chính (buttons, badges)
  - `--cream`: Màu nền nhạt
  - `--text-dark`, `--text-light`: Màu text
- ✅ **Components**:
  - Table list items với hover effect
  - Order items với quantity controls
  - Payment buttons
  - Form controls với focus state
  - Badges, badges status

### 5. **JavaScript** (`@section Scripts` trong Index.cshtml)

**Functions được tạo**:
```javascript
selectTable(tableId)        // Chọn bàn, gọi SelectTable endpoint
increaseQty(orderDetailId)  // Tăng số lượng
decreaseQty(orderDetailId)  // Giảm số lượng
removeItem(orderDetailId)   // Xóa món
searchCustomer()            // Tìm khách hàng
setDiscountType(type)       // Chọn loại giảm giá
calculateTotal()            // Tính lại tổng tiền
calculateChange()           // Tính tiền thối lại
selectPaymentMethod(method) // Chọn phương thức thanh toán
proceedCheckout()           // Xác nhận thanh toán
closeTable()                // Đóng bàn
```

**Ghi chú**: Các hàm có TODO markers để implement logic AJAX tương tác với controller

---

## 🔧 Chi tiết kỹ thuật

### Entity Mappings
- ✅ **RestaurantTable** → **POSTableViewModel**
  - `TableStatus` (không phải `Status`)
  - Lấy bàn có `IsDeleted = false` và status = "Occupied" hoặc "WaitingPayment"

- ✅ **Customer** → **POSCustomerViewModel**
  - Tìm khách theo `Phone` (không phải `PhoneNumber`)
  - Tìm khách có `IsDeleted = false`

- ✅ **OrderDetail** → **POSOrderItemViewModel**
  - Mềm dẻo để thêm size, notes, topping trong tương lai

### Kiến trúc

```
POS Module
├── Controllers/
│   └── POSController.cs          (Index, AJAX endpoints)
├── ViewModels/
│   └── POSViewModel.cs           (Main + 4 supporting ViewModels)
├── Views/
│   └── POS/
│       └── Index.cshtml          (Main view, layout + scripts)
│   └── Shared/
│       ├── _TableList.cshtml     (Left column)
│       ├── _OrderDetails.cshtml  (Middle column)
│       ├── _PaymentSummary.cshtml (Right column)
│       └── _ViewImports.cshtml   (Using statements for partials)
└── CSS/
	└── wwwroot/css/pos.css       (All styles, responsive)
```

---

## 📝 Ghi chú quan trọng

### ✅ Đã hoàn thành
- [x] Giao diện 3 cột bám sát Figma
- [x] Responsive design (mobile, tablet, desktop)
- [x] Tái sử dụng theme Admin (CSS variables)
- [x] ViewModel layers (không truyền Entity trực tiếp)
- [x] Partial Views modular
- [x] AJAX endpoints skeleton
- [x] Client-side interaction hooks
- [x] Build successful (0 compilation errors)

### ⏳ TODO (Để phát triển tiếp)
- [ ] Implement DB operations trong POSController:
  - Lấy Order Items từ OrderDetail table
  - Thêm/Cập nhật/Xóa OrderDetail
  - Tính tổng tiền từ OrderDetails
  - Tạo Payment record khi checkout
  - Cập nhật Order status
- [ ] Wiring client-side JS đầy đủ để update UI AJAX
- [ ] Tính toán reward points
- [ ] Logic thanh toán (cash vs QR)
- [ ] In hóa đơn integration
- [ ] Validation & error handling
- [ ] Authentication/Authorization checks
- [ ] Test suites

### 🚀 Khởi động

Để chạy ứng dụng:

1. **Nếu debug mode đang chạy**: Nhấn `Ctrl+Shift+F5` (restart debugger)
   - Hoặc sử dụng Hot Reload (Ctrl+Shift+Alt+F5) nếu bật

2. **Truy cập**: `http://localhost:xxxx/Cashier/POS`

3. **Xem giao diện**: 3 cột layout sẽ hiển thị

---

## 📚 Cấu trúc dữ liệu (JSON responses)

### SelectTable Response
```json
{
  "table": {
	"tableID": 1,
	"tableName": "Bàn 1",
	"orderCode": "#1",
	"status": "occupied"
  },
  "items": [
	{
	  "orderDetailID": 1,
	  "productID": 1,
	  "productName": "Caramel Macchiato",
	  "size": "L",
	  "price": 65000,
	  "quantity": 1,
	  "total": 65000,
	  "notes": ""
	}
  ]
}
```

### SearchCustomer Response
```json
{
  "found": true,
  "customer": {
	"customerID": 1,
	"name": "Nguyễn Văn A",
	"phone": "0912345678",
	"rewardPoints": 150,
	"membershipTier": "Silver"
  }
}
```

---

## ✨ Đặc điểm nổi bật

1. **Bảo trì dễ dàng**: Partial Views riêng cho mỗi cột
2. **Mở rộng linh hoạt**: ViewModels cho phép thêm trường mới dễ dàng
3. **Responsive**: Tự động điều chỉnh layout trên mobile/tablet/desktop
4. **Đồng nhất**: Sử dụng theme Admin (CSS variables, icons)
5. **Modular**: Dễ tích hợp tính năng mới (topping, note chi tiết, v.v.)

---

## 📞 Hỗ trợ

Để hoàn thành POS, bạn cần:
1. Implement DB operations trong POSController endpoints
2. Wiring AJAX client-side với server responses
3. Add validation & error handling
4. Test end-to-end

Mọi constraints ban đầu (không đổi Program.cs, Auth, Entity, Repository, Service, Route, Namespace) đều được tuân thủ.
