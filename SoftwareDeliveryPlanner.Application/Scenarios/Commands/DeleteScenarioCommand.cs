using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed record DeleteScenarioCommand(int Id) : IRequest<Result>;
