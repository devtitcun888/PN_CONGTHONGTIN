# 🚗⚡ Hệ thống Thuê Xe Điện — Kế hoạch triển khai chi tiết

## Bối cảnh & Mục tiêu

Chuyển đổi project `PN_HDSWeb` từ hệ thống **cổng tin tức** sang **hệ thống thuê xe điện**, kế thừa toàn bộ kiến trúc Blazor Server + hDataLib đã có. Hệ thống phục vụ 2 luồng chính:

- **Luồng Khách hàng (Public):** Tìm xe → Đặt xe → Thanh toán → Theo dõi đơn
- **Luồng Admin:** Quản lý xe → Quản lý đơn thuê → Quản lý khách hàng → Thống kê

---

## Phân tích kế thừa từ Source cũ

### ✅ Giữ lại (không xóa)
| Module | Lý do |
|---|---|
| `Authentication/` | Toàn bộ xác thực user/admin |
| `Hubs/DataHub.cs` | SignalR real-time |
| `Services/Auth/` | Login/Logout/Session |
| `PN_HDSWeb_Library/` | PN_LoginService, BrowserStorage, Sessions |
| `Program.cs` | Giữ cấu hình gốc, chỉ thêm service mới |
| `Shared/` layout | Header/Footer layout cơ bản |
| `wwwroot/` assets | CSS, JS nền |

### 🗑️ Dọn sạch (xóa hoặc đổi dùng)
| Module | Hành động |
|---|---|
| `Services/Content/` (Post, Document, Banner, Tag...) | Xóa — không còn dùng |
| `Services/Schools/` | Xóa |
| `Services/Public/PublicPostService.cs` | Xóa |
| `Services/Public/PublicDocumentService.cs` | Xóa |
| `Services/Public/PublicHomepageService.cs` | Xóa → thay bằng xe điện |
| `Pages/Public/Posts/` | Xóa |
| `Pages/Public/Documents/` | Xóa |
| `Pages/Public/Organization/` | Xóa |
| `Pages/Admin/Content/` | Xóa |
| `Pages/Admin/Documents/` | Xóa |
| `Pages/Admin/Dashboard/` | Refactor → Dashboard xe điện |
| Controllers cũ (CKEditor) | Xóa |

---

## Open Questions

> [!IMPORTANT]
> **Cần xác nhận trước khi code:**
> 1. **LoginID_DEV** trong `PN_LoginService` → tên account DB cho hệ thống xe điện là gì? (Hiện tại có: `LoginID_CongThongTin`, `LoginID_CongDiem`, `LoginID_School_Dev`)
> 2. **Tên database** (connection string) sẽ dùng cho xe điện?
> 3. **Đơn vị tiền tệ:** VNĐ/giờ, VNĐ/ngày hay cả hai?
> 4. **Thanh toán:** Tích hợp cổng thanh toán (VNPay, MoMo) hay chỉ ghi nhận thủ công?
> 5. **Upload ảnh xe:** Dùng file service hiện tại (`Url_FileService`) hay local?

> [!WARNING]
> Bạn đề cập `private static readonly string LoginID_Index = PN_LoginService.LoginID_DEV` — hiện trong `PN_LoginService.cs` **không có** `LoginID_DEV`. Cần bổ sung vào `PN_LoginService.cs` hoặc dùng tên hiện có.

---

## Đề xuất thay đổi — Chi tiết theo Step

---

## STEP 1 — Database Schema (SQL gửi cho bạn import)

### Bảng cần tạo:

```sql
-- 1. Danh mục xe điện
CREATE TABLE ev_vehicles (
    id SERIAL PRIMARY KEY,
    ma_don_vi VARCHAR(50) NOT NULL,          -- mã đơn vị/garage
    ten_xe VARCHAR(200) NOT NULL,
    loai_xe VARCHAR(50),                      -- 'car', 'scooter', 'bike', 'van'
    bien_so VARCHAR(20),
    nam_san_xuat INT,
    mau_xe VARCHAR(50),
    mo_ta TEXT,
    gia_thue_gio DECIMAL(15,0),
    gia_thue_ngay DECIMAL(15,0),
    dat_coc DECIMAL(15,0) DEFAULT 0,
    tinh_trang VARCHAR(20) DEFAULT 'available', -- 'available','rented','maintenance'
    pin_phan_tram INT DEFAULT 100,
    km_hang_lan_sac INT DEFAULT 0,
    hinh_anh_json TEXT,                        -- JSON array of image URLs
    vi_tri_lat DECIMAL(10,7),
    vi_tri_lng DECIMAL(10,7),
    dia_chi TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- 2. Đơn thuê xe
CREATE TABLE ev_rentals (
    id SERIAL PRIMARY KEY,
    ma_don VARCHAR(30) UNIQUE NOT NULL,       -- VD: EV20260622001
    vehicle_id INT REFERENCES ev_vehicles(id),
    khach_ten VARCHAR(200) NOT NULL,
    khach_sdt VARCHAR(20) NOT NULL,
    khach_email VARCHAR(100),
    khach_cmnd VARCHAR(20),
    bat_dau_thue TIMESTAMP NOT NULL,
    ket_thuc_thue TIMESTAMP NOT NULL,
    so_gio INT,
    so_ngay INT,
    tong_tien DECIMAL(15,0),
    tien_dat_coc DECIMAL(15,0) DEFAULT 0,
    trang_thai VARCHAR(30) DEFAULT 'pending',  -- 'pending','confirmed','active','completed','cancelled'
    ghi_chu TEXT,
    confirmed_at TIMESTAMP,
    returned_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- 3. Thanh toán
CREATE TABLE ev_payments (
    id SERIAL PRIMARY KEY,
    rental_id INT REFERENCES ev_rentals(id),
    so_tien DECIMAL(15,0) NOT NULL,
    phuong_thuc VARCHAR(50),                  -- 'cash','transfer','vnpay','momo'
    trang_thai VARCHAR(20) DEFAULT 'pending', -- 'pending','paid','refunded'
    ma_giao_dich VARCHAR(100),
    ghi_chu TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 4. Lịch sử xe (trạng thái pin, km)
CREATE TABLE ev_vehicle_logs (
    id SERIAL PRIMARY KEY,
    vehicle_id INT REFERENCES ev_vehicles(id),
    rental_id INT REFERENCES ev_rentals(id),
    su_kien VARCHAR(50),                      -- 'rented','returned','charged','maintained'
    pin_truoc INT,
    pin_sau INT,
    km_truoc INT,
    km_sau INT,
    ghi_chu TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 5. Đánh giá
CREATE TABLE ev_reviews (
    id SERIAL PRIMARY KEY,
    rental_id INT REFERENCES ev_rentals(id),
    vehicle_id INT REFERENCES ev_vehicles(id),
    so_sao INT CHECK (so_sao BETWEEN 1 AND 5),
    noi_dung TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 6. Site settings (kế thừa bảng cũ nếu có, hoặc tạo mới)
-- Dùng bảng site_settings hiện tại với key riêng cho xe điện
```

> [!NOTE]
> AI sẽ gửi lệnh SQL đầy đủ. Bạn tự import vào database. AI không cần kết nối trực tiếp.

---

## STEP 2 — Cập nhật PN_LoginService

#### [MODIFY] [PN_LoginService.cs](file:///d:/WORK/TITKUL/2_CONGTHONGITN/PN_HDSWeb/PN_HDSWeb_Library/PN_LoginService.cs)

Thêm `LoginID_DEV` (hoặc tên account xe điện):
```csharp
public static readonly string LoginID_XeDien = hdataLib.hgetLoginID("ten_account_xe_dien");
// Hoặc nếu dùng tên DEV:
public static readonly string LoginID_DEV = hdataLib.hgetLoginID("ten_account_dev");
```

---

## STEP 3 — Dọn dẹp Services cũ & tạo Services mới

### Services mới — Luồng Khách

#### [NEW] `Services/Public/IPublicVehicleService.cs`
- `GetAvailableVehiclesAsync(filter)` — Tìm xe theo loại, ngày, giá
- `GetVehicleDetailAsync(id)` — Chi tiết xe
- `CheckAvailabilityAsync(vehicleId, start, end)` — Kiểm tra xe còn trống

#### [NEW] `Services/Public/IPublicRentalService.cs`
- `CreateRentalAsync(dto)` — Tạo đơn thuê mới
- `GetRentalByMaDonAsync(maDon)` — Tra cứu đơn theo mã
- `CancelRentalAsync(maDon, reason)` — Hủy đơn

### Services mới — Luồng Admin

#### [NEW] `Services/Admin/IAdminVehicleService.cs`
- `GetAllAsync(filter)` — Danh sách xe (có phân trang)
- `CreateAsync(dto)` — Thêm xe mới
- `UpdateAsync(id, dto)` — Cập nhật xe
- `UpdateStatusAsync(id, status)` — Đổi trạng thái
- `DeleteAsync(id)` — Xóa mềm

#### [NEW] `Services/Admin/IAdminRentalService.cs`
- `GetAllAsync(filter)` — Danh sách đơn thuê
- `ConfirmAsync(id)` — Xác nhận đơn
- `CompleteAsync(id)` — Hoàn thành trả xe
- `CancelAsync(id, reason)` — Hủy đơn

#### [MODIFY] `Services/Admin/AdminDashboardService.cs`
- Refactor: thống kê xe điện (tổng xe, đơn hôm nay, doanh thu tháng...)

---

## STEP 4 — Pages

### Luồng Khách (Public)

#### [NEW] `Pages/Public/Home/Index.razor`
- Trang chủ: banner, tìm kiếm xe, danh sách xe nổi bật

#### [NEW] `Pages/Public/Vehicles/Index.razor`
- Tìm xe: filter loại xe, ngày thuê, giá → danh sách kết quả

#### [NEW] `Pages/Public/Vehicles/Detail.razor`
- Chi tiết xe: ảnh, thông số, giá → form đặt xe

#### [NEW] `Pages/Public/Booking/Checkout.razor`
- Nhập thông tin khách, xác nhận đơn

#### [NEW] `Pages/Public/Booking/Confirmation.razor`
- Xác nhận thành công, hiển thị mã đơn

#### [NEW] `Pages/Public/Booking/TrackOrder.razor`
- Tra cứu đơn thuê theo mã

### Luồng Admin

#### [MODIFY] `Pages/Admin/Dashboard/Index.razor`
- Dashboard: thống kê xe, đơn hôm nay, doanh thu

#### [NEW] `Pages/Admin/Vehicles/Index.razor`
- Danh sách xe, thêm/sửa/xóa

#### [NEW] `Pages/Admin/Rentals/Index.razor`
- Danh sách đơn thuê, xác nhận/hoàn thành/hủy

#### [NEW] `Pages/Admin/Rentals/Detail.razor`
- Chi tiết đơn thuê

---

## STEP 5 — Cập nhật Program.cs

Thêm DI registration cho các service mới, xóa các service cũ không dùng.

---

## STEP 6 — UI/UX

- Trang chủ xe điện: dark theme + gradient xanh/tím (hiện đại, premium)
- Card xe: ảnh lớn, pin %, giá rõ ràng, nút Đặt ngay
- Admin: bảng dữ liệu Radzen/MudBlazor đã có sẵn

---

## Verification Plan

### Automated Tests
```bash
dotnet build d:\WORK\TITKUL\2_CONGTHONGITN\PN_HDSWeb\PN_HDSWeb.sln
```

### Manual Verification
1. Import SQL → kiểm tra tạo bảng thành công
2. Build project → không lỗi compile
3. Chạy `dotnet run` → vào trang chủ hiển thị danh sách xe
4. Test đặt xe → kiểm tra đơn lưu vào DB
5. Vào Admin → xem dashboard, danh sách đơn

---

## Thứ tự triển khai (Steps)

| Bước | Nội dung | Người thực hiện |
|------|----------|----------------|
| **1** | AI gửi SQL → Bạn import vào DB | Bạn |
| **2** | AI cập nhật `PN_LoginService.cs` (thêm LoginID) | AI |
| **3** | AI dọn dẹp services/pages cũ không cần | AI |
| **4** | AI tạo Service layer (Vehicle, Rental) | AI |
| **5** | AI tạo Pages Public (Home, Vehicles, Booking) | AI |
| **6** | AI tạo Pages Admin (Dashboard, Vehicles, Rentals) | AI |
| **7** | AI cập nhật Program.cs | AI |
| **8** | Build & test | Cả hai |
