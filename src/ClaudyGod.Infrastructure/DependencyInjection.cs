using ClaudyGod.Application.Common.Interfaces;
using ClaudyGod.Infrastructure.Configuration;
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
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();

        // Website upload pipeline — fails fast at boot if misconfigured rather than
        // degrading silently on the first admin upload attempt weeks later (the
        // exact failure mode this whole pipeline was built to eliminate).
        services.Configure<WebsiteStorageOptions>(configuration.GetSection(WebsiteStorageOptions.SectionName));
        var storageOptions = configuration.GetSection(WebsiteStorageOptions.SectionName).Get<WebsiteStorageOptions>()
            ?? new WebsiteStorageOptions();
        var missingKeys = storageOptions.MissingKeys().ToList();
        if (missingKeys.Count > 0)
            throw new InvalidOperationException(
                $"Website storage is not configured — missing: {string.Join(", ", missingKeys)}. " +
                "Set these via the Storage__Website__* environment variables before starting the app.");
        services.AddScoped<IWebsiteStorageService, WebsiteS3StorageService>();

        // AI service — typed HttpClient with 30-second timeout
        services.AddHttpClient<IAIService, AIAIService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Paystack verification — typed HttpClient with 20-second timeout
        services.AddHttpClient<IPaystackService, PaystackService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpContextAccessor();

        return services;
    }
}
