namespace CryptoBank.WebApi.Tests.Integration.Errors.Contracts;

public class ValidationErrorProblemDetailsContract
{
    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public int Status { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public ErrorData[] Errors { get; set; } = Array.Empty<ErrorData>();
}

public record ErrorData(
    string Field,
    string Message,
    string Code
);
