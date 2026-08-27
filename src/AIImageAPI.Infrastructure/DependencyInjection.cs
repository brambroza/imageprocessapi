using AIImageAPI.Core.Services;
using AIImageAPI.Infrastructure.Data;
using AIImageAPI.Infrastructure.Face;
using AIImageAPI.Infrastructure.Options;
using AIImageAPI.Infrastructure.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIImageAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAIImageInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FaceOptions>(configuration.GetSection(FaceOptions.SectionName));

        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddSingleton<IFaceRecognitionService, OnnxFaceRecognitionService>();
        services.AddSingleton<ICustomerMatchService, InMemoryCustomerMatchService>();
        services.AddScoped<IRecommendationService, CoOccurrenceRecommendationService>();

        return services;
    }
}
