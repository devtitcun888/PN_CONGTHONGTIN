using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicSearchService
{
    Task<PublicSearchResult> SearchAsync(string maTruongBo, string keyword, int page = 1, int pageSize = 10);
}

public class PublicSearchService : IPublicSearchService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicSearchService> _logger;

    public PublicSearchService(ILogger<PublicSearchService> logger)
    {
        _logger = logger;
    }

    public async Task<PublicSearchResult> SearchAsync(string maTruongBo, string keyword, int page = 1, int pageSize = 10)
    {
        var result = new PublicSearchResult();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var k = Escape(keyword);

        try
        {
            var sqlPosts = $@"
                SELECT id, title, slug, summary, view_count
                FROM posts
                WHERE ma_truong_bo = '{Escape(maTruongBo)}'
                  AND is_deleted = FALSE
                  AND status = 'Published'
                  AND (title ILIKE '%{k}%' OR summary ILIKE '%{k}%')
                ORDER BY publish_at DESC, created_at DESC
                LIMIT {pageSize} OFFSET {offset}";
            var dtPosts = await hdataLib.hgetDataTableAsync(LoginID_Index, sqlPosts);
            foreach (DataRow row in dtPosts.Rows)
            {
                result.Posts.Add(new PublicSearchItem
                {
                    Id = row["id"]?.ToString(),
                    Title = row["title"]?.ToString(),
                    Slug = row["slug"]?.ToString(),
                    Summary = row["summary"]?.ToString(),
                    ViewCount = row["view_count"] == DBNull.Value ? 0 : Convert.ToInt64(row["view_count"]),
                    Type = "Post"
                });
            }

            var sqlDocs = $@"
                SELECT id, doc_title, doc_number, summary
                FROM documents
                WHERE ma_truong_bo = '{Escape(maTruongBo)}'
                  AND is_deleted = FALSE
                  AND status = 'Published'
                  AND (doc_title ILIKE '%{k}%' OR doc_number ILIKE '%{k}%')
                ORDER BY issued_date DESC, created_at DESC
                LIMIT {pageSize} OFFSET {offset}";
            var dtDocs = await hdataLib.hgetDataTableAsync(LoginID_Index, sqlDocs);
            foreach (DataRow row in dtDocs.Rows)
            {
                result.Documents.Add(new PublicSearchItem { Id = row["id"]?.ToString(), Title = row["doc_title"]?.ToString(), Slug = row["id"]?.ToString(), Summary = row["summary"]?.ToString(), Type = "Document" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAsync failed");
            throw;
        }

        return result;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicSearchResult
{
    public List<PublicSearchItem> Posts { get; set; } = [];
    public List<PublicSearchItem> Documents { get; set; } = [];
}

public class PublicSearchItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public long ViewCount { get; set; }
    public string? Type { get; set; }
}
