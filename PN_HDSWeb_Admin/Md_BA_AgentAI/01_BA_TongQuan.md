# FILE 1 - BA TỔNG QUAN
## Cổng thông tin điện tử trường học đa cơ sở

**Phiên bản:** 1.0  
**Ngày:** 28/04/2026  
**Mục tiêu:** Xác định phạm vi, mục tiêu, actor, chức năng và quy tắc nghiệp vụ tổng quát cho cổng thông tin điện tử trường học dùng chung cho nhiều trường, phân tách dữ liệu theo `ma_truong_bo`.

---

## 1. Mục đích
Tài liệu mô tả toàn cảnh nghiệp vụ của hệ thống để làm cơ sở cho phân tích, thiết kế, phát triển và kiểm thử.

## 2. Bối cảnh
Hệ thống là cổng thông tin điện tử của trường học/cơ sở giáo dục, không chỉ đăng tin tức mà còn công khai thông tin bắt buộc, quản lý nội dung, phân quyền và hỗ trợ vận hành đa trường trên một cơ sở dữ liệu dùng chung.

## 3. Mục tiêu
- Công khai thông tin chính thống theo quy định.
- Đăng tải tin tức, sự kiện, thông báo.
- Quản lý văn bản công khai, nhân sự, học phí, báo cáo.
- Hỗ trợ nhiều trường dùng chung hệ thống.
- Phân quyền và kiểm soát theo `ma_truong_bo`.

## 4. Phạm vi
### Trong phạm vi
- Trang chủ
- Giới thiệu
- Tin tức / sự kiện / thông báo
- Văn bản công khai
- Tuyển sinh
- Góc phụ huynh
- Câu lạc bộ / hoạt động ngoại khóa
- Tài nguyên
- Quản trị nội dung, phân quyền, duyệt, báo cáo

### Ngoài phạm vi
- LMS
- Quản lý học sinh chuyên sâu
- Thanh toán học phí
- Đồng bộ hai chiều phức tạp với hệ thống ngoài nếu chưa có yêu cầu

## 5. Actor
- Khách truy cập
- Quản trị viên nội dung
- Người phê duyệt nội dung
- Quản trị trường
- Quản trị hệ thống

**Lưu ý cập nhật theo kiến trúc hiện tại:** Hệ thống quản trị thực tế chỉ còn một role triển khai là `Administrator`. Các vai trò bên dưới là vai trò nghiệp vụ tham chiếu cho luồng xử lý nội dung, không phải role phân quyền trong code.

## 6. Quy tắc nghiệp vụ lõi
- Mọi dữ liệu nghiệp vụ phải gắn `ma_truong_bo`.
- Chỉ nội dung đã duyệt mới được công khai.
- Dữ liệu của trường này không được trộn với trường khác.
- Nội dung sửa đổi sau khi công khai phải lưu lịch sử nếu có yêu cầu.

## 7. Danh mục chức năng chính
- Quản lý trang chủ
- Quản lý giới thiệu
- Quản lý tin tức / sự kiện / thông báo
- Quản lý văn bản công khai
- Quản lý tuyển sinh
- Quản lý góc phụ huynh
- Quản lý câu lạc bộ
- Quản lý tài nguyên
- Quản lý banner / menu / widget
- Quản lý user, role, permission
- Quản lý duyệt nội dung
- Quản lý báo cáo và audit log

## 8. Yêu cầu phi chức năng
- Bảo mật theo vai trò và trường
- Hiệu năng tốt trên mobile/desktop
- Dễ mở rộng
- Có backup/restore
- Có log thao tác

## 9. Đầu ra mong muốn
Tài liệu này là nền để triển khai các file tiếp theo: ERD, sitemap, API spec và backlog.
