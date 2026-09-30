namespace EventsApi.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseSwaggerWithVersions(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi().WithDocumentPerVersion();

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
