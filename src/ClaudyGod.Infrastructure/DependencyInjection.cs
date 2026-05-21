using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Infrastructure.Persistence;
using ClaudyGod.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudyGod.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            ));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddHttpContextAccessor();

        return services;
    }
}
