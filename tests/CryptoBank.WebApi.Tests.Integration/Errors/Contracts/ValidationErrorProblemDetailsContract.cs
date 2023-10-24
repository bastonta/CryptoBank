namespace CryptoBank.WebApi.Tests.Integration.Errors.Contracts;

public class ValidationErrorProblemDetailsContract
{
    public string Title { get; set; }

    public string Type { get; set; }

    public string Detail { get; set; }

    public int Status { get; set; }

    public string TraceId { get; set; }

    public ErrorData[] Errors { get; set; }
}

public record ErrorData(
    string Field,
    string Message,
    string Code
);
