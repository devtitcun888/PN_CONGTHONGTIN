# Hướng Dẫn API Camera Door Và Điểm Danh Ra/Vào Kho

Tài liệu này mô tả các API trong controller:

`PN_HDSWeb_Admin\Controllers\CameraDoorController.cs`

Base path:

```text
/api/camera-door
```

## 1. Xác Thực

Tất cả API trong tài liệu này đều cần `SecurityKey`.

Có 2 cách truyền key:

```http
X-Security-Key: YOUR_SECURITY_KEY
```

Hoặc truyền qua query string:

```http
?key=YOUR_SECURITY_KEY
```

Khuyến nghị dùng header `X-Security-Key`.

Response khi thiếu hoặc sai key:

```json
{
  "success": false,
  "message": "Security key khong hop le hoac bi thieu."
}
```

## 2. API Lấy Danh Sách Nhóm Camera

Lấy danh sách nhóm camera từ Hikvision theo `devName`.

```http
GET /api/camera-door/groups
```

Ví dụ:

```bash
curl -X GET "https://your-domain/api/camera-door/groups" \
  -H "X-Security-Key: YOUR_SECURITY_KEY"
```

Response mẫu:

```json
{
  "success": true,
  "message": "Lay danh sach nhom camera thanh cong.",
  "totalGroups": 8,
  "totalCameras": 13,
  "onlineCount": 13,
  "offlineCount": 0,
  "groups": [
    {
      "groupKey": "khochanthuocthuong",
      "groupName": "KhoChanThuocThuong",
      "cameraCount": 1,
      "onlineCount": 1,
      "offlineCount": 0,
      "cameras": [
        {
          "devIndex": "abc123",
          "ehomeId": "KhoChanThuocThuong",
          "devName": "KhoChanThuocThuong",
          "displayGroupName": "KhoChanThuocThuong",
          "devSerial": "GK0230861",
          "devMode": "DS-K1T341CMF",
          "devType": "",
          "devStatus": "online",
          "isOnline": true
        }
      ]
    }
  ]
}
```

Ghi chú:

- `groupKey`: key chuẩn hóa để nhóm dữ liệu.
- `groupName`: tên nhóm camera hiển thị.
- `cameras[].ehomeId`: mã camera ghi nhận trong dữ liệu điểm danh nếu thiết bị trả về EhomeID.
- `cameras[].devName`: nhóm camera/kho đang gán trên Hikvision.

## 3. API Lấy Danh Sách Nhóm Camera Theo Kho

API này dùng để lấy danh sách `Id | Tên Kho` đã gán camera. Cột `id` dùng để đối chiếu với `cameraGroupId` trong API điểm danh ra/vào kho và cũng là giá trị truyền vào filter `nhomCamera`.

```http
GET /api/camera-door/warehouse-camera-groups
```

Query:

| Tham số | Bắt buộc | Mô tả |
| --- | --- | --- |
| `khoId` | Không | Lọc theo một kho cụ thể. Ví dụ: `KhoChanThuocThuong`. |
| `includeInactive` | Không | `true` để lấy cả camera-kho đã inactive. Mặc định `false`. |

Ví dụ:

```bash
curl -X GET "https://your-domain/api/camera-door/warehouse-camera-groups" \
  -H "X-Security-Key: YOUR_SECURITY_KEY"
```

Response mẫu:

```json
{
  "success": true,
  "message": "Lay danh sach nhom camera theo kho thanh cong.",
  "total": 3,
  "items": [
    {
      "id": "KhoChanThuocThuong",
      "tenKho": "Kho chan thuoc thuong"
    },
    {
      "id": "KhoChanThuocDB",
      "tenKho": "Kho chan thuoc dac biet"
    },
    {
      "id": "QuayThuocE1T4",
      "tenKho": "Quay thuoc E1T4"
    }
  ]
}
```

Cách dùng với API điểm danh:

1. Gọi `/warehouse-camera-groups` để lấy `items[].id`.
2. Truyền giá trị đó vào body của `/attendance` tại trường `nhomCamera`.

Ví dụ:

```json
{
  "nhomCamera": "KhoChanThuocThuong"
}
```

## 4. API Lấy Dữ Liệu Điểm Danh Ra/Vào Kho

API POST lấy dữ liệu điểm danh nhân viên/khách, trạng thái ra-vào kho, các lượt ra/vào và thông tin camera.

```http
POST /api/camera-door/attendance
```

API đang gán cứng `namHoc = "2025-2026"`, client không cần truyền `namHoc`.

Body:

| Trường | Kiểu | Bắt buộc | Mô tả |
| --- | --- | --- | --- |
| `date` | string | Không | Ngày cần lấy dữ liệu. Hỗ trợ `yyyy-MM-dd` hoặc `dd/MM/yyyy`. Nếu bỏ trống lấy ngày hiện tại. |
| `ngay` | datetime | Không | Có thể dùng thay `date`. Nếu có cả hai, ưu tiên `ngay`. |
| `maTruongBo` | string | Nên truyền | Mã trường/bệnh viện. Nếu bỏ trống API thử lấy từ session server. |
| `audience` | string | Không | `staff`, `guest`, `all`. Mặc định `staff`. |
| `maPhongBan` | number | Không | Lọc theo khoa/phòng. |
| `trangThai` | number | Không | `1`: có vào kho, `0`: chưa vào kho. |
| `maNhanVien` | string | Không | Lọc theo mã nhân viên. |
| `nhomCamera` | string | Không | Lọc theo nhóm camera/kho. Giá trị lấy từ `warehouse-camera-groups.items[].id`. |
| `includeCameraInfo` | bool | Không | `true` để map thêm thông tin camera. Mặc định `true`. |

Ví dụ lấy toàn bộ nhân viên vào ngày 25/06/2026:

```bash
curl -X POST "https://your-domain/api/camera-door/attendance" \
  -H "Content-Type: application/json" \
  -H "X-Security-Key: YOUR_SECURITY_KEY" \
  -d '{
    "date": "2026-06-25",
    "maTruongBo": "PN",
    "audience": "staff"
  }'
```

Ví dụ lọc theo nhân viên:

```json
{
  "date": "2026-06-25",
  "maTruongBo": "PN",
  "audience": "staff",
  "maNhanVien": "NV001"
}
```

Ví dụ lọc theo nhóm camera/kho:

```json
{
  "date": "2026-06-25",
  "maTruongBo": "PN",
  "audience": "all",
  "nhomCamera": "KhoChanThuocThuong"
}
```

Response mẫu:

```json
{
  "thanhCong": true,
  "thongBao": "Lay du lieu diem danh ra vao kho thanh cong.",
  "ngay": "2026-06-25T00:00:00",
  "maTruongBo": "PN",
  "doiTuong": "staff",
  "tongHop": {
    "tongSo": 2,
    "soNhanVien": 2,
    "soKhach": 0,
    "soCoVaoKho": 1,
    "soChuaVaoKho": 1,
    "soDaVaoKho": 1,
    "soKhongVaoKho": 1,
    "soKhachHetHan": 0,
    "tongSoLanVaoKho": 1,
    "thoiGianVaoDauTien": "08:15:00",
    "thoiGianGhiNhanCuoi": "08:15:00",
    "thoiGianTao": "2026-06-25T09:00:00+07:00"
  },
  "duLieu": [
    {
      "maNhanVien": "NV001",
      "hoTen": "Nguyen Van A",
      "maPhongBan": 12,
      "tenPhongBan": "Khoa Duoc",
      "ngay": "2026-06-25T00:00:00",
      "laKhach": false,
      "thoiGianHetHan": null,
      "daHetHan": false,
      "trangThaiDiemDanh": 1,
      "coVaoKho": true,
      "trangThaiHieuLuc": "VALID",
      "trangThaiKho": "DA_VAO_KHO",
      "trangThaiNghiepVu": "DA_VAO_KHO",
      "moTaTrangThai": "Da vao kho",
      "thoiGianVaoDauTien": "08:15:00",
      "thoiGianGhiNhanCuoi": "08:15:00",
      "soLanVaoKho": 1,
      "danhSachLanVaoKho": [
        {
          "stt": 1,
          "thoiGian": "08:15:00",
          "thoiGianText": "08:15:00",
          "loaiGhiNhan": "VAO_KHO",
          "tenLoaiGhiNhan": "Vao kho",
          "maCameraDiemDanh": "KhoChanThuocThuong",
          "tenCamera": "KhoChanThuocThuong",
          "maNhomCamera": "KhoChanThuocThuong",
          "tenNhomCamera": "KhoChanThuocThuong",
          "devIndex": "abc123",
          "serial": "GK0230861",
          "daNhanDienCamera": true,
          "cameraOnline": true
        }
      ]
    }
  ]
}
```

Ý nghĩa trạng thái:

| Trường | Giá trị | Ý nghĩa |
| --- | --- | --- |
| `trangThaiDiemDanh` | `1` | Có dữ liệu vào kho trong ngày. |
| `trangThaiDiemDanh` | `0` | Chưa có dữ liệu vào kho trong ngày. |
| `trangThaiKho` | `CHUA_VAO_KHO` | Chưa vào kho. |
| `trangThaiKho` | `DA_VAO_KHO` | Đã có log vào kho. |
| `trangThaiNghiepVu` | `CHUA_VAO_KHO` | Chưa vào kho. |
| `trangThaiNghiepVu` | `DA_VAO_KHO` | Đã có log vào kho. |
| `trangThaiNghiepVu` | `EXPIRED_GUEST` | Khách đã hết hạn. |

Quy tắc dữ liệu vào kho:

- API sắp xếp các lượt điểm danh theo thời gian tăng dần.
- Mỗi log camera được xem là một `VAO_KHO`.
- API không suy luận vào/ra theo lượt lẻ/chẵn.
- `trangThaiKho = DA_VAO_KHO` khi nhân viên có ít nhất một log phù hợp bộ lọc.
- `trangThaiKho = CHUA_VAO_KHO` khi không có log vào kho.

Ghi chú lọc `nhomCamera`:

- Trường `nhomCamera` nên truyền bằng `id` lấy từ API `/warehouse-camera-groups`.
- Ví dụ `nhomCamera = "KhoChanThuocThuong"`.
- API sẽ lọc các lượt có `danhSachLanVaoKho[].maNhomCamera = "KhoChanThuocThuong"`.

## 5. API Mở Cửa Nhanh

API mở cửa nhanh. Nội bộ sẽ chuyển thành lệnh `cmd = "open"` và gọi API control.

```http
PUT  /api/camera-door/open
POST /api/camera-door/open
```

Body:

| Trường | Kiểu | Bắt buộc | Mô tả |
| --- | --- | --- | --- |
| `devIndex` | string | Có | Mã thiết bị camera/door trên Hikvision. |
| `doorNo` | number | Không | Số cửa. Nếu bỏ trống hoặc <= 0, mặc định `1`. |
| `requestedBy` | string | Không | Người yêu cầu. |
| `userId` | string | Không | ID user nếu có. |
| `note` | string | Không | Ghi chú. |

Ví dụ:

```bash
curl -X PUT "https://your-domain/api/camera-door/open" \
  -H "Content-Type: application/json" \
  -d '{
    "devIndex": "abc123",
    "doorNo": 1,
    "requestedBy": "External API",
    "userId": "00000000-0000-0000-0000-000000000000",
    "note": "Mo cua tu he thong tich hop"
  }'
```

Response thành công:

```json
{
  "success": true,
  "message": "Dieu khien cua thanh cong.",
  "result": {
    "success": true,
    "statusCode": 200,
    "responseBody": "..."
  }
}
```

## 6. API Điều Khiển Cửa

API điều khiển cửa theo command.

```http
PUT  /api/camera-door/control
POST /api/camera-door/control
```

Body:

| Trường | Kiểu | Bắt buộc | Mô tả |
| --- | --- | --- | --- |
| `devIndex` | string | Có | Mã thiết bị camera/door trên Hikvision. |
| `cmd` | string | Có | Lệnh điều khiển. |
| `doorNo` | number | Không | Số cửa. Nếu bỏ trống hoặc <= 0, mặc định `1`. |
| `requestedBy` | string | Không | Người yêu cầu. |
| `userId` | string | Không | ID user nếu có. |
| `note` | string | Không | Ghi chú. |

Các giá trị `cmd` đang hỗ trợ:

| `cmd` | Ý nghĩa |
| --- | --- |
| `open` | Mở cửa |
| `close` | Đóng cửa |
| `alwaysOpen` | Luôn mở cửa |
| `alwaysClose` | Luôn đóng cửa |

Ví dụ mở cửa:

```json
{
  "devIndex": "abc123",
  "cmd": "open",
  "doorNo": 1,
  "requestedBy": "External API",
  "note": "Mo cua kho"
}
```

Ví dụ luôn đóng cửa:

```json
{
  "devIndex": "abc123",
  "cmd": "alwaysClose",
  "doorNo": 1,
  "requestedBy": "External API",
  "note": "Khoa cua sau gio lam viec"
}
```

Response lỗi từ Hikvision:

```json
{
  "success": false,
  "message": "Hikvision API tra ve loi.",
  "result": {
    "success": false,
    "statusCode": 400,
    "responseBody": "..."
  }
}
```

## 7. Header Ghi Log Cho API Door

Khi gọi API `/open` hoặc `/control`, hệ thống có ghi audit log. Có thể truyền thêm header để log rõ người thao tác:

```http
X-User-Name: Nguyen Van A
X-Requested-By: External System
X-User-Id: 00000000-0000-0000-0000-000000000000
```

Nếu body có `requestedBy` thì ưu tiên giá trị này.

## 8. Luồng Tích Hợp Gợi Ý

1. Gọi `GET /api/camera-door/warehouse-camera-groups` để lấy danh sách kho/nhóm camera.
2. Người dùng chọn kho, lấy `items[].id`.
3. Gọi `POST /api/camera-door/attendance` với `nhomCamera = id`.
4. Nếu cần mở cửa cho thiết bị cụ thể, lấy `devIndex` từ API `/groups` hoặc từ dữ liệu camera đã có.
5. Gọi `POST /api/camera-door/open` hoặc `POST /api/camera-door/control`. Vẫn có thể dùng `PUT` nếu IIS/server đã cho phép HTTP verb `PUT`.

## 9. Mã Lỗi Chung

| HTTP status | Trường hợp |
| --- | --- |
| `200` | Thành công. |
| `400` | Body/query không hợp lệ. |
| `401` | Thiếu hoặc sai security key. |
| `500` | Lỗi server hoặc lỗi gọi dịch vụ bên dưới. |
 
## 10. Luu Y Loi 405 Khi Goi PUT

Neu Postman nhan trang HTML:

```text
405 - HTTP verb used to access this page is not allowed.
```

Nguyen nhan thuong gap la IIS/WebDAV chan HTTP verb `PUT` truoc khi request vao ASP.NET Core. Project da bo sung `web.config` de remove `WebDAVModule`/`WebDAV` handler khi publish.

Cach goi khuyen nghi cho he thong tich hop:

```http
POST /api/camera-door/control
```

Body giu nguyen:

```json
{
  "devIndex": "abc6F566C8E-1F3F-403E-956C-635F6A89C58A123",
  "cmd": "close",
  "doorNo": 1,
  "requestedBy": "Thai",
  "note": "dong cua kho"
}
```
