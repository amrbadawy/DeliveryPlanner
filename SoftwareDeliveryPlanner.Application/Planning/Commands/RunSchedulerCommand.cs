using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Commands;

public sealed record RunSchedulerCommand : IRequest<Result<string>>;
