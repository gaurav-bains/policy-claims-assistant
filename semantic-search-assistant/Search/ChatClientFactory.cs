using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace SemanticSearchAssistant.Search;

public static class ChatClientFactory
{
    public static IChatClient Create(LlmOptions options)
    {
        return options.Provider.Trim().ToLowerInvariant() switch
        {
            "anthropic" => CreateAnthropic(options),
            "openai" => CreateOpenAI(options),
            _ => throw new InvalidOperationException(
                $"Unsupported Llm:Provider '{options.Provider}'. Use 'OpenAI' or 'Anthropic'.")
        };
    }

    private static IChatClient CreateOpenAI(LlmOptions options)
    {
        var apiKey = string.IsNullOrWhiteSpace(options.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "No LLM API key configured. Set Llm:ApiKey in appsettings, or the OPENAI_API_KEY environment variable.");
        }

        var builder = Kernel.CreateBuilder();

        HttpClient? httpClient = null;
        if (options.DisableCertificateRevocationCheck)
        {
            var handler = new SocketsHttpHandler
            {
                SslOptions = new SslClientAuthenticationOptions
                {
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }
            };
            httpClient = new HttpClient(handler);
        }

        builder.AddOpenAIChatClient(options.ModelId, apiKey, httpClient: httpClient);
        var kernel = builder.Build();
        return kernel.GetRequiredService<IChatClient>();
    }

    private static IChatClient CreateAnthropic(LlmOptions options)
    {
        var apiKey = string.IsNullOrWhiteSpace(options.ApiKey)
            ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            : options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "No LLM API key configured. Set Llm:ApiKey in appsettings, or the ANTHROPIC_API_KEY environment variable.");
        }

        return new AnthropicClient(apiKey).Messages;
    }
}
