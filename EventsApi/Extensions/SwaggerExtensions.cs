namespace EventsApi.Extensions;

public static class SwaggerExtensions
{
    public static WebApplication MapOpenApiWithVersions(this WebApplication app)
    {
        app.MapOpenApi().WithDocumentPerVersion();
        return app;
    }

    public static WebApplication UseSwaggerUiWithVersions(this WebApplication app)
    {
        app.UseSwaggerUI(options =>
        {
            foreach (var description in app.DescribeApiVersions().Reverse())
            {
                options.SwaggerEndpoint(
                    $"/openapi/{description.GroupName}.json",
                    description.GroupName.ToUpperInvariant());
            }
        });
        return app;
    }
}
