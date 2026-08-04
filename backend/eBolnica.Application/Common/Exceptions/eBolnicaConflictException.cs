namespace eBolnica.Application.Common.Exceptions;

public sealed class eBolnicaConflictException : Exception
{
    public eBolnicaConflictException(string message) : base(message) { }
}
