using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PN_HDSWeb_Admin.Services.Admin;

namespace PN_HDSWeb_Admin.Controllers;

[ApiController]
[Route("api/admin/editor-upload")]
[Authorize(Roles = "Administrator")]
public class EditorUploadController : ControllerBase
{
    private readonly IAdminFileStorageService _fileStorage;

    public EditorUploadController(IAdminFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpPost("image")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "File upload không hợp lệ." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var bytes = new byte[file.Length];
            var read = await stream.ReadAsync(bytes, 0, bytes.Length);
            if (read <= 0)
            {
                return BadRequest(new { error = "Không thể đọc file upload." });
            }

            var url = await _fileStorage.SaveImageAsync(bytes, file.FileName, file.ContentType, "posts");
            if (string.IsNullOrWhiteSpace(url))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Không thể lưu ảnh." });
            }

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
