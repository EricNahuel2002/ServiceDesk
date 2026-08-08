using Microsoft.AspNetCore.Identity;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.UnitTests;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ReturnsBcryptHash()
    {
        string hash = _sut.HashPassword(new ApplicationUser(), "Password123");

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void HashPassword_SamePasswordProducesDifferentHashes()
    {
        ApplicationUser user = new();

        string first = _sut.HashPassword(user, "Password123");
        string second = _sut.HashPassword(user, "Password123");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void VerifyHashedPassword_CorrectPassword_ReturnsSuccess()
    {
        string hash = _sut.HashPassword(new ApplicationUser(), "Password123");

        PasswordVerificationResult result = _sut.VerifyHashedPassword(new ApplicationUser(), hash, "Password123");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPassword_WrongPassword_ReturnsFailed()
    {
        string hash = _sut.HashPassword(new ApplicationUser(), "Password123");

        PasswordVerificationResult result = _sut.VerifyHashedPassword(new ApplicationUser(), hash, "WrongPassword");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void VerifyHashedPassword_NonBcryptHash_ReturnsFailedInsteadOfThrowing()
    {
        const string legacyPbkdf2Hash = "AQAAAAEAACcQAAAAEBDpXW1VnZP4kP0zF7dG9mY2xlY2lwZXJlYQ==";

        PasswordVerificationResult result = _sut.VerifyHashedPassword(
            new ApplicationUser(),
            legacyPbkdf2Hash,
            "Password123");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
