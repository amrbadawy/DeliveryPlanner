using SoftwareDeliveryPlanner.Web.Services.Heatmap;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="HeatmapColorPalette.GetCellColor"/>. Verifies the
/// five utilization buckets and their boundary conditions, including the
/// defensive negative-value guard (which differs from the original razor
/// implementation that would have leaked negatives into the light-load bucket).
/// </summary>
public class HeatmapColorPaletteTests
{
    // ---------- Idle bucket: pct <= 0 ----------

    [Fact]
    public void GetCellColor_returns_idle_for_zero()
    {
        Assert.Equal(HeatmapColorPalette.IdleColor, HeatmapColorPalette.GetCellColor(0));
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(-50)]
    [InlineData(-1_000_000)]
    public void GetCellColor_returns_idle_for_negative_values(double pct)
    {
        // Defensive: negatives are non-physical for utilization. Original razor
        // implementation only matched the literal 0 and would have fallen
        // through to the light-load bucket for any negative value.
        Assert.Equal(HeatmapColorPalette.IdleColor, HeatmapColorPalette.GetCellColor(pct));
    }

    // ---------- Light load bucket: (0, 50] ----------

    [Theory]
    [InlineData(0.0001)]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(49.9999)]
    [InlineData(50)]
    public void GetCellColor_returns_light_load_for_pct_above_zero_through_fifty(double pct)
    {
        Assert.Equal(HeatmapColorPalette.LightLoadColor, HeatmapColorPalette.GetCellColor(pct));
    }

    // ---------- Healthy bucket: (50, 80] ----------

    [Theory]
    [InlineData(50.0001)]
    [InlineData(65)]
    [InlineData(79.9999)]
    [InlineData(80)]
    public void GetCellColor_returns_healthy_for_pct_above_fifty_through_eighty(double pct)
    {
        Assert.Equal(HeatmapColorPalette.HealthyColor, HeatmapColorPalette.GetCellColor(pct));
    }

    // ---------- Stretched bucket: (80, 100] ----------

    [Theory]
    [InlineData(80.0001)]
    [InlineData(90)]
    [InlineData(99.9999)]
    [InlineData(100)]
    public void GetCellColor_returns_stretched_for_pct_above_eighty_through_hundred(double pct)
    {
        Assert.Equal(HeatmapColorPalette.StretchedColor, HeatmapColorPalette.GetCellColor(pct));
    }

    // ---------- Overallocated bucket: > 100 ----------

    [Theory]
    [InlineData(100.0001)]
    [InlineData(125)]
    [InlineData(200)]
    [InlineData(double.MaxValue)]
    public void GetCellColor_returns_overallocated_for_pct_above_hundred(double pct)
    {
        Assert.Equal(HeatmapColorPalette.OverallocatedColor, HeatmapColorPalette.GetCellColor(pct));
    }

    // ---------- Output contract: always a CSS variable expression ----------

    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(65)]
    [InlineData(95)]
    [InlineData(150)]
    public void GetCellColor_always_returns_css_var_expression(double pct)
    {
        var result = HeatmapColorPalette.GetCellColor(pct);
        Assert.StartsWith("var(--color-", result);
        Assert.EndsWith(")", result);
    }

    // ---------- All five exposed constants are distinct ----------

    [Fact]
    public void Bucket_constants_are_all_distinct()
    {
        var all = new[]
        {
            HeatmapColorPalette.IdleColor,
            HeatmapColorPalette.LightLoadColor,
            HeatmapColorPalette.HealthyColor,
            HeatmapColorPalette.StretchedColor,
            HeatmapColorPalette.OverallocatedColor,
        };
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    // ---------- Sanity: NaN routes to overallocated (last switch arm) ----------

    [Fact]
    public void GetCellColor_routes_NaN_to_overallocated_via_default_arm()
    {
        // NaN compares false against every numeric pattern, so it falls through
        // to the catch-all `_` arm. This documents (rather than endorses) the
        // current behaviour; callers should never pass NaN.
        Assert.Equal(HeatmapColorPalette.OverallocatedColor, HeatmapColorPalette.GetCellColor(double.NaN));
    }
}
