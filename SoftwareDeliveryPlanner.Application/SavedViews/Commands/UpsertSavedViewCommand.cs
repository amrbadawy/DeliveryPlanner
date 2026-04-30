using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed record UpsertSavedViewCommand(
    string Name,
    string PageKey,
    string PayloadJson,
    string? OwnerKey = null) : IRequest<Result<SavedView>>;
