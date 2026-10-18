using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using NexAccess.API.Configuration;

namespace NexAccess.IntegrationTests.Support;

public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = CreateJsonOptions();

    public static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 10, 40)].ToUpperInvariant();

    public static async Task<HttpClient> AdminAsync(NexAccessApiFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, factory.AdminUsername, factory.AdminPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { username, password }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync())["accessToken"]!.GetValue<string>();
    }

    public static HttpClient Device(NexAccessApiFactory factory, string key)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-DEVICE-KEY", key);
        return client;
    }

    public static Task<HttpResponseMessage> PostAsync(this HttpClient client, string url, object? body) =>
        client.PostAsJsonAsync(url, body ?? new { }, Json);

    public static Task<HttpResponseMessage> PutAsync(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string url, object body) =>
        client.PatchAsJsonAsync(url, body, Json);

    public static async Task<JsonNode> ReadJsonAsync(this HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync()) ?? throw new InvalidOperationException("Respuesta vacía.");

    public static async Task<JsonNode> ExpectAsync(this Task<HttpResponseMessage> request, System.Net.HttpStatusCode status)
    {
        var response = await request;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Se esperaba {(int)status} y se obtuvo {(int)response.StatusCode}: {body}");
        return string.IsNullOrWhiteSpace(body) ? new JsonObject() : JsonNode.Parse(body)!;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions();
        ApiServiceExtensions.ConfigureJson(options);
        return options;
    }
}
