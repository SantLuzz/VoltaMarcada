namespace VoltaMarcada.Web.Routing;

public static class LocalReturnUrl
{
    public static string Normalize(string? returnUrl, string fallback)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            || Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return fallback;
        }

        return returnUrl;
    }

    public static string GetCurrentPath(Uri currentUri, Uri baseUri)
    {
        var relativePath = baseUri.MakeRelativeUri(currentUri).ToString();
        return Normalize('/' + relativePath, "/");
    }
}
