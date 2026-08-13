namespace ServiceDesk.Application.Common.Interfaces;

public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyDictionary<string, string[]> Errors)
{
    public static IdentityOperationResult Success() => new(true, new Dictionary<string, string[]>());
}
