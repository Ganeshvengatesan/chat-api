using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using ChatApplicationAPI.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ChatApplicationAPI.Services;

public class S3StorageService : IS3StorageService
{
    private readonly ILogger<S3StorageService> _logger;
    private readonly string? _accessKey;
    private readonly string? _secretKey;
    private readonly string _regionName;
    private readonly string _bucketName;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_accessKey) &&
                                 !string.IsNullOrWhiteSpace(_secretKey) &&
                                 !string.IsNullOrWhiteSpace(_bucketName);

    public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
    {
        _logger = logger;

        // Read from Environment Variables first, fallback to Configuration
        _accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") 
                     ?? configuration["AWS_ACCESS_KEY_ID"] 
                     ?? configuration["AWS:AccessKeyId"];

        _secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") 
                     ?? configuration["AWS_SECRET_ACCESS_KEY"] 
                     ?? configuration["AWS:SecretAccessKey"];

        var rawRegion = Environment.GetEnvironmentVariable("AWS_REGION") 
                        ?? configuration["AWS_REGION"] 
                        ?? configuration["AWS:Region"] 
                        ?? "us-east-1";

        _bucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME") 
                      ?? configuration["AWS_BUCKET_NAME"] 
                      ?? configuration["AWS:BucketName"] 
                      ?? "linkup-chat-storage";

        _regionName = ExtractRegionSystemName(rawRegion);

        if (IsConfigured)
        {
            _logger.LogInformation("AWS S3 Storage Service initialized successfully for bucket '{Bucket}' in region '{Region}'.", _bucketName, _regionName);
        }
        else
        {
            _logger.LogWarning("AWS S3 credentials not fully configured. Falling back to local storage.");
        }
    }

    public async Task<string?> UploadFileAsync(IFormFile file, string folder)
    {
        if (!IsConfigured || file == null || file.Length == 0)
        {
            return null;
        }

        try
        {
            var credentials = new BasicAWSCredentials(_accessKey, _secretKey);
            var region = RegionEndpoint.GetBySystemName(_regionName);
            using var s3Client = new AmazonS3Client(credentials, region);

            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExt}";
            var cleanFolder = folder.Trim('/');
            var s3Key = $"{cleanFolder}/{fileName}";

            using var stream = file.OpenReadStream();

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = stream,
                ContentType = GetContentType(fileExt, file.ContentType),
                AutoCloseStream = true
            };

            var response = await s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var s3Url = $"https://{_bucketName}.s3.{_regionName}.amazonaws.com/{s3Key}";
                _logger.LogInformation("Successfully uploaded file to AWS S3: {Url}", s3Url);
                return s3Url;
            }

            _logger.LogError("AWS S3 returned non-OK status code: {StatusCode}", response.HttpStatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to AWS S3 bucket {Bucket}.", _bucketName);
            return null;
        }
    }

    private static string ExtractRegionSystemName(string rawRegion)
    {
        if (string.IsNullOrWhiteSpace(rawRegion)) return "us-east-1";

        // Handle full strings like "US East (N. Virginia) us-east-1" -> "us-east-1"
        var match = Regex.Match(rawRegion, @"[a-z]{2}-[a-z]+-\d");
        if (match.Success)
        {
            return match.Value;
        }

        return rawRegion.Trim().ToLowerInvariant();
    }

    private static string GetContentType(string extension, string defaultContentType)
    {
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/m4a",
            ".ogg" => "audio/ogg",
            _ => string.IsNullOrWhiteSpace(defaultContentType) ? "application/octet-stream" : defaultContentType
        };
    }
}
