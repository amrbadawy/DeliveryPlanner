using MediatR;
using SoftwareDeliveryPlanner.Domain.Models;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed record RenameSavedViewCommand(int Id, string Name) : IRequest<Result<SavedView>>;
