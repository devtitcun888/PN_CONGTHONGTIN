using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicContactService
{
    Task<PublicContactInfo> GetContactAsync(string maTruongBo);
    Task<bool> SendContactMessageAsync(PublicContactMessage model);
}

public class PublicContactService : IPublicContactService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicContactService> _logger;

    public PublicContactService(ILogger<PublicContactService> logger)
    {
        _logger = logger;
    }

    public async Task<PublicContactInfo> GetContactAsync(string maTruongBo)
    {
        var sql = $@"
            SELECT tentruong, diachi_truong, logo_truong, hieutruong->>'ho_ten' AS leader_name, hieutruong->>'so_dien_thoai' AS leader_phone,
                   email, hotline, website_url, facebook_url, youtube_url, zalo_url
            FROM l_truong
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            if (dt.Rows.Count == 0)
            {
                return new PublicContactInfo { MaTruongBo = maTruongBo };
            }

            var row = dt.Rows[0];
            return new PublicContactInfo
            {
                MaTruongBo = maTruongBo,
                SchoolName = row["tentruong"]?.ToString(),
                Address = row["diachi_truong"]?.ToString(),
                LogoUrl = row["logo_truong"]?.ToString(),
                LeaderName = row["leader_name"]?.ToString(),
                LeaderPhone = row["leader_phone"]?.ToString(),
                Phone = row["hotline"]?.ToString() ?? row["leader_phone"]?.ToString(),
                Email = row["email"]?.ToString(),
                WebsiteUrl = row["website_url"]?.ToString(),
                FacebookUrl = row["facebook_url"]?.ToString(),
                YoutubeUrl = row["youtube_url"]?.ToString(),
                ZaloUrl = row["zalo_url"]?.ToString(),
                MapEmbedUrl = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetContactAsync failed");
            throw;
        }
    }

    public async Task<bool> SendContactMessageAsync(PublicContactMessage model)
    {
        var sql = $@"
            INSERT INTO contact_messages (ma_truong_bo, sender_name, sender_email, sender_phone, subject, message, created_at, is_read)
            VALUES ('{Escape(model.MaTruongBo)}', '{Escape(model.SenderName)}', '{Escape(model.SenderEmail)}', '{Escape(model.SenderPhone)}',
                    '{Escape(model.Subject)}', '{Escape(model.Message)}', NOW(), FALSE)";

        try
        {
            await hdataLib.hrunQueryAsync(LoginID_Index, sql);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendContactMessageAsync failed");
            throw;
        }
    }

    private static string Escape(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("'", "''");
}

public class PublicContactInfo
{
    public string? MaTruongBo { get; set; }
    public string? SchoolName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? MapEmbedUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? LeaderName { get; set; }
    public string? LeaderPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? ZaloUrl { get; set; }
}

public class PublicContactMessage
{
    public string? MaTruongBo { get; set; }
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderPhone { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
}
