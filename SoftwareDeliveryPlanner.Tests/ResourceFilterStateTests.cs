using SoftwareDeliveryPlanner.Web.Services;
using static SoftwareDeliveryPlanner.Web.Services.ResourceFilterState;

namespace SoftwareDeliveryPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="ResourceFilterState"/>: the shared chip/search
/// filter used by both the Tasks sidebar and the Heatmap sidebar. Verifies
/// toggle semantics, search trim/equality short-circuit, clear, IsAnyActive,
/// the OnChange event, and the <see cref="ResourceFilterState.Matches"/> rules
/// (search OR over id+name; role/seniority intersection across groups).
/// </summary>
public class ResourceFilterStateTests
{
    private static ResourceFilterItem Item(
        string id = "RES-001",
        string name = "Alice Anderson",
        string role = "DEV",
        string seniority = "Senior")
        => new(id, name, role, seniority);

    // ---------- Initial state ----------

    [Fact]
    public void New_state_is_empty_and_not_active()
    {
        var s = new ResourceFilterState();
        Assert.Equal(string.Empty, s.SearchTerm);
        Assert.Empty(s.SelectedRoles);
        Assert.Empty(s.SelectedSeniorities);
        Assert.False(s.IsAnyActive);
    }

    [Fact]
    public void New_state_matches_every_item()
    {
        var s = new ResourceFilterState();
        Assert.True(s.Matches(Item(role: "DEV", seniority: "Junior")));
        Assert.True(s.Matches(Item(role: "QA", seniority: "Senior")));
    }

    // ---------- Search ----------

    [Fact]
    public void SetSearchTerm_trims_whitespace()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("  alice  ");
        Assert.Equal("alice", s.SearchTerm);
        Assert.True(s.IsAnyActive);
    }

    [Fact]
    public void SetSearchTerm_is_idempotent_and_does_not_raise_event_when_unchanged()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("alice");
        var fires = 0;
        s.OnChange += () => fires++;
        s.SetSearchTerm("alice");
        Assert.Equal(0, fires);
    }

    [Fact]
    public void SetSearchTerm_raises_OnChange_when_value_changes()
    {
        var s = new ResourceFilterState();
        var fires = 0;
        s.OnChange += () => fires++;
        s.SetSearchTerm("alice");
        s.SetSearchTerm("bob");
        Assert.Equal(2, fires);
    }

    [Fact]
    public void Matches_is_case_insensitive_for_id_and_name()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("ALICE");
        Assert.True(s.Matches(Item(name: "alice anderson")));
        s.SetSearchTerm("res-002");
        Assert.True(s.Matches(Item(id: "RES-002")));
    }

    [Fact]
    public void Matches_search_is_OR_across_id_and_name_only()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("xyz");
        Assert.False(s.Matches(Item(id: "RES-001", name: "Alice")));
        Assert.True(s.Matches(Item(id: "xyz-9", name: "Alice")));
        Assert.True(s.Matches(Item(id: "RES-1", name: "Mr Xyz")));
    }

    [Fact]
    public void Search_does_not_match_against_role_or_seniority()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("DEV");
        // Role/seniority are NOT searched — only id + name.
        Assert.False(s.Matches(Item(id: "RES-1", name: "Alice", role: "DEV", seniority: "Junior")));
    }

    // ---------- Role toggles ----------

    [Fact]
    public void ToggleRole_adds_then_removes()
    {
        var s = new ResourceFilterState();
        s.ToggleRole("DEV");
        Assert.Contains("DEV", s.SelectedRoles);
        s.ToggleRole("DEV");
        Assert.DoesNotContain("DEV", s.SelectedRoles);
    }

    [Fact]
    public void ToggleRole_is_case_insensitive()
    {
        var s = new ResourceFilterState();
        s.ToggleRole("DEV");
        s.ToggleRole("dev");           // should remove the existing entry
        Assert.Empty(s.SelectedRoles);
    }

    [Fact]
    public void ToggleRole_ignores_null_or_whitespace()
    {
        var s = new ResourceFilterState();
        var fires = 0;
        s.OnChange += () => fires++;
        s.ToggleRole("");
        s.ToggleRole("   ");
        s.ToggleRole(null!);
        Assert.Empty(s.SelectedRoles);
        Assert.Equal(0, fires);
    }

    // ---------- Seniority toggles ----------

    [Fact]
    public void ToggleSeniority_adds_then_removes()
    {
        var s = new ResourceFilterState();
        s.ToggleSeniority("Junior");
        Assert.Contains("Junior", s.SelectedSeniorities);
        s.ToggleSeniority("Junior");
        Assert.DoesNotContain("Junior", s.SelectedSeniorities);
    }

    [Fact]
    public void ToggleSeniority_ignores_null_or_whitespace()
    {
        var s = new ResourceFilterState();
        s.ToggleSeniority("");
        s.ToggleSeniority(" ");
        s.ToggleSeniority(null!);
        Assert.Empty(s.SelectedSeniorities);
    }

    // ---------- IsAnyActive ----------

    [Fact]
    public void IsAnyActive_is_true_when_search_set()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("a");
        Assert.True(s.IsAnyActive);
    }

    [Fact]
    public void IsAnyActive_is_true_when_role_selected()
    {
        var s = new ResourceFilterState();
        s.ToggleRole("DEV");
        Assert.True(s.IsAnyActive);
    }

    [Fact]
    public void IsAnyActive_is_true_when_seniority_selected()
    {
        var s = new ResourceFilterState();
        s.ToggleSeniority("Senior");
        Assert.True(s.IsAnyActive);
    }

    // ---------- Clear ----------

    [Fact]
    public void Clear_resets_all_three_dimensions_and_fires_OnChange()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("alice");
        s.ToggleRole("DEV");
        s.ToggleSeniority("Senior");

        var fires = 0;
        s.OnChange += () => fires++;
        s.Clear();

        Assert.Equal(string.Empty, s.SearchTerm);
        Assert.Empty(s.SelectedRoles);
        Assert.Empty(s.SelectedSeniorities);
        Assert.False(s.IsAnyActive);
        Assert.Equal(1, fires);
    }

    // ---------- Matches: combined dimensions ----------

    [Fact]
    public void Matches_role_filter_is_OR_within_group()
    {
        var s = new ResourceFilterState();
        s.ToggleRole("DEV");
        s.ToggleRole("QA");
        Assert.True(s.Matches(Item(role: "DEV")));
        Assert.True(s.Matches(Item(role: "QA")));
        Assert.False(s.Matches(Item(role: "BA")));
    }

    [Fact]
    public void Matches_seniority_filter_is_OR_within_group()
    {
        var s = new ResourceFilterState();
        s.ToggleSeniority("Junior");
        s.ToggleSeniority("Mid");
        Assert.True(s.Matches(Item(seniority: "Junior")));
        Assert.True(s.Matches(Item(seniority: "Mid")));
        Assert.False(s.Matches(Item(seniority: "Senior")));
    }

    [Fact]
    public void Matches_combines_dimensions_with_AND()
    {
        var s = new ResourceFilterState();
        s.SetSearchTerm("alice");
        s.ToggleRole("DEV");
        s.ToggleSeniority("Senior");

        Assert.True(s.Matches(Item(name: "Alice", role: "DEV", seniority: "Senior")));
        Assert.False(s.Matches(Item(name: "Alice", role: "QA", seniority: "Senior")));
        Assert.False(s.Matches(Item(name: "Alice", role: "DEV", seniority: "Junior")));
        Assert.False(s.Matches(Item(name: "Bob", role: "DEV", seniority: "Senior")));
    }
}
