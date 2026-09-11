namespace Backend.Core.Common.Exceptions;

/// <summary>
/// Indicates that a database relationship prevents an operation from being
/// completed. This is a domain/integrity rule, not an optimistic concurrency
/// conflict.
/// </summary>
public sealed class ReferencedRecordException : DomainException
{
    public string Code { get; }

    public ReferencedRecordException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public ReferencedRecordException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
