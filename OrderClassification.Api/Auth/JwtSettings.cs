namespace OrderClassification.Api.Auth;

/// <summary>
/// JWT settings loaded from configuration.
///
/// Python analogy: a settings object loaded from environment variables.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
}

