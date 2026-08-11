using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ServiceDesk.Api.Middleware;
using ServiceDesk.Application;
using ServiceDesk.Infrastructure;
using ServiceDesk.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

using IServiceScope scope = app.Services.CreateScope();
ServiceDeskDbInitializer initializer = scope.ServiceProvider.GetRequiredService<ServiceDeskDbInitializer>();
await initializer.SeedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "/openapi/{documentName}.json");
    app.UseSwaggerUI();

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;

