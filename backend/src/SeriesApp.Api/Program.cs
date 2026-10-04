using SeriesApp.Api;
using SeriesApp.SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddModules(builder.Configuration, ModuleRegistry.Modules);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.MapModules(ModuleRegistry.Modules);

app.Run();

// Exposed so the integration test project can boot the real host through
// WebApplicationFactory<Program>.
public partial class Program;
