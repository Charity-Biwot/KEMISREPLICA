
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace KemisOnboarding.Api.Services;
public interface IFileStorageService
{
    Task<StoredFile> SaveFileAsync(
        IFormFile file,
        string category,
        CancellationToken cancellationToken = default);

    string? GetFilePath(string relativePath);

    Task<bool> DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default);
}

public sealed class StoredFile
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 25 * 1024 * 1024; // 25 MB

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Documents
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx",
            ".txt",
            ".csv",

            // Images
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".bmp",

            // Archives
            ".zip",
            ".rar",
            ".7z"
        };

    public FileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<StoredFile> SaveFileAsync(
        IFormFile file,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Please select a file to upload.");

        if (file.Length > MaxFileSize)
            throw new ArgumentException("File size cannot exceed 25 MB.");

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                "This file type is not supported.");
        }

        var safeCategory = SanitizeCategory(category);

        var year = DateTime.UtcNow.Year.ToString();
        var month = DateTime.UtcNow.Month.ToString("D2");

        var relativeFolder = Path.Combine(
            "uploads",
            safeCategory,
            year,
            month);

        var uploadFolder = Path.Combine(
            _environment.ContentRootPath,
            relativeFolder);

        Directory.CreateDirectory(uploadFolder);

        var storedFileName =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var fullPath = Path.Combine(
            uploadFolder,
            storedFileName);

        await using (var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return new StoredFile
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            RelativePath = Path.Combine(
                relativeFolder,
                storedFileName).Replace('\\', '/'),
            ContentType = file.ContentType,
            Size = file.Length
        };
    }

    public string? GetFilePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var normalisedPath = relativePath.Replace(
            '/',
            Path.DirectorySeparatorChar);

        var uploadsRoot = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, "uploads"));

        var fullPath = Path.GetFullPath(
            Path.Combine(_environment.ContentRootPath, normalisedPath));

        if (!fullPath.StartsWith(
            uploadsRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(fullPath) ? fullPath : null;
    }

    public Task<bool> DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = GetFilePath(relativePath);

        if (fullPath == null)
            return Task.FromResult(false);

        File.Delete(fullPath);

        return Task.FromResult(true);
    }

    private static string SanitizeCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("A file category is required.");

        var safeCategory = new string(
            category.Trim()
                .ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .ToArray());

        if (string.IsNullOrWhiteSpace(safeCategory))
            throw new ArgumentException("Invalid file category.");

        return safeCategory;
    }
}
