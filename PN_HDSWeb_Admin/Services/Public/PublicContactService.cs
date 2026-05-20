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
    private readonly IPublicSiteSettingService _siteSettingService;

    public PublicContactService(ILogger<PublicContactService> logger, IPublicSiteSettingService siteSettingService)
    {
        _logger = logger;
        _siteSettingService = siteSettingService;
    }

    public async Task<PublicContactInfo> GetContactAsync(string maTruongBo)
    {
        var sql = $@"
            SELECT tentruong, phuongxa, thongtin
            FROM l_truong
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            if (dt.Rows.Count == 0)
            {
                var fallbackContact = new PublicContactInfo { MaTruongBo = maTruongBo };
                var fallbackSettings = await _siteSettingService.GetSettingsAsync(maTruongBo);
                ApplySettings(fallbackContact, fallbackSettings);
                return fallbackContact;
            }

            var row = dt.Rows[0];
            var contact = new PublicContactInfo
            {
                MaTruongBo = maTruongBo,
                SchoolName = row["tentruong"]?.ToString(),
                Address = row["phuongxa"]?.ToString(),
                MapEmbedUrl = string.Empty
            };

            ApplyContactJson(contact, row["thongtin"]?.ToString());

            var settings = await _siteSettingService.GetSettingsAsync(maTruongBo);
            ApplySettings(contact, settings);

            return contact;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetContactAsync failed. Contact page will use site settings fallback.");

            var fallbackContact = new PublicContactInfo { MaTruongBo = maTruongBo };
            var fallbackSettings = await _siteSettingService.GetSettingsAsync(maTruongBo);
            ApplySettings(fallbackContact, fallbackSettings);
            return fallbackContact;
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

    private static void ApplySettings(PublicContactInfo contact, IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Count == 0)
            return;

        contact.SchoolName = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_name", "site_name"), contact.SchoolName);
        contact.Address = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_address"), contact.Address);
        contact.LogoUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_logo"), contact.LogoUrl);
        contact.Phone = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_phone", "contact_phone", "contact_hotline"), contact.Phone);
        contact.Email = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_email", "contact_email"), contact.Email);
        contact.WebsiteUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_website"), contact.WebsiteUrl);
        contact.FacebookUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_facebook"), contact.FacebookUrl);
        contact.YoutubeUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_youtube"), contact.YoutubeUrl);
        contact.ZaloUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_zalo"), contact.ZaloUrl);
        contact.MapEmbedUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "contact_map_url"), contact.MapEmbedUrl);
    }

    private static void ApplyContactJson(PublicContactInfo contact, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            contact.SchoolName = FirstNonBlank(GetString(root, "school_name"), GetString(root, "ten_truong"), contact.SchoolName);
            contact.Address = FirstNonBlank(GetString(root, "school_address"), GetString(root, "diachi_truong"), GetString(root, "address"), contact.Address);
            contact.LogoUrl = FirstNonBlank(GetString(root, "school_logo"), GetString(root, "logo_truong"), GetString(root, "logo_url"), contact.LogoUrl);
            contact.LeaderName = FirstNonBlank(GetString(root, "leader_name"), GetString(root, "ho_ten"), contact.LeaderName);
            contact.LeaderPhone = FirstNonBlank(GetString(root, "leader_phone"), GetString(root, "so_dien_thoai_hieu_truong"), contact.LeaderPhone);
            contact.Phone = FirstNonBlank(GetString(root, "school_phone"), GetString(root, "contact_phone"), GetString(root, "contact_hotline"), GetString(root, "so_dien_thoai"), contact.Phone, contact.LeaderPhone);
            contact.Email = FirstNonBlank(GetString(root, "school_email"), GetString(root, "contact_email"), GetString(root, "email"), contact.Email);
            contact.WebsiteUrl = FirstNonBlank(GetString(root, "school_website"), GetString(root, "website_url"), contact.WebsiteUrl);
            contact.FacebookUrl = FirstNonBlank(GetString(root, "contact_facebook"), GetString(root, "facebook_url"), contact.FacebookUrl);
            contact.YoutubeUrl = FirstNonBlank(GetString(root, "contact_youtube"), GetString(root, "youtube_url"), contact.YoutubeUrl);
            contact.ZaloUrl = FirstNonBlank(GetString(root, "contact_zalo"), GetString(root, "zalo_url"), contact.ZaloUrl);
            contact.MapEmbedUrl = FirstNonBlank(GetString(root, "contact_map_url"), GetString(root, "map_embed_url"), contact.MapEmbedUrl);
        }
        catch
        {
            // Invalid school JSON should not block the public contact page.
        }
    }

    private static string? GetString(System.Text.Json.JsonElement root, string key)
        => root.TryGetProperty(key, out var value) ? value.ToString() : null;

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
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
