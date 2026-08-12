namespace ServiceDesk.Domain.Catalog;

public static class CompanyCatalogPolicy
{
    public static bool BelongsToCompany(ICatalogItem item, Guid companyId) =>
        item.CompanyId == companyId;
}
