using System.Text;
using FluentAssertions;
using Infrastructure.Security;
using Xunit;

namespace Tests.Unit;

public class EvidenceAndSecurityTests
{
    [Fact]
    public void Sha256HashService_ShouldComputeAuthenticLowercaseHexHash()
    {
        var hashService = new Sha256HashService();
        var content = "Criminal Network Analysis System - Authentic Evidence Payload";
        var bytes = Encoding.UTF8.GetBytes(content);

        // Known SHA-256 for this exact byte sequence
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var expectedHash = Convert.ToHexString(sha256.ComputeHash(bytes)).ToLowerInvariant();

        // 1. From stream
        using var stream = new MemoryStream(bytes);
        var streamHash = hashService.ComputeSha256Hex(stream);
        streamHash.Should().Be(expectedHash);
        streamHash.Should().MatchRegex("^[a-f0-9]{64}$");

        // 2. From byte array
        var byteHash = hashService.ComputeSha256Hex(bytes);
        byteHash.Should().Be(expectedHash);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerifySuccessfully()
    {
        var password = "SecureOfficerPassword#2026!";
        var hash = PasswordHasher.HashPassword(password);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);

        PasswordHasher.VerifyPassword(hash, password).Should().BeTrue();
        PasswordHasher.VerifyPassword(hash, "WrongPassword").Should().BeFalse();
    }
}
