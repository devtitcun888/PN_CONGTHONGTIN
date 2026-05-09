namespace PN_HDSWeb_Admin.Data.Model;

public class UserAccountData_
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Roles { get; set; }
    public string? AuthType { get; set; }
    public string? SsoUserName { get; set; }
    public string? SsoUserId { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
}

public class AdminAccountListItemDto
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Roles { get; set; }
    public string? AuthType { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminAccountDetailDto
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Roles { get; set; }
    public string? AuthType { get; set; }
    public string? SsoUserName { get; set; }
    public string? SsoUserId { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
}

public class AdminAccountUpsertDto
{
    public string? Id { get; set; }
    public string MaTruongBo { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Roles { get; set; } = "Administrator";
    public string AuthType { get; set; } = "Local";
    public string? SsoUserName { get; set; }
    public string? SsoUserId { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; }
    public string? LockReason { get; set; }
}
