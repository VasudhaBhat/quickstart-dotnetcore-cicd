using Databases.Configuration.SeedWork;
using Databases.Identity;
using Databases.Identity.SeedWork;
using Domain.Configuration;
using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Auth.Server.Extensions
{
    public static class WebHostExtension
    {
        public static void SeedDatabase(this IWebHost host, IConfigurationRoot configuration)
        {
            var domainSettings = new DomainSettings();
            configuration.GetSection(nameof(DomainSettings)).Bind(domainSettings);
            var _environment = configuration["ActiveEnvironment"];

            var _logger = host.Services.GetRequiredService<ILogger<Program>>();

            using (var scope = host.Services.CreateScope())
            {
                // Migrate and seed the database during startup. Must be synchronous
                try
                {
                    {
                        var configurationDBContext = scope.ServiceProvider.GetService<ConfigurationDbContext>();
                        var persistentDBContext = scope.ServiceProvider.GetService<PersistedGrantDbContext>();
                        var identityDBContext = scope.ServiceProvider.GetService<IdentityContext>();
                        var identitySeedService = scope.ServiceProvider.GetService<IIdentitySeedWork>();
                        var configurationSeedService = scope.ServiceProvider.GetService<IConfigurationSeedWork>();

                        _logger.LogInformation("Database migration started");

                        configurationDBContext.Database.Migrate();
                        persistentDBContext.Database.Migrate();
                        identityDBContext.Database.Migrate();

                        _logger.LogInformation("Database migration complete");

                        
                        _logger.LogInformation("Seeding Identity data");
                        identitySeedService.SeedIdentityData(_environment).Wait();

                        if (_environment == "UAT")
                        {
                            _logger.LogInformation("Seeding Identity: Testing Data");
                            identitySeedService.SeedAdminTestingData().Wait();
                        }

                        if (_environment != "Production" && _environment != "UAT")
                        {
                            _logger.LogInformation("Seeding Identity: Testing Data");
                            identitySeedService.SeedTestingData().Wait();
                        }

                        _logger.LogInformation("Seeding configuration data");
                        configurationSeedService.SeedConfigurationData(domainSettings).Wait();

                        _logger.LogInformation("Seeding database complete");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical($"Failed to migrate or seed database. \n " +
                                      $"Message: {e.Message} \n " +
                                      $"Inner Exception: {e.InnerException} \n " +
                                      $"Stack trace: {e.StackTrace} \n ");
                }
            }
        }
    }
}