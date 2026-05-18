using hDataLibraryN8;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Admin.Services.Admin;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminPostMediaService
{
    Task<List<AdminPostMediaItem>> GetMediaAsync(string maTruongBo, string postId);
    Task<AdminPostMediaItem?> GetMediaByIdAsync(string id);
    Task<bool> CreateMediaAsync(AdminPostMediaDetail model);
    Task<bool> CreateMediaFromFileAsync(AdminPostMediaDetail model, IBrowserFile file, string subFolder = "posts/media");
    Task<bool> UpdateMediaAsync(AdminPostMediaDetail model);
    Task<bool> ReplaceMediaFileAsync(AdminPostMediaDetail model, IBrowserFile file, string subFolder = "posts/media");
    Task<bool> DeleteMediaAsync(string id);
    Task<bool> MoveMediaAsync(string id, int sortOrder);
}

public class AdminPostMediaService : IAdminPostMediaService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminPostMediaService> _logger;
    private readonly IAdminFileStorageService _fileStorage;

    public AdminPostMediaService(ILogger<AdminPostMediaService> logger, IAdminFileStorageService fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
    }

    public async Task<List<AdminPostMediaItem>> GetMediaAsync(string maTruongBo, string postId)
    {
        var result = new List<AdminPostMediaItem>();
        var sql = $@"
            SELECT id, post_id, media_type, file_name, file_url, thumbnail_url, file_size, mime_type, sort_order, caption, created_at
            FROM post_media
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND post_id = '{Escape(postId)}'
              AND is_deleted = FALSE
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            result.Add(MapItem(row));
        }
        return result;
    }

    public async Task<AdminPostMediaItem?> GetMediaByIdAsync(string id)
    {
        var sql = $@"
            SELECT id, post_id, media_type, file_name, file_url, thumbnail_url, file_size, mime_type, sort_order, caption, created_at
            FROM post_media
            WHERE id = '{Escape(id)}' AND is_deleted = FALSE
            LIMIT 1";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        if (dt.Rows.Count == 0) return null;
        return MapItem(dt.Rows[0]);
    }

    public async Task<bool> CreateMediaAsync(AdminPostMediaDetail model)
    {
        var sql = $@"
            INSERT INTO post_media
            (ma_truong_bo, post_id, media_type, file_name, file_url, thumbnail_url, file_size, mime_type, sort_order, caption, created_at, created_by, is_deleted)
            VALUES
            ('{Escape(model.MaTruongBo)}', '{Escape(model.PostId)}', '{Escape(model.MediaType)}', '{Escape(model.FileName)}',
             '{Escape(model.FileUrl)}', '{Escape(model.ThumbnailUrl)}', {ToNullableBigIntSql(model.FileSize?.ToString())}, '{Escape(model.MimeType)}',
             {model.SortOrder}, '{Escape(model.Caption)}', NOW(), '{Escape(model.CreatedBy)}', FALSE)";

        return await RunAsync(sql, "CreateMediaAsync");
    }

    public async Task<bool> CreateMediaFromFileAsync(AdminPostMediaDetail model, IBrowserFile file, string subFolder = "posts")
    {
        var saved = await _fileStorage.SaveFileAsync(file, subFolder);
        if (string.IsNullOrWhiteSpace(saved))
            return false;

        model.FileUrl = saved;
        model.FileName ??= file.Name;
        model.FileSize ??= file.Size;
        model.MimeType ??= file.ContentType;
        return await CreateMediaAsync(model);
    }

    public async Task<bool> UpdateMediaAsync(AdminPostMediaDetail model)
    {
        var sql = $@"
            UPDATE post_media
               SET media_type = '{Escape(model.MediaType)}',
                   file_name = '{Escape(model.FileName)}',
                   file_url = '{Escape(model.FileUrl)}',
                   thumbnail_url = '{Escape(model.ThumbnailUrl)}',
                   file_size = {ToNullableBigIntSql(model.FileSize?.ToString())},
                   mime_type = '{Escape(model.MimeType)}',
                   sort_order = {model.SortOrder},
                   caption = '{Escape(model.Caption)}'
             WHERE id = '{Escape(model.Id)}'";

        return await RunAsync(sql, "UpdateMediaAsync");
    }

    public async Task<bool> ReplaceMediaFileAsync(AdminPostMediaDetail model, IBrowserFile file, string subFolder = "posts/media")
    {
        if (string.IsNullOrWhiteSpace(model.Id)) return false;

        var existing = await GetMediaByIdAsync(model.Id);
        if (existing?.FileUrl is not null)
            await _fileStorage.DeleteFileAsync(existing.FileUrl);

        var saved = await _fileStorage.SaveFileAsync(file, subFolder);
        if (string.IsNullOrWhiteSpace(saved))
            return false;

        model.FileUrl = saved;
        model.FileName = file.Name;
        model.FileSize = file.Size;
        model.MimeType = file.ContentType;
        return await UpdateMediaAsync(model);
    }

    //public async Task<bool> ReplaceMediaFileAsync(AdminPostMediaDetail model, IBrowserFile file, string subFolder = "posts/media")
    //{
    //    var oldFileUrl = model.FileUrl;
    //    var saved = await _fileStorage.SaveFileAsync(file, subFolder);
    //    if (string.IsNullOrWhiteSpace(saved))
    //        return false;

    //    model.FileUrl = saved;
    //    model.FileName = file.Name;
    //    model.FileSize = file.Size;
    //    model.MimeType = file.ContentType;

    //    var updated = await UpdateMediaAsync(model);
    //    if (updated && !string.IsNullOrWhiteSpace(oldFileUrl) && !string.Equals(oldFileUrl, saved, StringComparison.OrdinalIgnoreCase))
    //        await _fileStorage.DeleteFileAsync(oldFileUrl);

    //    return updated;
    //}

    public async Task<bool> DeleteMediaAsync(string id)
    {
        var media = await GetMediaByIdAsync(id);
        var sql = $@"
            UPDATE post_media
               SET is_deleted = TRUE
             WHERE id = '{Escape(id)}'";

        var deleted = await RunAsync(sql, "DeleteMediaAsync");
        if (deleted && media?.FileUrl is not null)
            await _fileStorage.DeleteFileAsync(media.FileUrl);

        return deleted;
    }

    public async Task<bool> MoveMediaAsync(string id, int sortOrder)
    {
        var sql = $@"
            UPDATE post_media
               SET sort_order = {sortOrder}
             WHERE id = '{Escape(id)}'";

        return await RunAsync(sql, "MoveMediaAsync");
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

    private static AdminPostMediaItem MapItem(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        PostId = row["post_id"]?.ToString(),
        MediaType = row["media_type"]?.ToString(),
        FileName = row["file_name"]?.ToString(),
        FileUrl = row["file_url"]?.ToString(),
        ThumbnailUrl = row["thumbnail_url"]?.ToString(),
        FileSize = row["file_size"] == DBNull.Value ? null : Convert.ToInt64(row["file_size"]),
        MimeType = row["mime_type"]?.ToString(),
        SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"]),
        Caption = row["caption"]?.ToString(),
        CreatedAt = row["created_at"] == DBNull.Value ? null : Convert.ToDateTime(row["created_at"])
    };

    private static string ToNullableBigIntSql(string? value) => string.IsNullOrWhiteSpace(value) ? "NULL" : value;
    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class AdminPostMediaItem
{
    public string? Id { get; set; }
    public string? PostId { get; set; }
    public string? MediaType { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public int SortOrder { get; set; }
    public string? Caption { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class AdminPostMediaDetail
{
    public string? Id { get; set; }
    public string? MaTruongBo { get; set; }
    public string? PostId { get; set; }
    public string? MediaType { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public int SortOrder { get; set; }
    public string? Caption { get; set; }
    public string? CreatedBy { get; set; }
}
