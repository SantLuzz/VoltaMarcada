using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Abstractions.Routing;

namespace VoltaMarcada.Web.Configuration;

public sealed class ApplicationUrlOptions
{
    public const string SectionName = "ApplicationUrls";

    public string ApplicationBaseUrl { get; init; } = "http://localhost:5225";

    public string LandingPageUrl { get; init; } = "http://localhost:5225";
}

public sealed class ApplicationUrlBuilder(IOptions<ApplicationUrlOptions> options) : IApplicationUrlBuilder
{
    public string LandingPageUrl => options.Value.LandingPageUrl;

    public string BuildApplicationUrl(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !Uri.TryCreate(relativePath, UriKind.Relative, out _))
        {
            throw new ArgumentException("A relative application path is required.", nameof(relativePath));
        }

        return new Uri(new Uri(options.Value.ApplicationBaseUrl, UriKind.Absolute), relativePath.TrimStart('/')).ToString();
    }
}
