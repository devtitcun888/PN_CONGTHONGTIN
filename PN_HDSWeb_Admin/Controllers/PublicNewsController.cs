using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PN_HDSWeb_Admin.Services.Public;
using PN_HDSWeb_Library;

namespace PN_HDSWeb_Admin.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/public")]
    public class PublicNewsController : ControllerBase
    {
        private readonly IPublicPostCategoryService _categoryService;
        private readonly IPublicPostService _postService;
        private readonly IPublicPostTagService _tagService;
        private readonly IPublicPostMediaService _mediaService;
        private readonly IPublicDocumentService _documentService;
        private readonly IPublicStaffProfileService _staffProfileService;

        public PublicNewsController(
            IPublicPostCategoryService categoryService,
            IPublicPostService postService,
            IPublicPostTagService tagService,
            IPublicPostMediaService mediaService,
            IPublicDocumentService documentService,
            IPublicStaffProfileService staffProfileService)
        {
            _categoryService = categoryService;
            _postService = postService;
            _tagService = tagService;
            _mediaService = mediaService;
            _documentService = documentService;
            _staffProfileService = staffProfileService;
        }

        #region 1. Danh mục bài viết (Categories)

        [HttpGet("posts/categories")]
        public async Task<IActionResult> GetCategories([FromQuery] string? maTruongBo)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var categories = await _categoryService.GetCategoriesAsync(targetMaTruong);
            return Ok(categories);
        }

        [HttpGet("posts/categories/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug(string slug, [FromQuery] string? maTruongBo)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var category = await _categoryService.GetCategoryBySlugAsync(targetMaTruong, slug);
            if (category == null)
            {
                return NotFound(new { message = $"Không tìm thấy danh mục với slug: {slug}" });
            }
            return Ok(category);
        }

        #endregion

        #region 2. Danh sách tin tức & bài viết (Posts)

        [HttpGet("posts")]
        public async Task<IActionResult> GetPosts(
            [FromQuery] string? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;

            var postsTask = _postService.GetPostsAsync(targetMaTruong, keyword, categoryId, page, pageSize);
            var countTask = _postService.GetPostsCountAsync(targetMaTruong, keyword, categoryId);

            await Task.WhenAll(postsTask, countTask);

            var posts = await postsTask;
            var totalItems = await countTask;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Ok(new
            {
                items = posts,
                totalItems,
                page,
                pageSize,
                totalPages
            });
        }

        [HttpGet("posts/{slug}")]
        public async Task<IActionResult> GetPostBySlug(string slug, [FromQuery] string? maTruongBo)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var post = await _postService.GetPostBySlugAsync(targetMaTruong, slug);
            if (post == null)
            {
                return NotFound(new { message = $"Không tìm thấy bài viết với slug: {slug}" });
            }

            var tagsTask = _tagService.GetTagsByPostIdAsync(post.Id ?? string.Empty);
            var mediaTask = _mediaService.GetMediaAsync(targetMaTruong, post.Id ?? string.Empty);

            await Task.WhenAll(tagsTask, mediaTask);

            return Ok(new
            {
                id = post.Id,
                title = post.Title,
                slug = post.Slug,
                summary = post.Summary,
                content = post.Content,
                coverImageUrl = post.CoverImageUrl,
                publishAt = post.PublishAt,
                viewCount = post.ViewCount,
                categoryId = post.CategoryId,
                categoryName = post.CategoryName,
                categorySlug = post.CategorySlug,
                tags = await tagsTask,
                attachments = await mediaTask
            });
        }

        [HttpGet("posts/{slug}/related")]
        public async Task<IActionResult> GetRelatedPosts(string slug, [FromQuery] int take = 4, [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var post = await _postService.GetPostBySlugAsync(targetMaTruong, slug);
            if (post == null)
            {
                return NotFound(new { message = $"Không tìm thấy bài viết với slug: {slug}" });
            }

            var related = await _postService.GetRelatedPostsAsync(targetMaTruong, post.CategoryId, post.Id ?? string.Empty, take);
            return Ok(related);
        }

        [HttpGet("posts/popular")]
        public async Task<IActionResult> GetPopularPosts([FromQuery] int take = 8, [FromQuery] string? excludePostId = null, [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var popular = await _postService.GetMostViewedPostsAsync(targetMaTruong, take, excludePostId);
            return Ok(popular);
        }

        [HttpGet("posts/tag/{tagSlug}")]
        public async Task<IActionResult> GetPostsByTag(string tagSlug, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var tag = await _tagService.GetTagBySlugAsync(targetMaTruong, tagSlug);
            if (tag == null)
            {
                return NotFound(new { message = $"Không tìm thấy tag với slug: {tagSlug}" });
            }

            var posts = await _postService.GetPostsByTagIdAsync(targetMaTruong, tag.Id ?? string.Empty, page, pageSize);
            return Ok(posts);
        }

        [HttpPost("posts/{postId}/view")]
        public async Task<IActionResult> IncrementViewCount(string postId, [FromQuery] string? maTruongBo)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var success = await _postService.IncrementPostViewAsync(targetMaTruong, postId);
            return Ok(new { success });
        }

        #endregion

        #region 3. Văn bản (Documents)

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] string? keyword,
            [FromQuery] string? documentTypeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;

            var docsTask = _documentService.GetDocumentsAsync(targetMaTruong, keyword, documentTypeId, page, pageSize);
            var countTask = _documentService.GetDocumentsCountAsync(targetMaTruong, keyword, documentTypeId);

            await Task.WhenAll(docsTask, countTask);

            var docs = await docsTask;
            var totalItems = await countTask;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var itemsMapped = docs.Select(d => new
            {
                id = d.Id,
                title = d.DocTitle,
                documentNumber = d.DocNumber,
                issuedAt = d.IssuedDate,
                fileUrl = d.FileUrl,
                documentTypeId = d.DocumentTypeId,
                typeName = d.TypeName,
                typeSlug = d.TypeSlug
            });

            return Ok(new
            {
                items = itemsMapped,
                totalItems,
                page,
                pageSize,
                totalPages
            });
        }

        [HttpGet("documents/{id}")]
        public async Task<IActionResult> GetDocumentById(string id, [FromQuery] string? maTruongBo)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;
            var doc = await _documentService.GetDocumentByIdAsync(targetMaTruong, id);
            if (doc == null)
            {
                return NotFound(new { message = $"Không tìm thấy văn bản với id: {id}" });
            }

            return Ok(new
            {
                id = doc.Id,
                title = doc.DocTitle,
                documentNumber = doc.DocNumber,
                description = doc.Summary,
                content = doc.Content,
                issuedAt = doc.IssuedDate,
                fileUrl = doc.FileUrl,
                documentTypeId = doc.DocumentTypeId,
                typeName = doc.TypeName,
                typeSlug = doc.TypeSlug,
                issuer = doc.Issuer
            });
        }

        #endregion

        #region 4. Sơ đồ tổ chức & Danh bạ (Staff Profiles)

        [HttpGet("staff-profiles")]
        public async Task<IActionResult> GetStaffProfiles(
            [FromQuery] string? keyword,
            [FromQuery] string? departmentId,
            [FromQuery] string? maTruongBo = null)
        {
            var targetMaTruong = string.IsNullOrWhiteSpace(maTruongBo) ? PN_PublicVariables.MaTruong : maTruongBo;

            List<PublicStaffProfileItem> profiles;
            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                profiles = await _staffProfileService.GetPublicStaffProfilesByGroupAsync(targetMaTruong, departmentId, keyword);
            }
            else
            {
                profiles = await _staffProfileService.GetPublicStaffProfilesAsync(targetMaTruong, keyword);
            }

            var result = profiles.Select(p => new
            {
                id = p.Id,
                fullName = p.FullName,
                position = p.PositionName,
                department = p.GroupName,
                email = p.Email,
                phone = p.Phone,
                avatarUrl = p.AvatarUrl,
                sortOrder = p.SortOrder,
                qualification = p.Qualification,
                certificateInfo = p.CertificateInfo,
                bio = p.Bio
            });

            return Ok(result);
        }

        #endregion
    }
}
