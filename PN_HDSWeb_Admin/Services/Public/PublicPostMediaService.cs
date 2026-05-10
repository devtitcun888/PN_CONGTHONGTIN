using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicPostMediaService
{
    Task<List<PublicPostMediaItem>> GetMediaAsync(string maTruongBo, string postId);
}

public class PublicPostMediaService : IPublicPostMediaService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicPostMediaService> _logger;

    public PublicPostMediaService(ILogger<PublicPostMediaService> logger)
    {
        _logger = logger;
    }

    public async Task<List<PublicPostMediaItem>> GetMediaAsync(string maTruongBo, string postId)
    {
        var result = new List<PublicPostMediaItem>();
        var sql = $@"
            SELECT id, media_type, file_name, file_url, thumbnail_url, file_size, mime_type, sort_order, caption
            FROM post_media
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND post_id = '{Escape(postId)}'
              AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(new PublicPostMediaItem
            {
                Id = row["id"]?.ToString(),
                MediaType = row["media_type"]?.ToString(),
                FileName = row["file_name"]?.ToString(),
                FileUrl = row["file_url"]?.ToString(),
                ThumbnailUrl = row["thumbnail_url"]?.ToString(),
                FileSize = row["file_size"] == DBNull.Value ? null : Convert.ToInt64(row["file_size"]),
                MimeType = row["mime_type"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
                Caption = row["caption"]?.ToString()
            });
        }
        return result;
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicPostMediaItem
{
    public string? Id { get; set; }
    public string? MediaType { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public int SortOrder { get; set; }
    public string? Caption { get; set; }
}
