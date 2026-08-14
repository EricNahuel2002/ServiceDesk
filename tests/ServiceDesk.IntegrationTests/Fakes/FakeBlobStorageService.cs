using System.Collections.Concurrent;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.IntegrationTests.Fakes;

public sealed class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _blobs = new();

    public Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using MemoryStream memory = new();
        content.CopyTo(memory);

        _blobs[blobName] = (memory.ToArray(), contentType);

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (!_blobs.TryGetValue(blobName, out (byte[] Content, string ContentType) blob))
        {
            throw new InvalidOperationException($"El blob {blobName} no existe.");
        }

        return Task.FromResult<Stream>(new MemoryStream(blob.Content));
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(blobName, out _);

        return Task.CompletedTask;
    }

    public Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
