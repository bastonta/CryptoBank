namespace CryptoBank.WebApi.Tests.Integration.Errors.Contracts;

public class LogicConflictProblemDetailsContract
{
    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public int Status { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
