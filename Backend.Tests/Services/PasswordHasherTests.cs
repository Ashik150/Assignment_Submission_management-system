using Backend.Services;

namespace Backend.Tests.Services;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher hasher = new();

    [Fact]
    public void Hash_CanBeVerifiedWithOriginalPassword()
    {
        var hash = hasher.Hash("CorrectHorseBatteryStaple!");

        Assert.True(hasher.Verify("CorrectHorseBatteryStaple!", hash));
    }

    [Fact]
    public void Verify_RejectsWrongPassword()
    {
        var hash = hasher.Hash("CorrectPassword123!");

        Assert.False(hasher.Verify("WrongPassword123!", hash));
    }

    [Fact]
    public void Hash_UsesUniqueSaltForEveryPassword()
    {
        var first = hasher.Hash("SamePassword123!");
        var second = hasher.Hash("SamePassword123!");

        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify("SamePassword123!", first));
        Assert.True(hasher.Verify("SamePassword123!", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-password-hash")]
    [InlineData("iterations.invalid-base64.invalid-base64")]
    [InlineData("0.YWJjZGVmZ2hpamtsbW5vcA==.YWJjZGVmZ2hpamtsbW5vcA==")]
    [InlineData("1000001.YWJjZGVmZ2hpamtsbW5vcA==.YWJjZGVmZ2hpamtsbW5vcA==")]
    [InlineData("120000.YQ==.YQ==")]
    public void Verify_RejectsMalformedStoredHashes(string storedHash)
    {
        Assert.False(hasher.Verify("AnyPassword123!", storedHash));
    }
}
