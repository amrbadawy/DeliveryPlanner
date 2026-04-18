namespace SoftwareDeliveryPlanner.Domain.Models;

public class SchedulerSnapshot
{
    public int Id { get; private set; }
    public DateTime RunTimestamp { get; private set; }
    public int OnTrackCount { get; private set; }
    public int AtRiskCount { get; private set; }
    public int LateCount { get; private set; }
    public int TotalTasks { get; private set; }

    private SchedulerSnapshot() { }

    public static SchedulerSnapshot Create(DateTime runTimestamp, int onTrack, int atRisk, int late, int total)
    {
        return new SchedulerSnapshot
        {
            RunTimestamp = runTimestamp,
            OnTrackCount = onTrack,
            AtRiskCount = atRisk,
            LateCount = late,
            TotalTasks = total
        };
    }
}
