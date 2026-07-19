using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace SemanticSearchAssistant.Ingestion;

public static class EmbeddingGeneratorFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> Create(EmbeddingOptions options)
    {
        var apiKey = string.IsNullOrWhiteSpace(options.ApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : options.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "No embedding API key configured. Set Embedding:ApiKey in appsettings, or the OPENAI_API_KEY environment variable.");
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

        builder.AddOpenAIEmbeddingGenerator(
            options.ModelId,
            apiKey,
            dimensions: options.Dimensions,
            httpClient: httpClient);

        var kernel = builder.Build();
        return kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    }
}
