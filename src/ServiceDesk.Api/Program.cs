using Azure.Identity;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServiceDesk.Api.Hubs;
using ServiceDesk.Api.Middleware;
using ServiceDesk.Application;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Infrastructure;
using ServiceDesk.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

string? keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Copie el token JWT en el campo de valor."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        string[] allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (allowedOrigins.Length == 0)
        {
            allowedOrigins = ["http://localhost:5173", "http://127.0.0.1:5173"];
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

using IServiceScope scope = app.Services.CreateScope();
ServiceDeskDbInitializer initializer = scope.ServiceProvider.GetRequiredService<ServiceDeskDbInitializer>();
await initializer.SeedAsync();

IBlobStorageService blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
await blobStorage.EnsureContainerExistsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
    app.UseSwaggerUI();

    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

public partial class Program;

