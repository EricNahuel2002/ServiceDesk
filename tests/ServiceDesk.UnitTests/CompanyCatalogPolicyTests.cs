using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.UnitTests;

public sealed class CompanyCatalogPolicyTests
{
    [Fact]
    public void BelongsToCompany_ItemOfSameCompany_ReturnsTrue()
    {
        Guid companyId = Guid.NewGuid();
        Category category = CreateCategory(companyId);

        bool result = CompanyCatalogPolicy.BelongsToCompany(category, companyId);

        Assert.True(result);
    }

    [Fact]
    public void BelongsToCompany_ItemOfAnotherCompany_ReturnsFalse()
    {
        Category category = CreateCategory(Guid.NewGuid());

        bool result = CompanyCatalogPolicy.BelongsToCompany(category, Guid.NewGuid());

        Assert.False(result);
    }

    private static Category CreateCategory(Guid companyId) =>
        new() { Id = Guid.NewGuid(), CompanyId = companyId, Name = "Hardware", IsActive = true };
}
