using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Content;

public interface IAdminPostTagMapService
{
    Task<List<string>> GetTagIdsByPostIdAsync(string postId);
    Task<bool> ReplaceTagsAsync(string postId, IEnumerable<string> tagIds);
}

public class AdminPostTagMapService : IAdminPostTagMapService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<AdminPostTagMapService> _logger;

    public AdminPostTagMapService(ILogger<AdminPostTagMapService> logger)
    {
        _logger = logger;
    }

    public async Task<List<string>> GetTagIdsByPostIdAsync(string postId)
    {
        var result = new List<string>();
        var sql = $@"
            SELECT tag_id
            FROM post_tag_map
            WHERE post_id = '{Escape(postId)}'";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            var tagId = row["tag_id"]?.ToString();
            if (!string.IsNullOrWhiteSpace(tagId)) result.Add(tagId);
        }
        return result;
    }

    public async Task<bool> ReplaceTagsAsync(string postId, IEnumerable<string> tagIds)
    {
        var tags = tagIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var deleteSql = $"DELETE FROM post_tag_map WHERE post_id = '{Escape(postId)}'";

        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, deleteSql);

            foreach (var tagId in tags)
            {
                var insertSql = $@"
                    INSERT INTO post_tag_map (post_id, tag_id, created_at)
                    VALUES ('{Escape(postId)}', '{Escape(tagId)}', NOW())";
                await hdataLib.hrunQueryAsync(LoginID_Index, insertSql);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReplaceTagsAsync failed");
            throw;
        }
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}
