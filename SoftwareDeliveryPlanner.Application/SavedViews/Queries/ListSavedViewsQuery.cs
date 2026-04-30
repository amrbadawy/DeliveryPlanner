using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Queries;

public sealed record ListSavedViewsQuery(string PageKey, string? OwnerKey = null)
    : IRequest<Result<List<SavedView>>>;
