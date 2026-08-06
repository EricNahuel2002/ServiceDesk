using ServiceDesk.Domain.Common;

namespace ServiceDesk.UnitTests;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_NewInstance_HasDefaultIdAndNoAuditDates()
    {
        var entity = new TestEntity();

        Assert.Equal(Guid.Empty, entity.Id);
        Assert.Equal(default, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
    }

    private sealed class TestEntity : BaseEntity;
}
