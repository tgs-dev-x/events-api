using EventsApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersioningWithOpenApi();

var app = builder.Build();

app.UseSwaggerWithVersions();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
