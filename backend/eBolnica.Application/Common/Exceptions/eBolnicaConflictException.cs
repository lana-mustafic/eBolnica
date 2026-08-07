namespace eBolnica.Application.Common.Exceptions;

public sealed class eBolnicaConflictException : Exception
{
    public string Code { get; }

    public eBolnicaConflictException(string message)
        : this("conflict", message)
    {
    }

    public eBolnicaConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
