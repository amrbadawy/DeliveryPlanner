using SoftwareDeliveryPlanner.Web.Services;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for the pure, dependency-free helpers in the Task Filter feature:
/// - <see cref="SavedViewPayloadComparer"/> (drift detection)
/// - <see cref="TaskFilterState.PriorityBuckets"/> (priority bucketing)
///
/// TaskFilterState itself depends on NavigationManager (ASP.NET Core), so its
/// full behaviour is integration-tested by the Playwright e2e suite. The
/// dimension toggle logic (ToggleStatus, SelectAllDimension, etc.) is exercised
/// indirectly through those e2e specs.
/// </summary>
public class TaskFilterStateTests
{
    // ── SavedViewPayloadComparer ─────────────────────────────────────────────

    [Fact]
    public void Comparer_IdenticalStrings_ReturnsTrue()
    {
        var payload = """{"statuses":["IN_PROGRESS","NOT_STARTED"]}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(payload, payload));
    }

    [Fact]
    public void Comparer_EmptyObjects_ReturnsTrue()
    {
        Assert.True(SavedViewPayloadComparer.AreEqual("{}", "{}"));
    }

    [Fact]
    public void Comparer_BothNull_ReturnsFalse()
    {
        Assert.False(SavedViewPayloadComparer.AreEqual(null, null));
    }

    [Fact]
    public void Comparer_OneNull_ReturnsFalse()
    {
        Assert.False(SavedViewPayloadComparer.AreEqual("""{"statuses":["IN_PROGRESS"]}""", null));
        Assert.False(SavedViewPayloadComparer.AreEqual(null, """{"statuses":["IN_PROGRESS"]}"""));
    }

    [Fact]
    public void Comparer_ArrayOrderIgnored_ReturnsTrue()
    {
        var a = """{"statuses":["IN_PROGRESS","NOT_STARTED"]}""";
        var b = """{"statuses":["NOT_STARTED","IN_PROGRESS"]}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_DifferentArrayValues_ReturnsFalse()
    {
        var a = """{"statuses":["IN_PROGRESS"]}""";
        var b = """{"statuses":["COMPLETED"]}""";
        Assert.False(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_NullVsEmptyArray_ReturnsTrue()
    {
        // null field and absent field are both treated as "empty" → equal
        var withNull  = """{"statuses":null}""";
        var withEmpty = """{"statuses":[]}""";
        var absent    = """{}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(withNull,  absent));
        Assert.True(SavedViewPayloadComparer.AreEqual(withEmpty, absent));
        Assert.True(SavedViewPayloadComparer.AreEqual(withNull,  withEmpty));
    }

    [Fact]
    public void Comparer_UnknownFieldsIgnored_ReturnsTrue()
    {
        var a = """{"statuses":["IN_PROGRESS"],"futureField":"someValue"}""";
        var b = """{"statuses":["IN_PROGRESS"]}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_CaseInsensitiveArrayValues_ReturnsTrue()
    {
        var a = """{"statuses":["in_progress"]}""";
        var b = """{"statuses":["IN_PROGRESS"]}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_DifferentSearchTerms_ReturnsFalse()
    {
        var a = """{"searchTerm":"foo"}""";
        var b = """{"searchTerm":"bar"}""";
        Assert.False(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_SearchTermAbsentVsEmpty_ReturnsTrue()
    {
        // null/absent searchTerm is treated as empty singleton list [] → equal to ""
        var withNull  = """{"searchTerm":null}""";
        var absent    = """{}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(withNull, absent));
    }

    [Fact]
    public void Comparer_MultiDimensionAllMatch_ReturnsTrue()
    {
        var a = """{"statuses":["IN_PROGRESS"],"risks":["ON_TRACK","AT_RISK"],"roles":["DEV"]}""";
        var b = """{"roles":["DEV"],"risks":["AT_RISK","ON_TRACK"],"statuses":["IN_PROGRESS"]}""";
        Assert.True(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_MultiDimensionOneDiffers_ReturnsFalse()
    {
        var a = """{"statuses":["IN_PROGRESS"],"risks":["ON_TRACK"]}""";
        var b = """{"statuses":["IN_PROGRESS"],"risks":["LATE"]}""";
        Assert.False(SavedViewPayloadComparer.AreEqual(a, b));
    }

    [Fact]
    public void Comparer_InvalidJson_ReturnsFalse()
    {
        Assert.False(SavedViewPayloadComparer.AreEqual("{not json}", "{}"));
        Assert.False(SavedViewPayloadComparer.AreEqual("{}", "{not json}"));
    }

    // ── PriorityBuckets ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  "HIGH")]
    [InlineData(2,  "HIGH")]
    [InlineData(3,  "HIGH")]
    [InlineData(4,  "MEDIUM")]
    [InlineData(5,  "MEDIUM")]
    [InlineData(6,  "MEDIUM")]
    [InlineData(7,  "LOW")]
    [InlineData(9,  "LOW")]
    [InlineData(10, "LOW")]
    public void PriorityBuckets_FromPriority_ReturnsCorrectBucket(int priority, string expected)
    {
        Assert.Equal(expected, TaskFilterState.PriorityBuckets.FromPriority(priority));
    }

    [Fact]
    public void PriorityBuckets_Constants_MatchExpectedValues()
    {
        Assert.Equal("HIGH",   TaskFilterState.PriorityBuckets.High);
        Assert.Equal("MEDIUM", TaskFilterState.PriorityBuckets.Medium);
        Assert.Equal("LOW",    TaskFilterState.PriorityBuckets.Low);
    }
}
