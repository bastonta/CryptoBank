using CryptoBank.WebApi.Tests.Integration.Errors.Contracts;
using Microsoft.AspNetCore.Http;

namespace CryptoBank.WebApi.Tests.Integration.Helpers;

public static class ProblemDetailExtensions
{
    public static void LogicConflictShouldContain(this LogicConflictProblemDetailsContract contract, string expectedMessage, string expectedCode)
    {
        contract.Should().NotBeNull();
        contract.Title.Should().Be("Logic conflict");
        contract.Type.Should().Be("https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/422");
        contract.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        contract.Detail.Should().Be(expectedMessage);
        contract.Code.Should().Be(expectedCode);
    }
}
