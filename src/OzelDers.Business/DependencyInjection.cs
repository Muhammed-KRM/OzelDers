using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OzelDers.Business.Interfaces;
using OzelDers.Business.Services;

namespace OzelDers.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Manager'lar (Servisler)
        services.AddScoped<IAuthService, AuthManager>();
        services.AddScoped<IListingService, ListingManager>();
        services.AddScoped<ITokenService, TokenManager>();
        services.AddScoped<IMessageService, MessageManager>();
        services.AddScoped<IReviewService, ReviewManager>();

        // FluentValidation — Bu assembly'deki tüm Validator'ları otomatik tarayıp kaydet
        services.AddValidatorsFromAssemblyContaining<AuthManager>();

        return services;
    }
}
