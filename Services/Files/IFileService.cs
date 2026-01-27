using FarmazonDemo.Models.Dto;

namespace FarmazonDemo.Services.Files
{
    public interface IFileService
    {
        Task<FileUploadResponseDto> UploadAsync(IFormFile file, string folder = "general");
        Task<FileUploadResponseDto> UploadProductImageAsync(IFormFile file, int productId);
        Task DeleteAsync(string fileUrl);
        Task<bool> ExistsAsync(string fileUrl);
        string GetFullPath(string relativePath);
    }
}
