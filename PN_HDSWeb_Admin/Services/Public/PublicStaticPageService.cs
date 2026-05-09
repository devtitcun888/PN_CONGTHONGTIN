using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicStaticPageService
{
    Task<PublicStaticPageDetail?> GetBySlugAsync(string maTruongBo, string slug);
    Task<List<PublicStaticPageMenuItem>> GetPagesByMenuAsync(string maTruongBo);
}

public class PublicStaticPageService : IPublicStaticPageService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicStaticPageService> _logger;

    public PublicStaticPageService(ILogger<PublicStaticPageService> logger)
    {
        _logger = logger;
    }

    public async Task<PublicStaticPageDetail?> GetBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, title, slug, content
            FROM static_pages
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicStaticPageDetail
        {
            Id = row["id"]?.ToString(),
            Title = row["title"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            Content = row["content"]?.ToString()
        };
    }

    public async Task<List<PublicStaticPageMenuItem>> GetPagesByMenuAsync(string maTruongBo)
    {
        var list = new List<PublicStaticPageMenuItem>();
        var sql = $@"
            SELECT id, title, slug
            FROM static_pages
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new PublicStaticPageMenuItem
            {
                Id = row["id"]?.ToString(),
                Title = row["title"]?.ToString(),
                Slug = row["slug"]?.ToString()
            });
        }
        return list;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicStaticPageDetail
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
}

public class PublicStaticPageMenuItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
}
