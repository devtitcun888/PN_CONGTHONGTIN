using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummary> GetSummaryAsync(string maTruongBo);
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
                (SELECT COUNT(*) FROM documents WHERE ma_truong_bo = '{Escape(maTruongBo)}' AND status = 'Published' AND is_deleted = FALSE) AS published_documents";

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
                PublishedDocuments = ToInt(row["published_documents"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSummaryAsync failed");
            throw;
        }
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
}
