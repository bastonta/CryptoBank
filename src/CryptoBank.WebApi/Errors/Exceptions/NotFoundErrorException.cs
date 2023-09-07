namespace CryptoBank.WebApi.Errors.Exceptions;

public class NotFoundErrorException : ErrorException
{
    public NotFoundErrorException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
