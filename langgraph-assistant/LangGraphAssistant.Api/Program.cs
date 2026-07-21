using System.Net.Http.Json;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Adding services to the container.
// Configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var pythonServiceBaseUrl = builder.Configuration["PythonService:BaseUrl"] ?? "http://localhost:8000";
var pythonServiceTimeoutSeconds = builder.Configuration.GetValue<int?>("PythonService:TimeoutSeconds") ?? 30;
var maxQueryLength = builder.Configuration.GetValue<int?>("Validation:MaxQueryLength") ?? 2000;

builder.Services.AddHttpClient("PythonService", client =>
{
    client.BaseAddress = new Uri(pythonServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(pythonServiceTimeoutSeconds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/api/search", async (SearchRequest request, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new { error = "Query must not be empty." });
    }

    if (request.Query.Length > maxQueryLength)
    {
        return Results.BadRequest(new { error = $"Query must not exceed {maxQueryLength} characters." });
    }

    var client = httpClientFactory.CreateClient("PythonService");

    HttpResponseMessage upstreamResponse;
    try
    {
        upstreamResponse = await client.PostAsJsonAsync("/api/search", request);
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        logger.LogError(ex, "Failed to reach the LangGraph search service at {BaseUrl}.", pythonServiceBaseUrl);
        return Results.Problem(
            detail: "The search service is currently unavailable. Please try again shortly.",
            statusCode: StatusCodes.Status502BadGateway);
    }

    if (!upstreamResponse.IsSuccessStatusCode)
    {
        var upstreamBody = await upstreamResponse.Content.ReadAsStringAsync();
        logger.LogWarning(
            "LangGraph search service returned {StatusCode}: {Body}",
            upstreamResponse.StatusCode,
            upstreamBody);

        // Pass through clean 4xx validation errors (e.g. its own empty-query check) as-is.
        if ((int)upstreamResponse.StatusCode is >= 400 and < 500)
        {
            return Results.Content(upstreamBody, "application/json", statusCode: (int)upstreamResponse.StatusCode);
        }

        return Results.Problem(
            detail: "The search service returned an unexpected error.",
            statusCode: StatusCodes.Status502BadGateway);
    }

    SearchResponse? result;
    try
    {
        result = await upstreamResponse.Content.ReadFromJsonAsync<SearchResponse>();
    }
    catch (JsonException ex)
    {
        logger.LogError(ex, "Failed to parse the LangGraph search service's response.");
        return Results.Problem(
            detail: "The search service returned an unexpected response.",
            statusCode: StatusCodes.Status502BadGateway);
    }

    if (result is null)
    {
        return Results.Problem(
            detail: "The search service returned an empty response.",
            statusCode: StatusCodes.Status502BadGateway);
    }

    return Results.Ok(result);
})
.WithName("Search")
.WithOpenApi();

app.Run();

record SearchRequest(string Query);

record Citation(string Source, int ChunkIndex, string Excerpt);

record SearchResponse(string Answer, List<Citation> Citations, bool HasGroundedAnswer);
