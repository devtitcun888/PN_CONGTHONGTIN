using hDataLibraryN8;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Admin.Services.Admin;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminDocumentService
{
    Task<List<AdminDocumentItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? status = null, string? documentTypeId = null, int page = 1, int pageSize = 20);
    Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? status = null, string? documentTypeId = null);
    Task<AdminDocumentDetail?> GetDocumentByIdAsync(string id);
    Task<bool> CreateDocumentAsync(AdminDocumentDetail model);
    Task<bool> UpdateDocumentAsync(AdminDocumentDetail model);
    Task<bool> DeleteDocumentAsync(string id);
}

public class AdminDocumentService : IAdminDocumentService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminDocumentService> _logger;
    private readonly IAdminFileStorageService _fileStorage;

    public AdminDocumentService(ILogger<AdminDocumentService> logger, IAdminFileStorageService fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
    }

    public async Task<List<AdminDocumentItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? status = null, string? documentTypeId = null, int page = 1, int pageSize = 20)
    {
        var result = new List<AdminDocumentItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, status, documentTypeId);
        var sql = $@"
            SELECT d.id, d.doc_title, d.doc_number, d.doc_type, d.status, d.issued_date, d.created_at,
                   d.document_type_id, dt.type_name, dt.slug AS type_slug
            FROM documents d
            LEFT JOIN document_types dt ON dt.id = d.document_type_id AND dt.is_deleted = FALSE
            {where}
            ORDER BY d.created_at DESC
            LIMIT {pageSize} OFFSET {offset}";

        try
        {
            DataTable dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            foreach (DataRow row in dt.Rows)
            {
                result.Add(new AdminDocumentItem
                {
                    Id = row["id"]?.ToString(),
                    DocTitle = row["doc_title"]?.ToString(),
                    DocNumber = row["doc_number"]?.ToString(),
                    DocType = row["doc_type"]?.ToString(),
                    DocumentTypeId = row["document_type_id"]?.ToString(),
                    DocumentTypeName = row["type_name"]?.ToString() ?? row["doc_type"]?.ToString(),
                    DocumentTypeSlug = row["type_slug"]?.ToString(),
                    Status = row["status"]?.ToString(),
                    IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"]),
                    CreatedAt = row["created_at"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["created_at"])
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocumentsAsync failed");
            throw;
        }

        return result;
    }

    public async Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? status = null, string? documentTypeId = null)
    {
        var where = BuildWhere(maTruongBo, keyword, status, documentTypeId);
        var sql = $"SELECT COUNT(*) AS total FROM documents d {where}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<AdminDocumentDetail?> GetDocumentByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, document_type_id, doc_number, doc_title, doc_type, doc_code, issuer, summary, content, file_url, file_name, mime_type,
                   status, version_no, issued_date, effective_date, expiry_date, is_deleted
            FROM documents
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new AdminDocumentDetail
        {
            Id = row["id"]?.ToString(),
            MaTruongBo = row["ma_truong_bo"]?.ToString(),
            DocumentTypeId = row["document_type_id"]?.ToString(),
            DocNumber = row["doc_number"]?.ToString(),
            DocTitle = row["doc_title"]?.ToString(),
            DocType = row["doc_type"]?.ToString(),
            DocCode = row["doc_code"]?.ToString(),
            Issuer = row["issuer"]?.ToString(),
            Summary = row["summary"]?.ToString(),
            Content = row["content"]?.ToString(),
            FileUrl = row["file_url"]?.ToString(),
            FileName = row["file_name"]?.ToString(),
            MimeType = row["mime_type"]?.ToString(),
            Status = row["status"]?.ToString(),
            VersionNo = row["version_no"] == DBNull.Value ? 1 : Convert.ToInt32(row["version_no"]),
            IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"]),
            EffectiveDate = row["effective_date"] == DBNull.Value ? null : Convert.ToDateTime(row["effective_date"]),
            ExpireDate = row["expiry_date"] == DBNull.Value ? null : Convert.ToDateTime(row["expiry_date"])
        };
    }

    public async Task<bool> CreateDocumentAsync(AdminDocumentDetail model)
    {
        var sql = $@"
            INSERT INTO documents
            (ma_truong_bo, document_type_id, doc_type, doc_number, doc_title, doc_code, issuer, summary, content, file_url, file_name, mime_type,
             status, version_no, issued_date, effective_date, expiry_date, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', {ToNullableBigIntSql(model.DocumentTypeId)}, '{Escape(model.DocType)}', '{Escape(model.DocNumber)}', '{Escape(model.DocTitle)}', '{Escape(model.DocCode)}',
             '{Escape(model.Issuer)}', '{Escape(model.Summary)}', '{Escape(model.Content)}', '{Escape(model.FileUrl)}', '{Escape(model.FileName)}', '{Escape(model.MimeType)}',
             '{Escape(model.Status)}', {model.VersionNo},
             {(model.IssuedDate.HasValue ? $"'{model.IssuedDate:yyyy-MM-dd}'" : "NULL")},
             {(model.EffectiveDate.HasValue ? $"'{model.EffectiveDate:yyyy-MM-dd}'" : "NULL")},
             {(model.ExpireDate.HasValue ? $"'{model.ExpireDate:yyyy-MM-dd}'" : "NULL")}, NOW(), NOW(), FALSE)";

        return await RunAsync(sql, "CreateDocumentAsync");
    }

    public async Task<bool> UpdateDocumentAsync(AdminDocumentDetail model)
    {
        var sql = $@"
            UPDATE documents
               SET document_type_id = {ToNullableBigIntSql(model.DocumentTypeId)},
                   doc_type = '{Escape(model.DocType)}',
                   doc_number = '{Escape(model.DocNumber)}',
                   doc_title = '{Escape(model.DocTitle)}',
                   doc_code = '{Escape(model.DocCode)}',
                   issuer = '{Escape(model.Issuer)}',
                   summary = '{Escape(model.Summary)}',
                   content = '{Escape(model.Content)}',
                   file_url = '{Escape(model.FileUrl)}',
                   file_name = '{Escape(model.FileName)}',
                   mime_type = '{Escape(model.MimeType)}',
                   status = '{Escape(model.Status)}',
                   version_no = {model.VersionNo},
                   issued_date = {(model.IssuedDate.HasValue ? $"'{model.IssuedDate:yyyy-MM-dd}'" : "NULL")},
                   effective_date = {(model.EffectiveDate.HasValue ? $"'{model.EffectiveDate:yyyy-MM-dd}'" : "NULL")},
                   expiry_date = {(model.ExpireDate.HasValue ? $"'{model.ExpireDate:yyyy-MM-dd}'" : "NULL")},
                   updated_at = NOW()
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateDocumentAsync");
    }

    public async Task<bool> DeleteDocumentAsync(string id)
    {
        var sql = $@"
            UPDATE documents
               SET is_deleted = TRUE,
                   updated_at = NOW()
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "DeleteDocumentAsync");
    }

    private async Task<bool> RunAsync(string sql, string action)
    {
        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, sql);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Action} failed", action);
            throw;
        }
    }

    private static string BuildWhere(string maTruongBo, string? keyword, string? status, string? documentTypeId)
    {
        var clauses = new List<string>
        {
            "d.is_deleted = FALSE",
            $"d.ma_truong_bo = '{Escape(maTruongBo)}'"
        };
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(d.doc_title ILIKE '%{k}%' OR d.doc_number ILIKE '%{k}%' OR d.doc_type ILIKE '%{k}%')");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            clauses.Add($"d.status = '{Escape(status)}'");
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

public class AdminDocumentItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? DocType { get; set; }
    public string? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? DocumentTypeSlug { get; set; }
    public string? Status { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminDocumentDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? DocumentTypeId { get; set; }
    public string? DocNumber { get; set; }
    public string? DocTitle { get; set; }
    public string? DocType { get; set; }
    public string? DocCode { get; set; }
    public string? Issuer { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? Status { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTime? IssuedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpireDate { get; set; }
}
