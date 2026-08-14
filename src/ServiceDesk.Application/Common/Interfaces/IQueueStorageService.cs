namespace ServiceDesk.Application.Common.Interfaces;

public interface IQueueStorageService
{
    Task EnqueueAsync(string message, CancellationToken cancellationToken = default);
}
