using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicDocumentVersionService
{
    Task<List<PublicDocumentVersionItem>> GetVersionsAsync(string maTruongBo, string documentId);
}

public class PublicDocumentVersionService : IPublicDocumentVersionService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicDocumentVersionService> _logger;

    public PublicDocumentVersionService(ILogger<PublicDocumentVersionService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicDocumentVersionItem>> GetVersionsAsync(string maTruongBo, string documentId)
    {
        var result = new List<PublicDocumentVersionItem>();
        var sql = $@"
            SELECT id, version_no, file_url, file_name, change_summary, created_at
            FROM document_versions
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND document_id = '{Escape(documentId)}'
            ORDER BY version_no DESC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicDocumentVersionItem
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

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicDocumentVersionItem
{
    public string? Id { get; set; }
    public int VersionNo { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? ChangeSummary { get; set; }
    public DateTime? CreatedAt { get; set; }
}
