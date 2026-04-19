using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Queries;

public sealed record GetScenarioDetailQuery(int ScenarioId) : IRequest<Result<PlanScenario>>;
