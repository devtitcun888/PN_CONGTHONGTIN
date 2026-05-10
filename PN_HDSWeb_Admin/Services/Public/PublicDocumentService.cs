using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicDocumentService
{
    Task<List<PublicDocumentListItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? documentTypeId = null, int page = 1, int pageSize = 10);
    Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? documentTypeId = null);
    Task<PublicDocumentDetail?> GetDocumentByIdAsync(string maTruongBo, string id);
    Task<List<PublicDocumentListItem>> GetRelatedDocumentsAsync(string maTruongBo, string? documentTypeId, string currentDocumentId, int take = 4);
}

public class PublicDocumentService : IPublicDocumentService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicDocumentService> _logger;

    public PublicDocumentService(ILogger<PublicDocumentService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicDocumentListItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? documentTypeId = null, int page = 1, int pageSize = 10)
    {
        var result = new List<PublicDocumentListItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, documentTypeId);
        var sql = $@"
            SELECT d.id, d.doc_title, d.doc_number, d.file_url, d.issued_date, d.document_type_id,
                   COALESCE(dt.type_name, d.doc_type) AS type_name,
                   COALESCE(dt.slug, '') AS type_slug
            FROM documents d
            LEFT JOIN document_types dt ON dt.id = d.document_type_id AND dt.is_deleted = FALSE
            {where}
            ORDER BY d.issued_date DESC, d.created_at DESC
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
                DocumentTypeId = row["document_type_id"]?.ToString(),
                TypeName = row["type_name"]?.ToString(),
                TypeSlug = row["type_slug"]?.ToString()
            });
        }
        return result;
    }

    public async Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? documentTypeId = null)
    {
        var sql = $"SELECT COUNT(*) AS total FROM documents d {BuildWhere(maTruongBo, keyword, documentTypeId)}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<PublicDocumentDetail?> GetDocumentByIdAsync(string maTruongBo, string id)
    {
        var sql = $@"
            SELECT d.id, d.document_type_id, d.doc_title, d.doc_number, d.doc_type, d.issuer, d.summary, d.content, d.file_url, d.issued_date,
                   COALESCE(dt.type_name, d.doc_type) AS type_name,
                   COALESCE(dt.slug, '') AS type_slug
            FROM documents d
            LEFT JOIN document_types dt ON dt.id = d.document_type_id AND dt.is_deleted = FALSE
            WHERE d.ma_truong_bo = '{Escape(maTruongBo)}'
              AND d.id = '{Escape(id)}'
              AND d.is_deleted = FALSE
              AND d.status = 'Published'
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicDocumentDetail
        {
            Id = row["id"]?.ToString(),
            DocumentTypeId = row["document_type_id"]?.ToString(),
            DocTitle = row["doc_title"]?.ToString(),
            DocNumber = row["doc_number"]?.ToString(),
            TypeName = row["type_name"]?.ToString(),
            TypeSlug = row["type_slug"]?.ToString(),
            Issuer = row["issuer"]?.ToString(),
            Summary = row["summary"]?.ToString(),
            Content = row["content"]?.ToString(),
            FileUrl = row["file_url"]?.ToString(),
            IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"])
        };
    }

    public async Task<List<PublicDocumentListItem>> GetRelatedDocumentsAsync(string maTruongBo, string? documentTypeId, string currentDocumentId, int take = 4)
    {
        var typeFilter = string.IsNullOrWhiteSpace(documentTypeId) ? string.Empty : $"AND d.document_type_id = {ToNullableBigIntSql(documentTypeId)}";
        var sql = $@"
            SELECT d.id, d.doc_title, d.doc_number, d.file_url, d.issued_date, d.document_type_id,
                   COALESCE(dt.type_name, d.doc_type) AS type_name,
                   COALESCE(dt.slug, '') AS type_slug
            FROM documents d
            LEFT JOIN document_types dt ON dt.id = d.document_type_id AND dt.is_deleted = FALSE
            WHERE d.ma_truong_bo = '{Escape(maTruongBo)}'
              AND d.is_deleted = FALSE
              AND d.status = 'Published'
              AND d.id <> '{Escape(currentDocumentId)}'
              {typeFilter}
            ORDER BY d.issued_date DESC, d.created_at DESC
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
                DocumentTypeId = row["document_type_id"]?.ToString(),
                TypeName = row["type_name"]?.ToString(),
                TypeSlug = row["type_slug"]?.ToString()
            });
        }
        return result;
    }

    private static string BuildWhere(string maTruongBo, string? keyword, string? documentTypeId)
    {
        var clauses = new List<string>
        {
            "d.is_deleted = FALSE",
            "d.status = 'Published'",
            $"d.ma_truong_bo = '{Escape(maTruongBo)}'"
        };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(d.doc_title ILIKE '%{k}%' OR d.doc_number ILIKE '%{k}%')");
        }

        if (!string.IsNullOrWhiteSpace(documentTypeId))
        {
            clauses.Add($"d.document_type_id = {ToNullableBigIntSql(documentTypeId)}");
        }

        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static string ToNullableBigIntSql(string? value)
        => string.IsNullOrWhiteSpace(value) ? "NULL" : $"'{Escape(value)}'";

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicDocumentListItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
    public string? DocumentTypeId { get; set; }
    public string? TypeName { get; set; }
    public string? TypeSlug { get; set; }
}

public class PublicDocumentDetail
{
    public string? Id { get; set; }
    public string? DocumentTypeId { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? TypeName { get; set; }
    public string? TypeSlug { get; set; }
    public string? Issuer { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
}
