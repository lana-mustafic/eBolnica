namespace eBolnica.Application.Common.Exceptions;

public sealed class eBolnicaNotFoundException : Exception
{
    public eBolnicaNotFoundException(string message, object? id = null) : base(message) { }
}
