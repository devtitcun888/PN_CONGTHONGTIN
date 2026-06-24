-- =================================================================
-- HỆ THỐNG THUÊ XE ĐIỆN — DATABASE SCHEMA
-- Database: mới hoàn toàn (hd_xedien)
-- Chạy lệnh này để import toàn bộ schema + data mẫu
-- =================================================================

-- ---------------------------------------------------------------
-- 1. BẢNG XE ĐIỆN (ev_vehicles)
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_vehicles (
    id              SERIAL PRIMARY KEY,
    ma_don_vi       VARCHAR(50)     NOT NULL DEFAULT 'default',
    ten_xe          VARCHAR(200)    NOT NULL,
    loai_xe         VARCHAR(50)     NOT NULL DEFAULT 'scooter',
    -- loai_xe: 'car' | 'scooter' | 'bike' | 'van'
    bien_so         VARCHAR(20),
    nam_san_xuat    INT,
    mau_xe          VARCHAR(50),
    hang_xe         VARCHAR(100),
    mo_ta           TEXT,
    gia_thue_gio    DECIMAL(15,0),
    gia_thue_ngay   DECIMAL(15,0),
    dat_coc         DECIMAL(15,0)   DEFAULT 0,
    tinh_trang      VARCHAR(20)     NOT NULL DEFAULT 'available',
    -- tinh_trang: 'available' | 'rented' | 'maintenance' | 'inactive'
    pin_phan_tram   INT             DEFAULT 100 CHECK (pin_phan_tram BETWEEN 0 AND 100),
    km_tong         INT             DEFAULT 0,
    km_hang_lan_sac INT             DEFAULT 0,
    hinh_anh_json   TEXT,           -- JSON array: ["url1","url2",...]
    hinh_anh_chinh  TEXT,           -- URL ảnh chính (dùng cho card)
    vi_tri_lat      DECIMAL(10,7),
    vi_tri_lng      DECIMAL(10,7),
    dia_chi         TEXT,
    tinh_nang_json  TEXT,           -- JSON: {"bao_hiem":true,"mu_bao_hiem":2,...}
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ev_vehicles_tinh_trang    ON ev_vehicles(tinh_trang);
CREATE INDEX IF NOT EXISTS idx_ev_vehicles_loai_xe       ON ev_vehicles(loai_xe);
CREATE INDEX IF NOT EXISTS idx_ev_vehicles_is_active     ON ev_vehicles(is_active, is_deleted);

-- ---------------------------------------------------------------
-- 2. BẢNG KHÁCH HÀNG (ev_customers)
-- Luồng đăng ký / đăng nhập của khách thuê xe
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_customers (
    id              SERIAL PRIMARY KEY,
    ho_ten          VARCHAR(200)    NOT NULL,
    so_dien_thoai   VARCHAR(20)     NOT NULL UNIQUE,
    email           VARCHAR(150),
    cmnd_cccd       VARCHAR(20),
    ngay_sinh       DATE,
    dia_chi         TEXT,
    password_hash   TEXT,           -- BCrypt hash
    otp_code        VARCHAR(10),
    otp_expires_at  TIMESTAMP,
    is_verified     BOOLEAN         NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    last_login_at   TIMESTAMP,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ev_customers_sdt    ON ev_customers(so_dien_thoai);
CREATE INDEX IF NOT EXISTS idx_ev_customers_email  ON ev_customers(email);

-- ---------------------------------------------------------------
-- 3. BẢNG ĐƠN THUÊ XE (ev_rentals)
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_rentals (
    id              SERIAL PRIMARY KEY,
    ma_don          VARCHAR(30)     NOT NULL UNIQUE,
    -- VD: EV20260622001 (tự sinh)
    vehicle_id      INT             NOT NULL REFERENCES ev_vehicles(id),
    customer_id     INT             REFERENCES ev_customers(id),
    -- customer_id NULL = khách vãng lai (không đăng ký)
    khach_ten       VARCHAR(200)    NOT NULL,
    khach_sdt       VARCHAR(20)     NOT NULL,
    khach_email     VARCHAR(150),
    khach_cmnd      VARCHAR(20),
    bat_dau_thue    TIMESTAMP       NOT NULL,
    ket_thuc_thue   TIMESTAMP       NOT NULL,
    so_gio          INT,
    so_ngay         INT,
    don_gia         DECIMAL(15,0),
    tong_tien       DECIMAL(15,0),
    tien_dat_coc    DECIMAL(15,0)   DEFAULT 0,
    trang_thai      VARCHAR(30)     NOT NULL DEFAULT 'pending',
    -- trang_thai: 'pending' | 'confirmed' | 'active' | 'completed' | 'cancelled'
    ly_do_huy       TEXT,
    ghi_chu         TEXT,
    confirmed_at    TIMESTAMP,
    start_km        INT,
    end_km          INT,
    start_pin       INT,
    end_pin         INT,
    returned_at     TIMESTAMP,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ev_rentals_trang_thai  ON ev_rentals(trang_thai);
CREATE INDEX IF NOT EXISTS idx_ev_rentals_vehicle_id  ON ev_rentals(vehicle_id);
CREATE INDEX IF NOT EXISTS idx_ev_rentals_customer_id ON ev_rentals(customer_id);
CREATE INDEX IF NOT EXISTS idx_ev_rentals_created_at  ON ev_rentals(created_at DESC);

-- ---------------------------------------------------------------
-- 4. BẢNG THANH TOÁN (ev_payments)
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_payments (
    id              SERIAL PRIMARY KEY,
    rental_id       INT             NOT NULL REFERENCES ev_rentals(id),
    loai_thanh_toan VARCHAR(30)     NOT NULL DEFAULT 'full',
    -- loai: 'deposit' | 'full' | 'refund'
    so_tien         DECIMAL(15,0)   NOT NULL,
    phuong_thuc     VARCHAR(50)     NOT NULL DEFAULT 'cash',
    -- phuong_thuc: 'cash' | 'transfer' | 'vnpay' | 'momo' | 'zalopay'
    trang_thai      VARCHAR(20)     NOT NULL DEFAULT 'pending',
    -- trang_thai: 'pending' | 'paid' | 'failed' | 'refunded'
    ma_giao_dich    VARCHAR(100),
    noi_dung        TEXT,
    ghi_chu         TEXT,
    paid_at         TIMESTAMP,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ev_payments_rental_id ON ev_payments(rental_id);

-- ---------------------------------------------------------------
-- 5. BẢNG LỊCH SỬ XE (ev_vehicle_logs)
-- Ghi lại trạng thái pin, km mỗi lần thuê/trả/sạc
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_vehicle_logs (
    id              SERIAL PRIMARY KEY,
    vehicle_id      INT             NOT NULL REFERENCES ev_vehicles(id),
    rental_id       INT             REFERENCES ev_rentals(id),
    su_kien         VARCHAR(50)     NOT NULL,
    -- su_kien: 'rented' | 'returned' | 'charged' | 'maintained' | 'checked'
    pin_truoc       INT,
    pin_sau         INT,
    km_truoc        INT,
    km_sau          INT,
    ghi_chu         TEXT,
    created_by      VARCHAR(100),   -- username admin thực hiện
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ev_vehicle_logs_vehicle_id ON ev_vehicle_logs(vehicle_id);

-- ---------------------------------------------------------------
-- 6. BẢNG ĐÁNH GIÁ (ev_reviews)
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_reviews (
    id              SERIAL PRIMARY KEY,
    rental_id       INT             NOT NULL REFERENCES ev_rentals(id),
    vehicle_id      INT             NOT NULL REFERENCES ev_vehicles(id),
    customer_id     INT             REFERENCES ev_customers(id),
    so_sao          INT             NOT NULL CHECK (so_sao BETWEEN 1 AND 5),
    noi_dung        TEXT,
    is_published    BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- ---------------------------------------------------------------
-- 7. BẢNG CÀI ĐẶT SITE (ev_site_settings)
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ev_site_settings (
    id              SERIAL PRIMARY KEY,
    setting_key     VARCHAR(100)    NOT NULL UNIQUE,
    setting_value   TEXT,
    mo_ta           TEXT,
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW()
);

-- ---------------------------------------------------------------
-- 8. BẢNG TÀI KHOẢN ADMIN (l_user_account)
-- Dùng chung với hệ thống auth Blazor hiện tại
-- ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS l_user_account (
    id              VARCHAR(50)     PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ma_truong_bo    VARCHAR(50)     NOT NULL DEFAULT 'xedien',
    username        VARCHAR(100)    NOT NULL,
    password_hash   TEXT,
    full_name       VARCHAR(200),
    display_name    VARCHAR(200),
    email           VARCHAR(150),
    phone           VARCHAR(20),
    role_code       VARCHAR(50)     DEFAULT 'Administrator',
    auth_type       VARCHAR(20)     DEFAULT 'local',
    sso_username    VARCHAR(100),
    sso_user_id     VARCHAR(100),
    device_name     VARCHAR(200),
    last_login_at   TIMESTAMP,
    last_login_ip   VARCHAR(50),
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    is_locked       BOOLEAN         NOT NULL DEFAULT FALSE,
    lock_reason     TEXT,
    is_deleted      BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMP       NOT NULL DEFAULT NOW(),
    UNIQUE(ma_truong_bo, username)
);

-- ---------------------------------------------------------------
-- TRIGGER: tự động cập nhật updated_at
-- ---------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_ev_vehicles_updated_at
    BEFORE UPDATE ON ev_vehicles
    FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

CREATE TRIGGER trg_ev_rentals_updated_at
    BEFORE UPDATE ON ev_rentals
    FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

CREATE TRIGGER trg_ev_customers_updated_at
    BEFORE UPDATE ON ev_customers
    FOR EACH ROW EXECUTE FUNCTION fn_update_timestamp();

-- ---------------------------------------------------------------
-- DỮ LIỆU MẪU
-- ---------------------------------------------------------------

-- Xe mẫu
INSERT INTO ev_vehicles (ten_xe, loai_xe, bien_so, hang_xe, mau_xe, nam_san_xuat,
    mo_ta, gia_thue_gio, gia_thue_ngay, dat_coc, pin_phan_tram, km_hang_lan_sac,
    hinh_anh_chinh, tinh_nang_json)
VALUES
(
    'VinFast Klara S', 'scooter', '51F-001.01', 'VinFast', 'Trắng', 2024,
    'Xe máy điện VinFast Klara S, thiết kế hiện đại, phù hợp di chuyển trong thành phố.',
    20000, 150000, 200000, 95, 180,
    '/img/vehicles/klara-s.jpg',
    '{"bao_hiem":true,"mu_bao_hiem":2,"sac_du_phong":true,"bluetooth":false}'
),
(
    'VinFast VF 5 Plus', 'car', '51A-001.01', 'VinFast', 'Đen', 2024,
    'Ô tô điện VinFast VF 5 Plus, 5 chỗ, phù hợp gia đình và du lịch.',
    80000, 600000, 2000000, 88, 326,
    '/img/vehicles/vf5.jpg',
    '{"bao_hiem":true,"gps":true,"camera_lui":true,"bluetooth":true,"sac_nhanh":true}'
)
ON CONFLICT DO NOTHING;

-- Cài đặt site mặc định
INSERT INTO ev_site_settings (setting_key, setting_value, mo_ta) VALUES
    ('site_name',         'EV Rental',                          'Tên hệ thống'),
    ('site_slogan',       'Đặt xe điện — Nhanh, sạch, thông minh', 'Slogan'),
    ('site_logo',         '/img/logo.png',                      'Logo URL'),
    ('contact_phone',     '1900 0000',                          'Số điện thoại liên hệ'),
    ('contact_email',     'info@evrental.vn',                   'Email liên hệ'),
    ('contact_address',   'TP. Hồ Chí Minh',                   'Địa chỉ'),
    ('gia_thue_don_vi',   'both',                               'both | gio | ngay'),
    ('dat_coc_bat_buoc',  'true',                               'Yêu cầu đặt cọc'),
    ('gio_mo_cua',        '07:00',                              'Giờ mở cửa'),
    ('gio_dong_cua',      '21:00',                              'Giờ đóng cửa')
ON CONFLICT (setting_key) DO NOTHING;

-- Admin account mặc định (mật khẩu: Admin@123 — đổi ngay sau khi import)
-- password_hash bên dưới là BCrypt của chuỗi "Admin@123"
INSERT INTO l_user_account (ma_truong_bo, username, password_hash, full_name, display_name, role_code)
VALUES (
    'xedien',
    'admin',
    '$2a$11$xQJH9G3eXZy1W8e5cUGPsOO6ZM0nNkS2xCeTqTT3HBuY9A8l3ILFq',
    'Quản trị viên',
    'Admin',
    'Administrator'
)
ON CONFLICT DO NOTHING;

-- =================================================================
-- HOÀN TẤT — Kiểm tra:
-- SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';
-- =================================================================
