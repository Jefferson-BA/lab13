using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VeterinariaApp.Api.Swagger;

public class AutorizacionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var atributos = context.MethodInfo.GetCustomAttributes(true)
            .Concat(context.MethodInfo.DeclaringType!.GetCustomAttributes(true)).ToArray();
        if (atributos.OfType<AllowAnonymousAttribute>().Any() || !atributos.OfType<AuthorizeAttribute>().Any())
            return;

        operation.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        }];
        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Falta un token válido." });
        if (atributos.OfType<AuthorizeAttribute>().Any(a => !string.IsNullOrWhiteSpace(a.Roles)))
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "El rol no permite esta operación." });
    }
}
