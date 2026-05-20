using hDataLibraryN8;
using Microsoft.Extensions.Logging;
using PN_HDSWeb_Library;
using System.Data;
using System.Text.Json;

namespace PN_HDSWeb_Admin.Services.Public;

public interface IPublicHomepageService
{
    Task<PublicHomepageViewModel> GetHomepageAsync(string maTruongBo);
}

public class PublicHomepageService : IPublicHomepageService
{
    private static readonly string LoginID_Index = PN_LoginService.LoginID_CongThongTin;
    private readonly ILogger<PublicHomepageService> _logger;
    private readonly IPublicSiteSettingService _siteSettingService;

    public PublicHomepageService(ILogger<PublicHomepageService> logger, IPublicSiteSettingService siteSettingService)
    {
        _logger = logger;
        _siteSettingService = siteSettingService;
    }

    public async Task<PublicHomepageViewModel> GetHomepageAsync(string maTruongBo)
    {
        var model = new PublicHomepageViewModel();
        try
        {
            var settings = await _siteSettingService.GetSettingsAsync(maTruongBo);
            ApplySettings(model, settings);
            model.SchoolIntro = await GetSchoolIntroAsync(maTruongBo);
            model.SchoolIntro ??= CreateSchoolIntroFromSettings(maTruongBo, settings);
            if (model.SchoolIntro != null)
                ApplySchoolSettings(model.SchoolIntro, settings);

            if (model.FeatureBannersEnabled)
            {
                model.Banners = await GetBannersAsync(maTruongBo, "HomeTop");
                model.HomeMiddleBanners = await GetBannersAsync(maTruongBo, "HomeMiddle");
                model.HomeBottomBanners = await GetBannersAsync(maTruongBo, "HomeBottom");
                model.SidebarBanners = await GetBannersAsync(maTruongBo, "Sidebar");
            }

            if (model.FeatureNewsEnabled)
            {
                model.FeaturedPosts = await GetFeaturedPostsAsync(maTruongBo, model.HomepageFeaturedPostsLimit);
                model.LatestPosts = await GetLatestPostsAsync(maTruongBo, model.HomepageLatestPostsLimit);
            }

            if (model.FeatureDocumentsEnabled)
            {
                model.PublishedDocuments = await GetPublishedDocumentsAsync(maTruongBo, model.HomepageDocumentsLimit);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHomepageAsync failed");
            throw;
        }

        return model;
    }

    private async Task<PublicSchoolIntroInfo?> GetSchoolIntroAsync(string maTruongBo)
    {
        var sql = $@"
            SELECT to_jsonb(t) AS school_data
            FROM l_truong t
            WHERE t.ma_truong_bo = '{Escape(maTruongBo)}'
            LIMIT 1";

        try
        {
            var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            return ParseSchoolIntroRow(row["school_data"]?.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetSchoolIntroAsync failed. Homepage will render without school intro data.");
            return null;
        }
    }

    private async Task<List<PublicBannerItem>> GetBannersAsync(string maTruongBo, string position)
    {
        var list = new List<PublicBannerItem>();
        var sql = $@"
            SELECT id, title, image_url, link_url, position, sort_order
            FROM banners
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND is_active = TRUE
              AND position = '{Escape(position)}'
            ORDER BY sort_order ASC, created_at DESC";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new PublicBannerItem
            {
                Id = row["id"]?.ToString(),
                Title = row["title"]?.ToString(),
                ImageUrl = row["image_url"]?.ToString(),
                LinkUrl = row["link_url"]?.ToString(),
                Position = row["position"]?.ToString(),
                SortOrder = row["sort_order"] == DBNull.Value ? 0 : Convert.ToInt32(row["sort_order"])
            });
        }
        return list;
    }

    private async Task<List<PublicPostItem>> GetFeaturedPostsAsync(string maTruongBo, int limit)
    {
        var list = new List<PublicPostItem>();
        var safeLimit = ClampLimit(limit, 6);
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
              AND is_featured = TRUE
            ORDER BY publish_at DESC, created_at DESC
            LIMIT {safeLimit}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(MapPost(row));
        }
        return list;
    }

    private async Task<List<PublicPostItem>> GetLatestPostsAsync(string maTruongBo, int limit)
    {
        var list = new List<PublicPostItem>();
        var safeLimit = ClampLimit(limit, 10);
        var sql = $@"
            SELECT id, title, slug, summary, cover_image_url, publish_at
            FROM posts
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            ORDER BY publish_at DESC, created_at DESC
            LIMIT {safeLimit}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(MapPost(row));
        }
        return list;
    }

    private async Task<List<PublicDocumentItem>> GetPublishedDocumentsAsync(string maTruongBo, int limit)
    {
        var list = new List<PublicDocumentItem>();
        var safeLimit = ClampLimit(limit, 10);
        var sql = $@"
            SELECT id, doc_title, doc_number, file_url, issued_date
            FROM documents
            WHERE ma_truong_bo = '{Escape(maTruongBo)}'
              AND is_deleted = FALSE
              AND status = 'Published'
            ORDER BY issued_date DESC, created_at DESC
            LIMIT {safeLimit}";

        var dt = await hdataLib.hgetDataTableAsync(LoginID_Index, sql);
        foreach (DataRow row in dt.Rows)
        {
            list.Add(new PublicDocumentItem
            {
                Id = row["id"]?.ToString(),
                DocTitle = row["doc_title"]?.ToString(),
                DocNumber = row["doc_number"]?.ToString(),
                FileUrl = row["file_url"]?.ToString(),
                IssuedDate = row["issued_date"] == DBNull.Value ? null : Convert.ToDateTime(row["issued_date"])
            });
        }
        return list;
    }

    private static PublicPostItem MapPost(DataRow row) => new()
    {
        Id = row["id"]?.ToString(),
        Title = row["title"]?.ToString(),
        Slug = row["slug"]?.ToString(),
        Summary = row["summary"]?.ToString(),
        CoverImageUrl = row["cover_image_url"]?.ToString(),
        PublishAt = row["publish_at"] == DBNull.Value ? null : Convert.ToDateTime(row["publish_at"])
    };

    private static void ApplySettings(PublicHomepageViewModel model, IReadOnlyDictionary<string, string> settings)
    {
        model.SiteName = PublicSiteSettingReader.First(settings, "site_name", "school_name");
        model.SiteTitle = PublicSiteSettingReader.First(settings, "seo_title", "site_name", "school_name");
        model.SiteSlogan = PublicSiteSettingReader.First(settings, "site_slogan");
        model.SiteLogo = PublicSiteSettingReader.First(settings, "school_logo");
        model.ContactHotline = PublicSiteSettingReader.First(settings, "contact_hotline", "contact_phone", "school_phone");
        model.ContactEmail = PublicSiteSettingReader.First(settings, "contact_email", "school_email");
        model.ContactAddress = PublicSiteSettingReader.First(settings, "school_address");

        model.HomepageFeaturedPostsLimit = PublicSiteSettingReader.Int(settings, model.HomepageFeaturedPostsLimit, 1, 12, "homepage_featured_posts_limit");
        model.HomepageLatestPostsLimit = PublicSiteSettingReader.Int(settings, model.HomepageLatestPostsLimit, 1, 24, "homepage_latest_posts_limit");
        model.HomepageDocumentsLimit = PublicSiteSettingReader.Int(settings, model.HomepageDocumentsLimit, 1, 24, "homepage_documents_limit");
        model.FeatureNewsEnabled = PublicSiteSettingReader.Bool(settings, model.FeatureNewsEnabled, "feature_news_enabled");
        model.FeatureDocumentsEnabled = PublicSiteSettingReader.Bool(settings, model.FeatureDocumentsEnabled, "feature_documents_enabled");
        model.FeatureBannersEnabled = PublicSiteSettingReader.Bool(settings, model.FeatureBannersEnabled, "feature_banners_enabled");
        model.FeatureSearchEnabled = PublicSiteSettingReader.Bool(settings, model.FeatureSearchEnabled, "feature_search_enabled");
    }

    private static int ClampLimit(int limit, int fallback)
        => Math.Clamp(limit <= 0 ? fallback : limit, 1, 24);

    private static PublicSchoolIntroInfo? ParseSchoolIntroRow(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var intro = new PublicSchoolIntroInfo
            {
                MaTruongBo = GetCleanString(root, "ma_truong_bo"),
                SchoolName = FirstNonBlank(GetCleanString(root, "tentruong"), GetCleanString(root, "school_name")),
                Address = FirstNonBlank(GetCleanString(root, "diachi_truong"), GetCleanString(root, "phuongxa"), GetCleanString(root, "school_address")),
                LogoUrl = FirstNonBlank(GetCleanString(root, "logo_truong"), GetCleanString(root, "school_logo"), GetCleanString(root, "img_base64")),
                LeaderName = FirstNonBlank(GetNestedString(root, "hieutruong", "ho_ten"), GetCleanString(root, "ho_ten"), GetCleanString(root, "leader_name")),
                Phone = FirstNonBlank(GetNestedString(root, "hieutruong", "so_dien_thoai"), GetCleanString(root, "so_dien_thoai"), GetCleanString(root, "contact_phone"), GetCleanString(root, "hotline")),
                Email = FirstNonBlank(GetNestedString(root, "hieutruong", "email"), GetCleanString(root, "email"), GetCleanString(root, "school_email")),
                WebsiteUrl = FirstNonBlank(GetCleanString(root, "website_url"), GetCleanString(root, "school_website")),
                LevelText = FormatLevelText(root),
                MorningTime = FormatTimeRange(GetCleanString(root, "tgbatdaubuoisang"), GetCleanString(root, "tgketthucbuoisang")),
                AfternoonTime = FormatTimeRange(GetCleanString(root, "tgbatdaubuoichieu"), GetCleanString(root, "tgketthucbuoichieu"))
            };

            intro.StudyTimeText = FormatStudyTimeText(intro.MorningTime, intro.AfternoonTime);
            ApplyIntroJson(intro, GetJsonPayload(root, "thongtin"));
            return intro;
        }
        catch
        {
            return null;
        }
    }

    private static PublicSchoolIntroInfo? CreateSchoolIntroFromSettings(string maTruongBo, IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Count == 0)
            return null;

        var intro = new PublicSchoolIntroInfo { MaTruongBo = maTruongBo };
        ApplySchoolSettings(intro, settings);
        return string.IsNullOrWhiteSpace(intro.SchoolName)
            && string.IsNullOrWhiteSpace(intro.IntroText)
            && string.IsNullOrWhiteSpace(intro.Address)
                ? null
                : intro;
    }

    private static void ApplySchoolSettings(PublicSchoolIntroInfo intro, IReadOnlyDictionary<string, string> settings)
    {
        if (settings.Count == 0)
            return;

        ApplyIntroJson(intro, PublicSiteSettingReader.First(settings, "footer_json_info", "school_json_info"));

        intro.SchoolName = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_name", "site_name"), intro.SchoolName);
        intro.IntroText = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_intro", "site_slogan", "footer_text"), intro.IntroText);
        intro.Address = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_address"), intro.Address);
        intro.LogoUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_logo"), intro.LogoUrl);
        intro.Phone = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_phone", "contact_phone", "contact_hotline"), intro.Phone);
        intro.Email = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_email", "contact_email"), intro.Email);
        intro.WebsiteUrl = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_website", "website_url"), intro.WebsiteUrl);
        intro.LeaderName = FirstNonBlank(PublicSiteSettingReader.First(settings, "school_leader", "leader_name"), intro.LeaderName);
    }

    private static void ApplyIntroJson(PublicSchoolIntroInfo intro, string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            intro.IntroText = FirstNonBlank(
                GetCleanString(root, "gioi_thieu"),
                GetCleanString(root, "intro_text"),
                GetCleanString(root, "mo_ta"),
                GetCleanString(root, "description"),
                GetCleanString(root, "footer_text"),
                intro.IntroText);

            intro.Address = FirstNonBlank(GetCleanString(root, "diachi_truong"), GetCleanString(root, "school_address"), intro.Address);
            intro.LogoUrl = FirstNonBlank(GetCleanString(root, "logo_truong"), GetCleanString(root, "school_logo"), intro.LogoUrl);
            intro.Phone = FirstNonBlank(GetCleanString(root, "so_dien_thoai"), GetCleanString(root, "contact_phone"), GetCleanString(root, "contact_hotline"), intro.Phone);
            intro.Email = FirstNonBlank(GetCleanString(root, "email"), GetCleanString(root, "school_email"), intro.Email);
            intro.WebsiteUrl = FirstNonBlank(GetCleanString(root, "website_url"), GetCleanString(root, "school_website"), intro.WebsiteUrl);
            intro.LeaderName = FirstNonBlank(GetCleanString(root, "ho_ten"), GetCleanString(root, "leader_name"), intro.LeaderName);
            intro.LevelText = FirstNonBlank(GetCleanString(root, "level_text"), GetCleanString(root, "cap_hoc"), intro.LevelText);
            intro.StudyTimeText = FirstNonBlank(GetCleanString(root, "study_time"), GetCleanString(root, "school_time"), intro.StudyTimeText);
        }
        catch
        {
            // Invalid school JSON should not block rendering the homepage intro box.
        }
    }

    private static string? GetCleanString(JsonElement root, string key)
        => root.TryGetProperty(key, out var value) ? CleanText(value.ToString()) : null;

    private static string? GetNestedString(JsonElement root, string parentKey, string childKey)
    {
        if (!root.TryGetProperty(parentKey, out var parent) || parent.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        if (parent.ValueKind == JsonValueKind.Object)
            return GetCleanString(parent, childKey);

        var raw = CleanText(parent.ToString());
        if (string.IsNullOrWhiteSpace(raw) || !raw.TrimStart().StartsWith("{", StringComparison.Ordinal))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            return GetCleanString(doc.RootElement, childKey);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetJsonPayload(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            return value.GetRawText();

        return CleanText(value.ToString());
    }

    private static string? FormatLevelText(JsonElement root)
    {
        var levels = GetStringValues(root, "caphoc").ToList();
        if (levels.Count == 0)
            levels = GetStringValues(root, "cap").ToList();

        var labels = levels
            .Select(MapLevelLabel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return labels.Count == 0 ? null : string.Join(", ", labels);
    }

    private static IEnumerable<string> GetStringValues(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return [];

        if (value.ValueKind == JsonValueKind.Array)
            return value.EnumerateArray()
                .Select(item => CleanText(item.ToString()))
                .Where(item => !string.IsNullOrWhiteSpace(item))!;

        var raw = CleanText(value.ToString());
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw.Trim('{', '}')
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CleanText)
            .Where(item => !string.IsNullOrWhiteSpace(item))!;
    }

    private static string? MapLevelLabel(string value)
    {
        var code = value.Trim().Trim('"');
        return code switch
        {
            "1" or "01" => "Mầm non",
            "2" or "02" => "Tiểu học",
            "3" or "03" => "THCS",
            "4" or "04" => "THPT",
            _ => code
        };
    }

    private static string? FormatTimeRange(string? start, string? end)
    {
        var startText = FormatTime(start);
        var endText = FormatTime(end);

        if (!string.IsNullOrWhiteSpace(startText) && !string.IsNullOrWhiteSpace(endText))
            return $"{startText} - {endText}";

        return FirstNonBlank(startText, endText);
    }

    private static string? FormatStudyTimeText(string? morning, string? afternoon)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(morning))
            parts.Add($"Sáng {morning}");
        if (!string.IsNullOrWhiteSpace(afternoon))
            parts.Add($"Chiều {afternoon}");

        return parts.Count == 0 ? null : string.Join(" | ", parts);
    }

    private static string? FormatTime(string? value)
    {
        var text = CleanText(value);
        if (string.IsNullOrWhiteSpace(text) || text == "00:00:00" || text == "00:00")
            return null;

        return TimeSpan.TryParse(text, out var time)
            ? $"{time.Hours:00}:{time.Minutes:00}"
            : text;
    }

    private static string? CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim();
        if (text.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return null;

        return string.IsNullOrWhiteSpace(text.Replace(",", string.Empty))
            ? null
            : text;
    }

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

public class PublicHomepageViewModel
{
    public List<PublicBannerItem> Banners { get; set; } = [];
    public List<PublicBannerItem> HomeMiddleBanners { get; set; } = [];
    public List<PublicBannerItem> HomeBottomBanners { get; set; } = [];
    public List<PublicBannerItem> SidebarBanners { get; set; } = [];
    public List<PublicPostItem> FeaturedPosts { get; set; } = [];
    public List<PublicPostItem> LatestPosts { get; set; } = [];
    public List<PublicDocumentItem> PublishedDocuments { get; set; } = [];
    public PublicSchoolIntroInfo? SchoolIntro { get; set; }

    public string? SiteName { get; set; }
    public string? SiteTitle { get; set; }
    public string? SiteSlogan { get; set; }
    public string? SiteLogo { get; set; }
    public string? ContactHotline { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactAddress { get; set; }
    public int HomepageFeaturedPostsLimit { get; set; } = 6;
    public int HomepageLatestPostsLimit { get; set; } = 10;
    public int HomepageDocumentsLimit { get; set; } = 10;
    public bool FeatureNewsEnabled { get; set; } = true;
    public bool FeatureDocumentsEnabled { get; set; } = true;
    public bool FeatureBannersEnabled { get; set; } = true;
    public bool FeatureSearchEnabled { get; set; } = true;
}

public class PublicSchoolIntroInfo
{
    public string? MaTruongBo { get; set; }
    public string? SchoolName { get; set; }
    public string? IntroText { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? LeaderName { get; set; }
    public string? LevelText { get; set; }
    public string? MorningTime { get; set; }
    public string? AfternoonTime { get; set; }
    public string? StudyTimeText { get; set; }
}

public class PublicBannerItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? Position { get; set; }
    public int SortOrder { get; set; }
}

public class PublicPostItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime? PublishAt { get; set; }
}

public class PublicDocumentItem
{
    public string? Id { get; set; }
    public string? DocTitle { get; set; }
    public string? DocNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedDate { get; set; }
}
