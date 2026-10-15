namespace EventsApi.Extensions;

public static class SwaggerExtensions
{
    public static WebApplication UseSwaggerUi(this WebApplication app)
    {
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Events API");
        });
        return app;
    }
}
