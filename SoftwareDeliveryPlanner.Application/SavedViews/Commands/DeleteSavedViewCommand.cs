using MediatR;
using SoftwareDeliveryPlanner.SharedKernel;

namespace SoftwareDeliveryPlanner.Application.SavedViews.Commands;

public sealed record DeleteSavedViewCommand(int Id) : IRequest<Result>;
