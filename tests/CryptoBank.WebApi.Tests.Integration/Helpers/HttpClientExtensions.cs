using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CryptoBank.WebApi.Tests.Integration.Helpers;

public static class HttpClientExtensions
{
    public static async Task<TResponse> PostAsJsonAsync<TResponse>(this HttpClient client, string url, object? body,
        HttpStatusCode expectedStatus = HttpStatusCode.OK)
    {
        var httpResponse = await client.PostAsync(url, JsonContent.Create(body));
        httpResponse.StatusCode.Should().Be(expectedStatus);

        var responseString = await httpResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        })!;
    }
}
