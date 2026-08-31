using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace TheRichScript_WinUI.Helpers;

/// <summary>
/// Configures LiveCharts2 global theme to match WinUI 3 app styling.
/// Call <see cref="Configure"/> once at app startup.
/// </summary>
public static class LiveChartsConfig
{
    public static void Configure()
    {
        LiveCharts.Configure(config =>
            config
                .AddDarkTheme()
        );
    }
}
