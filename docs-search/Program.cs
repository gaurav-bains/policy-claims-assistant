var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var rscmSnippets = new[]
{
    "All employees must complete workplace hazard training within 30 days of hire.",
    "Personal protective equipment is required in all designated high-risk zones.",
    "Incidents resulting in injury must be reported to a supervisor within 24 hours.",
    "Fire extinguishers must be inspected monthly and logged by facility staff.",
    "Employees experiencing fatigue during a shift should notify their team lead immediately.",
    "Machine guards must not be removed or bypassed while equipment is in operation.",
    "Emergency exits must remain unobstructed at all times during business hours.",
    "Workers exposed to loud machinery must wear hearing protection at all times.",
    "Spills of hazardous materials must be contained and reported before cleanup begins.",
    "Annual safety audits are mandatory for all departments handling chemical storage."
};

app.MapPost("/api/search", (SearchRequest request) =>
{
    var words = request.Query
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .ToArray();

    var matches = rscmSnippets
        .Where(snippet => words.Any(word =>
            snippet.Contains(word, StringComparison.OrdinalIgnoreCase)))
        .ToArray();

    var response = new SearchResponse(matches, "keyword-search");
    return Results.Ok(response);
})
.WithName("Search")
.WithOpenApi();

app.Run();

record SearchRequest(string Query);

record SearchResponse(string[] Results, string Source);
