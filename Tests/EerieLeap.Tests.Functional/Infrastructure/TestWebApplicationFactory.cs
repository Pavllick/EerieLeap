using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EerieLeap.Repositories;

namespace EerieLeap.Tests.Functional.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program> {
    public static readonly string TestConfigPath = Path.Combine(Path.GetTempPath(), "EerieLeapTests", "Config");

    protected override IHost CreateHost(IHostBuilder builder) {
        builder.ConfigureServices(services => {
            services.AddLogging(logging => {
                logging.ClearProviders();
                // logging.AddConsole(); // NOTE: Uncomment to enable console logging
                logging.SetMinimumLevel(LogLevel.Debug);
                // logging.AddFilter((category, level) =>
                //     level >= LogLevel.Error);
            });

            // Replace file-based repository with in-memory one
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IConfigurationRepository));
            if (descriptor != null) {
                services.Remove(descriptor);
            }

            services.AddSingleton<IConfigurationRepository>(sp => {
                var logger = sp.GetRequiredService<ILogger<InMemoryConfigurationRepository>>();
                return new InMemoryConfigurationRepository(logger);
            });
        });

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing) =>
        base.Dispose(disposing);
}
