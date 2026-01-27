using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Services.Common;
using FarmazonDemo.Services.Security;
using Microsoft.Extensions.Options;

namespace FarmazonDemo.Services.Files
{
    public class FileService : IFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly FileUploadSettings _settings;
        private readonly IInputSanitizerService _sanitizer;
        private readonly IWebHostEnvironment _environment;

        public FileService(
            ApplicationDbContext context,
            IOptions<FileUploadSettings> settings,
            IInputSanitizerService sanitizer,
            IWebHostEnvironment environment)
        {
            _context = context;
            _settings = settings.Value;
            _sanitizer = sanitizer;
            _environment = environment;
        }

        public async Task<FileUploadResponseDto> UploadAsync(IFormFile file, string folder = "general")
        {
            ValidateFile(file);

            var fileId = Guid.NewGuid().ToString("N");
            var extension = Path.GetExtension(file.FileName).ToLower();
            var sanitizedFileName = _sanitizer.SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var fileName = $"{fileId}_{sanitizedFileName}{extension}";

            var folderPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", folder);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            var relativeUrl = $"/uploads/{folder}/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResponseDto
            {
                FileId = fileId,
                Url = relativeUrl,
                FileName = fileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length
            };
        }

        public async Task<FileUploadResponseDto> UploadProductImageAsync(IFormFile file, int productId)
        {
            // Validate it's an image
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!_settings.AllowedImageExtensions.Contains(extension))
                throw new BadRequestException($"Invalid image format. Allowed: {string.Join(", ", _settings.AllowedImageExtensions)}");

            var result = await UploadAsync(file, $"products/{productId}");

            // Save to database
            var productImage = new ProductImage
            {
                Url = result.Url,
                ProductId = productId,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                AltText = Path.GetFileNameWithoutExtension(file.FileName)
            };

            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();

            return result;
        }

        public async Task DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            var fullPath = GetFullPath(fileUrl);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Also remove from database if it's a product image
            var productImage = _context.ProductImages.FirstOrDefault(pi => pi.Url == fileUrl);
            if (productImage != null)
            {
                _context.ProductImages.Remove(productImage);
                await _context.SaveChangesAsync();
            }
        }

        public Task<bool> ExistsAsync(string fileUrl)
        {
            var fullPath = GetFullPath(fileUrl);
            return Task.FromResult(File.Exists(fullPath));
        }

        public string GetFullPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;

            // Remove leading slash if present
            if (relativePath.StartsWith('/'))
                relativePath = relativePath.Substring(1);

            return Path.Combine(_environment.WebRootPath ?? "wwwroot", relativePath);
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("No file provided");

            if (file.Length > _settings.MaxFileSizeBytes)
                throw new BadRequestException($"File size exceeds limit of {_settings.MaxFileSizeBytes / 1024 / 1024}MB");

            var extension = Path.GetExtension(file.FileName).ToLower();
            var allAllowedExtensions = _settings.AllowedImageExtensions
                .Concat(_settings.AllowedDocumentExtensions)
                .ToArray();

            if (!allAllowedExtensions.Contains(extension))
                throw new BadRequestException($"Invalid file type. Allowed: {string.Join(", ", allAllowedExtensions)}");

            // Check for malicious content in filename
            if (_sanitizer.ContainsMaliciousContent(file.FileName))
                throw new BadRequestException("Invalid file name");
        }
    }
}
