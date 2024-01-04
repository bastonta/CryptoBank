namespace CryptoBank.WebApi.Tests.Integration.Errors.Contracts;

public class LogicConflictProblemDetailsContract
{
    public string Title { get; set; }

    public string Type { get; set; }

    public string Detail { get; set; }

    public int Status { get; set; }

    public string TraceId { get; set; }

    public string Code { get; set; }
}
