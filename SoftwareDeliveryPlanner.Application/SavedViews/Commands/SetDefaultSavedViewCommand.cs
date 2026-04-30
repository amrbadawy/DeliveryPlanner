using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed record SetDefaultSavedViewCommand(int Id, bool IsDefault)
    : IRequest<Result<SavedView>>;
