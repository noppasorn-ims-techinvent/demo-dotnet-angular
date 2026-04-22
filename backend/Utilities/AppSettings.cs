namespace backend.Utilities;

public sealed class AppSettings
{
    public const string SectionName = "AppSettings";

    public ErrorMessageSettings ErrorMessage { get; set; } = new();

    public SuccessMessageSettings SuccessMessage { get; set; } = new();

    public JwtTokenAppSettings Jwt { get; set; } = new();
}

public sealed class SuccessMessageSettings
{
    public string Success { get; set; } = string.Empty;
}

public sealed class JwtTokenAppSettings
{
    public string Secret { get; set; } = string.Empty;

    public int ExpireInSec { get; set; } = 3600;
}

public sealed class ErrorMessageSettings
{
    public string General { get; set; } = string.Empty;

    public string NotFound { get; set; } = string.Empty;

    public string Unauthorized { get; set; } = string.Empty;

    public string EmailAlreadyRegistered { get; set; } = string.Empty;

    public string InvalidCredentials { get; set; } = string.Empty;

    public string InvalidOrderLines { get; set; } = string.Empty;
}
