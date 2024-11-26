using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;
using EerieLeap.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using EerieLeap.Utilities;
using EerieLeap.Tests.Functional.Models;

namespace EerieLeap.Tests.Functional.Infrastructure;

public class FunctionalTestBase : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime {
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected FunctionalTestBase(WebApplicationFactory<Program> factory) {
        Factory = factory.WithWebHostBuilder(builder => {
            builder.ConfigureServices(services => {
                services.AddLogging(logging => {
                    logging.ClearProviders();
                    logging.AddFilter((category, level) =>
                        level >= LogLevel.Error);
                    // logging.AddDebug();
                    // logging.SetMinimumLevel(LogLevel.Debug);
                });
            });
        });
        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync() {
        // Set up initial ADC configuration
        var adcConfig = AdcConfigRequest.CreateValid();

        var response = await Client.PostAsJsonAsync("api/v1/config/adc", adcConfig);
        if (!response.IsSuccessStatusCode) {
            var content = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to initialize ADC config: {response.StatusCode} - {content}");
        }
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

    protected async Task<HttpResponseMessage> PostWithFullResponse<T>(string url, T content) where T : class {
        var json = JsonSerializer.Serialize(content);

        return await Client.PostAsJsonAsync(url, content);
    }

    protected async Task<HttpResponseMessage> PostWithFullResponse(string url, object content) =>
        await Client.PostAsJsonAsync(url, content);

    protected async Task<HttpResponseMessage> PostWithFullResponse(string url, string jsonContent) {
        using var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        return await Client.PostAsync(url, stringContent);
    }

    protected async Task PostSensorConfigsWithDelay(IEnumerable<SensorConfigRequest> configs, int delayMs = 1000) {
        var response = await PostAsync("api/v1/config/sensors", configs);
        response.EnsureSuccessStatusCode();

        // Allow time for readings to be collected
        await Task.Delay(delayMs);
    }
}
