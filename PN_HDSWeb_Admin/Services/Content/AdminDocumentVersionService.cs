using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminDocumentVersionService
{
    Task<List<AdminDocumentVersionItem>> GetVersionsAsync(string maTruongBo, string documentId);
    Task<int> GetNextVersionNoAsync(string documentId);
    Task<bool> CreateVersionAsync(AdminDocumentVersionDetail model);
}

public class AdminDocumentVersionService : IAdminDocumentVersionService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminDocumentVersionService> _logger;

    public AdminDocumentVersionService(ILogger<AdminDocumentVersionService> logger)
    {
        _logger = logger;
    }

    public async Task<List<AdminDocumentVersionItem>> GetVersionsAsync(string maTruongBo, string documentId)
    {
        var result = new List<AdminDocumentVersionItem>();
        var sql = $@"
            SELECT id, version_no, file_url, file_name, change_summary, created_at
            FROM document_versions
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND document_id = '{Escape(documentId)}'
            ORDER BY version_no DESC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new AdminDocumentVersionItem
            {
                Id = row["id"]?.ToString(),
                VersionNo = row["version_no"] == DBNull.Value ? 0 : Convert.ToInt32(row["version_no"]),
                FileUrl = row["file_url"]?.ToString(),
                FileName = row["file_name"]?.ToString(),
                ChangeSummary = row["change_summary"]?.ToString(),
                CreatedAt = row["created_at"] == DBNull.Value ? null : Convert.ToDateTime(row["created_at"])
            });
        }

        return result;
    }

    public async Task<int> GetNextVersionNoAsync(string documentId)
    {
        var sql = $@"
            SELECT COALESCE(MAX(version_no), 0) + 1 AS next_no
            FROM document_versions
            WHERE document_id = '{Escape(documentId)}'";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["next_no"] == DBNull.Value ? 1 : Convert.ToInt32(dt.Rows[0]["next_no"]);
    }

    public async Task<bool> CreateVersionAsync(AdminDocumentVersionDetail model)
    {
        var sql = $@"
            INSERT INTO document_versions
            (ma_truong_bo, document_id, version_no, file_url, file_name, change_summary, created_by, created_at)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.DocumentId)}', {model.VersionNo},
             '{Escape(model.FileUrl)}', '{Escape(model.FileName)}', '{Escape(model.ChangeSummary)}',
             '{Escape(model.CreatedBy)}', NOW())";

        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, sql);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateVersionAsync failed");
            throw;
        }
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminDocumentVersionItem
{
    public string? Id { get; set; }
    public int VersionNo { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? ChangeSummary { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class AdminDocumentVersionDetail
{
    public string? DocumentId { get; set; }
    public string? MaTruongBo { get; set; }
    public int VersionNo { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? ChangeSummary { get; set; }
    public string? CreatedBy { get; set; }
}
