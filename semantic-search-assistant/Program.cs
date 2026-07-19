using SemanticSearchAssistant.Ingestion;
using SemanticSearchAssistant.Search;

EnvFile.Load(Directory.GetCurrentDirectory());

if (args.Length > 0 && args[0] == "ingest")
{
    var contentRootPath = Directory.GetCurrentDirectory();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(contentRootPath)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    await IngestionCommand.RunAsync(configuration, contentRootPath);
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Adding services to the container.
// configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var embeddingOptions = builder.Configuration.GetSection(EmbeddingOptions.SectionName).Get<EmbeddingOptions>()
    ?? new EmbeddingOptions();
var llmOptions = builder.Configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>()
    ?? new LlmOptions();
var retrievalOptions = builder.Configuration.GetSection(RetrievalOptions.SectionName).Get<RetrievalOptions>()
    ?? new RetrievalOptions();
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres configuration.");

builder.Services.AddSingleton(embeddingOptions);
builder.Services.AddSingleton(llmOptions);
builder.Services.AddSingleton(retrievalOptions);
builder.Services.AddSingleton(EmbeddingGeneratorFactory.Create(embeddingOptions));
builder.Services.AddSingleton(ChatClientFactory.Create(llmOptions));
builder.Services.AddSingleton(new ChunkStore(connectionString));
builder.Services.AddSingleton<SearchService>();

var app = builder.Build();

await app.Services.GetRequiredService<ChunkStore>().EnsureSchemaAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/search", async (SearchRequest request, SearchService searchService) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new { error = "Query must not be empty." });
    }

    var response = await searchService.SearchAsync(request.Query);
    return Results.Ok(response);
})
.WithName("Search")
.WithOpenApi();

app.Run();
