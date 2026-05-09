# FILE 5 - BACKLOG VÀ THỨ TỰ TRIỂN KHAI
## Cổng thông tin điện tử trường học đa cơ sở

---

## 1. Mục tiêu
Tài liệu này chia nhỏ phạm vi hệ thống thành các hạng mục triển khai để team dev có thể làm theo thứ tự rõ ràng, giảm rủi ro và đảm bảo đúng nền tảng trước khi làm tính năng mở rộng.

---

## 2. Thứ tự triển khai đề xuất

### Giai đoạn 1 - Nền tảng hệ thống
1. Khởi tạo project.
2. Thiết lập cấu trúc thư mục.
3. Thiết kế database lõi.
4. Thiết lập auth/login.
5. Thiết lập phân quyền.
6. Thiết lập cơ chế `ma_truong_bo`.
7. Thiết lập logging / audit.

### Giai đoạn 2 - Quản trị nội dung cơ bản
1. Quản lý trường.
2. Quản lý tài khoản local.
3. Quản lý role/permission.
4. Quản lý menu.
5. Quản lý banner.
6. Quản lý trang tĩnh.

### Giai đoạn 3 - Nội dung công khai
1. Quản lý bài viết.
2. Quản lý chuyên mục.
3. Quản lý tag.
4. Quản lý media.
5. Trang chủ công khai.
6. Danh sách và chi tiết bài viết.

### Giai đoạn 4 - Nội dung pháp lý
1. Quản lý văn bản công khai.
2. Quản lý học phí.
3. Quản lý nhân sự.
4. Quản lý báo cáo hoạt động.

### Giai đoạn 5 - Mở rộng nghiệp vụ
1. Tuyển sinh.
2. Góc phụ huynh.
3. Câu lạc bộ.
4. Tài nguyên.
5. Tìm kiếm nâng cao.

### Giai đoạn 6 - Báo cáo và hoàn thiện
1. Dashboard tổng hợp.
2. Báo cáo theo trường.
3. Báo cáo công khai.
4. Nhật ký hệ thống.
5. Tối ưu UI/UX.
6. Tối ưu hiệu năng.

---

## 3. Backlog theo epics

### Epic 1 - Nền tảng dự án
- Khởi tạo project frontend/backend.
- Cấu hình database.
- Cấu hình migration/seed.
- Tạo base layout.
- Tạo module cấu hình chung.

### Epic 2 - Xác thực và phân quyền
- Đăng nhập SSO.
- Đăng nhập local account.
- Đăng xuất.
- Refresh session.
- Gán role.
- Kiểm tra quyền theo middleware/service.
- Kiểm soát truy cập theo trường.

### Epic 3 - Quản lý trường
- Danh sách trường.
- Thêm mới trường.
- Cập nhật thông tin trường.
- Cấu hình logo/banner/liên hệ.
- Gán `ma_truong_bo`.

### Epic 4 - Quản lý tài khoản
- Tạo tài khoản local.
- Gán role Administrator.
- Khóa/mở khóa tài khoản.
- Reset mật khẩu.
- Map tài khoản SSO nếu cần.

### Epic 5 - Quản lý nội dung
- Quản lý bài viết.
- Quản lý chuyên mục.
- Quản lý tag.
- Upload media.
- Gửi duyệt / duyệt / từ chối.

### Epic 6 - Văn bản công khai
- Danh sách văn bản.
- Thêm mới văn bản.
- Upload file.
- Duyệt và công khai.
- Theo dõi version/lịch sử.

### Epic 7 - Giao diện công khai
- Trang chủ.
- Trang chuyên mục.
- Trang chi tiết bài viết.
- Trang chi tiết văn bản.
- Trang tìm kiếm.

### Epic 8 - Mở rộng nội dung
- Tuyển sinh.
- Góc phụ huynh.
- Câu lạc bộ.
- Tài nguyên.

### Epic 9 - Báo cáo và vận hành
- Dashboard.
- Báo cáo nội dung.
- Báo cáo theo trường.
- Audit log.
- Monitoring cơ bản.

---

## 4. Backlog chi tiết theo mức ưu tiên

### P0 - Bắt buộc có trước
- Login SSO.
- Login local account.
- `ma_truong_bo`.
- Quản lý tài khoản local.
- Quản lý bài viết.
- Quản lý văn bản công khai.
- Trang chủ công khai.
- Duyệt nội dung.

### P1 - Nên có trong release đầu
- Banner.
- Menu.
- Trang tĩnh.
- Tìm kiếm.
- Chuyên mục tin tức.
- Upload media.

### P2 - Mở rộng sau
- Tuyển sinh.
- Góc phụ huynh.
- Câu lạc bộ.
- Tài nguyên.
- Báo cáo nâng cao.

---

## 5. Danh sách task triển khai mẫu

### Task nhóm backend/service
- Tạo auth service cho SSO.
- Tạo auth service cho local account.
- Tạo middleware phân quyền.
- Tạo service theo module.
- Tạo service công khai.
- Tạo service quản trị.
- Tạo audit log service.

### Task nhóm frontend
- Tạo layout public.
- Tạo layout admin.
- Tạo trang dashboard.
- Tạo màn hình quản lý tài khoản.
- Tạo màn hình quản lý bài viết.
- Tạo màn hình quản lý văn bản.
- Tạo màn hình duyệt nội dung.

### Task nhóm database
- Tạo bảng lõi.
- Tạo bảng tài khoản.
- Tạo bảng nội dung.
- Tạo index.
- Tạo seed dữ liệu mẫu.

### Task nhóm QA
- Test login SSO.
- Test login local.
- Test phân quyền.
- Test tách dữ liệu theo trường.
- Test duyệt nội dung.
- Test public display.

---

## 6. Đề xuất thứ tự triển khai sprint

### Sprint 1
- Project setup.
- Database lõi.
- Auth + role.
- `ma_truong_bo`.

### Sprint 2
- Quản lý trường.
- Quản lý tài khoản.
- Quản lý menu/banner.

### Sprint 3
- Quản lý bài viết.
- Chuyên mục.
- Upload media.
- Public posts.

### Sprint 4
- Quản lý văn bản công khai.
- Public documents.
- Trang tĩnh.

### Sprint 5
- Duyệt nội dung.
- Dashboard.
- Search.
- Audit log.

### Sprint 6
- Tuyển sinh.
- Góc phụ huynh.
- Câu lạc bộ.
- Tài nguyên.

---

## 7. Definition of Done cơ bản
Một hạng mục được xem là hoàn thành khi:
- Có thiết kế hoặc requirement rõ ràng.
- Có code backend/frontend hoàn chỉnh.
- Có service/handler hoạt động.
- Có kiểm thử cơ bản.
- Có dữ liệu tách đúng theo trường.
- Không phát sinh lỗi nghiêm trọng.

---

## 8. Kết luận
Backlog được sắp theo nguyên tắc:
1. Làm nền tảng trước.
2. Làm auth/login trước.
3. Làm quản trị nội dung trước.
4. Làm công khai trước.
5. Mở rộng chức năng sau.

Cách triển khai này phù hợp cho hệ thống cổng thông tin đa trường có 2 luồng login: SSO và local account.
