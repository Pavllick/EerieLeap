using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;
using EerieLeap.Configuration;
using System.Device.Spi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EerieLeap.Tests.Functional.Infrastructure;

public class FunctionalTestBase : IClassFixture<WebApplicationFactory<TestStartup>>, IAsyncLifetime {
    protected readonly WebApplicationFactory<TestStartup> Factory;
    protected readonly HttpClient Client;

    protected FunctionalTestBase(WebApplicationFactory<TestStartup> factory) {
        Factory = factory.WithWebHostBuilder(builder => {
            builder.ConfigureServices(services => {
                services.AddLogging(logging => {
                    // logging.ClearProviders();
                    // logging.AddFilter((category, level) =>
                    //     level >= LogLevel.Error);
                    logging.AddDebug();
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            });
        });
        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync() {
        // Set up initial ADC configuration
        var adcConfig = new AdcConfig {
            Type = "MCP3008",
            BusId = 0,
            ChipSelect = 0,
            ReferenceVoltage = 3.3,
            Resolution = 10,
            ClockFrequency = 1_000_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8,
            Protocol = new AdcProtocolConfig()
        };

        await PostAsync("api/v1/config/adc", adcConfig);
    }

    public Task DisposeAsync() =>
        Task.CompletedTask;

    protected async Task<T?> GetAsync<T>(string url) where T : class {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<HttpResponseMessage> GetWithFullResponse(string url) =>
        await Client.GetAsync(url);

    protected async Task<HttpResponseMessage> PostAsync<T>(string url, T content) where T : class {
        var response = await Client.PostAsJsonAsync(url, content);
        response.EnsureSuccessStatusCode();
        return response;
    }

    protected async Task<HttpResponseMessage> PostWithFullResponse<T>(string url, T content) where T : class =>
        await Client.PostAsJsonAsync(url, content);

    protected async Task PostSensorConfigsWithDelay(IEnumerable<SensorConfig> configs, int delayMs = 1000) {
        var response = await PostAsync("api/v1/config/sensors", configs);
        response.EnsureSuccessStatusCode();

        // Allow time for readings to be collected
        await Task.Delay(delayMs);
    }
}
