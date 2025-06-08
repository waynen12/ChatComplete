// Knowledge.Api/Program.cs
using KnowledgeEngine;                 // namespace where ChatCompleteSettings lives
using Microsoft.AspNetCore.Mvc;
using KnowledgeEngine.Logging;   // whatever namespace holds LoggerProvider
using ChatCompletion.Config;
using Serilog;
using Knowledge.Api.Options;
using Knowledge.Api.Contracts;
using Microsoft.AspNetCore.OpenApi;


LoggerProvider.ConfigureLogger();   // boots Log.Logger

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(LoggerProvider.Logger, dispose: true);

// ── Configuration binding ─────────────────────────────────────────────────────
builder.Services.Configure<ChatCompleteSettings>(
    builder.Configuration.GetSection(nameof(ChatCompleteSettings)));

builder.Services.Configure<CorsOptions>(
    builder.Configuration.GetSection("Cors"));


// ── CORS policy for the Vite front-end ────────────────────────────────────────
const string DevCors = "DevFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, (policyBuilder) =>
    {
        var cors = builder.Configuration
                           .GetSection("Cors")
                           .Get<CorsOptions>()!;   // safe in dev; validate later

        policyBuilder.WithOrigins(cors.AllowedOrigins)
                     .WithHeaders(cors.AllowedHeaders)
                     .AllowAnyMethod()
                     .SetPreflightMaxAge(TimeSpan.FromHours(cors.MaxAgeHours));
        // no .AllowCredentials() yet
    });
});

// (later) services for Swagger, endpoints, KnowledgeEngine DI, etc.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Pull in minimal-API metadata automatically
    options.SwaggerDoc("v1", new() { Title = "Knowledge API", Version = "v1" });

    // ✦ XML comments for enriched Swagger descriptions
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "Knowledge.Api.xml");
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSerilogRequestLogging();  // keep logs consistent
app.UseCors(DevCors);            // CORS must appear before MapXXX

if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI(options =>
    {
        // ---- Customisations start here ----
        options.RoutePrefix = "docs";          // UI will be at /docs
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Knowledge API v1");
        options.DocumentTitle = "Knowledge API – Swagger";
        // you can add more tweaks here later
    });
}



// ─── Stub endpoints (no group) ───────────────────────────────────────────────

// 1) GET /api/knowledge
app.MapGet("/api/knowledge", () =>
{
    var demo = new KnowledgeSummaryDto
    {
        Id = "demo",
        Name = "Sample Collection",
        DocumentCount = 4
    };
    return Results.Ok(new[] { demo });
})
.WithOpenApi()
.Produces<IEnumerable<KnowledgeSummaryDto>>(StatusCodes.Status200OK);


// 2) POST /api/knowledge
app.MapPost("/api/knowledge", (HttpRequest _) =>
{
    var response = new CreateKnowledgeResponseDto { Id = "demo" };
    return Results.Created($"/api/knowledge/{response.Id}", response);
})
.Accepts<IFormFileCollection>("multipart/form-data")
.WithOpenApi()
.Produces<CreateKnowledgeResponseDto>(StatusCodes.Status201Created);

// 3) POST /api/chat
app.MapPost("/api/chat", (ChatRequestDto req) =>
{
    var reply = new ChatResponseDto { Reply = "Hello from stub" };
    return Results.Ok(reply);
})
.WithOpenApi()
.Produces<ChatResponseDto>(StatusCodes.Status200OK);
  
app.MapGet("/api/ping", () => Results.Ok("pong"))
   .WithTags("Health")
   .WithOpenApi();

app.Run();
