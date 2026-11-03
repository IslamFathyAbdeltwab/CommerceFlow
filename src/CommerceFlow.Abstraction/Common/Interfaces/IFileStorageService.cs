namespace CommerceFlow.Application.Common.Interfaces;

public interface IFileStorageService
{
    // Returns the storage key, which is what ProductImage.StorageKey holds.
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    string GetPublicUrl(string storageKey);
}