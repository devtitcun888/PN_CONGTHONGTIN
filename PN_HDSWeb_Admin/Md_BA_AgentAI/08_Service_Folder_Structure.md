# FILE 8 - SERVICE FOLDER STRUCTURE
## Cổng thông tin điện tử trường học đa cơ sở

Mục tiêu của file này là chuẩn hóa cây thư mục `Services/` để source dễ bảo trì, tách bạch public, admin, auth và common.

---

## 1. Nguyên tắc tổ chức
- Tách theo nghiệp vụ, không tách theo UI.
- Auth, account, session phải nằm riêng.
- Public portal chỉ đọc dữ liệu công khai.
- Admin portal xử lý nội dung, tài khoản, cấu hình.
- Common chứa helper và tiện ích dùng chung.
- Mỗi service chỉ nên làm một việc chính.

---

## 2. Cây thư mục đề xuất

```text
Data/
  Model/
  Services/
    Auth/
    Accounts/
    Sessions/
    Common/
    Public/
    Admin/
    Content/
    Configuration/
    Reporting/
    Audit/
```

---

## 3. Mô tả từng nhóm

### 3.1. `Services/Auth/`
Chứa các service liên quan đến xác thực.
- `AuthService`
- `SSOAuthService`
- `LocalAuthService`
- `AuthenticationStateBuilder`

### 3.2. `Services/Accounts/`
Quản lý tài khoản local và ánh xạ user.
- `UserAccountService`
- `AccountService`
- `AccountValidator`

### 3.3. `Services/Sessions/`
Quản lý session của user.
- `SessionService`
- `SessionStoreService`
- `SessionFingerprintService`

### 3.4. `Services/Common/`
Các helper dùng chung.
- `PasswordHasherService`
- `DateTimeService`
- `CurrentSchoolContextService`
- `SlugService`

### 3.5. `Services/Public/`
Chỉ đọc dữ liệu đã công khai.
- `PublicHomepageService`
- `PublicPostService`
- `PublicDocumentService`
- `PublicPageService`
- `PublicMenuService`
- `PublicBannerService`

### 3.6. `Services/Admin/`
Service nghiệp vụ dành cho quản trị.
- `AdminDashboardService`
- `AdminMenuService`
- `AdminBannerService`
- `AdminPageService`
- `AdminUserService`
- `AdminSiteSettingService`

### 3.7. `Services/Content/`
Quản lý nội dung.
- `PostService`
- `PostCategoryService`
- `PostTagService`
- `DocumentService`
- `DocumentTypeService`
- `MediaService`
- `AnnouncementService`
- `EventService`
- `StaffProfileService`
- `TuitionFeeService`

### 3.8. `Services/Configuration/`
Cấu hình hệ thống và site.
- `ConfigService`
- `SiteContextService`
- `SchoolService`
- `SiteSettingService`

### 3.9. `Services/Reporting/`
Báo cáo và tổng hợp.
- `ReportService`
- `TrafficCounterService`

### 3.10. `Services/Audit/`
Nhật ký và truy vết.
- `AuditLogService`

---

## 4. Mapping source hiện tại
- `Authentication/UserAccountService.cs` → chuyển về `Services/Accounts/` khi refactor tiếp.
- `Authentication/CustomAuthenticationStateProvider.cs` → chuyển về `Services/Auth/` hoặc giữ tại `Authentication/` tùy chuẩn project.
- `Pages/LoginPagesAdmin.razor` → chỉ giữ UI, không chứa logic quá nặng; logic login sẽ gọi service.

---

## 5. Kết luận
Cây thư mục này giúp source tách rõ 3 miền:
- `Auth`
- `Public`
- `Admin`

và phù hợp với hệ thống cổng thông tin đa trường chỉ giữ core role `Administrator`.
