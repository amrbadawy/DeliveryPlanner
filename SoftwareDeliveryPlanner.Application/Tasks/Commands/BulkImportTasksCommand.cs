using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record BulkTaskRowDto(
    string TaskId, string ServiceName,
    double MaxResource, int Priority,
    List<EffortBreakdownInput> EffortBreakdown,
    DateTime? StrictDate, string? DependsOnTaskIds,
    string? Phase = null, string? PreferredResourceIds = null);

public sealed record BulkImportTasksCommand(List<BulkTaskRowDto> Tasks) : IRequest<Result<int>>;
