using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SmartGym.Api.DTOs.Facility;

namespace SmartGym.Api.Services;

public interface IImageStorageService
{
    Task<IssueImageDto> SaveIssueImageAsync(Guid issueId, IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);
}

public class ImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageStorageService> _logger;

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public ImageStorageService(IWebHostEnvironment environment, ILogger<ImageStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<IssueImageDto> SaveIssueImageAsync(Guid issueId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Uploaded image file cannot be empty.", nameof(file));
        }

        // 1. Validate File Size
        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException($"File size ({file.Length / 1024} KB) exceeds the maximum allowed limit of 5 MB.");
        }

        // 2. Validate Extension
        var rawFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(rawFileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File extension '{extension}' is not supported. Allowed formats: JPG, JPEG, PNG, WEBP.");
        }

        // 3. Validate Content-Type
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ArgumentException($"Content type '{file.ContentType}' is not permitted. Only image uploads are allowed.");
        }

        // 4. Secure File Path & Directory
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "issues");

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Sanitize original file name
        var sanitizedOriginal = string.Concat(rawFileName.Split(Path.GetInvalidFileNameChars()));
        var safeStoredFileName = $"issue_{issueId:N}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsDir, safeStoredFileName);

        // 5. Save File
        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativeUrl = $"/uploads/issues/{safeStoredFileName}";

        _logger.LogInformation("Image saved successfully for issue {IssueId} at {RelativeUrl} ({Size} bytes)",
            issueId, relativeUrl, file.Length);

        return new IssueImageDto
        {
            Id = Guid.NewGuid(),
            IssueId = issueId,
            ImageUrl = relativeUrl,
            ThumbnailUrl = relativeUrl,
            FileSizeBytes = file.Length,
            ContentType = file.ContentType,
            OriginalFileName = sanitizedOriginal,
            UploadedAt = DateTime.UtcNow
        };
    }

    public Task<bool> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return Task.FromResult(false);

            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var normalizedPath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, normalizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete image at {ImageUrl}", imageUrl);
        }

        return Task.FromResult(false);
    }
}
