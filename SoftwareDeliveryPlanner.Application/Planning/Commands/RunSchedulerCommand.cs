using MediatR;

namespace SoftwareDeliveryPlanner.Application.Planning.Commands;

public sealed record RunSchedulerCommand : IRequest<string>;
