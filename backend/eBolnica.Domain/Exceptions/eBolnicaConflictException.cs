namespace eBolnica.Domain.Exceptions;

public sealed class eBolnicaConflictException : Exception
{
    public eBolnicaConflictException(string message) : base(message)
    {
    }
}
