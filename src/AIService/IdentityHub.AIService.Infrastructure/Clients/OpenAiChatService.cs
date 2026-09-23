using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Infrastructure.Clients;

public sealed class OpenAiChatService(
    HttpClient httpClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiChatService> logger) : IChatService
{
    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY or OpenAI__ApiKey.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = settings.Model,
            instructions = systemPrompt,
            input = userPrompt,
            store = false
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("OpenAI response request failed with status {StatusCode}", (int)response.StatusCode);
            throw new HttpRequestException($"OpenAI response request failed with status {(int)response.StatusCode}.");
        }

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("output_text", out var outputText) &&
            !string.IsNullOrWhiteSpace(outputText.GetString()))
        {
            return outputText.GetString()!;
        }

        var text = document.RootElement.TryGetProperty("output", out var output)
            ? output.EnumerateArray()
                .Where(item => item.TryGetProperty("content", out _))
                .SelectMany(item => item.GetProperty("content").EnumerateArray())
                .Where(item => item.TryGetProperty("text", out _))
                .Select(item => item.GetProperty("text").GetString())
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            : null;

        return string.IsNullOrWhiteSpace(text)
            ? throw new InvalidOperationException("OpenAI returned no text output.")
            : text;
    }
}
