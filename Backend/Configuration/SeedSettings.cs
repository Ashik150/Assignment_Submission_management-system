namespace Backend.Configuration;

public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public string AdminName { get; init; } = "System Administrator";

    public string AdminEmail { get; init; } = "admin@example.com";

    public string AdminPassword { get; init; } = string.Empty;
}
