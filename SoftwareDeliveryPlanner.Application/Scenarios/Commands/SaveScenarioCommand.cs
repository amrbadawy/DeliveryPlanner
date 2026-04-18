using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed record SaveScenarioCommand(string ScenarioName, string? Notes) : IRequest<Result>;
