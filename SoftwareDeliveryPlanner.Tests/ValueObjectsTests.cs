using SoftwareDeliveryPlanner.Domain.SharedKernel.ValueObjects;

namespace SoftwareDeliveryPlanner.Tests;

public class TaskIdTests
{
    #region TryCreate — valid inputs

    [Fact]
    public void TryCreate_ValidFormat_ReturnsTrue()
    {
        var result = TaskId.TryCreate("AAA-000", out var id);
        Assert.True(result);
        Assert.Equal("AAA-000", id.Value);
    }

    [Fact]
    public void TryCreate_NormalizesToUppercase()
    {
        TaskId.TryCreate("abc-123", out var id);
        Assert.Equal("ABC-123", id.Value);
    }

    [Fact]
    public void TryCreate_TrimsWhitespace()
    {
        var result = TaskId.TryCreate("  SV-01  ", out var id);
        Assert.True(result);
        Assert.Equal("SV-01", id.Value);
    }

    [Fact]
    public void TryCreate_LongerPrefix_ReturnsTrue()
    {
        var result = TaskId.TryCreate("ABCDE-9999", out _);
        Assert.True(result);
    }

    #endregion

    #region TryCreate — invalid inputs

    [Fact]
    public void TryCreate_Null_ReturnsFalse()
    {
        var result = TaskId.TryCreate(null, out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_EmptyString_ReturnsFalse()
    {
        var result = TaskId.TryCreate("", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_WhitespaceOnly_ReturnsFalse()
    {
        var result = TaskId.TryCreate("   ", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_TooShort_ReturnsFalse()
    {
        // "A-1" is 3 chars → length < 5
        var result = TaskId.TryCreate("A-1", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_MissingDash_ReturnsFalse()
    {
        var result = TaskId.TryCreate("AAA000", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_DigitsInPrefix_ReturnsFalse()
    {
        var result = TaskId.TryCreate("A1A-000", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_LettersInSuffix_ReturnsFalse()
    {
        var result = TaskId.TryCreate("AAA-00X", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_SingleCharPrefix_ReturnsFalse()
    {
        // parts[0].Length < 2
        var result = TaskId.TryCreate("A-123", out _);
        Assert.False(result);
    }

    [Fact]
    public void TryCreate_SingleCharSuffix_ReturnsFalse()
    {
        // parts[1].Length < 2
        var result = TaskId.TryCreate("AAA-1", out _);
        Assert.False(result);
    }

    #endregion

    #region Create — exceptions

    [Fact]
    public void Create_InvalidFormat_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => TaskId.Create("INVALID"));
    }

    [Fact]
    public void Create_ValidFormat_ReturnsValue()
    {
        var id = TaskId.Create("SV-01");
        Assert.Equal("SV-01", id.Value);
    }

    #endregion

    #region Value semantics

    [Fact]
    public void EqualIds_AreEqual()
    {
        var a = TaskId.Create("SV-01");
        var b = TaskId.Create("SV-01");
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentIds_AreNotEqual()
    {
        var a = TaskId.Create("SV-01");
        var b = TaskId.Create("SV-02");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = TaskId.Create("SV-01");
        Assert.Equal("SV-01", id.ToString());
    }

    #endregion
}

public class ResourceIdTests
{
    #region TryCreate — valid inputs

    [Fact]
    public void TryCreate_ValidFormat_ReturnsTrue()
    {
        var result = ResourceId.TryCreate("RES-01", out var id);
        Assert.True(result);
        Assert.Equal("RES-01", id.Value);
    }

    [Fact]
    public void TryCreate_NormalizesToUppercase()
    {
        ResourceId.TryCreate("res-01", out var id);
        Assert.Equal("RES-01", id.Value);
    }

    [Fact]
    public void TryCreate_TrimsWhitespace()
    {
        var result = ResourceId.TryCreate("  RES-01  ", out var id);
        Assert.True(result);
        Assert.Equal("RES-01", id.Value);
    }

    #endregion

    #region TryCreate — invalid inputs

    [Fact]
    public void TryCreate_Null_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate(null, out _));
    }

    [Fact]
    public void TryCreate_Empty_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("", out _));
    }

    [Fact]
    public void TryCreate_MissingDash_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("RES01", out _));
    }

    [Fact]
    public void TryCreate_DigitsInPrefix_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("R1S-01", out _));
    }

    [Fact]
    public void TryCreate_LettersInSuffix_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("RES-AB", out _));
    }

    [Fact]
    public void TryCreate_SingleCharPrefix_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("R-01", out _));
    }

    [Fact]
    public void TryCreate_SingleCharSuffix_ReturnsFalse()
    {
        Assert.False(ResourceId.TryCreate("RES-1", out _));
    }

    #endregion

    #region Create — exceptions

    [Fact]
    public void Create_InvalidFormat_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ResourceId.Create("BAD"));
    }

    [Fact]
    public void Create_ValidFormat_ReturnsValue()
    {
        var id = ResourceId.Create("RES-01");
        Assert.Equal("RES-01", id.Value);
    }

    #endregion

    #region Value semantics

    [Fact]
    public void EqualIds_AreEqual()
    {
        var a = ResourceId.Create("RES-01");
        var b = ResourceId.Create("RES-01");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = ResourceId.Create("RES-01");
        Assert.Equal("RES-01", id.ToString());
    }

    #endregion
}

public class PercentageTests
{
    #region TryCreate — valid inputs

    [Fact]
    public void TryCreate_Zero_ReturnsTrue()
    {
        Assert.True(Percentage.TryCreate(0, out var p));
        Assert.Equal(0, p.Value);
    }

    [Fact]
    public void TryCreate_100_ReturnsTrue()
    {
        Assert.True(Percentage.TryCreate(100, out var p));
        Assert.Equal(100, p.Value);
    }

    [Fact]
    public void TryCreate_MidValue_ReturnsTrue()
    {
        Assert.True(Percentage.TryCreate(50.5, out var p));
        Assert.Equal(50.5, p.Value);
    }

    #endregion

    #region TryCreate — invalid inputs

    [Fact]
    public void TryCreate_Negative_ReturnsFalse()
    {
        Assert.False(Percentage.TryCreate(-0.001, out _));
    }

    [Fact]
    public void TryCreate_Over100_ReturnsFalse()
    {
        Assert.False(Percentage.TryCreate(100.001, out _));
    }

    #endregion

    #region Create — exceptions

    [Fact]
    public void Create_Negative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Percentage.Create(-1));
    }

    [Fact]
    public void Create_Over100_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Percentage.Create(101));
    }

    [Fact]
    public void Create_Valid_ReturnsValue()
    {
        var p = Percentage.Create(75);
        Assert.Equal(75, p.Value);
    }

    #endregion

    #region Value semantics

    [Fact]
    public void EqualPercentages_AreEqual()
    {
        var a = Percentage.Create(50);
        var b = Percentage.Create(50);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentPercentages_AreNotEqual()
    {
        var a = Percentage.Create(50);
        var b = Percentage.Create(51);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_FormatsWithOneDecimal()
    {
        var p = Percentage.Create(75);
        Assert.Equal("75.0", p.ToString());
    }

    #endregion
}

public class DateRangeTests
{
    private static readonly DateTime _today = new DateTime(2026, 5, 4);

    #region TryCreate — valid inputs

    [Fact]
    public void TryCreate_StartEqualsEnd_ReturnsTrue()
    {
        Assert.True(DateRange.TryCreate(_today, _today, out var r));
        Assert.Equal(_today.Date, r.Start);
        Assert.Equal(_today.Date, r.End);
    }

    [Fact]
    public void TryCreate_StartBeforeEnd_ReturnsTrue()
    {
        Assert.True(DateRange.TryCreate(_today, _today.AddDays(10), out _));
    }

    [Fact]
    public void TryCreate_StripsTimeComponent()
    {
        var startWithTime = new DateTime(2026, 5, 4, 14, 30, 0);
        var endWithTime = new DateTime(2026, 5, 10, 8, 0, 0);
        DateRange.TryCreate(startWithTime, endWithTime, out var r);
        Assert.Equal(new DateTime(2026, 5, 4), r.Start);
        Assert.Equal(new DateTime(2026, 5, 10), r.End);
    }

    #endregion

    #region TryCreate — invalid inputs

    [Fact]
    public void TryCreate_StartAfterEnd_ReturnsFalse()
    {
        Assert.False(DateRange.TryCreate(_today.AddDays(1), _today, out _));
    }

    #endregion

    #region Create — exceptions

    [Fact]
    public void Create_StartAfterEnd_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DateRange.Create(_today.AddDays(5), _today));
    }

    [Fact]
    public void Create_Valid_ReturnsRange()
    {
        var r = DateRange.Create(_today, _today.AddDays(7));
        Assert.Equal(_today.Date, r.Start);
        Assert.Equal(_today.AddDays(7).Date, r.End);
    }

    #endregion

    #region Value semantics

    [Fact]
    public void EqualRanges_AreEqual()
    {
        var a = DateRange.Create(_today, _today.AddDays(5));
        var b = DateRange.Create(_today, _today.AddDays(5));
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentRanges_AreNotEqual()
    {
        var a = DateRange.Create(_today, _today.AddDays(5));
        var b = DateRange.Create(_today, _today.AddDays(6));
        Assert.NotEqual(a, b);
    }

    #endregion
}
