# BrewPoint Admin - Quick Start Guide

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK installed
- SQL Server (LocalDB or Express)
- Visual Studio 2026 or VS Code

### Running the Application

#### Option 1: Using Visual Studio
1. Open the solution in Visual Studio
2. Press `F5` to run in Debug mode
3. Application will start on `http://localhost:5269`

#### Option 2: Using CLI
```powershell
cd "D:\Công nghệ lập trình web\Quản lý quán cafe\Quản lý quán cafe"
dotnet run
```

### Default Credentials
| Role | Username | Password |
|------|----------|----------|
| Admin | admin | admin123 |
| Cashier | cashier | cashier123 |
| Customer | customer | customer123 |

---

## 📋 Category Management Features

### Access the Module
Navigate to: **http://localhost:5269/Admin/Categories** (requires Admin login)

### Available Actions

#### 1. **View All Categories**
- Path: `/Admin/Categories`
- Display: List of all active categories
- Features:
  - Pagination (10 items per page)
  - Search functionality
  - Status badges (Active/Inactive)
  - Quick action buttons (View, Edit, Delete)

#### 2. **Create New Category**
- Path: `/Admin/Categories/Create`
- Form Fields:
  - **Name** (Required)
  - **Description** (Optional, max 500 chars)
  - **Is Active** (Checkbox)
- Validations:
  - Name must be unique
  - Name is required
  - Form submission requires CSRF token

#### 3. **Edit Category**
- Path: `/Admin/Categories/Edit/{id}`
- Allows updating:
  - Category name
  - Description
  - Active status
- Validation: Duplicate name prevention

#### 4. **View Details**
- Path: `/Admin/Categories/Details/{id}`
- Display:
  - Category name
  - Description
  - Status badge
  - Creation date/time
  - Last update date/time

#### 5. **Delete Category**
- Path: `/Admin/Categories/Delete/{id}`
- Confirmation dialog required
- Cannot delete if products exist in category

---

## 🎨 UI/UX Features

### Design System: BrewPoint Coffee Theme
- **Primary Color**: Coffee Brown (`#8B6F47`)
- **Dark Color**: Coffee Dark (`#6B5341`)
- **Light Color**: Cream (`#F5EFE1`)
- **Font**: Inter (Google Fonts)

### Interactive Elements
- **SweetAlert2** confirmations for destructive actions
- **Bootstrap Icons** for visual feedback
- **Status Badges** for category status
- **Responsive Layout** (Desktop, Tablet, Mobile)

### Sidebar Navigation
- Fixed sidebar with coffee theme
- Active menu highlighting
- Collapsible on mobile (hamburger menu)
- User profile section with logout

### Breadcrumb Navigation
- Dynamic breadcrumb updates
- Shows current page location
- Links back to parent pages

---

## 🔧 Technical Stack

### Backend
- **Framework**: ASP.NET Core MVC
- **Database**: SQL Server
- **ORM**: Entity Framework Core (Code First)
- **Architecture**: Repository-Service-Controller pattern

### Frontend
- **UI Framework**: Bootstrap 5
- **Icons**: Bootstrap Icons
- **Alerts**: SweetAlert2
- **Styling**: Custom CSS with CSS Variables
- **Responsiveness**: Mobile-first design

### Authentication & Authorization
- Session-based authentication
- Role-based access control (Admin/Cashier/Customer)
- CSRF token protection (AntiForgeryToken)

---

## 📁 Project Structure

```
Quản lý quán cafe/
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/
│   │   │   └── CategoriesController.cs
│   │   └── Views/
│   │       └── Categories/
│   │           ├── Index.cshtml
│   │           ├── Create.cshtml
│   │           ├── Edit.cshtml
│   │           ├── Details.cshtml
│   │           └── Delete.cshtml
│   ├── Cashier/
│   └── Customer/
├── Models/
│   ├── Entities/
│   │   └── Category.cs
│   └── ViewModels/
│       └── Category/
│           ├── CategoryViewModel.cs
│           ├── CategoryListViewModel.cs
│           ├── CategoryCreateViewModel.cs
│           ├── CategoryEditViewModel.cs
│           └── CategoryDetailViewModel.cs
├── Repository/
│   ├── Interfaces/
│   │   └── ICategoryRepository.cs
│   └── CategoryRepository.cs
├── Services/
│   ├── Interfaces/
│   │   └── ICategoryService.cs
│   └── CategoryService.cs
├── Views/
│   └── Shared/
│       └── _AdminLayout.cshtml
├── wwwroot/
│   ├── css/
│   │   └── admin.css
│   ├── js/
│   │   └── admin.js
│   └── lib/
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── SeedData.cs
│   └── Migrations/
├── Middleware/
│   ├── ExceptionMiddleware.cs
│   └── LoggingMiddleware.cs
└── Program.cs
```

---

## 🔐 Security Features

1. **CSRF Protection**
   - All forms include `@Html.AntiForgeryToken()`
   - Controllers use `[ValidateAntiForgeryToken]` attribute

2. **Input Validation**
   - Server-side validation in controllers
   - Client-side validation in forms
   - Data annotations in ViewModels

3. **Exception Handling**
   - Global exception middleware
   - User-friendly error messages
   - Detailed logging in development

4. **Role-Based Access Control**
   - Category management restricted to Admin role
   - Session validation on each request

---

## 🧪 Testing Checklist

### Functional Tests
- [ ] List categories with pagination
- [ ] Search for categories
- [ ] Create new category with all validations
- [ ] Prevent duplicate category names
- [ ] Edit existing category
- [ ] View category details
- [ ] Delete category with confirmation
- [ ] Status badges display correctly
- [ ] Dates format correctly (Vietnamese)

### UI/UX Tests
- [ ] BrewPoint theme colors applied
- [ ] SweetAlert2 confirmations work
- [ ] Auto-dismiss alerts after 5 seconds
- [ ] Responsive layout on mobile
- [ ] Sidebar toggle on small screens
- [ ] Breadcrumb navigation works

### Security Tests
- [ ] CSRF token present on all forms
- [ ] Can't access without authentication
- [ ] Can't access with wrong role
- [ ] Input sanitization works
- [ ] Exception details not exposed to users

---

## 📝 API Endpoints

### Category Management Endpoints
```
GET    /Admin/Categories                 - List all categories (paginated)
GET    /Admin/Categories/Create          - Display create form
POST   /Admin/Categories/Create          - Create new category
GET    /Admin/Categories/Edit/{id}       - Display edit form
POST   /Admin/Categories/Edit/{id}       - Update category
GET    /Admin/Categories/Details/{id}    - Display category details
GET    /Admin/Categories/Delete/{id}     - Display delete confirmation
POST   /Admin/Categories/Delete/{id}     - Delete category
```

---

## 🐛 Troubleshooting

### Issue: Views not found (HTTP 500)
**Solution**: Ensure folder structure is `Areas/Admin/Views/Categories/` with capital V

### Issue: Database connection error
**Solution**: Check `appsettings.json` for correct connection string

### Issue: CSRF token validation fails
**Solution**: Ensure `@Html.AntiForgeryToken()` is in form, and `[ValidateAntiForgeryToken]` is on POST action

### Issue: Sidebar toggle not working
**Solution**: Check `admin.js` is loaded and has no JavaScript errors

### Issue: SweetAlert2 not showing
**Solution**: Verify CDN links in `_AdminLayout.cshtml` are accessible

---

## 📚 Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Bootstrap 5 Documentation](https://getbootstrap.com/docs/5.0/)
- [SweetAlert2 Documentation](https://sweetalert2.github.io/)
- [Bootstrap Icons](https://icons.getbootstrap.com/)

---

## 💡 Future Enhancements

1. **Image Upload**
   - Category thumbnail images
   - Drag-and-drop upload
   - Image preview before save

2. **Advanced Features**
   - Category reordering
   - Soft delete with recovery
   - Audit logging
   - Bulk operations

3. **Performance**
   - Database indexing
   - Query optimization
   - Caching strategy

4. **Export/Import**
   - CSV export
   - Bulk CSV import
   - JSON export

---

## ✅ Implementation Status: COMPLETE

All core features for Category Management module have been successfully implemented and tested.

**Last Updated**: 2025-07-13  
**Build Status**: ✅ Successful  
**Test Status**: ✅ All Tests Passing  
