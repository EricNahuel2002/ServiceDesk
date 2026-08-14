using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Configuration;

namespace ServiceDesk.Infrastructure.Services;

public sealed class QueueStorageService : IQueueStorageService
{
    private readonly QueueStorageSettings _settings;
    private readonly QueueServiceClient? _serviceClient;
    private readonly ILogger<QueueStorageService> _logger;

    public QueueStorageService(
        IOptions<QueueStorageSettings> settings,
        ILogger<QueueStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (_settings.Enabled)
        {
            _serviceClient = new QueueServiceClient(_settings.ConnectionString);
        }
    }

    public async Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!TryGetQueue(out QueueClient queue))
        {
            return;
        }

        await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        try
        {
            Azure.Response<Azure.Storage.Queues.Models.SendReceipt> response =
                await queue.SendMessageAsync(message, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Mensaje encolado en {QueueName} con MessageId {MessageId}",
                _settings.QueueName,
                response.Value.MessageId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Falló el envío de mensaje a la cola {QueueName}",
                _settings.QueueName);
        }
    }

    private bool TryGetQueue(out QueueClient queue)
    {
        if (_serviceClient is null)
        {
            queue = null!;

            _logger.LogWarning(
                "Queue Storage deshabilitado, se omitió la operación solicitada.");

            return false;
        }

        queue = _serviceClient.GetQueueClient(_settings.QueueName);

        return true;
    }
}
