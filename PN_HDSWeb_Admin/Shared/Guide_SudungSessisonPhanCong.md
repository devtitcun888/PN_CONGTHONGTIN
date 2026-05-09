# Hướng Dẫn Lấy Dữ Liệu Phân Công Giáo Viên Từ Session

Sau khi giáo viên đăng nhập, toàn bộ dữ liệu **Phân công Chủ nhiệm** và **Phân công Giảng dạy** của giáo viên đó trong năm học hiện tại đã được trích xuất sẵn từ Database, map các ID thành tên chữ (Tên Lớp, Tên Môn) và được lưu vào đối tượng `UserSession`.

Dưới đây là 2 cách để bạn có thể gọi và sử dụng các danh sách này ở bất kì Component `.razor` nào bên trong hệ thống mà **không cần phải Query lại Database**.

---

### Cách 1: Thông qua `UserState` (Khuyên Dùng)

Sử dụng trực tiếp file State Management đã được register sẵn trong hệ thống (nhanh và ít dòng code nhất).

```razor
@page "/module-cua-ban"
@using PN_HDSWeb_Library

@* 1. Inject service UserState *@
@inject UserState userState

<h3>Thông tin phân công:</h3>

@if (danhSachChuNhiem != null)
{
    <ul>
        @foreach(var item in danhSachChuNhiem)
        {
            <li>Chủ nhiệm lớp: @item.TenLop (ID: @item.IdLopBo)</li>
        }
    </ul>
}

@if (danhSachGiangDay != null)
{
    <ul>
        @foreach(var item in danhSachGiangDay)
        {
            <li>Giảng dạy môn: @item.TenMonHoc - Lớp: @item.TenLop</li>
        }
    </ul>
}

@code {
    private List<SessionLopChuNhiem>? danhSachChuNhiem = new();
    private List<SessionPhanCongGiangDay>? danhSachGiangDay = new();

    protected override void OnInitialized()
    {
        // 2. Lấy dữ liệu từ CurrentUser thuộc UserState
        if (userState.CurrentUser != null)
        {
            danhSachChuNhiem = userState.CurrentUser.DanhSachLopChuNhiem;
            danhSachGiangDay = userState.CurrentUser.DanhSachLopGiangDay;
        }
    }
}
```

---

### Cách 2: Thông qua `CustomAuthenticationStateProvider`

Phương pháp này đảm bảo lấy session trực tiếp thông qua luồng quản lý Authentication (Phù hợp và an toàn nhất cho việc dùng ở hàm `OnInitializedAsync`).

```razor
@page "/module-cua-ban"
@using PN_HDSWeb_Admin.Authentication
@using PN_HDSWeb_Library
@using Microsoft.AspNetCore.Components.Authorization

@* 1. Inject AuthenticationStateProvider *@
@inject AuthenticationStateProvider authStateProvider

@code {
    private List<SessionLopChuNhiem>? danhSachChuNhiem = new();
    private List<SessionPhanCongGiangDay>? danhSachGiangDay = new();

    protected override async Task OnInitializedAsync()
    {
        // Lấy Authentication Data Provider và cast về CustomAuthenticationStateProvider
        var customAuthStateProvider = (CustomAuthenticationStateProvider)authStateProvider;
        
        // Gọi hàm GetCurrentUserSession()
        var session = await customAuthStateProvider.GetCurrentUserSession();
        
        if (session != null)
        {
            danhSachChuNhiem = session.DanhSachLopChuNhiem;
            danhSachGiangDay = session.DanhSachLopGiangDay;
        }
    }
}
```

### Cấu trúc dữ liệu có sẵn trong các Class
Dành cho việc truy cập thuộc tính khi sử dụng:

**Lớp Chủ Nhiệm (`SessionLopChuNhiem`)**:
- `IdLopBo` (int): ID chính của lớp trong DB
- `TenLop` (string): Tên hiển thị (VD: "10A1")
- `MaKhoiBo` (string): Mã khối học (VD: "10")

**Lớp Giảng Dạy (`SessionPhanCongGiangDay`)**:
- `IdLopBo` (int): ID chính của lớp học
- `TenLop` (string): Tên hiển thị
- `MaMonHoc` (string): Mã môn (Id môn học đã Pad 0)
- `TenMonHoc` (string): Tên hiển thị môn học (VD: "Toán học")
- `MaHocKy` (string): Học kỳ giảng dạy
