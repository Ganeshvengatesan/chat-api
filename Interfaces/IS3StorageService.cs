using Microsoft.AspNetCore.Http;

namespace ChatApplicationAPI.Interfaces;

public interface IS3StorageService
{
    bool IsConfigured { get; }
    Task<string?> UploadFileAsync(IFormFile file, string folder);
}
