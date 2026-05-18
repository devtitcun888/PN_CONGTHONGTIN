using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummary> GetSummaryAsync(string maTruongBo);
    Task<List<AdminTrafficPageItem>> GetTopTrafficPagesAsync(string maTruongBo, int limit = 10);
}

public class AdminDashboardService : IAdminDashboardService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(ILogger<AdminDashboardService> logger)
    {
        _logger = logger;
    }

    public async Task<AdminDashboardSummary> GetSummaryAsync(string maTruongBo)
    {
        var sql = $@"
            SELECT
                (SELECT COUNT(*) FROM posts WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE) AS total_posts,
                (SELECT COUNT(*) FROM posts WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND status = 'Pending' AND is_deleted = FALSE) AS pending_posts,
                (SELECT COUNT(*) FROM posts WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND status = 'Published' AND is_deleted = FALSE) AS published_posts,
                (SELECT COUNT(*) FROM documents WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND is_deleted = FALSE) AS total_documents,
                (SELECT COUNT(*) FROM documents WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND status = 'Pending' AND is_deleted = FALSE) AS pending_documents,
                (SELECT COUNT(*) FROM documents WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND status = 'Published' AND is_deleted = FALSE) AS published_documents,
                (SELECT COUNT(*) FROM counter_traffic WHERE ma_truong_bo = '{Escape(maTruongBo)}') AS total_visits,
                (SELECT COUNT(*) FROM counter_traffic WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND visit_date = CURRENT_DATE) AS visits_today,
                (SELECT COUNT(DISTINCT ip_address) FROM counter_traffic WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND visit_date = CURRENT_DATE) AS unique_today";

        try
        {
            DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            if (dt.Rows.Count == 0)
                return new AdminDashboardSummary();

            DataRow row = dt.Rows[0];
            return new AdminDashboardSummary
            {
                TotalPosts = ToInt(row["total_posts"]),
                PendingPosts = ToInt(row["pending_posts"]),
                PublishedPosts = ToInt(row["published_posts"]),
                TotalDocuments = ToInt(row["total_documents"]),
                PendingDocuments = ToInt(row["pending_documents"]),
                PublishedDocuments = ToInt(row["published_documents"]),
                TotalVisits = ToInt(row["total_visits"]),
                VisitsToday = ToInt(row["visits_today"]),
                UniqueToday = ToInt(row["unique_today"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSummaryAsync failed");
            throw;
        }
    }

    public async Task<List<AdminTrafficPageItem>> GetTopTrafficPagesAsync(string maTruongBo, int limit = 10)
    {
        var items = new List<AdminTrafficPageItem>();
        var sql = $@"
            SELECT page_path,
                   COUNT(*) AS views,
                   COUNT(DISTINCT ip_address) AS unique_visitors
            FROM counter_traffic
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            GROUP BY page_path
            ORDER BY views DESC, page_path ASC
            LIMIT {limit}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            items.Add(new AdminTrafficPageItem
            {
                PagePath = row["page_path"]?.ToString(),
                Views = ToInt(row["views"]),
                UniqueVisitors = ToInt(row["unique_visitors"])
            });
        }

        return items;
    }

    private static int ToInt(object value) => value == DBNull.Value ? 0 : Convert.ToInt32(value);
    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminDashboardSummary
{
    public int TotalPosts { get; set; }
    public int PendingPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int TotalDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int PublishedDocuments { get; set; }
    public int TotalVisits { get; set; }
    public int VisitsToday { get; set; }
    public int UniqueToday { get; set; }
}

public class AdminTrafficPageItem
{
    public string? PagePath { get; set; }
    public int Views { get; set; }
    public int UniqueVisitors { get; set; }
}
