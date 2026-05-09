# FILE 3 - SITEMAP VÀ LUỒNG MÀN HÌNH
## Cổng thông tin điện tử trường học đa cơ sở

**Mục tiêu:** Chuẩn hóa sitemap và mô tả chi tiết từng màn hình cho hệ thống cổng thông tin điện tử trường học, gồm khu vực công khai và khu vực quản trị.

---

## 1. Nguyên tắc thiết kế sitemap
- Chia rõ 2 vùng: công khai và quản trị.
- Mỗi màn hình phải gắn với một mục tiêu nghiệp vụ rõ ràng.
- Màn hình công khai chỉ hiển thị dữ liệu đã duyệt/đã xuất bản.
- Màn hình quản trị phục vụ nhập liệu, kiểm duyệt, cấu hình và báo cáo.
- Dữ liệu từng trường phải được lọc theo `ma_truong_bo`.

---

## 2. Sitemap khu vực công khai

### 2.1. Trang chủ
**Mục tiêu:** Là điểm vào chính của website, hiển thị thông tin nổi bật và các khối nội dung chính của trường.

**Thành phần:**
- Banner chính
- Thông báo nổi bật
- Tin nổi bật
- Tin mới nhất
- Sự kiện sắp diễn ra
- Video / media nổi bật
- Liên kết nhanh
- Thống kê truy cập
- Footer thông tin liên hệ

**Nghiệp vụ:**
- Hiển thị theo `ma_truong_bo`.
- Ưu tiên bài ghim và thông báo quan trọng.
- Có thể cấu hình thứ tự các khối.

---

### 2.2. Màn hình Giới thiệu
**Mục tiêu:** Cung cấp thông tin tổng quan về đơn vị.

**Trang con:**
- Lịch sử hình thành
- Cơ cấu tổ chức
- Chức năng nhiệm vụ
- Ban lãnh đạo
- Thành tích
- Tầm nhìn - sứ mạng
- Thông tin liên hệ

**Nghiệp vụ:**
- Có thể là các trang tĩnh hoặc bài viết chuyên biệt.
- Mỗi trang có nội dung riêng theo trường.

---

### 2.3. Màn hình Tin tức
**Mục tiêu:** Hiển thị tin bài hoạt động, sự kiện, thông báo, truyền thông giáo dục.

**Trang con:**
- Danh sách bài viết
- Chi tiết bài viết
- Lọc theo chuyên mục
- Lọc theo tag
- Bài liên quan

**Nghiệp vụ:**
- Lọc theo `ma_truong_bo`.
- Chỉ hiển thị bài đã xuất bản.
- Cho phép phân loại theo chuyên mục và tag.

---

### 2.4. Màn hình Thông báo
**Mục tiêu:** Hiển thị các thông báo ngắn, khẩn, lịch học, lịch thi, nội dung điều hành.

**Trang con:**
- Danh sách thông báo
- Chi tiết thông báo

**Nghiệp vụ:**
- Có thể phân mức độ ưu tiên.
- Có thời gian hiệu lực hiển thị.

---

### 2.5. Màn hình Văn bản công khai
**Mục tiêu:** Công khai các văn bản bắt buộc theo quy định.

**Trang con:**
- Danh sách văn bản
- Chi tiết văn bản
- Tải file đính kèm

**Nghiệp vụ:**
- Văn bản phải có metadata bắt buộc.
- Chỉ bản đã duyệt/công khai mới hiển thị.
- Có thể phân nhóm theo loại văn bản.

---

### 2.6. Màn hình Tuyển sinh
**Mục tiêu:** Cung cấp thông tin tuyển sinh theo từng năm học hoặc đợt tuyển sinh.

**Trang con:**
- Thông báo tuyển sinh
- Kế hoạch tuyển sinh
- Chỉ tiêu tuyển sinh
- Biểu mẫu tải xuống
- Câu hỏi thường gặp

**Nghiệp vụ:**
- Có thể bật/tắt theo từng đợt.
- Có thể gắn thời gian hiển thị.

---

### 2.7. Màn hình Góc phụ huynh
**Mục tiêu:** Cung cấp thông tin phục vụ phối hợp giữa nhà trường và phụ huynh.

**Trang con:**
- Nội quy học sinh
- Lịch học
- Lịch thi
- Thực đơn
- Hướng dẫn phối hợp
- Tài liệu cần biết

**Nghiệp vụ:**
- Có thể phân theo khối/lớp nếu yêu cầu.
- Nội dung được quản lý riêng bởi trường.

---

### 2.8. Màn hình Câu lạc bộ / hoạt động ngoại khóa
**Mục tiêu:** Giới thiệu hoạt động câu lạc bộ, ngoại khóa, phong trào.

**Trang con:**
- Danh sách câu lạc bộ
- Chi tiết hoạt động
- Tin liên quan
- Lịch sinh hoạt

**Nghiệp vụ:**
- Có thể quản lý theo học kỳ hoặc năm học.

---

### 2.9. Màn hình Tài nguyên
**Mục tiêu:** Cung cấp tài liệu học tập, biểu mẫu và liên kết hữu ích.

**Trang con:**
- Tài liệu học tập
- Biểu mẫu
- Video / học liệu
- Liên kết hữu ích

**Nghiệp vụ:**
- Có hỗ trợ tải xuống.
- Có thể lọc theo nhóm tài nguyên.

---

### 2.10. Màn hình Liên hệ
**Mục tiêu:** Hiển thị thông tin liên hệ và tiếp nhận phản hồi.

**Thành phần:**
- Địa chỉ
- Điện thoại
- Email
- Bản đồ
- Form liên hệ

**Nghiệp vụ:**
- Form liên hệ có kiểm tra đầu vào.
- Có thể ghi nhận lịch sử tiếp nhận.

---

### 2.11. Màn hình Tìm kiếm
**Mục tiêu:** Giúp người dùng tìm nhanh nội dung trên website.

**Thành phần:**
- Ô tìm kiếm
- Bộ lọc loại nội dung
- Danh sách kết quả
- Phân trang

**Nghiệp vụ:**
- Tìm theo từ khóa.
- Tìm trong phạm vi trường đang xem.

---

### 2.12. Màn hình Chi tiết nội dung
Áp dụng cho bài viết, thông báo, văn bản, sự kiện, trang tĩnh.

**Thành phần:**
- Tiêu đề
- Ngày đăng
- Người đăng (nếu hiển thị)
- Nội dung chính
- Media đính kèm
- Bài liên quan / nội dung liên quan

**Nghiệp vụ:**
- Có thể hiển thị ảnh, video, file.
- Có nút tải file nếu là tài liệu.

---

## 3. Sitemap khu vực quản trị

### 3.1. Màn hình Đăng nhập
**Mục tiêu:** Xác thực người dùng vào hệ thống quản trị.

**Thành phần:**
- Username
- Password
- Nút đăng nhập
- Quên mật khẩu (nếu có)

**Nghiệp vụ:**
- Xác thực tài khoản.
- Xác định role và `ma_truong_bo`.

---

### 3.2. Dashboard
**Mục tiêu:** Hiển thị tổng quan trạng thái hệ thống.

**Thành phần:**
- Tổng số bài viết
- Nội dung chờ duyệt
- Nội dung đã xuất bản
- Văn bản công khai
- Thông báo
- Truy cập nhanh

**Nghiệp vụ:**
- Số liệu theo trường.
- Có thể xem tổng hợp nếu là quản trị hệ thống.

---

### 3.3. Màn hình Quản lý bài viết - Danh sách
**Mục tiêu:** Xem và quản lý toàn bộ bài viết của trường.

**Thành phần:**
- Bộ lọc theo chuyên mục, trạng thái, thời gian
- Danh sách bài viết
- Nút thêm mới
- Nút sửa, xóa, gửi duyệt, xuất bản, lưu trữ

**Nghiệp vụ:**
- Chỉ hiển thị dữ liệu theo `ma_truong_bo`.
- Có phân trang, tìm kiếm.

---

### 3.4. Màn hình Quản lý bài viết - Thêm/Sửa
**Mục tiêu:** Tạo hoặc chỉnh sửa bài viết.

**Thành phần:**
- Tiêu đề
- Chuyên mục
- Mô tả ngắn
- Nội dung chi tiết
- Ảnh đại diện
- Album ảnh / file đính kèm
- Tag
- Trạng thái
- Ghi chú nội bộ

**Nghiệp vụ:**
- Có thể lưu nháp.
- Có thể gửi duyệt.
- Có thể đặt lịch xuất bản.

---

### 3.5. Màn hình Quản lý bài viết - Chi tiết duyệt
**Mục tiêu:** Người duyệt xem nội dung trước khi công khai.

**Thành phần:**
- Nội dung bài viết
- Trạng thái hiện tại
- Lịch sử chỉnh sửa
- Ghi chú phản hồi
- Nút duyệt / từ chối / trả về sửa

**Nghiệp vụ:**
- Ghi nhận người duyệt và thời điểm duyệt.
- Khi từ chối phải nhập lý do.

---

### 3.6. Màn hình Quản lý văn bản công khai - Danh sách
**Mục tiêu:** Quản lý văn bản công khai của trường.

**Thành phần:**
- Bộ lọc loại văn bản, trạng thái, thời gian
- Danh sách văn bản
- Nút thêm mới
- Nút sửa, gửi duyệt, công khai, lưu trữ

**Nghiệp vụ:**
- Chỉ nội dung của trường đang thao tác.

---

### 3.7. Màn hình Quản lý văn bản công khai - Thêm/Sửa
**Mục tiêu:** Nhập dữ liệu văn bản công khai.

**Thành phần:**
- Loại văn bản
- Số hiệu
- Tên văn bản
- Cơ quan ban hành
- Ngày ban hành
- Ngày hiệu lực
- Hết hiệu lực
- Tóm tắt
- File đính kèm
- Trạng thái

**Nghiệp vụ:**
- Kiểm tra thông tin bắt buộc.
- Hỗ trợ thay thế văn bản cũ.

---

### 3.8. Màn hình Quản lý trang tĩnh
**Mục tiêu:** Quản lý các trang giới thiệu, liên hệ, cơ cấu tổ chức.

**Thành phần:**
- Danh sách trang
- Mã trang
- Tiêu đề
- Nội dung
- Trạng thái
- Nút thêm/sửa/ẩn

**Nghiệp vụ:**
- Trang tĩnh gắn với slug và hiển thị trên website.

---

### 3.9. Màn hình Quản lý banner
**Mục tiêu:** Cấu hình banner đầu trang và banner phụ.

**Thành phần:**
- Danh sách banner
- Upload ảnh
- Link đi kèm
- Vị trí hiển thị
- Thời gian hiệu lực
- Trạng thái

**Nghiệp vụ:**
- Banner có thể hết hạn theo ngày.
- Banner phải sắp xếp được thứ tự.

---

### 3.10. Màn hình Quản lý menu
**Mục tiêu:** Quản lý cấu trúc menu website.

**Thành phần:**
- Danh sách menu cây
- Thêm menu con
- Sắp xếp thứ tự
- Gắn link nội bộ/ngoại bộ
- Bật/tắt hiển thị

**Nghiệp vụ:**
- Hỗ trợ menu cha - con.
- Menu công khai phải đồng bộ với site.

---

### 3.11. Màn hình Quản lý người dùng
**Mục tiêu:** Quản lý tài khoản nội bộ.

**Thành phần:**
- Danh sách user
- Tìm kiếm theo tên/tài khoản
- Thêm mới
- Phân vai trò
- Khóa/mở khóa
- Reset mật khẩu

**Nghiệp vụ:**
- User phải được gắn trường đúng phạm vi.

---

### 3.12. Màn hình Quản lý trường
**Mục tiêu:** Quản lý thông tin từng trường trên hệ thống.

**Thành phần:**
- Danh sách trường
- Thêm trường
- Sửa trường
- Cấu hình `ma_truong_bo`
- Logo/banner/liên hệ
- Trạng thái hoạt động

**Nghiệp vụ:**
- `ma_truong_bo` là mã duy nhất.
- Trường inactive không được phép hiển thị website công khai.

---

### 3.13. Màn hình Quản lý duyệt nội dung
**Mục tiêu:** Tổng hợp các nội dung chờ duyệt.

**Thành phần:**
- Danh sách chờ duyệt
- Bộ lọc theo loại nội dung
- Xem chi tiết
- Duyệt / từ chối / trả lại
- Ghi chú kiểm duyệt

**Nghiệp vụ:**
- Ghi nhận người duyệt.
- Lưu lý do từ chối.

---

### 3.14. Màn hình Báo cáo
**Mục tiêu:** Thống kê và tổng hợp dữ liệu hệ thống.

**Thành phần:**
- Báo cáo nội dung
- Báo cáo công khai
- Báo cáo theo trường
- Báo cáo theo thời gian
- Xuất file nếu cần

**Nghiệp vụ:**
- Có thể lọc theo khoảng thời gian.
- Có thể xem theo từng trường hoặc toàn hệ thống.

---

### 3.15. Màn hình Nhật ký hệ thống
**Mục tiêu:** Theo dõi hoạt động thao tác của người dùng.

**Thành phần:**
- Nhật ký đăng nhập
- Nhật ký tạo/sửa/xóa
- Nhật ký duyệt
- Bộ lọc theo người dùng, thời gian, module

**Nghiệp vụ:**
- Phục vụ kiểm tra và truy vết khi có sự cố.

---

## 4. Luồng công khai chi tiết

### 4.1. Luồng xem bài viết
Trang chủ -> Danh mục tin -> Danh sách bài viết -> Chi tiết bài viết -> Bài liên quan

### 4.2. Luồng xem văn bản công khai
Trang chủ -> Văn bản công khai -> Danh sách văn bản -> Chi tiết văn bản -> Tải file đính kèm

### 4.3. Luồng xem tuyển sinh
Trang chủ -> Tuyển sinh -> Danh sách nội dung -> Chi tiết -> Tải biểu mẫu

### 4.4. Luồng tìm kiếm
Tìm kiếm -> Nhập từ khóa -> Xem kết quả -> Mở chi tiết nội dung

### 4.5. Luồng liên hệ
Trang liên hệ -> Điền form -> Gửi -> Xác nhận thành công

---

## 5. Luồng quản trị chi tiết

### 5.1. Luồng tạo bài viết
Đăng nhập -> Dashboard -> Bài viết -> Thêm mới -> Nhập dữ liệu -> Lưu nháp -> Gửi duyệt -> Duyệt -> Xuất bản

### 5.2. Luồng tạo văn bản công khai
Đăng nhập -> Dashboard -> Văn bản công khai -> Thêm mới -> Nhập metadata -> Upload file -> Gửi duyệt -> Duyệt -> Công khai

### 5.3. Luồng quản lý banner
Đăng nhập -> Banner -> Thêm mới -> Upload ảnh -> Chọn vị trí -> Lưu -> Kích hoạt

### 5.4. Luồng quản lý user
Đăng nhập -> Người dùng -> Thêm mới -> Chọn trường -> Gán vai trò -> Lưu

### 5.5. Luồng duyệt nội dung
Đăng nhập -> Chờ duyệt -> Xem chi tiết -> Duyệt hoặc từ chối -> Ghi chú -> Cập nhật trạng thái

---

## 6. Trạng thái màn hình theo nghiệp vụ

### 6.1. Bài viết
- Nháp
- Chờ duyệt
- Đã duyệt
- Đã xuất bản
- Từ chối
- Lưu trữ

### 6.2. Văn bản
- Nháp
- Chờ duyệt
- Đã duyệt
- Công khai
- Lưu trữ

### 6.3. Banner
- Chưa kích hoạt
- Đang kích hoạt
- Hết hạn
- Ẩn

### 6.4. User
- Hoạt động
- Bị khóa
- Vô hiệu hóa

---

## 7. Quy ước điều hướng
- Menu công khai nằm ở header.
- Nội dung chính hiển thị ở vùng body.
- Thông tin phụ nằm ở sidebar hoặc footer.
- Khu vực quản trị dùng sidebar menu trái, topbar và vùng nội dung chính.

---

## 8. Kết luận
Tài liệu sitemap này đã được chuẩn hóa theo hướng:
- mô tả rõ từng màn hình,
- có mục tiêu nghiệp vụ,
- có thành phần chính,
- có luồng sử dụng,
- có trạng thái màn hình,
- phù hợp để chuyển sang thiết kế UI/UX và phát triển source code.
