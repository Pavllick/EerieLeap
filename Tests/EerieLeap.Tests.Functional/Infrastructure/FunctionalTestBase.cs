using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EerieLeap.Domain.SensorDomain.Models;
using EerieLeap.Tests.Functional.Models;
using EerieLeap.Utilities.IO;

namespace EerieLeap.Tests.Functional.Infrastructure;

public class FunctionalTestBase : IDisposable {
    private readonly TestWebApplicationFactory _factory;
    protected readonly HttpClient Client;
    private bool _initialized;
    private bool _disposed; 

    protected FunctionalTestBase() {
        ClearConfigDirectory();

        _factory = new TestWebApplicationFactory();
        Client = _factory.CreateClient(); 
    }

    public async Task ConfigureAdc() {
        if (_initialized)
            return;

        try {
            string adcConfigScript = await File.ReadAllTextAsync(PathResolver.GetFullPath("adc_config_script.js"));
            using var adcConfigScriptContent = new StringContent(adcConfigScript, Encoding.UTF8, "application/javascript");
            var response = await Client.PostAsync("api/v1/config/adc/script", adcConfigScriptContent);

            if (!response.IsSuccessStatusCode) {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to initialize ADC config script: {(int)response.StatusCode} - {content}");
            }

            var adcConfig = AdcConfigRequest.CreateValid();
            response = await Client.PostAsJsonAsync("api/v1/config/adc", adcConfig);

            if (!response.IsSuccessStatusCode) {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to initialize ADC config: {(int)response.StatusCode} - {content}");
            }

            _initialized = true;
        } catch (Exception) {
            Dispose();
            throw;
        }
    }

    private void ClearConfigDirectory() {
        if (Directory.Exists(AppConstants.ConfigDirPath))
            Directory.Delete(AppConstants.ConfigDirPath, true);
    }

    protected virtual void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                ClearConfigDirectory();

                // Dispose managed resources
                Client?.Dispose();
                _factory.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected async Task<T?> GetAsync<T>(string url) where T : class {
        var response = await Client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get data: {(int)response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<string> Get2Async(string url) {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
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

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to post sensor configs: {(int)response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

        // Allow time for readings to be collected
        await Task.Delay(delayMs);
    }
}
