using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CryptoBank.WebApi.Tests.Integration.Helpers;

public static class HttpClientExtensions
{
    public static async Task<TResponse> GetAsJsonAsync<TResponse>(this HttpClient client, string url,
        HttpStatusCode expectedStatus = HttpStatusCode.OK, CancellationToken token = default)
    {
        var httpResponse = await client.GetAsync(url, token);
        httpResponse.StatusCode.Should().Be(expectedStatus);

        return await httpResponse.DeserializeResponse<TResponse>(token);
    }

    public static async Task<TResponse> PostAsJsonAsync<TResponse>(this HttpClient client, string url, object? body,
        HttpStatusCode expectedStatus = HttpStatusCode.OK, CancellationToken token = default)
    {
        var httpResponse = await client.PostAsync(url, JsonContent.Create(body), token);
        httpResponse.StatusCode.Should().Be(expectedStatus);

        return await httpResponse.DeserializeResponse<TResponse>(token);
    }

    private static async Task<TResponse> DeserializeResponse<TResponse>(this HttpResponseMessage httpResponse, CancellationToken token)
    {
        var responseString = await httpResponse.Content.ReadAsStringAsync(token);
        var result = JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;

        return result;
    }
}
