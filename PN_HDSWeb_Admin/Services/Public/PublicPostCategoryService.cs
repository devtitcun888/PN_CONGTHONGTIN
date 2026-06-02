using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicPostCategoryService
{
    Task<List<PublicPostCategoryItem>> GetCategoriesAsync(string maTruongBo);
    Task<PublicPostCategoryDetail?> GetCategoryBySlugAsync(string maTruongBo, string slug);
}

public class PublicPostCategoryService : IPublicPostCategoryService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicPostCategoryService> _logger;
    private readonly IMemoryCache _cache;

    public PublicPostCategoryService(ILogger<PublicPostCategoryService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<List<PublicPostCategoryItem>> GetCategoriesAsync(string maTruongBo)
    {
        string cacheKey = $"PostCategories_{maTruongBo}";
        if (_cache.TryGetValue(cacheKey, out List<PublicPostCategoryItem>? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var result = new List<PublicPostCategoryItem>();
        var sql = $@"
            SELECT id, category_name, slug, parent_id, description, sort_order
            FROM post_categories
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            ORDER BY sort_order ASC, created_at ASC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicPostCategoryItem
            {
                Id = row["id"]?.ToString(),
                CategoryName = row["category_name"]?.ToString(),
                Slug = row["slug"]?.ToString(),
                ParentId = row["parent_id"]?.ToString(),
                Description = row["description"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
            });
        }

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(2));
        return result;
    }

    public async Task<PublicPostCategoryDetail?> GetCategoryBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, category_name, slug, parent_id, description, sort_order
            FROM post_categories
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicPostCategoryDetail
        {
            Id = row["id"]?.ToString(),
            CategoryName = row["category_name"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            ParentId = row["parent_id"]?.ToString(),
            Description = row["description"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
        };
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicPostCategoryItem
{
    public string? Id { get; set; }
    public string? CategoryName { get; set; }
    public string? Slug { get; set; }
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class PublicPostCategoryDetail
{
    public string? Id { get; set; }
    public string? CategoryName { get; set; }
    public string? Slug { get; set; }
    public string? ParentId { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
