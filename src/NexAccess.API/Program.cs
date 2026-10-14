global using System;
using Microsoft.EntityFrameworkCore;
using NexAccess.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Ensure configuration reads appsettings from src/NexAccess.API/
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddOpenApi();

// Configure DbContext with placeholder connection (use User Secrets or env var named NEXACCESS_CONNECTION)
var connectionString = builder.Configuration.GetConnectionString("NexAccess") ?? Environment.GetEnvironmentVariable("NEXACCESS_CONNECTION");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<NexAccessDbContext>(options =>
        options.UseNpgsql(connectionString));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
