using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicStaffProfileService
{
    Task<List<PublicStaffProfileItem>> GetPublicStaffProfilesAsync(string maTruongBo, string? keyword = null);
    Task<List<PublicStaffProfileGroupItem>> GetPublicStaffProfileGroupsAsync(string maTruongBo, string? keyword = null);
    Task<List<PublicStaffProfileItem>> GetPublicStaffProfilesByGroupAsync(string maTruongBo, string groupName, string? keyword = null);
}

public class PublicStaffProfileService : IPublicStaffProfileService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicStaffProfileService> _logger;

    public PublicStaffProfileService(ILogger<PublicStaffProfileService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicStaffProfileItem>> GetPublicStaffProfilesAsync(string maTruongBo, string? keyword = null)
    {
        var result = new List<PublicStaffProfileItem>();
        var sql = $@"
            SELECT id, group_name, full_name, position_name, qualification, certificate_info, bio, avatar_url, email, phone, sort_order, created_at
            FROM staff_profiles
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
              AND is_public = TRUE
              {(string.IsNullOrWhiteSpace(keyword) ? string.Empty : $" AND (full_name ILIKE '%{Escape(keyword)}%' OR position_name ILIKE '%{Escape(keyword)}%' OR qualification ILIKE '%{Escape(keyword)}%' OR group_name ILIKE '%{Escape(keyword)}%')")}
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }

        return result;
    }

    public async Task<List<PublicStaffProfileGroupItem>> GetPublicStaffProfileGroupsAsync(string maTruongBo, string? keyword = null)
    {
        var all = await GetPublicStaffProfilesAsync(maTruongBo, keyword);
        return all
            .GroupBy(x => string.IsNullOrWhiteSpace(x.GroupName) ? GetGroupName(x.PositionName) : x.GroupName!)
            .OrderBy(g => GroupOrder(g.Key))
            .Select(g => new PublicStaffProfileGroupItem
            {
                GroupName = g.Key,
                Items = g.OrderBy(x => x.SortOrder).ToList()
            })
            .ToList();
    }

    public async Task<List<PublicStaffProfileItem>> GetPublicStaffProfilesByGroupAsync(string maTruongBo, string groupName, string? keyword = null)
    {
        var all = await GetPublicStaffProfilesAsync(maTruongBo, keyword);
        return all
            .Where(x => string.Equals(string.IsNullOrWhiteSpace(x.GroupName) ? GetGroupName(x.PositionName) : x.GroupName, groupName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

    private static PublicStaffProfileItem MapItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        GroupName = row.Table.Columns.Contains("group_name") ? row["group_name"]?.ToString() : null,
        FullName = row["full_name"]?.ToString(),
        PositionName = row["position_name"]?.ToString(),
        Qualification = row["qualification"]?.ToString(),
        CertificateInfo = row["certificate_info"]?.ToString(),
        Bio = row["bio"]?.ToString(),
        AvatarUrl = row["avatar_url"]?.ToString(),
        Email = row["email"]?.ToString(),
        Phone = row["phone"]?.ToString(),
        SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
        CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
    };

    private static string GetGroupName(string? positionName)
    {
        if (string.IsNullOrWhiteSpace(positionName)) return "Khác";
        var p = positionName.Trim().ToLowerInvariant();
        if (p.Contains("hiệu trưởng")) return "Ban giám hiệu";
        if (p.Contains("phó hiệu trưởng")) return "Ban giám hiệu";
        if (p.Contains("tổ")) return "Tổ / Phòng ban";
        if (p.Contains("giáo viên")) return "Giáo viên";
        if (p.Contains("văn phòng")) return "Văn phòng";
        return "Khác";
    }

    private static int GroupOrder(string groupName) => groupName switch
    {
        "Ban giám hiệu" => 1,
        "Tổ / Phòng ban" => 2,
        "Giáo viên" => 3,
        "Văn phòng" => 4,
        _ => 9
    };

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicStaffProfileItem
{
    public string? Id { get; set; }
    public string? GroupName { get; set; }
    public string? FullName { get; set; }
    public string? PositionName { get; set; }
    public string? Qualification { get; set; }
    public string? CertificateInfo { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicStaffProfileGroupItem
{
    public string GroupName { get; set; } = string.Empty;
    public List<PublicStaffProfileItem> Items { get; set; } = [];
}
