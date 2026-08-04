namespace eBolnica.Domain.Exceptions;

public sealed class eBolnicaNotFoundException : Exception
{
    public eBolnicaNotFoundException(string message) : base(message)
    {
    }

    public eBolnicaNotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}
