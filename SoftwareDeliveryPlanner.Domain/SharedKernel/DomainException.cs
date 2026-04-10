namespace SoftwareDeliveryPlanner.Domain.SharedKernel;

public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
