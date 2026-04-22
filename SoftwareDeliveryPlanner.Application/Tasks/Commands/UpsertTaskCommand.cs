using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record EffortBreakdownInput(string Role, double EstimationDays, double OverlapPct, string? MinSeniority = null);

public sealed record DependencyInput(string PredecessorTaskId, string Type, int LagDays, double OverlapPct);

public sealed record UpsertTaskCommand(
    int Id,
    string TaskId,
    string ServiceName,
    double MaxResource,
    int Priority,
    List<EffortBreakdownInput> EffortBreakdown,
    DateTime? StrictDate,
    List<DependencyInput>? Dependencies,
    bool IsNew,
    DateTime? OverrideStart = null,
    string? Phase = null,
    string? PreferredResourceIds = null) : IRequest<Result>;

public sealed record DeleteTaskCommand(int Id) : IRequest<Result>;
