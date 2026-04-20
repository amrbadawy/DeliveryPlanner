using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Tasks.Commands;

public sealed record BulkTaskRowDto(
    string TaskId, string ServiceName, double DevEstimation,
    double MaxResource, int Priority, DateTime? StrictDate,
    string? DependsOnTaskIds);

public sealed record BulkImportTasksCommand(List<BulkTaskRowDto> Tasks) : IRequest<Result<int>>;
