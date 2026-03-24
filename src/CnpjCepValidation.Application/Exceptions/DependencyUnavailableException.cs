namespace CnpjCepValidation.Application.Exceptions;

public sealed class DependencyUnavailableException : Exception
{
    public DependencyUnavailableException(string message) : base(message) { }

    public DependencyUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}
