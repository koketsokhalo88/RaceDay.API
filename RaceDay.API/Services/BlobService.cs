using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services;

public class BlobService
{
    private readonly BlobContainerClient _container;

    public BlobService(IConfiguration config)
    {
        var conn = config["AzureBlob:ConnectionString"];
        if (string.IsNullOrWhiteSpace(conn) || !conn.Contains("AccountKey=") && !conn.Contains("UseDevelopmentStorage"))
            conn = "UseDevelopmentStorage=true";
        conn = "UseDevelopmentStorage=true";

        var client = new BlobServiceClient(conn);
        _container = client.GetBlobContainerClient(config["AzureBlob:Container"] ?? "images");
    }

    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{folder}/{Guid.NewGuid():N}{ext}";
        var blob = _container.GetBlobClient(name);
        await blob.UploadAsync(file.OpenReadStream(), overwrite: true);
        return blob.Uri.ToString();
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        return await UploadImageAsync(file, "images");
    }

    public async Task DeleteAsync(string? url)
    {
        // TODO: implement real delete later
        await Task.CompletedTask;
    }
}
