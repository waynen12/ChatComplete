using ChatCompletion.Config;
using Knowledge.Analytics.Services;
using Knowledge.Data.Extensions;
using Knowledge.Mcp.Configuration;
using Knowledge.Mcp.Endpoints;
using Knowledge.Mcp.Middleware;
using KnowledgeEngine.Extensions;
using KnowledgeEngine.Services;
using KnowledgeEngine.Services.HealthCheckers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Knowledge.Mcp;

/// <summary>
/// Knowledge Management MCP Server - Exposes ChatComplete agent functionality via Model Context Protocol
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // Check if we should run the test program instead
        if (args.Length > 0 && args[0] == "--test-collections")
        {
            return await TestQdrantCollections.RunTest(args);
        }

        // Check if we should run minimal MCP server
        if (args.Length > 0 && args[0] == "--minimal")
        {
            return await MinimalProgram.RunMinimal(args);
        }

        // Check if we should run minimal HTTP MCP server (for testing)
        if (args.Contains("--minimal-http"))
        {
            return await MinimalHttpProgram.RunMinimalHttp(args);
        }

        // Check for HTTP transport mode
        bool useHttp = args.Contains("--http") || args.Contains("--http-transport");

        try
        {
            if (useHttp)
            {
                // Run as HTTP SSE server (for web clients)
                Console.WriteLine("Starting Knowledge MCP Server in HTTP SSE mode...");
                await RunHttpServer(args);
                return 0;
            }
            else
            {
                // Run as STDIO server (for Claude Desktop, default)
                Console.WriteLine("Starting Knowledge MCP Server in STDIO mode...");
                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Knowledge MCP Server: {ex.Message}");
            return 1;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(
                (context, config) =>
                {
                    // Set the base path to the directory where the executable is located
                    // This ensures appsettings.json is found regardless of working directory
                    var basePath = System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                    );
                    config.SetBasePath(basePath!);

                    // Clear existing sources and add our configuration files
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);

                    Console.WriteLine($"MCP Server configuration base path: {basePath}");
                }
            )
            .ConfigureServices(
                (context, services) =>
                {
                    // Add configuration
                    var configuration = context.Configuration;
                    // Create ChatCompleteSettings from configuration
                    var chatCompleteSettings = new ChatCompleteSettings();
                    configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);
                    var databasePath = chatCompleteSettings.DatabasePath;

                    if (string.IsNullOrWhiteSpace(databasePath))
                    {
                        Console.WriteLine(
                            "WARNING: DatabasePath not configured in appsettings.json."
                        );
                        Console.WriteLine(
                            "This likely means appsettings.json was not copied to the output directory."
                        );
                        throw new Exception("DatabasePath not configured in appsettings.json.");
                    }

                    Console.WriteLine($"MCP Server using database path: {databasePath}");

                    // Force correct Qdrant configuration (Bind() doesn't override defaults properly)
                    chatCompleteSettings.VectorStore.Provider = "Qdrant";
                    chatCompleteSettings.VectorStore.Qdrant.Port = 6334;

                    // Register settings as singleton
                    services.AddSingleton(chatCompleteSettings);

                    // Register MCP server specific settings
                    var mcpServerSettings = new McpServerSettings();
                    configuration.GetSection(McpServerSettings.SectionName).Bind(mcpServerSettings);
                    services.AddSingleton(mcpServerSettings);

                    // Add OpenTelemetry for observability
                    services
                        .AddOpenTelemetry()
                        .WithTracing(builder =>
                        {
                            builder
                                .AddSource("Knowledge.Mcp")
                                .SetSampler(new AlwaysOnSampler())
                                .AddConsoleExporter()
                                .AddOtlpExporter();
                        });

                    // Configure HTTP client services (required for health checkers)
                    services.AddHttpClient();

                    // Configure database services (required for health checks)
                    services.AddKnowledgeData(databasePath);

                    // Register Qdrant-only services (bypass MongoDB completely)

                    // Register QdrantSettings
                    services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);

                    // Register Qdrant services directly
                    services.AddSingleton<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>(
                        provider =>
                        {
                            var qdrantSettings = chatCompleteSettings.VectorStore.Qdrant;
                            var qdrantClient = new Qdrant.Client.QdrantClient(
                                host: qdrantSettings.Host,
                                port: qdrantSettings.Port,
                                https: qdrantSettings.UseHttps,
                                apiKey: qdrantSettings.ApiKey
                            );
                            return new Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore(
                                qdrantClient,
                                ownsClient: true
                            );
                        }
                    );

                    // Register Qdrant Index Manager
                    services.AddScoped<
                        KnowledgeEngine.Persistence.IndexManagers.IIndexManager,
                        KnowledgeEngine.Persistence.IndexManagers.QdrantIndexManager
                    >();

                    // Register Qdrant Vector Store Strategy
                    services.AddScoped<
                        KnowledgeEngine.Persistence.VectorStores.IVectorStoreStrategy,
                        KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy
                    >(provider =>
                    {
                        var vectorStore =
                            provider.GetRequiredService<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>();
                        var qdrantSettings =
                            provider.GetRequiredService<ChatCompletion.Config.QdrantSettings>();
                        var indexManager =
                            provider.GetRequiredService<KnowledgeEngine.Persistence.IndexManagers.IIndexManager>();
                        return new KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy(
                            vectorStore,
                            qdrantSettings,
                            indexManager,
                            chatCompleteSettings
                        );
                    });

                    // Register system health services and their dependencies
                    services.AddScoped<ISystemHealthService, SystemHealthService>();
                    services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();

                    // Register component health checkers (Qdrant-only, no MongoDB)
                    services.AddScoped<IComponentHealthChecker, SqliteHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, OllamaHealthChecker>();
                    services.AddScoped<IComponentHealthChecker, QdrantHealthChecker>(); // Should work now with pure Qdrant setup
                    // Still disabled for testing:
                    // services.AddScoped<IComponentHealthChecker, OpenAIHealthChecker>();
                    // services.AddScoped<IComponentHealthChecker, AnthropicHealthChecker>();
                    // services.AddScoped<IComponentHealthChecker, GoogleAIHealthChecker>();

                    // Register knowledge management services (required for search and analytics MCP tools)

                    // Register KnowledgeEngine SqliteDbContext (different from Knowledge.Data one)
                    services.AddScoped<KnowledgeEngine.Persistence.Sqlite.SqliteDbContext>(
                        provider =>
                        {
                            return new KnowledgeEngine.Persistence.Sqlite.SqliteDbContext(
                                databasePath
                            );
                        }
                    );

                    services.AddScoped<
                        KnowledgeEngine.Persistence.IKnowledgeRepository,
                        KnowledgeEngine.Persistence.Sqlite.Repositories.SqliteKnowledgeRepository
                    >();

                    // Register embedding services (required for knowledge search)
                    // Use Ollama embedding service (configured in appsettings.json)
                    var activeProvider =
                        chatCompleteSettings.EmbeddingProviders?.ActiveProvider ?? "Ollama";

                    if (activeProvider == "Ollama")
                    {
                        var ollamaSettings = chatCompleteSettings.EmbeddingProviders?.Ollama;
                        var ollamaBaseUrl =
                            chatCompleteSettings.OllamaBaseUrl ?? "http://localhost:11434";
                        var modelName = ollamaSettings?.ModelName ?? "nomic-embed-text";

                        Console.WriteLine(
                            $"MCP Server configuring Ollama embedding: {ollamaBaseUrl} with model {modelName}"
                        );

                        services.AddScoped<Microsoft.Extensions.AI.IEmbeddingGenerator<
                            string,
                            Microsoft.Extensions.AI.Embedding<float>
                        >>(provider =>
                        {
                            var httpClient = provider
                                .GetRequiredService<IHttpClientFactory>()
                                .CreateClient();
                            return new Knowledge.Mcp.Services.OllamaEmbeddingService(
                                httpClient,
                                ollamaBaseUrl,
                                modelName
                            );
                        });
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Embedding provider '{activeProvider}' not supported in MCP server. "
                                + "Currently only Ollama is supported. Set ActiveProvider to 'Ollama' in appsettings.json"
                        );
                    }

                    // Register KnowledgeManager (required for CrossKnowledgeSearchMcpTool)
                    services.AddScoped<KnowledgeEngine.KnowledgeManager>();

                    // Register agent plugins (required for MCP tool implementations)
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.CrossKnowledgeSearchPlugin>();
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.ModelRecommendationAgent>();
                    services.AddScoped<KnowledgeEngine.Agents.Plugins.KnowledgeAnalyticsAgent>();

                    // Register MCP resource provider (Phase 2B: Resources protocol)
                    // Note: KnowledgeResourceMethods is now an instance class registered via .WithResources<>()
                    services.AddScoped<Knowledge.Mcp.Resources.KnowledgeResourceProvider>();

                    // Configure MCP server with STDIO transport
                    // Note: .WithResources<>() handles both list and read operations automatically
                    // The SDK scans for [McpServerResource] attributes and builds the resource catalog
                    services
                        .AddMcpServer()
                        .WithStdioServerTransport()
                        .WithToolsFromAssembly()
                        .WithResources<Knowledge.Mcp.Resources.KnowledgeResourceMethods>();
                    // NOTE: resources/templates/list is automatically generated from McpServerResource attributes
                    // No need for explicit WithListResourceTemplatesHandler - SDK discovers templates automatically

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddConsole();
                    });
                }
            );

    /// <summary>
    /// Runs the MCP server in HTTP SSE mode for web clients
    /// </summary>
    static async Task RunHttpServer(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure base path for appsettings.json
        var basePath = System.IO.Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );
        builder.Configuration.SetBasePath(basePath!);
        builder.Configuration.AddJsonFile(
            "appsettings.json",
            optional: false,
            reloadOnChange: true
        );
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);

        Console.WriteLine($"MCP Server configuration base path: {basePath}");

        // Load ChatCompleteSettings from configuration
        var chatCompleteSettings = new ChatCompleteSettings();
        builder.Configuration.GetSection("ChatCompleteSettings").Bind(chatCompleteSettings);
        var databasePath = chatCompleteSettings.DatabasePath;

        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new Exception("DatabasePath not configured in appsettings.json.");
        }

        Console.WriteLine($"MCP Server using database path: {databasePath}");

        // Force Qdrant configuration
        chatCompleteSettings.VectorStore.Provider = "Qdrant";
        chatCompleteSettings.VectorStore.Qdrant.Port = 6334;

        // Register settings
        builder.Services.AddSingleton(chatCompleteSettings);

        // Register MCP server specific settings
        var mcpServerSettings = new McpServerSettings();
        builder.Configuration.GetSection(McpServerSettings.SectionName).Bind(mcpServerSettings);
        builder.Services.AddSingleton(mcpServerSettings);

        // Configure HTTP client services
        builder.Services.AddHttpClient();

        // Configure database services
        builder.Services.AddKnowledgeData(databasePath);

        // Register Qdrant services
        builder.Services.AddSingleton(chatCompleteSettings.VectorStore.Qdrant);
        builder.Services.AddSingleton<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>(
            provider =>
            {
                var qdrantSettings = chatCompleteSettings.VectorStore.Qdrant;
                var qdrantClient = new Qdrant.Client.QdrantClient(
                    host: qdrantSettings.Host,
                    port: qdrantSettings.Port,
                    https: qdrantSettings.UseHttps,
                    apiKey: qdrantSettings.ApiKey
                );
                return new Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore(
                    qdrantClient,
                    ownsClient: true
                );
            }
        );

        // Register Index Manager
        builder.Services.AddScoped<
            KnowledgeEngine.Persistence.IndexManagers.IIndexManager,
            KnowledgeEngine.Persistence.IndexManagers.QdrantIndexManager
        >();

        // Register Vector Store Strategy
        builder.Services.AddScoped<
            KnowledgeEngine.Persistence.VectorStores.IVectorStoreStrategy,
            KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy
        >(provider =>
        {
            var vectorStore =
                provider.GetRequiredService<Microsoft.SemanticKernel.Connectors.Qdrant.QdrantVectorStore>();
            var qdrantSettings =
                provider.GetRequiredService<ChatCompletion.Config.QdrantSettings>();
            var indexManager =
                provider.GetRequiredService<KnowledgeEngine.Persistence.IndexManagers.IIndexManager>();
            return new KnowledgeEngine.Persistence.VectorStores.QdrantVectorStoreStrategy(
                vectorStore,
                qdrantSettings,
                indexManager,
                chatCompleteSettings
            );
        });

        // Register system health services
        builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();
        builder.Services.AddScoped<IUsageTrackingService, SqliteUsageTrackingService>();

        // Register component health checkers
        builder.Services.AddScoped<IComponentHealthChecker, SqliteHealthChecker>();
        builder.Services.AddScoped<IComponentHealthChecker, OllamaHealthChecker>();
        builder.Services.AddScoped<IComponentHealthChecker, QdrantHealthChecker>();

        // Register knowledge management services
        builder.Services.AddScoped<KnowledgeEngine.Persistence.Sqlite.SqliteDbContext>(
            provider => new KnowledgeEngine.Persistence.Sqlite.SqliteDbContext(databasePath)
        );

        builder.Services.AddScoped<
            KnowledgeEngine.Persistence.IKnowledgeRepository,
            KnowledgeEngine.Persistence.Sqlite.Repositories.SqliteKnowledgeRepository
        >();

        // Register embedding services
        var activeProvider = chatCompleteSettings.EmbeddingProviders?.ActiveProvider ?? "Ollama";
        if (activeProvider == "Ollama")
        {
            var ollamaSettings = chatCompleteSettings.EmbeddingProviders?.Ollama;
            var ollamaBaseUrl = chatCompleteSettings.OllamaBaseUrl ?? "http://localhost:11434";
            var modelName = ollamaSettings?.ModelName ?? "nomic-embed-text";

            Console.WriteLine(
                $"MCP Server configuring Ollama embedding: {ollamaBaseUrl} with model {modelName}"
            );

            builder.Services.AddScoped<Microsoft.Extensions.AI.IEmbeddingGenerator<
                string,
                Microsoft.Extensions.AI.Embedding<float>
            >>(provider =>
            {
                var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Knowledge.Mcp.Services.OllamaEmbeddingService(
                    httpClient,
                    ollamaBaseUrl,
                    modelName
                );
            });
        }
        else
        {
            throw new NotSupportedException(
                $"Embedding provider '{activeProvider}' not supported in MCP server. "
                    + "Currently only Ollama is supported."
            );
        }

        // Register KnowledgeManager
        builder.Services.AddScoped<KnowledgeEngine.KnowledgeManager>();

        // Register agent plugins
        builder.Services.AddScoped<KnowledgeEngine.Agents.Plugins.CrossKnowledgeSearchPlugin>();
        builder.Services.AddScoped<KnowledgeEngine.Agents.Plugins.ModelRecommendationAgent>();
        builder.Services.AddScoped<KnowledgeEngine.Agents.Plugins.KnowledgeAnalyticsAgent>();

        // Register MCP resource provider
        builder.Services.AddScoped<Knowledge.Mcp.Resources.KnowledgeResourceProvider>();

        // ⭐ Configure URL from settings
        var httpTransportSettings = mcpServerSettings.HttpTransport;
        var serverUrl = $"http://{httpTransportSettings.Host}:{httpTransportSettings.Port}";
        builder.WebHost.UseUrls(serverUrl);

        Console.WriteLine($"MCP Server configured to listen on: {serverUrl}");

        // ⭐ Add CORS (required for web clients like MCP Inspector)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var corsSettings = httpTransportSettings.Cors;

                if (corsSettings.AllowAnyOrigin)
                {
                    Console.WriteLine(
                        "⚠️  WARNING: CORS AllowAnyOrigin is enabled - suitable for development only!"
                    );
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                }
                else
                {
                    Console.WriteLine(
                        $"CORS configured with allowed origins: {string.Join(", ", corsSettings.AllowedOrigins)}"
                    );
                    policy
                        .WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();

                    if (corsSettings.AllowCredentials)
                    {
                        policy.AllowCredentials();
                    }
                }

                if (corsSettings.ExposedHeaders.Length > 0)
                {
                    policy.WithExposedHeaders(corsSettings.ExposedHeaders);
                }
            });
        });

        // ⭐ Read Auth0 configuration
        var auth0Enabled = builder.Configuration.GetValue<bool>("Auth0:Enabled", false);

        if (auth0Enabled)
        {
            var domain = $"https://{builder.Configuration["Auth0:Domain"]}";
            var audience = builder.Configuration["Auth0:Audience"];

            // Auth0 JWT tokens have trailing slash in issuer claim, ensure we match it
            var issuer = domain.EndsWith("/") ? domain : $"{domain}/";

            Console.WriteLine($"⚠️  OAuth 2.1 Authentication ENABLED");
            Console.WriteLine($"  Authority: {domain}");
            Console.WriteLine($"  Issuer (for scope validation): {issuer}");
            Console.WriteLine($"  Audience: {audience}");

            // ⭐ Configure JWT Bearer authentication
            builder
            .Services.AddAuthentication(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
            )
            .AddJwtBearer(options =>
            {
                options.Authority = domain;
                options.Audience = audience;
                options.TokenValidationParameters =
                    new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true, // Verify signature using Auth0's public key
                        ValidateIssuer = true, // Verify token came from our Auth0 tenant
                        ValidateAudience = true, // CRITICAL: Prevents token passthrough attack
                        ValidateLifetime = true, // Verify token hasn't expired
                        ClockSkew = TimeSpan.FromSeconds(300), // Allow 5 min clock difference
                    };

                // Customize WWW-Authenticate header for 401 responses
                options.Challenge = $"Bearer realm=\"mcp-server\", authorization_uri=\"http://192.168.50.91:5001/.well-known/oauth-authorization-server\"";

                // Add event handlers for debugging OAuth flow
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[OAuth Debug] Authentication failed: {context.Exception.Message}");
                        if (context.Exception.InnerException != null)
                        {
                            Console.WriteLine($"[OAuth Debug] Inner exception: {context.Exception.InnerException.Message}");
                        }

                        // Try to decode token header to see kid
                        if (!string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
                        {
                            var authHeader = context.Request.Headers["Authorization"].ToString();
                            if (authHeader.StartsWith("Bearer "))
                            {
                                var token = authHeader.Substring(7);
                                var parts = token.Split('.');
                                if (parts.Length >= 2)
                                {
                                    try
                                    {
                                        var headerJson = System.Text.Encoding.UTF8.GetString(
                                            Convert.FromBase64String(parts[0].PadRight(parts[0].Length + (4 - parts[0].Length % 4) % 4, '=')));
                                        Console.WriteLine($"[OAuth Debug] Token header: {headerJson}");
                                    }
                                    catch { }
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                        Console.WriteLine($"[OAuth Debug] Token validated successfully. Claims: {string.Join(", ", claims ?? Array.Empty<string>())}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"[OAuth Debug] Challenge issued. Error: {context.Error}, Description: {context.ErrorDescription}");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Token;
                        Console.WriteLine($"[OAuth Debug] Token received: {(string.IsNullOrEmpty(token) ? "NONE" : "Present (length: " + token.Length + ")")}");
                        return Task.CompletedTask;
                    }
                };
            });

        // ⭐ Configure authorization policies for each scope
        builder.Services.AddAuthorization(options =>
        {
            // Policy for read-only operations (resources, health checks)
            options.AddPolicy(
                "mcp:read",
                policy =>
                    policy.Requirements.Add(
                        new Knowledge.Mcp.Authorization.HasScopeRequirement("mcp:read", issuer)
                    )
            );

            // Policy for tool execution (search, analytics, recommendations)
            options.AddPolicy(
                "mcp:execute",
                policy =>
                    policy.Requirements.Add(
                        new Knowledge.Mcp.Authorization.HasScopeRequirement("mcp:execute", issuer)
                    )
            );

            // Policy for admin operations (future: manage knowledge bases)
            options.AddPolicy(
                "mcp:admin",
                policy =>
                    policy.Requirements.Add(
                        new Knowledge.Mcp.Authorization.HasScopeRequirement("mcp:admin", issuer)
                    )
            );
        });

            // ⭐ Register the scope authorization handler as a singleton
            builder.Services.AddSingleton<
                Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
                Knowledge.Mcp.Authorization.HasScopeHandler
            >();
        }
        else
        {
            Console.WriteLine($"⚠️  OAuth 2.1 Authentication DISABLED - Server is running WITHOUT authentication!");
            Console.WriteLine($"  ⚠️  This should ONLY be used for local development/testing");
        }

        // ⭐ Configure MCP server with HTTP transport
        builder
            .Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                // Note: SessionTimeout and IsStateless properties may not be available in v0.4.0-preview.2
                // These will be configured when SDK is updated to support them
                // TODO: Uncomment when SDK supports these properties:
                // options.SessionTimeout = TimeSpan.FromMinutes(httpTransportSettings.SessionTimeoutMinutes);
                // options.IsStateless = httpTransportSettings.EnableStatelessMode;

                Console.WriteLine(
                    $"HTTP Transport configured with {httpTransportSettings.SessionTimeoutMinutes} minute session timeout (when SDK supports it)"
                );
            })
            .WithToolsFromAssembly()
            .WithResources<Knowledge.Mcp.Resources.KnowledgeResourceMethods>();

        // Log OAuth status
        if (httpTransportSettings.OAuth?.Enabled == true)
        {
            Console.WriteLine(
                "⚠️  OAuth 2.1 authentication configured but not yet implemented (Milestone #23)"
            );
            Console.WriteLine(
                $"   Authorization Server: {httpTransportSettings.OAuth.AuthorizationServerUrl}"
            );
        }

        var app = builder.Build();

        // ⭐ Register OAuth metadata endpoints (RFC 9728) - only if OAuth is enabled
        if (auth0Enabled)
        {
            var auth0Domain = builder.Configuration["Auth0:Domain"];
            var apiAudience = builder.Configuration["Auth0:Audience"] ?? "";
            app.MapOAuthMetadataEndpoints(auth0Domain!, apiAudience);
        }

        // ⭐ Enable CORS (must be before UseRouting)
        app.UseCors();

        // ⭐ Add routing middleware (required for endpoint mapping)
        app.UseRouting();

        // ⭐ Enable authentication and authorization middleware - only if OAuth is enabled
        if (auth0Enabled)
        {
            // IMPORTANT: Must come AFTER UseCors() and UseRouting(), BEFORE MapMcp()
            app.UseAuthentication(); // First: Validate JWT token
            app.UseAuthorization(); // Second: Check if user has required scopes

            Console.WriteLine("Authentication and authorization middleware enabled");

            // ⭐ Add WWW-Authenticate header to 401 responses (MCP spec requirement)
            app.UseWWWAuthenticateHeader(serverUrl);
            Console.WriteLine($"WWW-Authenticate header middleware enabled (authorization_uri: {serverUrl}/.well-known/oauth-authorization-server)");
        }

        // ⭐ Add exception handling for debugging
        app.Use(
            async (context, next) =>
            {
                try
                {
                    await next(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Unhandled exception in HTTP request:");
                    Console.WriteLine($"   Path: {context.Request.Path}");
                    Console.WriteLine($"   Method: {context.Request.Method}");
                    Console.WriteLine($"   Error: {ex.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                    throw;
                }
            }
        );

        // ⭐ Map health check endpoints (no auth required)
        app.MapHealthEndpoints();

        // ⭐ Map MCP endpoints (creates /sse and /messages endpoints)
        // Require mcp:execute scope for all MCP operations (tool execution) - only if OAuth enabled
        if (auth0Enabled)
        {
            app.MapMcp()
                .RequireAuthorization("mcp:execute");

            Console.WriteLine("Knowledge MCP Server HTTP endpoints:");
            Console.WriteLine("  GET  /sse       - Server-Sent Events stream (requires mcp:execute scope)");
            Console.WriteLine("  POST /messages  - JSON-RPC requests (requires mcp:execute scope)");
        }
        else
        {
            app.MapMcp(); // No authorization required when OAuth is disabled

            Console.WriteLine("⚠️  Knowledge MCP Server HTTP endpoints (UNAUTHENTICATED):");
            Console.WriteLine("  GET  /sse       - Server-Sent Events stream (NO AUTH)");
            Console.WriteLine("  POST /messages  - JSON-RPC requests (NO AUTH)");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Server listening on: {app.Urls.FirstOrDefault() ?? "http://localhost:5000"}"
        );

        await app.RunAsync();
    }
}
