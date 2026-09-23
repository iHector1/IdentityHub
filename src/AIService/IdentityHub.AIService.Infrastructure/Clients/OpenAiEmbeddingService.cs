using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Infrastructure.Clients;

public sealed class OpenAiEmbeddingService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    public async Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(
        IReadOnlyCollection<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0) return Array.Empty<float[]>();
        var settings = options.Value;
        EnsureApiKey(settings);

        using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = settings.EmbeddingModel,
            input = inputs.ToArray()
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenAI embeddings request failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException($"OpenAI embeddings request failed with status {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(body);
        var data = document.RootElement.GetProperty("data");
        return data.EnumerateArray()
            .OrderBy(item => item.GetProperty("index").GetInt32())
            .Select(item => item.GetProperty("embedding").EnumerateArray().Select(value => value.GetSingle()).ToArray())
            .ToArray();
    }

    private static void EnsureApiKey(OpenAiOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY or OpenAI__ApiKey.");
    }
}
