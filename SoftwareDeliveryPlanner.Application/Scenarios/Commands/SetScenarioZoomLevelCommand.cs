using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Commands;

public sealed record SetScenarioZoomLevelCommand(int ScenarioId, string? ZoomLevel) : IRequest<Result>;
