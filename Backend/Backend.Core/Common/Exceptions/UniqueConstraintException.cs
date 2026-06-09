namespace Backend.Core.Common.Exceptions;

public sealed class UniqueConstraintException : DomainException
{
    public UniqueConstraintException(string message)
        : base(message)
    {
    }

    public UniqueConstraintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
