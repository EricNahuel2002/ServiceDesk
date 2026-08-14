using System.Collections.Concurrent;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.IntegrationTests.Fakes;

public sealed class FakeQueueStorageService : IQueueStorageService
{
    private readonly ConcurrentQueue<string> _messages = new();

    public Task EnqueueAsync(string message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);

        return Task.CompletedTask;
    }

    public IReadOnlyList<string> Messages => _messages.ToArray();

    public void Clear() => _messages.Clear();
}
