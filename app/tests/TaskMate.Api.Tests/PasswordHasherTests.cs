using TaskMate.Api.Services;

namespace TaskMate.Api.Tests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesSaltedHash_AndVerifiesCorrectPassword()
    {
        const string password = "a-strong-private-password";

        var hash = _hasher.Hash(password);

        Assert.StartsWith("v1.600000.", hash);
        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_RejectsIncorrectPassword()
    {
        var hash = _hasher.Hash("correct-password");

        Assert.False(_hasher.Verify("incorrect-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("v1.not-a-number.salt.hash")]
    [InlineData("v2.600000.c2FsdA==.aGFzaA==")]
    [InlineData("v1.600000.@@@.###")]
    public void Verify_RejectsMalformedHash(string malformedHash)
    {
        Assert.False(_hasher.Verify("some-password", malformedHash));
    }
}
