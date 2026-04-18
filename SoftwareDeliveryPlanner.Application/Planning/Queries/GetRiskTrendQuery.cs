using MediatR;
using SoftwareDeliveryPlanner.Application.Abstractions;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.Planning.Queries;

public sealed record RiskTrendPointDto(DateTime RunTimestamp, int OnTrack, int AtRisk, int Late, int Total);

public sealed record GetRiskTrendQuery(int MaxPoints = 20) : IRequest<Result<List<RiskTrendPointDto>>>;
