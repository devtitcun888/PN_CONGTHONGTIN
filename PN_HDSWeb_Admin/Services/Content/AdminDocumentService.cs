using hDataLibraryN8;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Admin.Services.Admin;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminDocumentService
{
    Task<List<AdminDocumentItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? status = null, int page = 1, int pageSize = 20);
    Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? status = null);
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

    public async Task<List<AdminDocumentItem>> GetDocumentsAsync(string maTruongBo, string? keyword = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var result = new List<AdminDocumentItem>();
        var offset = Math.Max(page - 1, 0) * pageSize;
        var where = BuildWhere(maTruongBo, keyword, status);
        var sql = $@"
            SELECT id, doc_title, doc_number, status, issued_date, created_at
            FROM documents
            {where}
            ORDER BY created_at DESC
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

    public async Task<int> GetDocumentsCountAsync(string maTruongBo, string? keyword = null, string? status = null)
    {
        var where = BuildWhere(maTruongBo, keyword, status);
        var sql = $"SELECT COUNT(*) AS total FROM documents {where}";
        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        return dt.Rows.Count == 0 || dt.Rows[0]["total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["total"]);
    }

    public async Task<AdminDocumentDetail?> GetDocumentByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, ma_truong_bo, doc_number, doc_title, doc_type, issuer, summary, content, file_url,
                   status, version_no, issued_date, effective_date, expire_date, is_deleted
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
            DocNumber = row["doc_number"]?.ToString(),
            DocTitle = row["doc_title"]?.ToString(),
            DocType = row["doc_type"]?.ToString(),
            Issuer = row["issuer"]?.ToString(),
            Summary = row["summary"]?.ToString(),
            Content = row["content"]?.ToString(),
            FileUrl = row["file_url"]?.ToString(),
            Status = row["status"]?.ToString(),
            VersionNo = row["version_no"] == DBNull.Value ? 1 : Convert.ToInt32(row["version_no"]),
            IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"]),
            EffectiveDate = row["effective_date"] == DBNull.Value ? null : Convert.ToDateTime(row["effective_date"]),
            ExpireDate = row["expire_date"] == DBNull.Value ? null : Convert.ToDateTime(row["expire_date"])
        };
    }

    public async Task<bool> CreateDocumentAsync(AdminDocumentDetail model)
    {
        var sql = $@"
            INSERT INTO documents
            (ma_truong_bo, doc_number, doc_title, doc_type, issuer, summary, content, file_url,
             status, version_no, issued_date, effective_date, expire_date, created_at, updated_at, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.DocNumber)}', '{Escape(model.DocTitle)}', '{Escape(model.DocType)}',
             '{Escape(model.Issuer)}', '{Escape(model.Summary)}', '{Escape(model.Content)}', '{Escape(model.FileUrl)}',
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
               SET doc_number = '{Escape(model.DocNumber)}',
                   doc_title = '{Escape(model.DocTitle)}',
                   doc_type = '{Escape(model.DocType)}',
                   issuer = '{Escape(model.Issuer)}',
                   summary = '{Escape(model.Summary)}',
                   content = '{Escape(model.Content)}',
                   file_url = '{Escape(model.FileUrl)}',
                   status = '{Escape(model.Status)}',
                   version_no = {model.VersionNo},
                   issued_date = {(model.IssuedDate.HasValue ? $"'{model.IssuedDate:yyyy-MM-dd}'" : "NULL")},
                   effective_date = {(model.EffectiveDate.HasValue ? $"'{model.EffectiveDate:yyyy-MM-dd}'" : "NULL")},
                   expire_date = {(model.ExpireDate.HasValue ? $"'{model.ExpireDate:yyyy-MM-dd}'" : "NULL")},
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

    private static string BuildWhere(string maTruongBo, string? keyword, string? status)
    {
        var clauses = new List<string>
        {
            "is_deleted = FALSE",
            $"ma_truong_bo = '{Escape(maTruongBo)}'"
        };
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = Escape(keyword);
            clauses.Add($"(doc_title ILIKE '%{k}%' OR doc_number ILIKE '%{k}%')");
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            clauses.Add($"status = '{Escape(status)}'");
        }
        return "WHERE " + string.Join(" AND ", clauses);
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminDocumentItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminDocumentDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? DocNumber { get; set; }
    public string? DocTitle { get; set; }
    public string? DocType { get; set; }
    public string? Issuer { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? FileUrl { get; set; }
    public string? Status { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTime? IssuedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpireDate { get; set; }
}
