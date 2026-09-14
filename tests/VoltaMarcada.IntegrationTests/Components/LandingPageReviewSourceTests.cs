using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class LandingPageReviewSourceTests
{
    [Fact]
    public void LandingRevealStyles_ShouldLeaveContentVisibleUntilJavaScriptEnablesMotion()
    {
        var styles = ReadLandingFile("styles.css");
        var script = ReadLandingFile("landing.js");

        Assert.Contains(".js [data-reveal]", styles);
        Assert.DoesNotMatch(@"(?m)^\s*\[data-reveal\]\s*\{\s*opacity:\s*0", styles);
        Assert.Contains("document.documentElement.classList.add('js')", script);
    }

    [Fact]
    public void LandingMockups_ShouldNotExposeDecorativeControlsAsButtons()
    {
        var html = ReadLandingFile("index.html");

        Assert.Single(Regex.Matches(html, "<button", RegexOptions.CultureInvariant).Cast<Match>());
        Assert.Contains("class=\"menu-toggle\"", html);
        Assert.DoesNotContain("+ Novo serviço</button>", html);
        Assert.DoesNotContain("Enviar preferência</button>", html);
        Assert.DoesNotContain("Enviar link</button>", html);
        Assert.DoesNotContain("<div class=\"browser-frame hero-preview\" data-reveal aria-label", html);
        Assert.DoesNotContain("<div class=\"phone-frame\" data-reveal aria-label", html);
        Assert.Contains("class=\"browser-frame hero-preview\" aria-hidden=\"true\"", html);
        Assert.Contains("class=\"phone-frame\" aria-hidden=\"true\"", html);
        Assert.Contains("class=\"reminder-stack\" aria-hidden=\"true\"", html);
    }

    [Fact]
    public void LandingPage_ShouldProvideAllDocumentedMediaSlotsWithSafeFallbacks()
    {
        var html = ReadLandingFile("index.html");
        var styles = ReadLandingFile("styles.css");

        foreach (var asset in new[]
                 {
                     "hero-dashboard.webp",
                     "maintenance-reminders.webp",
                     "public-request-mobile.webp",
                     "schedule.webp",
                     "work-order.webp",
                     "quote.webp"
                 })
        {
            Assert.Contains($"assets/{asset}", html);
        }

        Assert.True(Regex.Matches(html, "<picture", RegexOptions.CultureInvariant).Count >= 6);
        Assert.Contains("fetchpriority=\"high\"", html);
        Assert.Contains("loading=\"lazy\"", html);
        Assert.Contains("width=\"1600\" height=\"1000\"", html);
        Assert.Contains("closest('picture').hidden = true", html);
        Assert.Contains(".replaceable-media[hidden] { display: none; }", styles);
    }

    private static string ReadLandingFile(string fileName) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "landing", fileName), Encoding.UTF8);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root could not be found.");
    }
}