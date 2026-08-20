using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public bool ThrowOnSave { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowOnSave)
        {
            throw new InvalidOperationException("Fallo simulado al guardar.");
        }

        SaveCount++;

        return Task.FromResult(1);
    }
}