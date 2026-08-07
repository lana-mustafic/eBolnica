namespace eBolnica.Application.Common.Exceptions;

public sealed class eBolnicaNotFoundException : Exception
{
    public string Code { get; }

    public eBolnicaNotFoundException(string message, object? id = null)
        : this("not_found", message)
    {
    }

    public eBolnicaNotFoundException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
