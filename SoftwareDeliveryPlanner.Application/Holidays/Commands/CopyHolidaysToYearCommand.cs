using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Holidays.Commands;

public record CopyHolidaysToYearCommand(int SourceYear, int TargetYear) : IRequest<Result<int>>;
