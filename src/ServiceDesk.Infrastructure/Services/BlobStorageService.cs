using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Configuration;

namespace ServiceDesk.Infrastructure.Services;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobStorageSettings _settings;
    private readonly BlobContainerClient? _container;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IOptions<BlobStorageSettings> settings,
        ILogger<BlobStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (_settings.Enabled)
        {
            _container = new BlobServiceClient(_settings.ConnectionString)
                .GetBlobContainerClient(_settings.ContainerName);
        }
    }

    public async Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetContainer(out BlobContainerClient container))
        {
            return;
        }

        BlobClient blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetContainer(out BlobContainerClient container))
        {
            throw new InvalidOperationException(
                "Blob Storage no está configurado. Verifique la sección BlobStorage.");
        }

        BlobClient blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            throw new RequestFailedException($"El blob {blobName} no existe.");
        }

        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        if (!TryGetContainer(out BlobContainerClient container))
        {
            return;
        }

        BlobClient blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        if (!TryGetContainer(out BlobContainerClient container))
        {
            _logger.LogWarning(
                "Blob Storage deshabilitado, no se verifica la existencia del contenedor {ContainerName}.",
                _settings.ContainerName);

            return;
        }

        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    private bool TryGetContainer(out BlobContainerClient container)
    {
        container = _container!;

        if (_container is not null)
        {
            return true;
        }

        _logger.LogWarning(
            "Blob Storage deshabilitado, se omitió la operación solicitada.");

        return false;
    }
}
