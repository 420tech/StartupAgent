using Microsoft.AspNetCore.Http;

namespace StartupAgent.Server.Services.Storage;

/// <summary>
/// Service for handling pitch deck file uploads
/// </summary>
public interface IDeckUploadService
{
    /// <summary>
    /// Validate and upload a pitch deck file
    /// </summary>
    Task<DeckUploadResult> UploadDeckAsync(
        IFormFile file,
        string founderId,
        string assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the file path for a deck
    /// </summary>
    string GetDeckFilePath(string founderId, string fileName);

    /// <summary>
    /// Delete a deck file
    /// </summary>
    Task<bool> DeleteDeckAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get deck file size in bytes
    /// </summary>
    Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default);
}

public class DeckUploadService : IDeckUploadService
{
    private readonly string _uploadPath;
    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".pptx", ".ppt" };
    private static readonly string[] AllowedMimeTypes = 
    { 
        "application/pdf",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    public DeckUploadService(IWebHostEnvironment environment)
    {
        // Store uploads in a dedicated folder
        _uploadPath = Path.Combine(environment.ContentRootPath, "uploads", "decks");
        
        // Ensure directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
            Console.WriteLine($"Created deck upload directory: {_uploadPath}");
        }
    }

    public async Task<DeckUploadResult> UploadDeckAsync(
        IFormFile file,
        string founderId,
        string assessmentId,
        CancellationToken cancellationToken = default)
    {
        var result = new DeckUploadResult
        {
            UploadedAt = DateTime.UtcNow
        };

        try
        {
            // Validate file exists
            if (file == null || file.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No file provided or file is empty";
                return result;
            }

            // Validate file size
            if (file.Length > MaxFileSizeBytes)
            {
                result.Success = false;
                result.ErrorMessage = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB";
                return result;
            }

            // Validate file extension
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(fileExtension))
            {
                result.Success = false;
                result.ErrorMessage = $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}";
                return result;
            }

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                result.Success = false;
                result.ErrorMessage = $"Invalid file content type: {file.ContentType}";
                return result;
            }

            // Generate safe file name
            var safeFileName = GenerateSafeFileName(file.FileName, founderId, assessmentId);
            var founderDirectory = Path.Combine(_uploadPath, founderId);
            
            // Create founder-specific directory
            if (!Directory.Exists(founderDirectory))
            {
                Directory.CreateDirectory(founderDirectory);
            }

            var filePath = Path.Combine(founderDirectory, safeFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            result.Success = true;
            result.FilePath = filePath;
            result.FileName = safeFileName;
            result.OriginalFileName = file.FileName;
            result.FileSizeBytes = file.Length;
            result.ContentType = file.ContentType;

            Console.WriteLine($"Deck uploaded successfully: {safeFileName} ({file.Length} bytes) for founder {founderId}");

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Upload failed: {ex.Message}";
            Console.Error.WriteLine($"Error uploading deck: {ex.Message}");
            return result;
        }
    }

    public string GetDeckFilePath(string founderId, string fileName)
    {
        var founderDirectory = Path.Combine(_uploadPath, founderId);
        return Path.Combine(founderDirectory, fileName);
    }

    public async Task<bool> DeleteDeckAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                Console.WriteLine($"Deleted deck file: {filePath}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deleting deck file: {ex.Message}");
            return false;
        }
    }

    public async Task<long> GetFileSizeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return await Task.FromResult(fileInfo.Length);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error getting file size: {ex.Message}");
            return 0;
        }
    }

    private static string GenerateSafeFileName(string originalFileName, string founderId, string assessmentId)
    {
        // Generate a safe filename with timestamp and IDs to prevent collisions
        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var sanitizedName = Path.GetFileNameWithoutExtension(originalFileName)
            .Replace(" ", "-")
            .Replace("_", "-");
        
        // Limit length and remove unsafe characters
        sanitizedName = new string(sanitizedName
            .Where(c => char.IsLetterOrDigit(c) || c == '-')
            .Take(50)
            .ToArray());

        return $"{sanitizedName}-{timestamp}-{assessmentId}{extension}";
    }
}

/// <summary>
/// Result of a deck upload operation
/// </summary>
public class DeckUploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string? ContentType { get; set; }
    public DateTime UploadedAt { get; set; }
}
