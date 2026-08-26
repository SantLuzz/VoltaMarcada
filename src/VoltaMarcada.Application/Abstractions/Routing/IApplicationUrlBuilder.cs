namespace VoltaMarcada.Application.Abstractions.Routing;

public interface IApplicationUrlBuilder
{
    string LandingPageUrl { get; }

    string BuildApplicationUrl(string relativePath);
}
