using System;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class PythonCodeSampleFilter : IOperationFilter
{
    public void Apply(OpenApiOperation op, OperationFilterContext ctx)
    {
        if (
            !string.Equals(
                ctx.ApiDescription.HttpMethod,
                "POST",
                StringComparison.OrdinalIgnoreCase
            )
            || !ctx.ApiDescription.RelativePath!.EndsWith(
                "api/chat",
                StringComparison.OrdinalIgnoreCase
            )
        )
            return;

        var sampleObj = new OpenApiObject
        {
            ["lang"] = new OpenApiString("Python"),
            ["label"] = new OpenApiString("requests"),
            ["source"] = new OpenApiString(
                @"import requests

url = ""https://your-host/api/chat""
payload = {
    ""knowledgeId"": ""TrackerSpec"", 
    ""message"": ""Hello"", 
    ""temperature"": ""0.7"",
    ""stripMarkdown"": false,
    ""useExtendedInstructions"": false,
}
print(requests.post(url, json=payload, timeout=30).json()[""reply""])"
            ),
        };

        op.Extensions["x-codeSamples"] = new OpenApiArray { sampleObj };
    }
}
