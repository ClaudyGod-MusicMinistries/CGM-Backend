namespace ClaudyGod.Infrastructure.Configuration;

/// <summary>Bound from the "Storage:Website" config section (env: Storage__Website__*).</summary>
public class WebsiteStorageOptions
{
    public const string SectionName = "Storage:Website";

    public string S3Endpoint { get; set; } = string.Empty;
    public string S3Region { get; set; } = string.Empty;
    public string S3AccessKeyId { get; set; } = string.Empty;
    public string S3SecretAccessKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(S3Endpoint) &&
        !string.IsNullOrWhiteSpace(S3AccessKeyId) &&
        !string.IsNullOrWhiteSpace(S3SecretAccessKey) &&
        !string.IsNullOrWhiteSpace(Bucket) &&
        !string.IsNullOrWhiteSpace(PublicBaseUrl);

    /// <summary>Every missing key, by config path — used to build a clear boot-failure message rather than a generic "not configured".</summary>
    public IEnumerable<string> MissingKeys()
    {
        if (string.IsNullOrWhiteSpace(S3Endpoint)) yield return $"{SectionName}:S3Endpoint";
        if (string.IsNullOrWhiteSpace(S3AccessKeyId)) yield return $"{SectionName}:S3AccessKeyId";
        if (string.IsNullOrWhiteSpace(S3SecretAccessKey)) yield return $"{SectionName}:S3SecretAccessKey";
        if (string.IsNullOrWhiteSpace(Bucket)) yield return $"{SectionName}:Bucket";
        if (string.IsNullOrWhiteSpace(PublicBaseUrl)) yield return $"{SectionName}:PublicBaseUrl";
    }
}
