# Hướng dẫn Quản lý Nhân viên (Employee Management System)

## Tổng Quan

Hệ thống quản lý nhân viên cho phép **chỉ Admin** tạo tài khoản cho nhân viên. Khi Admin tạo một nhân viên mới, hệ thống sẽ tự động tạo:
1. **Tài khoản người dùng (User Account)** - cho đăng nhập
2. **Hồ sơ nhân viên (Employee Profile)** - chứa thông tin cá nhân và công việc

## 1. Kiến trúc Hệ thống

### 1.1 Models (Mô hình dữ liệu)

#### Employee Entity
```csharp
public class Employee
{
	public int EmployeeID { get; set; }
	public int UserID { get; set; }                    // Liên kết đến User
	public string FullName { get; set; }               // Tên đầy đủ
	public string Email { get; set; }                  // Email
	public string Position { get; set; }               // Chức vụ (Quản lý, Pha chế, Phục vụ, etc.)
	public string Department { get; set; }             // Phòng ban (Chưa cập nhật mặc định)
	public string Gender { get; set; }                 // Giới tính
	public DateTime? BirthDate { get; set; }          // Ngày sinh
	public string Phone { get; set; }                  // Số điện thoại
	public string Address { get; set; }                // Địa chỉ
	public DateTime? HireDate { get; set; }           // Ngày vào làm
	public decimal? Salary { get; set; }               // Lương (tùy chọn)
	public bool IsActive { get; set; }                 // Trạng thái hoạt động
	public DateTime CreatedAt { get; set; }            // Ngày tạo
	public DateTime? UpdatedAt { get; set; }          // Ngày cập nhật
	public bool IsDeleted { get; set; }                // Soft delete

	// Navigation
	public virtual User User { get; set; }             // Liên kết đến User account
}
```

#### User Entity
```csharp
public class User
{
	public int UserID { get; set; }
	public string Username { get; set; }               // Tên đăng nhập
	public string PasswordHash { get; set; }           // Mật khẩu (được hash)
	public int RoleID { get; set; }                    // Role (Admin, Employee, Cashier, etc.)
	public int? EmployeeID { get; set; }              // ID nhân viên nếu là nhân viên
	public bool IsActive { get; set; }                 // Tài khoản hoạt động?
	public DateTime? LastLogin { get; set; }          // Lần đăng nhập cuối
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public bool IsDeleted { get; set; }

	// Navigation
	public virtual Role Role { get; set; }
	public virtual Employee Employee { get; set; }
}
```

### 1.2 ViewModels (Mô hình dữ liệu View)

#### EmployeeCreateViewModel
- **Mục đích**: Tạo nhân viên mới (Admin only)
- **Dữ liệu**:
  - `Username` - Tên đăng nhập (3-50 ký tự, bắt buộc)
  - `Password` - Mật khẩu (tối thiểu 6 ký tự, bắt buộc)
  - `ConfirmPassword` - Xác nhận mật khẩu
  - `FullName` - Tên nhân viên (2-100 ký tự, bắt buộc)
  - `Email` - Email (bắt buộc, phải hợp lệ)
  - `Position` - Chức vụ (bắt buộc): Quản lý, Phó Quản lý, Pha chế, Phục vụ, Thu ngân, Giao hàng, Khác
  - `Department` - Phòng ban (tùy chọn)
  - `Phone` - Số điện thoại (tùy chọn)
  - `Address` - Địa chỉ (tùy chọn)

#### EmployeeEditViewModel
- **Mục đích**: Chỉnh sửa thông tin nhân viên
- **Dữ liệu**: Tương tự CreateViewModel nhưng không có Password
- **Có thêm**: `IsActive` - Kích hoạt/Vô hiệu hóa tài khoản

#### EmployeeDetailViewModel
- **Mục đích**: Hiển thị chi tiết nhân viên
- **Dữ liệu**: Toàn bộ thông tin nhân viên + tên đăng nhập

#### EmployeeListViewModel
- **Mục đích**: Danh sách nhân viên với bộ lọc
- **Dữ liệu**:
  - `Employees` - Danh sách nhân viên
  - `TotalEmployees` - Tổng số nhân viên
  - `ActiveEmployees` - Số nhân viên hoạt động
  - `InactiveEmployees` - Số nhân viên không hoạt động
  - `SearchTerm` - Từ tìm kiếm
  - `SortBy` - Cách sắp xếp (newest, oldest, name_asc, name_desc)
  - `Department` - Phòng ban được chọn
  - `Departments` - Danh sách phòng ban

## 2. Controller (Điều khiển)

### EmployeesController
**Vị trí**: `Areas/Admin/Controllers/EmployeesController.cs`

**Bảo vệ**: Chỉ Admin được phép truy cập (được bảo vệ bởi `[SessionAuthorize("Admin")]`)

#### Các Action:

##### 1. **Index** - Danh sách nhân viên
- **Phương thức**: GET
- **Tham số**:
  - `search` - Tìm kiếm theo tên, email, số điện thoại
  - `sort` - Sắp xếp: "newest", "oldest", "name_asc", "name_desc"
  - `department` - Lọc theo phòng ban
- **Chức năng**:
  - Lấy danh sách nhân viên (không bao gồm những đã xóa)
  - Tìm kiếm theo FullName, Username, Email, Phone
  - Sắp xếp theo yêu cầu
  - Thống kê: Tổng số, hoạt động, không hoạt động, số phòng ban

##### 2. **Create** - Tạo nhân viên mới
- **Phương thức**: GET (hiển thị form), POST (xử lý)
- **Kiểm tra**:
  - Username phải duy nhất
  - Email phải duy nhất
  - Mật khẩu phải khớp
- **Quy trình**:
  1. Tạo User account với Role = "Employee"
  2. Hash mật khẩu bằng BCrypt
  3. Tạo Employee record và liên kết với User
  4. Lưu vào database
- **Chuyển hướng**: Về Index sau khi thành công
- **Thông báo**: Hiển thị message thành công

##### 3. **Details** - Xem chi tiết nhân viên
- **Phương thức**: GET
- **Tham số**: `id` - ID nhân viên
- **Chức năng**: Hiển thị toàn bộ thông tin nhân viên

##### 4. **Edit** - Chỉnh sửa nhân viên
- **Phương thức**: GET (hiển thị form), POST (xử lý)
- **Chức năng**:
  - Cập nhật thông tin nhân viên
  - Cập nhật trạng thái hoạt động (IsActive)
  - Cập nhật trạng thái User account đồng thời
  - Kiểm tra email duy nhất
- **Không cho thay đổi**:
  - Username (phải sử dụng ChangePassword)
  - Password (phải sử dụng ChangePassword)

##### 5. **Delete** - Xóa nhân viên (Soft Delete)
- **Phương thức**: POST
- **Chức năng**:
  - Đánh dấu nhân viên là xóa (IsDeleted = true)
  - Vô hiệu hóa tài khoản User đồng thời
  - Dữ liệu không bị xóa vĩnh viễn (cho phép khôi phục)

##### 6. **ChangePassword** - Thay đổi mật khẩu
- **Phương thức**: GET (hiển thị form), POST (xử lý)
- **Kiểm tra**:
  - Mật khẩu tối thiểu 6 ký tự
  - Mật khẩu phải khớp với xác nhận
- **Chức năng**:
  - Hash mật khẩu mới
  - Cập nhật User account
  - Ghi lại thời gian cập nhật

## 3. Views (Giao diện)

### 3.1 Danh sách nhân viên (`Index.cshtml`)
**Đặc điểm**:
- Hiển thị thống kê: Tổng, hoạt động, không hoạt động, phòng ban
- Tìm kiếm và lọc theo phòng ban
- Sắp xếp theo tên hoặc ngày
- Bảng danh sách với thông tin cơ bản
- Nút hành động: Xem chi tiết, Chỉnh sửa, Thay đổi mật khẩu, Xóa

### 3.2 Tạo nhân viên (`Create.cshtml`)
**Form bao gồm**:
- **Thông tin tài khoản**:
  - Tên đăng nhập (Username)
  - Mật khẩu
  - Xác nhận mật khẩu
- **Thông tin cá nhân**:
  - Tên đầy đủ
  - Email
  - Số điện thoại
  - Địa chỉ
- **Thông tin công việc**:
  - Chức vụ (dropdown)
  - Phòng ban

### 3.3 Xem chi tiết (`Details.cshtml`)
- Hiển thị toàn bộ thông tin nhân viên
- Nút chuyển sang edit hoặc quay lại

### 3.4 Chỉnh sửa (`Edit.cshtml`)
- Form tương tự Create nhưng không có mật khẩu
- Thêm toggle "Trạng thái hoạt động"
- Nút cập nhật

### 3.5 Đổi mật khẩu (`ChangePassword.cshtml`)
- Nhập mật khẩu mới
- Xác nhận mật khẩu
- Nút "Đặt lại mật khẩu"

## 4. Bảo mật (Security)

### 4.1 Xác thực (Authentication)
- Sử dụng **Session** lưu trữ thông tin đăng nhập
- Session keys:
  - `UserId` - ID người dùng
  - `Username` - Tên đăng nhập
  - `FullName` - Tên đầy đủ
  - `RoleName` - Vai trò (Admin, Employee, Cashier, Customer)
  - `EmployeeID` - ID nhân viên (nếu là nhân viên)

### 4.2 Ủy quyền (Authorization)
- **Admin chỉ**: Sử dụng `[SessionAuthorize("Admin")]`
  - Tất cả action trong EmployeesController được bảo vệ
  - Nếu không phải Admin: Trả về 403 Forbidden
  - Nếu chưa đăng nhập: Chuyển hướng đến Login

### 4.3 Mật khẩu (Passwords)
- **Hash Algorithm**: BCrypt
- **Độ dài tối thiểu**: 6 ký tự
- **Không lưu trữ**: Plain text password
- **Cách hash**: `UserRepository.HashPassword(password)`

### 4.4 Xác thực CSRF (Cross-Site Request Forgery)
- Sử dụng `[ValidateAntiForgeryToken]` trên POST actions
- Form tag sử dụng `<input asp-for="..." />` tự động thêm token

### 4.5 Xác thực dữ liệu (Data Validation)
- **Client-side**: HTML5 validation
- **Server-side**: Model validation bằng data annotations:
  - `[Required]` - Bắt buộc
  - `[StringLength(min, max)]` - Độ dài
  - `[EmailAddress]` - Email hợp lệ
  - `[Phone]` - Số điện thoại
  - `[Compare]` - So sánh (mật khẩu)

## 5. Quy trình Luồng (Workflows)

### 5.1 Tạo nhân viên mới
```
1. Admin vào Admin Dashboard
2. Chọn "Nhân viên" → "Thêm nhân viên"
3. Điền form:
   - Username (3-50 ký tự, duy nhất)
   - Password (tối thiểu 6 ký tự)
   - Tên đầy đủ
   - Email (duy nhất)
   - Chức vụ
   - Phòng ban (tùy chọn)
   - Số điện thoại (tùy chọn)
   - Địa chỉ (tùy chọn)
4. Nhấp "Tạo tài khoản"
5. System kiểm tra:
   ✓ Username chưa tồn tại
   ✓ Email chưa tồn tại
   ✓ Mật khẩu khớp
6. System tạo:
   - User account (role = Employee)
   - Employee profile
7. Lưu vào database
8. Chuyển hướng về danh sách
9. Hiển thị message thành công
```

### 5.2 Chỉnh sửa nhân viên
```
1. Admin chọn nhân viên từ danh sách
2. Nhấp "Chỉnh sửa"
3. Cập nhật thông tin (ngoại trừ Username, Password)
4. Nhấp "Cập nhật"
5. System kiểm tra email mới có duy nhất không
6. Cập nhật Employee và User record
7. Chuyển hướng về danh sách
8. Hiển thị message thành công
```

### 5.3 Đổi mật khẩu nhân viên
```
1. Admin chọn nhân viên từ danh sách
2. Nhấp "Đổi mật khẩu"
3. Nhập mật khẩu mới (tối thiểu 6 ký tự)
4. Xác nhận mật khẩu
5. Nhấp "Đặt lại mật khẩu"
6. System hash mật khẩu
7. Cập nhật User account
8. Chuyển hướng về danh sách
9. Hiển thị message thành công
```

### 5.4 Xóa nhân viên
```
1. Admin chọn nhân viên từ danh sách
2. Nhấp "Xóa"
3. Confirm xóa
4. System:
   - Đánh dấu Employee.IsDeleted = true
   - Vô hiệu hóa User account (IsActive = false)
   - Ghi lại thời gian cập nhật
5. Danh sách được cập nhật (nhân viên xóa không hiển thị)
```

## 6. Tính Năng Chi Tiết

### 6.1 Tìm kiếm
- **Tìm kiếm trong**:
  - Tên nhân viên (FullName)
  - Tên đăng nhập (Username)
  - Email
  - Số điện thoại (Phone)
- **Case-insensitive**: Không phân biệt chữ hoa/thường

### 6.2 Lọc
- **Theo phòng ban**: Chỉ hiển thị nhân viên của phòng ban đó
- **Các phòng ban** được tự động đưa vào dropdown từ dữ liệu

### 6.3 Sắp xếp
- `newest` - Nhân viên mới thêm đầu (mặc định)
- `oldest` - Nhân viên cũ đầu
- `name_asc` - Tên A → Z
- `name_desc` - Tên Z → A

### 6.4 Thống kê
Hiển thị trên trang danh sách:
- **Tổng nhân viên**: Đếm tất cả nhân viên chưa xóa
- **Đang hoạt động**: Đếm nhân viên có IsActive = true
- **Không hoạt động**: Tổng - Hoạt động
- **Phòng ban**: Số phòng ban khác nhau

## 7. Xử lý Lỗi

### 7.1 Lỗi thông thường
| Lỗi | Nguyên nhân | Giải pháp |
|-----|-----------|---------|
| "Tên đăng nhập đã tồn tại" | Username trùng | Chọn username khác |
| "Email đã được sử dụng" | Email trùng | Sử dụng email khác |
| "Mật khẩu không khớp" | Password ≠ ConfirmPassword | Gõ lại mật khẩu giống nhau |
| "Mật khẩu tối thiểu 6 ký tự" | Mật khẩu quá ngắn | Tăng độ dài mật khẩu |
| "Không tìm thấy nhân viên" | ID không tồn tại | Quay lại danh sách, tìm lại |

### 7.2 Lỗi ủy quyền
| Lỗi | Nguyên nhân | Giải pháp |
|-----|-----------|---------|
| 403 Forbidden | Không phải Admin | Đăng nhập bằng tài khoản Admin |
| Chuyển hướng đến Login | Session hết hạn | Đăng nhập lại |

## 8. Database Schema

### Employees Table
```sql
CREATE TABLE Employees (
	EmployeeID INT PRIMARY KEY IDENTITY,
	UserID INT NOT NULL FOREIGN KEY REFERENCES Users(UserID),
	FullName NVARCHAR(100) NOT NULL,
	Email NVARCHAR(100) NOT NULL UNIQUE,
	Position NVARCHAR(50) NOT NULL,
	Department NVARCHAR(50) DEFAULT 'Chưa cập nhật',
	Gender NVARCHAR(10),
	BirthDate DATETIME,
	Phone NVARCHAR(20),
	Address NVARCHAR(200),
	HireDate DATETIME,
	Salary DECIMAL(10,2),
	IsActive BIT DEFAULT 1,
	CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME,
	IsDeleted BIT DEFAULT 0
);
```

### Users Table
```sql
CREATE TABLE Users (
	UserID INT PRIMARY KEY IDENTITY,
	Username NVARCHAR(50) NOT NULL UNIQUE,
	PasswordHash NVARCHAR(255) NOT NULL,
	RoleID INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleID),
	EmployeeID INT FOREIGN KEY REFERENCES Employees(EmployeeID),
	IsActive BIT DEFAULT 1,
	CreatedBy NVARCHAR(50),
	UpdatedBy NVARCHAR(50),
	LastLogin DATETIME,
	CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
	UpdatedAt DATETIME,
	IsDeleted BIT DEFAULT 0
);
```

## 9. Quản lý trạng thái

### 9.1 IsActive
- `true` - Tài khoản hoạt động, nhân viên có thể đăng nhập
- `false` - Tài khoản bị vô hiệu hóa, nhân viên không thể đăng nhập

### 9.2 IsDeleted
- `false` - Nhân viên bình thường (hiển thị)
- `true` - Nhân viên đã xóa (ẩn, không hiển thị)
- **Lợi ích**: Có thể khôi phục nhân viên sau này

## 10. API/Endpoints

### Admin Area Endpoints
```
GET    /Admin/Employees                    - Danh sách nhân viên
GET    /Admin/Employees/Create             - Form tạo nhân viên
POST   /Admin/Employees/Create             - Tạo nhân viên
GET    /Admin/Employees/Details/{id}       - Xem chi tiết
GET    /Admin/Employees/Edit/{id}          - Form chỉnh sửa
POST   /Admin/Employees/Edit               - Cập nhật nhân viên
POST   /Admin/Employees/Delete/{id}        - Xóa nhân viên
GET    /Admin/Employees/ChangePassword/{id} - Form đổi mật khẩu
POST   /Admin/Employees/ChangePassword/{id} - Xử lý đổi mật khẩu
```

## 11. Best Practices

### 11.1 Khi tạo nhân viên
- ✓ Sử dụng email công ty (dễ theo dõi)
- ✓ Mật khẩu mạnh (8+ ký tự nếu có thể)
- ✓ Cập nhật đầy đủ thông tin
- ✗ Không sử dụng lại username/email cũ
- ✗ Không để lộ mật khẩu

### 11.2 Khi chỉnh sửa
- ✓ Kiểm tra trước khi lưu thay đổi lớn
- ✓ Ghi nhận lý do thay đổi (nếu có hệ thống audit)
- ✗ Không thay đổi username trực tiếp

### 11.3 Khi xóa
- ✓ Kiểm tra double check
- ✓ Xem nhân viên đó có công việc đang chạy không
- ✓ Giữ lại dữ liệu lịch sử (soft delete)
- ✗ Không xóa vĩnh viễn tạo thành phố chập chung dữ liệu

## 12. Troubleshooting

### Vấn đề: Không thể tạo nhân viên
**Kiểm tra**:
1. Đã đăng nhập với tài khoản Admin?
2. Form có báo lỗi validate gì không?
3. Database có đầy không?

### Vấn đề: Mật khẩu không thay đổi
**Kiểm tra**:
1. Mật khẩu có đúng 6+ ký tự?
2. Xác nhận mật khẩu có khớp không?
3. Session còn hiệu lực không?

### Vấn đề: Nhân viên không thể đăng nhập
**Kiểm tra**:
1. Tài khoản có được kích hoạt (IsActive = true)?
2. Username/Password chính xác?
3. Role của User có phù hợp?

## 13. Tính Năng Tương Lai (Roadmap)

- [ ] Quản lý lương (Salary management)
- [ ] Lịch công tác (Schedule)
- [ ] Chấm công (Attendance)
- [ ] Đánh giá hiệu suất (Performance review)
- [ ] Quản lý kỳ nghỉ (Leave management)
- [ ] Export danh sách nhân viên (Excel/PDF)
- [ ] Bulk import nhân viên
- [ ] Hai yếu tố xác thực (2FA)
- [ ] Lịch sử thay đổi (Audit log)
- [ ] Thông báo khi tạo nhân viên (Email)

---

**Phiên bản**: 1.0
**Cập nhật**: 2024
**Tác giả**: Admin Team
