using MudBlazor;

namespace VoltaMarcada.Web.Components.Theme;

public static class VoltaMarcadaTheme
{
    public static MudTheme Instance { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0969F0",
            Secondary = "#08BDF2",
            Tertiary = "#071D49",
            Background = "#F6F8FC",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#172033",
            TextPrimary = "#172033",
            TextSecondary = "#64748B",
            LinesDefault = "#E2E8F0",
            Success = "#16A34A",
            Warning = "#F59E0B",
            Error = "#DC2626"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Segoe UI", "Arial", "sans-serif"],
                FontSize = "0.9375rem"
            },
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.25" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.35" },
            H6 = new H6Typography { FontSize = "1rem", FontWeight = "600", LineHeight = "1.4" }
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px" }
    };
}
