namespace Backend.Configuration;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "OnnoRokom.AssignmentApi";

    public string Audience { get; init; } = "OnnoRokom.AdminClient";

    public string Secret { get; init; } = string.Empty;
}
