using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicPostTagService
{
    Task<List<PublicPostTagItem>> GetTagsByPostIdAsync(string postId);
    Task<PublicPostTagItem?> GetTagBySlugAsync(string maTruongBo, string slug);
}

public class PublicPostTagService : IPublicPostTagService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicPostTagService> _logger;

    public PublicPostTagService(ILogger<PublicPostTagService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicPostTagItem>> GetTagsByPostIdAsync(string postId)
    {
        var result = new List<PublicPostTagItem>();
        var sql = $@"
            SELECT t.id, t.tag_name, t.slug
            FROM post_tag_map m
            INNER JOIN post_tags t ON t.id = m.tag_id AND t.is_deleted = FALSE AND t.is_active = TRUE
            WHERE m.post_id = '{Escape(postId)}'
            ORDER BY t.tag_name ASC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicPostTagItem
            {
                Id = row["id"]?.ToString(),
                TagName = row["tag_name"]?.ToString(),
                Slug = row["slug"]?.ToString()
            });
        }
        return result;
    }

    public async Task<PublicPostTagItem?> GetTagBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, tag_name, slug
            FROM post_tags
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicPostTagItem
        {
            Id = row["id"]?.ToString(),
            TagName = row["tag_name"]?.ToString(),
            Slug = row["slug"]?.ToString()
        };
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicPostTagItem
{
    public string? Id { get; set; }
    public string? TagName { get; set; }
    public string? Slug { get; set; }
}
