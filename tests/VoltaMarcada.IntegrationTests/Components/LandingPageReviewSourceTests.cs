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
                     "hero-dashboard.png",
                     "maintenance-reminders.png",
                     "public-request-mobile.png",
                     "schedule.png",
                     "work-order.png",
                     "quote.png"
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

    [Fact]
    public void MaintenanceReminderScreenshot_ShouldPreserveTheFullImage()
    {
        var styles = ReadLandingFile("styles.css");

        Assert.Contains(".reminder-media .replaceable-media img { object-fit: contain; }", styles);
    }
    [Fact]
    public void EquipmentHistoryScreenshot_ShouldUseTheProvidedPngAsset()
    {
        var html = ReadLandingFile("index.html");

        Assert.Contains("assets/equipment-history.png", html);
    }
    [Fact]
    public void LandingResponsiveLayout_ShouldKeepTabletNavigationAndFlowReadable()
    {
        var html = ReadLandingFile("index.html");
        var styles = ReadLandingFile("styles.css");

        Assert.Contains("max-width: 63.999rem", styles);
        Assert.Contains(".recurrence-cycle { grid-template-columns: repeat(7, minmax(0, 1fr));", styles);
        Assert.Contains(".hero-media { aspect-ratio: 8 / 5; }", styles);
        Assert.Contains(".hero-media .app-preview { min-height: 0; }", styles);
        Assert.Equal(8, Regex.Matches(html, "class=\"connection-step\"", RegexOptions.CultureInvariant).Count);
    }

    [Fact]
    public void HeroContent_ShouldRemainWithinNarrowViewports()
    {
        var styles = ReadLandingFile("styles.css");

        Assert.Contains("@media (max-width: 23rem)", styles);
        Assert.Contains(".hero-actions { display: grid; gap: .75rem; }", styles);
        Assert.Contains(".hero-grid { grid-template-columns: minmax(0, 1fr); }", styles);
        Assert.Contains(".hero-actions .button { width: 100%; }", styles);
        Assert.Contains(".section[id] { scroll-margin-top: 5.5rem; }", styles);
        Assert.Contains(".container { width: min(100% - 3rem, 72rem); }", styles);
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