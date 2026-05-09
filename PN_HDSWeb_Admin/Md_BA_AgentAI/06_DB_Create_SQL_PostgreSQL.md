# FILE 6 - SQL TẠO DATABASE POSTGRESQL
## Cổng thông tin điện tử trường học đa cơ sở

Tài liệu này cung cấp lệnh khởi tạo các bảng lõi còn thiếu cho portal, phù hợp với PostgreSQL. Các bảng `l_truong` và `l_ssosession` được giả định đã tồn tại.

---

## 1. Nhóm bảng cần tạo
- `l_user_account`
- `l_user_session` (nếu cần lưu session server-side)
- `roles`
- `permissions`
- `user_roles`
- `role_permissions`
- `post_categories`
- `posts`
- `post_tags`
- `post_tag_map`
- `post_media`
- `document_types`
- `documents`
- `document_versions`
- `staff_profiles`
- `tuition_fees`
- `announcements`
- `events`
- `site_pages`
- `menus`
- `banners`
- `site_settings`
- `audit_logs`
- `contact_requests`
- `counter_traffic`

---

## 2. SQL khởi tạo

```sql
CREATE TABLE IF NOT EXISTS l_user_account (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    username        VARCHAR(100) NOT NULL,
    password_hash   TEXT NOT NULL,
    full_name       VARCHAR(255) NOT NULL,
    display_name    VARCHAR(255),
    email           VARCHAR(255),
    phone           VARCHAR(30),
    role_code       VARCHAR(50) NOT NULL DEFAULT 'Administrator',
    auth_type       VARCHAR(20) NOT NULL DEFAULT 'Local',
    sso_username    VARCHAR(100),
    sso_user_id     VARCHAR(100),
    device_name     VARCHAR(255),
    last_login_at   TIMESTAMPTZ,
    last_login_ip   VARCHAR(100),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_locked       BOOLEAN NOT NULL DEFAULT FALSE,
    lock_reason     TEXT,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_l_user_account_truong_username UNIQUE (ma_truong_bo, username)
);

CREATE INDEX IF NOT EXISTS ix_l_user_account_truong_role ON l_user_account(ma_truong_bo, role_code);
CREATE INDEX IF NOT EXISTS ix_l_user_account_truong_auth_type ON l_user_account(ma_truong_bo, auth_type);
CREATE INDEX IF NOT EXISTS ix_l_user_account_truong_active ON l_user_account(ma_truong_bo, is_active);

CREATE TABLE IF NOT EXISTS l_user_session (
    id              BIGSERIAL PRIMARY KEY,
    session_id      VARCHAR(100) NOT NULL,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    username        VARCHAR(100) NOT NULL,
    user_agent      TEXT,
    tab_id          VARCHAR(100),
    expiry_time     TIMESTAMPTZ NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_l_user_session_session_id UNIQUE (session_id)
);

CREATE INDEX IF NOT EXISTS ix_l_user_session_truong_username ON l_user_session(ma_truong_bo, username);
CREATE INDEX IF NOT EXISTS ix_l_user_session_truong_expiry ON l_user_session(ma_truong_bo, expiry_time);

CREATE TABLE IF NOT EXISTS roles (
    id              BIGSERIAL PRIMARY KEY,
    role_code       VARCHAR(50) NOT NULL,
    role_name       VARCHAR(255) NOT NULL,
    description     TEXT,
    scope_type      VARCHAR(20),
    is_system       BOOLEAN NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_roles_role_code UNIQUE (role_code)
);

CREATE TABLE IF NOT EXISTS permissions (
    id              BIGSERIAL PRIMARY KEY,
    permission_code VARCHAR(100) NOT NULL,
    permission_name VARCHAR(255) NOT NULL,
    module_name     VARCHAR(100),
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_permissions_code UNIQUE (permission_code)
);

CREATE TABLE IF NOT EXISTS user_roles (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL REFERENCES l_user_account(id) ON DELETE CASCADE,
    role_id         BIGINT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      VARCHAR(100),
    CONSTRAINT uq_user_roles UNIQUE (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS role_permissions (
    id              BIGSERIAL PRIMARY KEY,
    role_id         BIGINT NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id   BIGINT NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      VARCHAR(100),
    CONSTRAINT uq_role_permissions UNIQUE (role_id, permission_id)
);

CREATE TABLE IF NOT EXISTS post_categories (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    category_code   VARCHAR(50),
    category_name   VARCHAR(255) NOT NULL,
    slug            VARCHAR(255) NOT NULL,
    parent_id       BIGINT REFERENCES post_categories(id) ON DELETE SET NULL,
    description     TEXT,
    sort_order      INT NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_post_categories_truong_slug UNIQUE (ma_truong_bo, slug)
);

CREATE INDEX IF NOT EXISTS ix_post_categories_truong_parent ON post_categories(ma_truong_bo, parent_id);
CREATE INDEX IF NOT EXISTS ix_post_categories_truong_active ON post_categories(ma_truong_bo, is_active);

CREATE TABLE IF NOT EXISTS posts (
    id                  BIGSERIAL PRIMARY KEY,
    ma_truong_bo        VARCHAR(50) NOT NULL,
    category_id         BIGINT NOT NULL REFERENCES post_categories(id) ON DELETE RESTRICT,
    title               VARCHAR(500) NOT NULL,
    slug                VARCHAR(255) NOT NULL,
    summary             TEXT,
    content             TEXT NOT NULL,
    cover_image_url     TEXT,
    post_type           VARCHAR(50),
    status              VARCHAR(50) NOT NULL,
    is_featured         BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order          INT NOT NULL DEFAULT 0,
    publish_at          TIMESTAMPTZ,
    expire_at           TIMESTAMPTZ,
    view_count          BIGINT NOT NULL DEFAULT 0,
    created_by          VARCHAR(100),
    updated_by          VARCHAR(100),
    approved_by         VARCHAR(100),
    approved_at         TIMESTAMPTZ,
    rejected_by         VARCHAR(100),
    rejected_at         TIMESTAMPTZ,
    reject_reason       TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_posts_truong_slug UNIQUE (ma_truong_bo, slug)
);

CREATE INDEX IF NOT EXISTS ix_posts_truong_status_publish ON posts(ma_truong_bo, status, publish_at);
CREATE INDEX IF NOT EXISTS ix_posts_truong_category ON posts(ma_truong_bo, category_id);
CREATE INDEX IF NOT EXISTS ix_posts_truong_featured ON posts(ma_truong_bo, is_featured);

CREATE TABLE IF NOT EXISTS post_tags (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    tag_name        VARCHAR(255) NOT NULL,
    slug            VARCHAR(255) NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_post_tags_truong_slug UNIQUE (ma_truong_bo, slug)
);

CREATE TABLE IF NOT EXISTS post_tag_map (
    id              BIGSERIAL PRIMARY KEY,
    post_id         BIGINT NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    tag_id          BIGINT NOT NULL REFERENCES post_tags(id) ON DELETE CASCADE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_post_tag_map UNIQUE (post_id, tag_id)
);

CREATE TABLE IF NOT EXISTS post_media (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    post_id         BIGINT NOT NULL REFERENCES posts(id) ON DELETE CASCADE,
    media_type      VARCHAR(30) NOT NULL,
    file_name       VARCHAR(255) NOT NULL,
    file_url        TEXT NOT NULL,
    thumbnail_url   TEXT,
    file_size       BIGINT,
    mime_type       VARCHAR(100),
    sort_order      INT NOT NULL DEFAULT 0,
    caption         TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      VARCHAR(100),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS ix_post_media_post_id ON post_media(post_id);
CREATE INDEX IF NOT EXISTS ix_post_media_truong_type ON post_media(ma_truong_bo, media_type);

CREATE TABLE IF NOT EXISTS document_types (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    type_code       VARCHAR(50),
    type_name       VARCHAR(255) NOT NULL,
    slug            VARCHAR(255) NOT NULL,
    description     TEXT,
    sort_order      INT NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_document_types_truong_slug UNIQUE (ma_truong_bo, slug)
);

CREATE TABLE IF NOT EXISTS documents (
    id                  BIGSERIAL PRIMARY KEY,
    ma_truong_bo        VARCHAR(50) NOT NULL,
    document_type_id    BIGINT REFERENCES document_types(id) ON DELETE SET NULL,
    doc_type            VARCHAR(100),
    doc_number          VARCHAR(100) NOT NULL,
    doc_title           VARCHAR(500) NOT NULL,
    doc_code            VARCHAR(100),
    issuer              VARCHAR(255) NOT NULL,
    issued_date         DATE NOT NULL,
    effective_date      DATE,
    expiry_date         DATE,
    summary             TEXT,
    file_url            TEXT NOT NULL,
    file_name           VARCHAR(255),
    file_size           BIGINT,
    mime_type           VARCHAR(100),
    status              VARCHAR(50) NOT NULL,
    version_no          INT NOT NULL DEFAULT 1,
    created_by          VARCHAR(100),
    updated_by          VARCHAR(100),
    approved_by         VARCHAR(100),
    approved_at         TIMESTAMPTZ,
    rejected_by         VARCHAR(100),
    rejected_at         TIMESTAMPTZ,
    reject_reason       TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_documents_docnum_date UNIQUE (ma_truong_bo, doc_number, issued_date)
);

CREATE INDEX IF NOT EXISTS ix_documents_truong_status_date ON documents(ma_truong_bo, status, issued_date);
CREATE INDEX IF NOT EXISTS ix_documents_truong_type ON documents(ma_truong_bo, document_type_id);

CREATE TABLE IF NOT EXISTS document_versions (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    document_id     BIGINT NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    version_no      INT NOT NULL,
    file_url        TEXT NOT NULL,
    file_name       VARCHAR(255),
    change_summary  TEXT,
    created_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_document_versions UNIQUE (document_id, version_no)
);

CREATE TABLE IF NOT EXISTS staff_profiles (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    full_name       VARCHAR(255) NOT NULL,
    position_name   VARCHAR(255) NOT NULL,
    qualification   VARCHAR(255),
    certificate_info TEXT,
    bio             TEXT,
    avatar_url      TEXT,
    email           VARCHAR(255),
    phone           VARCHAR(30),
    sort_order      INT NOT NULL DEFAULT 0,
    is_public       BOOLEAN NOT NULL DEFAULT TRUE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS tuition_fees (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    school_year     VARCHAR(20) NOT NULL,
    grade_level     VARCHAR(50),
    fee_type        VARCHAR(100) NOT NULL,
    amount          NUMERIC(18,2) NOT NULL,
    currency        VARCHAR(10) NOT NULL DEFAULT 'VND',
    note            TEXT,
    file_url        TEXT,
    status          VARCHAR(50) NOT NULL,
    version_no      INT NOT NULL DEFAULT 1,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    approved_by     VARCHAR(100),
    approved_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS announcements (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    title           VARCHAR(500) NOT NULL,
    content         TEXT NOT NULL,
    priority_level  INT NOT NULL DEFAULT 0,
    status          VARCHAR(50) NOT NULL,
    publish_at      TIMESTAMPTZ,
    expire_at       TIMESTAMPTZ,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    approved_by     VARCHAR(100),
    approved_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS events (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    event_name      VARCHAR(500) NOT NULL,
    description     TEXT,
    start_time      TIMESTAMPTZ,
    end_time        TIMESTAMPTZ,
    location        VARCHAR(255),
    cover_image_url TEXT,
    status          VARCHAR(50) NOT NULL,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    approved_by     VARCHAR(100),
    approved_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS site_pages (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    page_code       VARCHAR(50),
    title           VARCHAR(500) NOT NULL,
    slug            VARCHAR(255) NOT NULL,
    content         TEXT NOT NULL,
    status          VARCHAR(50) NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    meta_title      VARCHAR(255),
    meta_description TEXT,
    created_by      VARCHAR(100),
    updated_by      VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_site_pages_truong_slug UNIQUE (ma_truong_bo, slug)
);

CREATE TABLE IF NOT EXISTS menus (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    menu_name       VARCHAR(255) NOT NULL,
    parent_id       BIGINT REFERENCES menus(id) ON DELETE SET NULL,
    url             TEXT,
    target          VARCHAR(20),
    sort_order      INT NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    icon_class      VARCHAR(255),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS banners (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    title           VARCHAR(500),
    image_url       TEXT NOT NULL,
    link_url        TEXT,
    position        VARCHAR(100) NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    start_date      DATE,
    end_date        DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS site_settings (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    setting_key     VARCHAR(255) NOT NULL,
    setting_value   TEXT NOT NULL,
    setting_group   VARCHAR(100),
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by      VARCHAR(100),
    CONSTRAINT uq_site_settings_truong_key UNIQUE (ma_truong_bo, setting_key)
);

CREATE TABLE IF NOT EXISTS audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50),
    user_id         BIGINT,
    action_type     VARCHAR(100) NOT NULL,
    entity_name     VARCHAR(100) NOT NULL,
    entity_id       VARCHAR(100),
    old_value       JSONB,
    new_value       JSONB,
    ip_address      VARCHAR(100),
    user_agent      TEXT,
    trace_id        VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS contact_requests (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    full_name       VARCHAR(255) NOT NULL,
    email           VARCHAR(255),
    phone           VARCHAR(30),
    subject         VARCHAR(500),
    message         TEXT NOT NULL,
    status          VARCHAR(50) NOT NULL DEFAULT 'New',
    is_read         BOOLEAN NOT NULL DEFAULT FALSE,
    read_by         VARCHAR(100),
    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS counter_traffic (
    id              BIGSERIAL PRIMARY KEY,
    ma_truong_bo    VARCHAR(50) NOT NULL,
    visit_date      DATE NOT NULL,
    page_path       TEXT,
    ip_address      VARCHAR(100),
    user_agent      TEXT,
    referrer        TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed tối thiểu cho role Administrator
INSERT INTO roles (role_code, role_name, description, scope_type, is_system, is_active)
SELECT 'Administrator', 'Administrator', 'Quản trị hệ thống', 'system', TRUE, TRUE
WHERE NOT EXISTS (SELECT 1 FROM roles WHERE role_code = 'Administrator');
```

---

## 3. Lưu ý triển khai
- Nếu `l_truong` đã có thì không tạo lại ở file này.
- Nếu `l_ssosession` đã có thì không tạo lại ở file này.
- Nếu hệ thống chưa dùng session server-side, có thể tạm hoãn `l_user_session`.
- Khi chạy thật, nên bọc script trong transaction theo từng nhóm bảng nếu muốn rollback dễ hơn.

---

## 4. Ghi chú cho `LoginID_Index` / `LoginID_School_Dev`
- Dữ liệu admin/global nên query qua `LoginID_Index`.
- Dữ liệu trường nên query qua `LoginID_School_Dev` hoặc login ID tương ứng.

---

## 5. Kết luận
Bộ SQL này đã bổ sung bảng tài khoản và các bảng lõi còn thiếu cho portal, dùng được cho PostgreSQL và phù hợp với mô hình 2 luồng login.
