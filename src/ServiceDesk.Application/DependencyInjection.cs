using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Features.Auth;
using ServiceDesk.Application.Features.Catalog;
using ServiceDesk.Application.Features.Chat;
using ServiceDesk.Application.Features.Tickets;

namespace ServiceDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogVerificationService, CatalogVerificationService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
