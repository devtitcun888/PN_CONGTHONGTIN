# FILE 7 - AGENT STEP BY STEP
## Cổng thông tin điện tử trường học đa cơ sở

**Mục tiêu:** Hướng dẫn Agent AI thực hiện từng bước để triển khai đúng thứ tự, không bỏ sót nền tảng và không tạo lại phần không còn dùng.

---

## 1. Nguyên tắc thực thi
- Chỉ có 1 role code thực thi là `Administrator` ở core hiện tại.
- Public portal không dựa vào role, chỉ dựa vào trạng thái công khai của dữ liệu.
- Dữ liệu phải gắn `ma_truong_bo`.
- Có 2 luồng login chính:
  - SSO
  - Local account
- Mỗi bước chỉ xử lý 1 nhóm việc nhỏ để dễ kiểm soát.

---

## 2. Thứ tự triển khai bắt buộc

### Bước 1 - Kiểm tra nền tảng hiện có
1. Xác nhận bảng `l_truong` đã tồn tại.
2. Xác nhận bảng `l_ssosession` đã tồn tại.
3. Kiểm tra cấu trúc session đang dùng.
4. Kiểm tra luồng login admin.

### Bước 2 - Chuẩn hóa tài liệu nghiệp vụ
1. Đọc `01_BA_TongQuan.md`.
2. Đọc `02_ERD_Database.md`.
3. Đọc `03_Sitemap_LuongManHinh.md`.
4. Đọc `04_Service_Spec.md`.
5. Đọc `05_Backlog_Implementation_Order.md`.
6. Đọc `06_DB_Create_SQL_PostgreSQL.md`.

### Bước 3 - Khởi tạo cấu hình site và tài khoản
1. Tạo bảng `l_user_account`.
2. Tạo bảng `l_user_session` nếu cần.
3. Tạo seed `Administrator`.
4. Tạo dữ liệu tài khoản local đầu tiên nếu dự án cần.

### Bước 4 - Khởi tạo điều hướng giao diện
1. Tạo `site_settings`.
2. Tạo `menus`.
3. Tạo `banners`.
4. Tạo `site_pages`.
5. Hiển thị layout public từ dữ liệu DB.

### Bước 5 - Khởi tạo content module
1. Tạo `post_categories`.
2. Tạo `posts`.
3. Tạo `post_media`.
4. Tạo `post_tags`.
5. Tạo `post_tag_map`.

### Bước 6 - Khởi tạo văn bản công khai
1. Tạo `document_types`.
2. Tạo `documents`.
3. Tạo `document_versions`.
4. Thiết lập luồng duyệt/công khai.

### Bước 7 - Khởi tạo module phụ trợ
1. Tạo `staff_profiles`.
2. Tạo `tuition_fees`.
3. Tạo `announcements`.
4. Tạo `events`.
5. Tạo `contact_requests`.
6. Tạo `audit_logs`.
7. Tạo `counter_traffic`.

### Bước 8 - Chốt luồng public
1. Xây homepage data.
2. Xây danh sách bài viết.
3. Xây chi tiết bài viết.
4. Xây danh sách văn bản.
5. Xây chi tiết văn bản.
6. Xây form liên hệ.

### Bước 9 - Chốt luồng admin
1. Kiểm tra admin login.
2. Kiểm tra dashboard.
3. Kiểm tra quản lý bài viết.
4. Kiểm tra quản lý văn bản.
5. Kiểm tra menu/banner/page.

---

## 3. Checklist thao tác cho mỗi bước

Mỗi khi Agent xử lý một file hoặc một module, phải làm đủ 5 việc sau:
1. Đọc file liên quan.
2. Xác định tác động tới DB / service / UI.
3. Ghi nhận thay đổi cần làm.
4. Thực hiện chỉnh sửa tối thiểu nhưng đúng.
5. Kiểm tra lại tính nhất quán.

---

## 4. Những việc tuyệt đối không làm lại
- Không tạo thêm role ngoài `Administrator` ở core hiện tại.
- Không đưa các role cũ trở lại authorization core nếu không có yêu cầu mới.
- Không trộn dữ liệu public của trường này sang trường khác.
- Không viết lại toàn bộ tài liệu nếu chỉ cần sửa cục bộ.

---

## 5. Mẫu cách Agent nên làm việc

### Mẫu 1 - Phân tích bảng
1. Đọc file ERD.
2. Đối chiếu luồng màn hình.
3. Chốt bảng cần thêm.
4. Viết SQL tạo bảng.
5. Kiểm tra tên cột và index.

### Mẫu 2 - Phân tích màn hình
1. Đọc sitemap.
2. Xác định màn hình public hay admin.
3. Map màn hình với service tương ứng.
4. Xác định dữ liệu đầu vào/đầu ra.
5. Cập nhật tài liệu nếu cần.

### Mẫu 3 - Phân tích service
1. Đọc service spec.
2. Xác định input/output.
3. Xác định workflow.
4. Xác định ràng buộc.
5. Cập nhật backlog triển khai.

---

## 6. Lệnh khởi tạo gợi ý cho Agent

Khi cần tạo DB, Agent phải ưu tiên file:
- `06_DB_Create_SQL_PostgreSQL.md`

Khi cần theo đúng trình tự làm việc, Agent phải đọc file:
- `07_Agent_Step_By_Step.md`

---

## 7. Kết luận
File này là file điều khiển thao tác cho Agent AI. Nó giúp Agent làm việc theo từng bước, tránh nhảy cóc, tránh tạo lại role cũ và giữ đúng kiến trúc hiện tại của Cổng Thông Tin.
