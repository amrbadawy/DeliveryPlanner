using SoftwareDeliveryPlanner.Domain;

namespace SoftwareDeliveryPlanner.Web.Components;

/// <summary>
/// Centralised Bootstrap badge CSS helpers.
/// All color decisions for status, risk, priority, adjustment type, and
/// holiday type live here. Update once — propagates everywhere.
/// </summary>
public static class BadgeHelpers
{
    // ── Task Status ──────────────────────────────────────────────────────────

    /// <summary>Returns the Bootstrap background class (e.g. "bg-success") for a task status.</summary>
    public static string StatusBg(string? status) => status switch
    {
        DomainConstants.TaskStatus.Completed  => "bg-success",
        DomainConstants.TaskStatus.InProgress => "bg-info",
        _                                     => "bg-secondary"
    };

    // ── Delivery Risk ────────────────────────────────────────────────────────

    /// <summary>Returns the full badge class string (bg + optional text modifier) for a delivery risk value.</summary>
    public static string RiskBadgeClass(string? risk) => risk switch
    {
        DomainConstants.DeliveryRisk.OnTrack => "bg-success",
        DomainConstants.DeliveryRisk.AtRisk  => "bg-warning text-dark",
        DomainConstants.DeliveryRisk.Late    => "bg-danger",
        _                                    => "bg-secondary"
    };

    // ── Priority ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a badge class for a numeric priority value.
    /// P1–P3 are highlighted; 0 / unset returns empty string (no badge rendered).
    /// </summary>
    public static string PriorityBadgeClass(int priority) => priority switch
    {
        1 => "bg-danger",
        2 => "bg-warning text-dark",
        3 => "bg-info",
        _ => "bg-secondary"
    };

    public static bool ShowPriorityBadge(int priority) => priority > 0;

    // ── Adjustment Type ──────────────────────────────────────────────────────

    /// <summary>Returns the full badge class string for an adjustment type.</summary>
    public static string AdjustmentTypeBadgeClass(string? adjType) => adjType switch
    {
        DomainConstants.AdjustmentType.Vacation  => "bg-info",
        DomainConstants.AdjustmentType.Training  => "bg-success",
        DomainConstants.AdjustmentType.SickLeave => "bg-warning text-dark",
        _                                        => "bg-secondary"
    };

    // ── Holiday Type ─────────────────────────────────────────────────────────

    /// <summary>Returns the full badge class string for a holiday type.</summary>
    public static string HolidayTypeBadgeClass(string? holidayType) => holidayType switch
    {
        DomainConstants.HolidayType.National  => "bg-success",
        DomainConstants.HolidayType.Religious => "bg-warning text-dark",
        DomainConstants.HolidayType.Company   => "bg-info",
        _                                     => "bg-secondary"
    };
}
