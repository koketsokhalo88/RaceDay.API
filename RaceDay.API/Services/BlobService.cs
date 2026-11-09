using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDayAPI.Services;

public class BlobService
{
    private readonly BlobContainerClient _container;
    public BlobService(IConfiguration config)
    {
        var client = new BlobServiceClient(config["AzureBlob:ConnectionString"]);
        _container = client.GetBlobContainerClient(config["AzureBlob:Container"] ?? "images");
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
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
