using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Scenarios.Queries;

public sealed record GetScenariosQuery : IRequest<Result<List<PlanScenario>>>;
