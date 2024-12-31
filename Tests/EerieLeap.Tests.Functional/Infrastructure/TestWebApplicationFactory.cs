using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EerieLeap.Repositories;
using Microsoft.Extensions.Options;
using EerieLeap.Configuration;

namespace EerieLeap.Tests.Functional.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program> {
    protected override IHost CreateHost(IHostBuilder builder) {
        builder.ConfigureServices(services => {
            services.AddLogging(logging => {
                logging.ClearProviders();
                //logging.AddConsole(); // NOTE: Uncomment to enable console logging
                logging.SetMinimumLevel(LogLevel.Debug);
                //logging.AddFilter((category, level) =>
                //    level >= LogLevel.Error);
            });

            // Replace file-based repository with in-memory one
            var configurationRepositoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConfigurationRepository));
            if (configurationRepositoryDescriptor != null)
                services.Remove(configurationRepositoryDescriptor);

            services.AddSingleton<IConfigurationRepository>(sp => {
                var logger = sp.GetRequiredService<ILogger<InMemoryConfigurationRepository>>();
                return new InMemoryConfigurationRepository(logger);
            });

            // Override ConfigurationOptions with test-specific configuration
            var configurationOptionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOptions<ConfigurationOptions>));
            if (configurationOptionsDescriptor != null)
                services.Remove(configurationOptionsDescriptor);

            services.Configure<ConfigurationOptions>(options =>
                options.ConfigurationLoadRetryMs = 100);
        });

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing) =>
        base.Dispose(disposing);
}
