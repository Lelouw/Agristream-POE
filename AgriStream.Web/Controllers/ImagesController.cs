using AgriStream.Web.Models;
using AgriStream.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriStream.Web.Controllers;

[Authorize]
public class ImagesController(StorageService storage) : Controller
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png"];

    public async Task<IActionResult> Index() => View(await storage.ListImagesAsync());

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? image)
    {
        if (image is null || image.Length == 0)
        {
            TempData["Error"] = "Choose an image to upload.";
        }
        else if (!AllowedTypes.Contains(image.ContentType))
        {
            TempData["Error"] = "Only JPEG and PNG images are accepted.";
        }
        else if (image.Length > MaxBytes)
        {
            TempData["Error"] = "The image must be 10 MB or smaller.";
        }
        else
        {
            await storage.UploadImageAsync(image, User.Identity?.Name ?? "unknown");
            TempData["Success"] = "Drone image uploaded. It will be audited shortly.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Preview(string name)
    {
        var image = await storage.OpenImageAsync(Path.GetFileName(name));
        return image is null ? NotFound() : File(image.Value.Content, image.Value.ContentType);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Agronomist}")]
    public async Task<IActionResult> Audit() => View(await storage.GetAuditAsync());
}
