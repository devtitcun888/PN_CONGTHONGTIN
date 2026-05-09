using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicDocumentService
{
    Task<List<PublicDocumentListItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? docType = null, int page = 1, int pageSize = 10);
    Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? docType = null);
    Task<PublicDocumentDetail?> GetDocumentByIdAsync(string maTruongBo, string id);
    Task<List<PublicDocumentListItem>> GetRelatedDocumentsAsync(string maTruongBo, string? docType, string currentDocumentId, int take = 4);
}

public class PublicDocumentService : IPublicDocumentService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicDocumentService> _logger;

    public PublicDocumentService(ILogger<PublicDocumentService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicDocumentListItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? docType = null, int page = 1, int pageSize = 10)
    {
        var result = new List<PublicDocumentListItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, docType);
        var sql = $@"
            SELECT id, doc_title, doc_number, file_url, issued_date, doc_type
            FROM documents
            {where}
            ORDER BY issued_date DESC, created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicDocumentListItem
            {
                Id = row["id"]?.ToString(),
                DocTitle = row["doc_title"]?.ToString(),
                DocNumber = row["doc_number"]?.ToString(),
                FileUrl = row["file_url"]?.ToString(),
                IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"]),
                DocType = row["doc_type"]?.ToString()
            });
        }
        return result;
    }

    public async Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? docType = null)
    {
        var sql = $"SELECT COUNT(*) AS total FROM documents {BuildWhere(maTruongBo, keyword, docType)}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<PublicDocumentDetail?> GetDocumentByIdAsync(string maTruongBo, string id)
    {
        var sql = $@"
            SELECT id, doc_title, doc_number, doc_type, issuer, summary, content, file_url, issued_date
            FROM documents
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND id = '{Escape(id)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicDocumentDetail
        {
            Id = row["id"]?.ToString(),
            DocTitle = row["doc_title"]?.ToString(),
            DocNumber = row["doc_number"]?.ToString(),
            DocType = row["doc_type"]?.ToString(),
            Issuer = row["issuer"]?.ToString(),
            Summary = row["summary"]?.ToString(),
            Content = row["content"]?.ToString(),
            FileUrl = row["file_url"]?.ToString(),
            IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"])
        };
    }

    public async Task<List<PublicDocumentListItem>> GetRelatedDocumentsAsync(string maTruongBo, string? docType, string currentDocumentId, int take = 4)
    {
        var typeFilter = string.IsNullOrWhiteSpace(docType) ? string.Empty : $"AND doc_type = '{Escape(docType)}'";
        var sql = $@"
            SELECT id, doc_title, doc_number, file_url, issued_date, doc_type
            FROM documents
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
              AND id <> '{Escape(currentDocumentId)}'
              {typeFilter}
            ORDER BY issued_date DESC, created_at DESC
            LIMIT {take}";

        var result = new List<PublicDocumentListItem>();
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicDocumentListItem
            {
                Id = row["id"]?.ToString(),
                DocTitle = row["doc_title"]?.ToString(),
                DocNumber = row["doc_number"]?.ToString(),
                FileUrl = row["file_url"]?.ToString(),
                IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"]),
                DocType = row["doc_type"]?.ToString()
            });
        }
        return result;
    }

    private static string BuildWhere(string maTruongBo, string? keyword, string? docType)
    {
        var clauses = new List<string>
        {
            "is_deleted = FALSE",
            "status = 'Published'",
            $"ma_truong_bo = '{Escape(maTruongBo)}'"
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(doc_title ILIKE '%{k}%' OR doc_number ILIKE '%{k}%')");
        }

        if (!string.IsNullOrWhiteSpace(docType))
        {
            clauses.Add($"doc_type = '{Escape(docType)}'");
        }

        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicDocumentListItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
    public string? DocType { get; set; }
}

public class PublicDocumentDetail
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? DocType { get; set; }
    public string? Issuer { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
}
