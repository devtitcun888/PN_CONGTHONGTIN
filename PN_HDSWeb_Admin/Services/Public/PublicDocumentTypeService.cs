using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicDocumentTypeService
{
    Task<List<PublicDocumentTypeItem>> GetDocumentTypesAsync(string maTruongBo);
    Task<PublicDocumentTypeDetail?> GetDocumentTypeBySlugAsync(string maTruongBo, string slug);
}

public class PublicDocumentTypeService : IPublicDocumentTypeService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicDocumentTypeService> _logger;

    public PublicDocumentTypeService(ILogger<PublicDocumentTypeService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicDocumentTypeItem>> GetDocumentTypesAsync(string maTruongBo)
    {
        var result = new List<PublicDocumentTypeItem>();
        var sql = $@"
            SELECT id, type_name, slug, description, sort_order
            FROM document_types
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            ORDER BY sort_order ASC, created_at ASC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicDocumentTypeItem
            {
                Id = row["id"]?.ToString(),
                TypeName = row["type_name"]?.ToString(),
                Slug = row["slug"]?.ToString(),
                Description = row["description"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
            });
        }
        return result;
    }

    public async Task<PublicDocumentTypeDetail?> GetDocumentTypeBySlugAsync(string maTruongBo, string slug)
    {
        var sql = $@"
            SELECT id, type_name, slug, description, sort_order
            FROM document_types
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND slug = '{Escape(slug)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new PublicDocumentTypeDetail
        {
            Id = row["id"]?.ToString(),
            TypeName = row["type_name"]?.ToString(),
            Slug = row["slug"]?.ToString(),
            Description = row["description"]?.ToString(),
            SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
        };
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicDocumentTypeItem
{
    public string? Id { get; set; }
    public string? TypeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class PublicDocumentTypeDetail
{
    public string? Id { get; set; }
    public string? TypeName { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
