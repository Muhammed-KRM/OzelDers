using OzelDers.Business.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace OzelDers.Business.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        // Dosya adını benzersiz yap
        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var filePath = Path.Combine(_basePath, uniqueName);

        // Resmi ImageSharp ile Yükle ve İşle
        using var image = await Image.LoadAsync(fileStream);

        // Max 500x500 px (En-boy oranını koruyarak)
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(500, 500)
        }));

        // Kayıt formatı belirleme (Önerilen format WebP)
        if (contentType == "image/png" || contentType == "image/webp")
        {
            await image.SaveAsWebpAsync(filePath, new WebpEncoder { Quality = 80 });
        }
        else
        {
            await image.SaveAsJpegAsync(filePath, new JpegEncoder { Quality = 80 });
        }

        // Relative URL döndür (frontend tarafından erişilebilir)
        return $"/uploads/{uniqueName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(_basePath, fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
