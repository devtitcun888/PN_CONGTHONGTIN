# FILE 4 - SERVICE SPECIFICATION
## Cổng thông tin điện tử trường học đa cơ sở

**Mục tiêu:** Mô tả chi tiết service/hàm nghiệp vụ, bao gồm input, output, workflow và ràng buộc để team dev triển khai trực tiếp trong source code.

---

## 1. Nguyên tắc thiết kế service
- Service xử lý nghiệp vụ, không mô tả endpoint API.
- Mọi service nghiệp vụ liên quan đến dữ liệu trường phải nhận hoặc suy ra `ma_truong_bo`.
- Service phải kiểm tra quyền trước khi đọc/ghi dữ liệu.
- Service phải đảm bảo dữ liệu công khai chỉ lấy từ nội dung đã duyệt/đã xuất bản.
- Service nên chia theo module rõ ràng để dễ bảo trì và test.
- Mỗi hàm service nên làm một việc rõ ràng, hạn chế logic quá dài.
- Website của mỗi trường phải được định danh từ file cấu hình `config.json`, từ đó system xác định `ma_truong_bo` để truy xuất đúng dữ liệu.
- Tầng khởi động ứng dụng phải đọc `config.json` trước, sau đó mới tạo ngữ cảnh trường và nạp service tương ứng.
- Hệ thống hiện có 2 luồng login chính:
  - SSO
  - Local account

---

## 2. Quy ước đầu vào/đầu ra chung

### 2.1. Input chung thường gặp
- `ma_truong_bo`: mã trường lấy từ `config.json` hoặc context hệ thống.
- `userId`: người thao tác.
- `id`: định danh bản ghi.
- `keyword`: từ khóa tìm kiếm.
- `status`: trạng thái.
- `page`, `pageSize`: phân trang.
- `sortBy`, `sortDirection`: sắp xếp.
- `publishAt`: thời điểm công khai.
- `note`: ghi chú xử lý.
- `schoolCode` hoặc `siteCode`: mã website nếu project dùng ký hiệu riêng trong config.

### 2.2. Output chung thường gặp
- `success`: true/false.
- `data`: dữ liệu trả về.
- `message`: thông báo.
- `errors`: danh sách lỗi nếu có.
- `total`: tổng số bản ghi.
- `page`: trang hiện tại.
- `pageSize`: số bản ghi mỗi trang.
- `schoolContext`: ngữ cảnh trường đã resolve từ `config.json`.

---

## 3. Quy ước đặt tên hàm
Khuyến nghị dùng các tiền tố:
- `Create...`
- `Update...`
- `Delete...`
- `Get...`
- `List...`
- `Submit...`
- `Approve...`
- `Reject...`
- `Publish...`
- `Archive...`
- `Toggle...`
- `Assign...`
- `Remove...`
- `Reorder...`
- `Validate...`
- `Login...`
- `Logout...`

---

## 4. Service lõi - chi tiết

### 4.0. ConfigService / SiteContextService
- Đọc `config.json` và xác định trường đang hoạt động.

### 4.1. AuthService
- Xử lý cả SSO và local account.
- Hàm đề xuất:
  - `LoginBySSO(token)`
  - `LoginByLocalAccount(username, password)`
  - `Logout(userId)`
  - `GetCurrentUserContext(token)`
  - `BuildUserClaims(userId)`
  - `ChangePassword(userId, oldPassword, newPassword)`

#### Workflow SSO
1. Nhận token từ SSO.
2. Gọi API session SSO.
3. Lấy user info.
4. Tra thông tin trường.
5. Map sang `UserSession`.
6. Cập nhật authentication state.

#### Workflow local account
1. Nhận username/password.
2. Tra bảng `l_user_account`.
3. Verify password.
4. Kiểm tra `is_active`, `is_locked`.
5. Tạo session.
6. Cập nhật authentication state.

#### Ràng buộc
- Local account chỉ cho phép role `Administrator` ở core hiện tại.
- SSO có thể map về `Administrator` hoặc user hợp lệ đã được cấp quyền.

---

### 4.2. UserAccountService
**Mục tiêu:** đọc thông tin trường và người dùng.

**Hàm đề xuất:**
- `GetThongTinTruong(maTruongBo)`
- `GetThongTinNguoiDung(userId)`
- `GetLocalAccount(username)`
- `GetLocalAccountBySsoUserId(ssoUserId)`
- `ValidateLocalAccount(username, password)`

---

### 4.3. AccountService
**Mục tiêu:** quản lý bảng tài khoản local.

**Input chính:**
- `ma_truong_bo`
- `username`
- `password`
- `full_name`
- `display_name`
- `role_code`
- `auth_type`
- `sso_username`
- `sso_user_id`

**Hàm đề xuất:**
- `CreateAccount(input)`
- `UpdateAccount(id, input)`
- `GetAccountById(id)`
- `GetAccountByUsername(ma_truong_bo, username)`
- `ListAccounts(filter)`
- `LockAccount(id, reason)`
- `UnlockAccount(id)`
- `ResetPassword(id, newPassword)`
- `AssignRole(id, roleCode)`
- `SetLastLogin(id, ipAddress)`

---

### 4.4. SessionService
**Mục tiêu:** tạo, cập nhật, kiểm tra và hủy session.

**Hàm đề xuất:**
- `CreateUserSession(sessionData)`
- `UpdateAuthenticationState(session)`
- `GetCurrentSession()`
- `ValidateSession(tabId, userAgent)`
- `LogoutCurrentSession()`

---

### 4.5. RoleService
**Mục tiêu:** quản lý vai trò hệ thống.

**Ghi chú:** hiện tại core chỉ cần role `Administrator`.

---

### 4.6. PermissionService
**Mục tiêu:** quản lý quyền chi tiết.

---

## 5. Service nội dung - chi tiết

Giữ nguyên các service nội dung đã mô tả trước đó:
- `PostCategoryService`
- `PostService`
- `DocumentService`
- `BannerService`
- `MenuService`
- `PageService`
- `HomepageService`
- `ContactService`
- `ReportService`
- `AuditLogService`

---

## 6. Service công khai - đọc dữ liệu
- `PublicPostService`
- `PublicDocumentService`
- `PublicPageService`
- `PublicMenuService`
- `PublicBannerService`

---

## 7. Cấu trúc service gợi ý
- `Services/Auth/`
- `Services/Accounts/`
- `Services/Sessions/`
- `Services/Public/`
- `Services/Admin/`
- `Services/Content/`
- `Services/Common/`

---

## 8. Kết luận
File này đã bổ sung rõ 2 luồng login và service quản lý tài khoản local, đồng thời giữ nguyên nguyên tắc service theo `ma_truong_bo` và DB helper có sẵn.
