# FILE 2 - ERD DATABASE
## Cổng thông tin điện tử trường học đa cơ sở

**Mục tiêu:** Thiết kế dữ liệu lõi cho hệ thống cổng thông tin điện tử trường học dùng chung nhiều trường, phân tách bằng `ma_truong_bo`, đồng thời bổ sung bảng tài khoản phục vụ 2 luồng đăng nhập: SSO và local account.

---

## 1. Nguyên tắc thiết kế
- Một database dùng chung.
- Dữ liệu nghiệp vụ của từng trường phải có `ma_truong_bo`.
- Nội dung công khai có trạng thái duyệt.
- Có nhật ký thao tác.
- Có thể mở rộng module mới.
- Hạn chế hard delete, ưu tiên soft delete.
- Mọi bảng nghiệp vụ nên có các cột audit cơ bản: `created_at`, `updated_at`, `created_by`, `updated_by` nếu phù hợp.
- Hệ thống có 2 luồng login:
  - SSO (đọc session từ nguồn SSO bên ngoài)
  - Local account (đăng nhập bằng tài khoản trong DB)

---

## 2. Quy ước chung cho các bảng

### 2.1. Cột chuẩn nên có
- `id`: khóa chính.
- `ma_truong_bo`: mã trường, phân vùng dữ liệu.
- `is_active`: trạng thái kích hoạt.
- `is_deleted`: trạng thái xóa mềm.
- `created_at`: thời điểm tạo.
- `updated_at`: thời điểm cập nhật.
- `created_by`: người tạo.
- `updated_by`: người cập nhật.

### 2.2. Quy ước kiểu dữ liệu gợi ý
- `id`: bigint / int identity.
- `ma_truong_bo`: varchar(50) hoặc varchar(100).
- `name/title/code/slug`: nvarchar/varchar tùy chuẩn hệ thống.
- `content/description/bio`: text/nvarchar(max).
- `status`: varchar hoặc tinyint theo enum nội bộ.
- `created_at/updated_at`: timestamp/timestamptz.
- `amount`: decimal(18,2).
- `is_active/is_deleted/is_featured/is_public`: boolean.

### 2.3. Quy tắc khóa
- PK: `id`.
- FK: liên kết theo `id` của bảng cha.
- Unique: áp dụng cho các trường mã hóa như `ma_truong_bo`, `username`, `slug` theo phạm vi trường.
- Index: ưu tiên cho `ma_truong_bo`, `status`, `publish_at`, `slug`, `is_active`.

---

## 3. Thực thể chính và chi tiết bảng

---

### 3.1. `schools`
Đại diện trường/cơ sở giáo dục.

#### Cột
- `id` PK
- `ma_truong_bo` unique, not null
- `ten_truong` not null
- `ten_viet_tat` nullable
- `loai_hinh` nullable
- `cap_hoc` nullable
- `ma_don_vi` nullable
- `dia_chi` nullable
- `phuong_xa` nullable
- `quan_huyen` nullable
- `tinh_thanh` nullable
- `dien_thoai` nullable
- `email` nullable
- `website` nullable
- `logo_url` nullable
- `banner_url` nullable
- `mo_ta` nullable
- `is_active` default true
- `created_by` nullable
- `updated_by` nullable
- `created_at` not null
- `updated_at` not null
- `is_deleted` default false

#### Chỉ mục / ràng buộc
- Unique: `ma_truong_bo`
- Index: `is_active`, `ten_truong`

---

### 3.2. `l_user_account`
Bảng tài khoản local để đăng nhập vào hệ thống admin.

#### Mục đích nghiệp vụ
- Dùng cho luồng đăng nhập bằng tài khoản trong DB.
- Có thể map sang trường quản lý.
- Không phụ thuộc SSO.
- Nếu cần, một tài khoản có thể được gắn thêm `sso_username` để đối chiếu.

#### Cột
- `id` PK
- `ma_truong_bo` not null
- `username` unique theo hệ thống hoặc unique theo trường
- `password_hash` not null
- `full_name` not null
- `display_name` nullable
- `email` nullable
- `phone` nullable
- `role_code` not null  -- hiện tại chuẩn triển khai chỉ giữ `Administrator`
- `auth_type` not null  -- `Local`, `SSO`, `Hybrid`
- `sso_username` nullable
- `sso_user_id` nullable
- `device_name` nullable
- `last_login_at` nullable
- `last_login_ip` nullable
- `is_active` default true
- `is_locked` default false
- `lock_reason` nullable
- `created_by` nullable
- `updated_by` nullable
- `created_at` not null
- `updated_at` not null
- `is_deleted` default false

#### Chỉ mục / ràng buộc
- Unique: `(ma_truong_bo, username)`
- Index: `(ma_truong_bo, role_code)`
- Index: `(ma_truong_bo, auth_type)`
- Index: `(ma_truong_bo, is_active)`

#### Ghi chú nghiệp vụ
- Hiện tại role triển khai thực tế chỉ dùng `Administrator`.
- Nếu về sau mở rộng, có thể bổ sung role nghiệp vụ nhưng không làm thay đổi core auth.
- `auth_type = SSO` chỉ để ghi nhận tài khoản SSO đã map, không nhất thiết phải lưu password local.

---

### 3.3. `l_user_session`
Bảng session nghiệp vụ nếu muốn lưu session server-side.

#### Cột
- `id` PK
- `session_id` unique not null
- `ma_truong_bo` not null
- `username` not null
- `user_agent` nullable
- `tab_id` nullable
- `expiry_time` not null
- `is_active` default true
- `created_at` not null
- `updated_at` not null

#### Chỉ mục / ràng buộc
- Unique: `session_id`
- Index: `(ma_truong_bo, username)`
- Index: `(ma_truong_bo, expiry_time)`

#### Ghi chú nghiệp vụ
- Có thể dùng nếu muốn đồng bộ session giữa browser và server.
- Nếu hệ thống đang giữ session chủ yếu ở browser storage thì bảng này có thể để phase sau.

---

### 3.4. `roles`
Vai trò hệ thống.

#### Cột
- `id` PK
- `role_code` unique, not null
- `role_name` not null
- `description` nullable
- `scope_type` nullable  -- system/school
- `is_system` default false
- `is_active` default true
- `created_at` not null
- `updated_at` not null
- `is_deleted` default false

#### Chỉ mục / ràng buộc
- Unique: `role_code`
- Index: `scope_type`, `is_active`

#### Ghi chú nghiệp vụ
- Với yêu cầu hiện tại, role triển khai thực tế là `Administrator`.
- Các vai trò khác trong tài liệu cũ sẽ không còn dùng ở core auth.

---

### 3.5. `permissions`
Quyền chức năng chi tiết.

#### Cột
- `id` PK
- `permission_code` unique, not null
- `permission_name` not null
- `module_name` nullable
- `description` nullable
- `is_active` default true
- `created_at` not null
- `updated_at` not null

---

### 3.6. `user_roles`
Bảng gán role cho user.

#### Cột
- `id` PK
- `user_id` FK -> `l_user_account.id`
- `role_id` FK -> `roles.id`
- `created_at` not null
- `created_by` nullable

#### Chỉ mục / ràng buộc
- Unique kép: `(user_id, role_id)`
- Index: `user_id`, `role_id`

---

### 3.7. `role_permissions`
Bảng gán quyền cho role.

#### Cột
- `id` PK
- `role_id` FK -> `roles.id`
- `permission_id` FK -> `permissions.id`
- `created_at` not null
- `created_by` nullable

---

### 3.8. `post_categories`
Danh mục tin bài.

#### Cột
- `id` PK
- `ma_truong_bo` not null
- `category_code` nullable
- `category_name` not null
- `slug` not null
- `parent_id` FK -> `post_categories.id`, nullable
- `description` nullable
- `sort_order` default 0
- `is_active` default true
- `created_by` nullable
- `updated_by` nullable
- `created_at` not null
- `updated_at` not null
- `is_deleted` default false

---

### 3.9. `posts`
Bài viết.

#### Cột
- `id` PK
- `ma_truong_bo` not null
- `category_id` FK -> `post_categories.id`, not null
- `title` not null
- `slug` not null
- `summary` nullable
- `content` not null
- `cover_image_url` nullable
- `post_type` nullable
- `status` not null
- `is_featured` default false
- `sort_order` default 0
- `publish_at` nullable
- `expire_at` nullable
- `view_count` default 0
- `created_by` nullable
- `updated_by` nullable
- `approved_by` nullable
- `approved_at` nullable
- `rejected_by` nullable
- `rejected_at` nullable
- `reject_reason` nullable
- `created_at` not null
- `updated_at` not null
- `is_deleted` default false

---

### 3.10. `post_tags`
Tag bài viết.

### 3.11. `post_tag_map`
Liên kết bài viết và tag.

### 3.12. `post_media`
Media của bài viết.

### 3.13. `document_types`
Danh mục loại văn bản công khai.

### 3.14. `documents`
Văn bản công khai.

### 3.15. `document_versions`
Lịch sử phiên bản văn bản.

### 3.16. `staff_profiles`
Hồ sơ nhân sự công khai.

### 3.17. `tuition_fees`
Thông tin học phí / phí dịch vụ.

### 3.18. `announcements`
Thông báo.

### 3.19. `events`
Sự kiện.

### 3.20. `site_pages`
Trang tĩnh.

### 3.21. `menus`
Menu.

### 3.22. `banners`
Banner.

### 3.23. `site_settings`
Cấu hình site.

### 3.24. `audit_logs`
Nhật ký thao tác.

### 3.25. `contact_requests`
Liên hệ từ người dùng công khai.

### 3.26. `counter_traffic`
Bảng thống kê lượt truy cập.

---

## 4. Quan hệ dữ liệu
- `schools` 1 - N `l_user_account`
- `schools` 1 - N `post_categories`
- `schools` 1 - N `posts`
- `schools` 1 - N `post_tags`
- `schools` 1 - N `documents`
- `schools` 1 - N `document_types`
- `schools` 1 - N `staff_profiles`
- `schools` 1 - N `tuition_fees`
- `schools` 1 - N `announcements`
- `schools` 1 - N `events`
- `schools` 1 - N `site_pages`
- `schools` 1 - N `menus`
- `schools` 1 - N `banners`
- `schools` 1 - N `site_settings`
- `schools` 1 - N `contact_requests`
- `schools` 1 - N `counter_traffic`
- `posts` 1 - N `post_media`
- `posts` N - N `post_tags` qua `post_tag_map`
- `l_user_account` N - N `roles` qua `user_roles`
- `roles` N - N `permissions` qua `role_permissions`
- `documents` 1 - N `document_versions`
- `menus` 1 - N `menus` qua `parent_id`

---

## 5. Quy ước login

### 5.1. SSO login
- SSO trả token về `LoginPagesAdmin.razor`.
- Hệ thống đọc token, gọi API session SSO.
- Lấy `SchoolId`, `UserID`, `UserName`.
- Tra cứu `l_truong` và bảng tài khoản/nguồn user tương ứng.
- Tạo `UserSession` và cập nhật `AuthenticationState`.

### 5.2. Local account login
- Người dùng nhập `username` và `password`.
- Hệ thống tra bảng `l_user_account`.
- Xác thực password hash hoặc password thô theo dữ liệu hiện có.
- Kiểm tra `is_active`, `is_locked`.
- Tạo `UserSession`.

---

## 6. Kết luận
ERD đã được bổ sung bảng tài khoản để phục vụ 2 luồng login, đồng thời vẫn giữ mô hình đa trường trên một database và khả năng mở rộng về sau.
