using IdentityHub.AIService.Application.Abstractions;
using IdentityHub.AIService.Application.Options;
using IdentityHub.AIService.Infrastructure.Clients;
using IdentityHub.AIService.Infrastructure.Knowledge;
using IdentityHub.AIService.Infrastructure.Options;
using IdentityHub.AIService.Infrastructure.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IdentityHub.AIService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(options =>
        {
            configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
            var configuredApiKey = configuration["OpenAI:ApiKey"];
            options.ApiKey = !string.IsNullOrWhiteSpace(configuredApiKey)
                ? configuredApiKey
                : configuration["OPENAI_API_KEY"] ?? options.ApiKey;
        });
        services.Configure<OpenAiCostOptions>(configuration.GetSection(OpenAiCostOptions.SectionName));
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));

        services.AddSingleton<IKnowledgeDocumentReader, FileKnowledgeDocumentReader>();
        services.AddHttpClient<IEmbeddingService, OpenAiEmbeddingService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddHttpClient<IChatService, OpenAiChatService>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddHttpClient<IUserServiceClient, UserServiceClient>((client) =>
        {
            var userServiceUrl = configuration["Services:UserService"]
                ?? throw new InvalidOperationException("UserService URL is not configured.");
            client.BaseAddress = new Uri(userServiceUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IVectorStore, QdrantVectorStore>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<QdrantOptions>>().Value;
            client.BaseAddress = new Uri(options.Url.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
