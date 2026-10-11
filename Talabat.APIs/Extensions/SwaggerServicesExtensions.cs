using Microsoft.OpenApi;

namespace Talabat.APIs.Extensions;

public static class  SwaggerServicesExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Version = "v1.0.0",
                Title = "Talabat APIs",
                Description = $"this is the API documentation for Talabat",
                Contact = new()
                {
                    Name = "maged taylor",
                    Email = "magedtawfek187@gmail.com",
                }
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter a valid JWT access token. Swagger will send it as: Bearer {token}.",
                In = ParameterLocation.Header,
                Scheme = "Bearer",
                Type = SecuritySchemeType.Http
            });

            c.OperationFilter<BearerSecurityOperationFilter>();
        });
        
        return services;
    }
    public static WebApplication UseSwaggerMiddleWare(this WebApplication app)
    {
        app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
        app.UseSwaggerUI(x =>
        {
            x.SwaggerEndpoint("/openapi/v1.json", "My v1");
            x.EnablePersistAuthorization();
        });
        return app;
    }
}

internal sealed class BearerSecurityOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(
        Microsoft.OpenApi.OpenApiOperation operation,
        Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>()
            .Any())
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}