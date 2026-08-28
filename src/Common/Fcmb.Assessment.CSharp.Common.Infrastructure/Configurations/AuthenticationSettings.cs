namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Configurations;

public sealed record AuthenticationSettings
{
    public const string Path = "Authentication";

    public string Audience { get; init; }
    public TokenValidationParameterSettings TokenValidationParameters { get; init; }
    public string MetadataAddress { get; init; }
    public bool RequireHttpsMetadata { get; init; }
}

public sealed record TokenValidationParameterSettings
{
    public List<string> ValidIssuers { get; init; }
}
