using SoftwareDeliveryPlanner.Domain;
using SoftwareDeliveryPlanner.Domain.Models;

namespace SoftwareDeliveryPlanner.Web.Services.Gantt;

/// <summary>
/// Pure rendering helpers for the Gantt chart. Contains task-level visual logic
/// extracted from <c>Gantt.razor</c> for unit-testability and reuse.
///
/// All methods are pure: deterministic, free of side effects, and free of DI / DbContext / IMediator.
/// (Enforced by an architecture rule.)
/// </summary>
public static class GanttRenderHelpers
{
    /// <summary>
    /// Returns the CSS status class for a task's coloured status dot.
    /// Risk takes precedence over status: Late > AtRisk > Completed > InProgress > NotStarted (default).
    /// </summary>
    public static string GetStatusClass(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.DeliveryRisk == DomainConstants.DeliveryRisk.Late)
            return "gantt-status-late";
        if (task.DeliveryRisk == DomainConstants.DeliveryRisk.AtRisk)
            return "gantt-status-atrisk";

        return task.Status switch
        {
            var s when s == DomainConstants.TaskStatus.Completed => "gantt-status-completed",
            var s when s == DomainConstants.TaskStatus.InProgress => "gantt-status-inprogress",
            _ => "gantt-status-notstarted",
        };
    }

    /// <summary>
    /// Counts the number of <paramref name="task"/>'s direct predecessors that are NOT
    /// in <paramref name="visibleTaskIds"/>. Used to render the "ghost dependency" stub
    /// when a predecessor was hidden by sidebar filters.
    /// </summary>
    public static int CountHiddenPredecessors(TaskItem task, IReadOnlySet<string> visibleTaskIds)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(visibleTaskIds);

        if (task.Dependencies.Count == 0) return 0;
        return task.Dependencies.Count(d => !visibleTaskIds.Contains(d.PredecessorTaskId));
    }

    /// <summary>
    /// Computes the effective end-date of a task's shell bar. If any role segment ends past
    /// the task's <see cref="TaskItem.PlannedFinish"/>, the bar extends to that segment's end
    /// so segments never overflow the bar visually.
    /// </summary>
    public static DateTime? EffectiveEnd(TaskItem task, IReadOnlyList<DateTime> segmentEnds)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(segmentEnds);

        if (!task.PlannedFinish.HasValue) return null;
        var planned = task.PlannedFinish.Value;

        if (segmentEnds.Count == 0) return planned;
        var maxSeg = segmentEnds.Max();
        return maxSeg > planned ? maxSeg : planned;
    }

    /// <summary>
    /// Truncates a label to <paramref name="maxLength"/> characters, replacing the trailing
    /// character with a Unicode ellipsis (\u2026) when truncation occurs.
    /// </summary>
    public static string TruncateName(string text, int maxLength = 20)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (maxLength < 1) throw new ArgumentOutOfRangeException(nameof(maxLength));
        return text.Length <= maxLength ? text : text[..(maxLength - 1)] + "\u2026";
    }

    /// <summary>
    /// Returns the CSS class for a priority badge. Priorities 1–3 are High, 4–6 Medium, 7–10 Low.
    /// </summary>
    public static string PriorityBadgeClass(int priority) => priority switch
    {
        <= 3 => "gantt-priority-high",
        <= 6 => "gantt-priority-medium",
        _ => "gantt-priority-low",
    };

    /// <summary>
    /// Computes the critical path (longest dependency chain by total task duration) over
    /// <paramref name="scheduledTasks"/>. Memoised, cycle-guarded. Returns the set of TaskIds
    /// on the longest path. If there are no dependencies, the longest single task wins.
    /// </summary>
    public static IReadOnlySet<string> ComputeCriticalPath(IReadOnlyList<TaskItem> scheduledTasks)
    {
        ArgumentNullException.ThrowIfNull(scheduledTasks);
        if (scheduledTasks.Count == 0) return new HashSet<string>();

        var taskMap = scheduledTasks.ToDictionary(t => t.TaskId, StringComparer.OrdinalIgnoreCase);
        var memo = new Dictionary<string, (int Length, List<string> Path)>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        (int Length, List<string> Path) GetLongestPath(string taskId)
        {
            if (memo.TryGetValue(taskId, out var cached)) return cached;
            if (!visiting.Add(taskId)) return (0, new List<string>()); // cycle

            if (!taskMap.TryGetValue(taskId, out var task))
            {
                visiting.Remove(taskId);
                return (0, new List<string>());
            }

            var deps = string.IsNullOrWhiteSpace(task.DependsOnTaskIds)
                ? Array.Empty<string>()
                : task.DependsOnTaskIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var maxLen = 0;
            var bestPath = new List<string>();

            foreach (var dep in deps)
            {
                var (len, path) = GetLongestPath(dep);
                if (len > maxLen)
                {
                    maxLen = len;
                    bestPath = new List<string>(path);
                }
            }

            bestPath.Add(taskId);
            var taskDuration = (task.Duration ?? 0) > 0 ? task.Duration!.Value : (int)task.TotalEstimationDays;
            var result = (maxLen + taskDuration, bestPath);
            memo[taskId] = result;
            visiting.Remove(taskId);
            return result;
        }

        var longest = scheduledTasks
            .Select(t => GetLongestPath(t.TaskId))
            .OrderByDescending(r => r.Length)
            .FirstOrDefault();

        return new HashSet<string>(longest.Path ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
    }
}
