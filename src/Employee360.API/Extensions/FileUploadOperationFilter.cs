using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Employee360.API.Extensions;

/// <summary>
/// Maps <see cref="IFormFile"/> parameters to multipart/form-data for Swagger generation.
/// </summary>
public sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source.Id.Equals("Form", StringComparison.OrdinalIgnoreCase)
                        || p.Type == typeof(IFormFile)
                        || p.Type == typeof(IFormFileCollection))
            .ToList();

        if (formParameters.Count == 0)
        {
            return;
        }

        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>();

        foreach (var parameter in formParameters)
        {
            if (parameter.Type == typeof(IFormFile) || parameter.Type == typeof(IFormFileCollection))
            {
                properties[parameter.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary",
                };
                required.Add(parameter.Name);
                continue;
            }

            properties[parameter.Name] = context.SchemaGenerator.GenerateSchema(
                parameter.Type,
                context.SchemaRepository);

            if (parameter.IsRequired)
            {
                required.Add(parameter.Name);
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required,
                    },
                },
            },
        };

        var formParameterNames = formParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        operation.Parameters = operation.Parameters
            .Where(p => !formParameterNames.Contains(p.Name))
            .ToList();
    }
}
