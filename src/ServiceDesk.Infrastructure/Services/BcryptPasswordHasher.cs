using Microsoft.AspNetCore.Identity;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher<ApplicationUser>
{
    public string HashPassword(ApplicationUser user, string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user,
        string hashedPassword,
        string providedPassword)
    {
        if (!IsBcryptHash(hashedPassword))
        {
            return PasswordVerificationResult.Failed;
        }

        bool isValid = BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);

        return isValid
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }

    private static bool IsBcryptHash(string hashedPassword) =>
        hashedPassword.StartsWith("$2a$", StringComparison.Ordinal)
        || hashedPassword.StartsWith("$2b$", StringComparison.Ordinal)
        || hashedPassword.StartsWith("$2y$", StringComparison.Ordinal);
}
