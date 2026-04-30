using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Web.Services;

/// <summary>
/// Pure predicate that evaluates the sidebar filter against a TaskItem.
/// Search term composes with caller-side search via OR (caller decides whether to AND/OR).
/// All multi-select chip dimensions act as OR-within-dimension and AND-across-dimensions:
///   e.g. status=[A,B] role=[DEV] → (status A or B) AND (any effort role = DEV)
/// </summary>
public static class TaskFilterEvaluator
{
    public static bool Matches(TaskItem task, TaskFilterState.PageFilters f)
    {
        // Search composes with task ID + service name + phase
        if (!string.IsNullOrWhiteSpace(f.SearchTerm))
        {
            var t = f.SearchTerm.Trim();
            var hit = task.TaskId.Contains(t, StringComparison.OrdinalIgnoreCase)
                || task.ServiceName.Contains(t, StringComparison.OrdinalIgnoreCase)
                || (task.Phase ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
            if (!hit) return false;
        }

        if (f.Statuses.Count > 0 && !f.Statuses.Contains(task.Status))
            return false;

        if (f.Risks.Count > 0 && !f.Risks.Contains(task.DeliveryRisk))
            return false;

        if (f.PriorityBuckets.Count > 0)
        {
            var bucket = TaskFilterState.PriorityBuckets.FromPriority(task.Priority);
            if (!f.PriorityBuckets.Contains(bucket)) return false;
        }

        if (f.Phases.Count > 0)
        {
            var phase = task.Phase ?? string.Empty;
            if (!f.Phases.Contains(phase)) return false;
        }

        if (f.Roles.Count > 0)
        {
            var taskRoles = task.EffortBreakdown.Select(e => e.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!f.Roles.Any(r => taskRoles.Contains(r))) return false;
        }

        if (f.DependencyStates.Count > 0)
        {
            var hasDeps = task.Dependencies.Count > 0;
            var matches =
                (hasDeps && f.DependencyStates.Contains(TaskFilterState.DependencyStates.HasDependencies))
                || (!hasDeps && f.DependencyStates.Contains(TaskFilterState.DependencyStates.NoDependencies));
            if (!matches) return false;
        }

        return true;
    }
}
