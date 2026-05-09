# FILE 9 - PAGE FOLDER STRUCTURE
## Cổng thông tin điện tử trường học đa cơ sở

Mục tiêu của file này là chuẩn hóa cây thư mục `Pages/` để source rõ public/admin, dễ mở rộng, và dễ map với service.

---

## 1. Nguyên tắc
- Tách public và admin riêng.
- Admin chỉ chứa màn hình quản trị.
- Public chỉ chứa màn hình hiển thị công khai.
- Login nên tách riêng.
- Dashboard nên là điểm vào admin.

---

## 2. Cây thư mục đề xuất

```text
Pages/
  Public/
    Home/
    Posts/
    Documents/
    Pages/
    Contact/
    Search/
    Shared/
  Admin/
    Dashboard/
    Auth/
    Accounts/
    Content/
    Documents/
    Settings/
    Reports/
    Audit/
    Shared/
  Auth/
    LoginPages.razor
    LoginPagesAdmin.razor
    Logout.razor
```

---

## 3. Mô tả từng nhóm

### 3.1. `Pages/Public/`
Chứa toàn bộ màn hình public.
- `Home/Index.razor`
- `Posts/PostList.razor`
- `Posts/PostDetail.razor`
- `Documents/DocumentList.razor`
- `Documents/DocumentDetail.razor`
- `Pages/StaticPage.razor`
- `Contact/ContactForm.razor`
- `Search/SearchResult.razor`

### 3.2. `Pages/Admin/`
Chứa toàn bộ màn hình quản trị.
- `Dashboard/Index.razor`
- `Auth/LoginPagesAdmin.razor`
- `Accounts/AccountList.razor`
- `Accounts/AccountEdit.razor`
- `Content/PostList.razor`
- `Content/PostEdit.razor`
- `Content/PostApprove.razor`
- `Documents/DocumentList.razor`
- `Documents/DocumentEdit.razor`
- `Settings/MenuList.razor`
- `Settings/BannerList.razor`
- `Settings/PageList.razor`
- `Settings/SiteSetting.razor`
- `Reports/Index.razor`
- `Audit/Index.razor`

### 3.3. `Pages/Auth/`
Chứa các màn hình xác thực.
- `LoginPages.razor`
- `LoginPagesAdmin.razor`
- `Logout.razor`

---

## 4. Mapping hiện tại
- `Pages/LoginPagesAdmin.razor` nên chuyển dần sang `Pages/Auth/LoginPagesAdmin.razor` nếu muốn chuẩn hóa lại hoàn toàn.
- `Pages/LoginPages.razor` là luồng public/login khác nếu cần.

---

## 5. Kết luận
Cây thư mục pages này giúp source chia rạch ròi 3 vùng:
- public
- admin
- auth

và phù hợp với kiến trúc portal đa trường đã chuẩn hóa.
