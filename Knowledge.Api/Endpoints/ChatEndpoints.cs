using Knowledge.Api.Filters;
using Knowledge.Contracts;
using KnowledgeEngine.Chat;
using KnowledgeEngine.Document;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Knowledge.Api.Endpoints;

/// <summary>
/// Chat endpoints for conversational AI interactions.
/// </summary>
public static class ChatEndpoints
{
    /// <summary>
    /// Maps chat-related endpoints to the application.
    /// </summary>
    public static RouteGroupBuilder MapChatEndpoints(this RouteGroupBuilder group)
    {
        // POST /api/chat
        group
            .MapPost(
                "/",
                async (
                    ChatRequestDto dto,
                    [FromServices] IChatService chat,
                    CancellationToken ct
                ) =>
                {
                    var reply = await chat.GetReplyAsync(dto, dto.Provider, ct);
                    if (dto.StripMarkdown)
                        reply = MarkdownStripper.ToPlain(reply);

                    return Results.Ok(
                        new ChatResponseDto { Reply = reply, ConversationId = dto.ConversationId! }
                    );
                }
            )
            .AddEndpointFilter<ValidationFilter>()
            .WithOpenApi(op =>
            {
                op.Summary = "Chat with a model";

                // Build a sample JSON object
                var sample = new OpenApiObject
                {
                    ["knowledgeId"] = new OpenApiNull(), // null = global chat
                    ["message"] = new OpenApiString("Hello"),
                    ["temperature"] = new OpenApiDouble(0.7),
                    ["stripMarkdown"] = new OpenApiBoolean(false),
                    ["useExtendedInstructions"] = new OpenApiBoolean(true),
                    ["conversationId"] = new OpenApiNull(),
                    ["provider"] = new OpenApiString("Ollama"),
                    ["ollamaModel"] = new OpenApiString("llama3.2:3b"),
                    ["useAgent"] = new OpenApiBoolean(false),
                };

                // Ensure requestBody is present and targeted at application/json
                op.RequestBody ??= new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>(),
                };
                op.RequestBody.Content["application/json"] = new OpenApiMediaType
                {
                    Example = sample, // 👈 here's the example
                };

                return op;
            })
            .Produces<ChatResponseDto>()
            .WithTags("Chat");

        return group;
    }
}
