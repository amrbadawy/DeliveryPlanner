using SoftwareDeliveryPlanner.Web.Services;

namespace SoftwareDeliveryPlanner.Tests;

public class ResourceFilterStateTests
{
    [Fact]
    public void Matches_NoFilters_ReturnsTrue()
    {
        var state = new ResourceFilterState();
        var item = new ResourceFilterState.ResourceFilterItem("DEV-001", "Developer 1", "DEV", "Mid");

        Assert.True(state.Matches(item));
    }

    [Fact]
    public void Matches_RoleFilter_OnlyMatchesSelectedRole()
    {
        var state = new ResourceFilterState();
        state.ToggleRole("QA");

        var qa = new ResourceFilterState.ResourceFilterItem("QA-001", "QA Engineer", "QA", "Senior");
        var dev = new ResourceFilterState.ResourceFilterItem("DEV-001", "Developer", "DEV", "Senior");

        Assert.True(state.Matches(qa));
        Assert.False(state.Matches(dev));
    }

    [Fact]
    public void Matches_SearchTerm_MatchesByIdOrName()
    {
        var state = new ResourceFilterState();
        state.SetSearchTerm("dev-002");

        var hit = new ResourceFilterState.ResourceFilterItem("DEV-002", "Developer 2", "DEV", "Mid");
        var miss = new ResourceFilterState.ResourceFilterItem("QA-001", "QA Engineer", "QA", "Mid");

        Assert.True(state.Matches(hit));
        Assert.False(state.Matches(miss));
    }
}
