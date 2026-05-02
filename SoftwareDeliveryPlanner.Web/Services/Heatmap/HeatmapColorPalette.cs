namespace SoftwareDeliveryPlanner.Web.Services.Heatmap;

/// <summary>
/// Pure colour-bucket palette for the workload heatmap. No DI, no DbContext,
/// no IMediator. Buckets are inclusive on the upper edge:
///   • 0%        → bg-secondary (idle)
///   • (0,  50]  → info-light    (light load)
///   • (50, 80]  → success-light (healthy)
///   • (80, 100] → warning-light (stretched)
///   • (100, ∞)  → danger-light  (overallocated)
///
/// Negative values are treated as 0% (defensive — UI should never produce them
/// but a test-only fault path could).
/// </summary>
public static class HeatmapColorPalette
{
    /// <summary>CSS variable name for the idle (0%) bucket.</summary>
    public const string IdleColor = "var(--color-bg-secondary)";

    /// <summary>CSS variable name for the (0, 50]% bucket.</summary>
    public const string LightLoadColor = "var(--color-info-light)";

    /// <summary>CSS variable name for the (50, 80]% bucket.</summary>
    public const string HealthyColor = "var(--color-success-light)";

    /// <summary>CSS variable name for the (80, 100]% bucket.</summary>
    public const string StretchedColor = "var(--color-warning-light)";

    /// <summary>CSS variable name for the &gt;100% bucket.</summary>
    public const string OverallocatedColor = "var(--color-danger-light)";

    /// <summary>
    /// Returns the CSS variable expression for the given utilization percent.
    /// Always emits a CSS custom-property reference, never a literal colour.
    /// </summary>
    public static string GetCellColor(double pct) => pct switch
    {
        <= 0 => IdleColor,
        <= 50 => LightLoadColor,
        <= 80 => HealthyColor,
        <= 100 => StretchedColor,
        _ => OverallocatedColor
    };
}
