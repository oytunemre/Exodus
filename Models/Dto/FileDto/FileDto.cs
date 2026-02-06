namespace Exodus.Models.Dto
{
    public class FileUploadResponseDto
    {
        public required string FileId { get; set; }
        public required string Url { get; set; }
        public string? ThumbnailUrl { get; set; }
        public required string FileName { get; set; }
        public required string ContentType { get; set; }
        public long FileSizeBytes { get; set; }
    }

    public class FileUploadSettings
    {
        public string UploadPath { get; set; } = "wwwroot/uploads";
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
        public string[] AllowedImageExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        public string[] AllowedDocumentExtensions { get; set; } = new[] { ".pdf", ".doc", ".docx" };
        public int ThumbnailWidth { get; set; } = 200;
        public int ThumbnailHeight { get; set; } = 200;
    }
}
