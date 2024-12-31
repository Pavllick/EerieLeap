using EerieLeap.Repositories;
using System.Text.Json.Serialization;
using EerieLeap.Configuration;
using EerieLeap.Controllers.Filters;
using EerieLeap.Domain.AdcDomain.Services;
using EerieLeap.Domain.AdcDomain.Hardware;
using EerieLeap.Domain.SensorDomain.Services;
using EerieLeap.Domain.SensorDomain.Processing;
using EerieLeap.Domain.SensorDomain.Processing.SensorTypeProcessors;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace EerieLeap;

public sealed class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services
            .AddControllers(options =>
                options.Filters.Add<ValidationExceptionFilter>())
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.IncludeFields = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.WriteIndented = true;
            })
            .ConfigureApiBehaviorOptions(options =>
                options.SuppressModelStateInvalidFilter = true);

        // Configure and register configuration options
        builder.ConfigureAppSettings();

        // Register services
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AdcFactory>();

        // Register configuration repository
        builder.Services.AddSingleton<IConfigurationRepository, JsonConfigurationRepository>();

        // Register configuration services
        builder.Services.AddSingleton<IAdcConfigurationService, AdcConfigurationService>();
        builder.Services.AddSingleton<ISensorConfigurationService, SensorConfigurationService>();

        // Register shared reading buffer
        builder.Services.AddSingleton<SensorReadingBuffer>();

        // Register SensorReading processors
        // builder.Services.AddSingleton<BaseSensorReadingProcessor>();
        builder.Services.AddSingleton<PhysicalSensorProcessor>();
        builder.Services.AddSingleton<VirtualSensorProcessor>();
        builder.Services.AddSingleton<SensorTypeRoutingProcessor>();

        // Register the processor chain as ISensorReadingProcessor
        builder.Services.AddSingleton<ISensorReadingProcessor>(sp => {
            var routingProcessor = sp.GetRequiredService<SensorTypeRoutingProcessor>();
            // var baseProcessor = sp.GetRequiredService<BaseSensorReadingProcessor>();
            // return new SensorReadingChainedProcessor(routingProcessor, baseProcessor);
            return new SensorReadingChainedProcessor(routingProcessor);
        });

        // Register SensorReadingService last since it depends on ISensorReadingProcessor
        builder.Services.AddSingleton<ISensorReadingService, SensorReadingService>();
        builder.Services.AddHostedService(sp => (SensorReadingService)sp.GetRequiredService<ISensorReadingService>());

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure logging
        builder.Services.AddLogging();
        builder.Services.AddSingleton(sp => {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger(typeof(Program).Namespace!);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();
        //app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}

public static class ProgramExtensions {
    public static IHostApplicationBuilder ConfigureAppSettings(this IHostApplicationBuilder app) {
        var configurationFilePath = Path.Combine(AppConstants.ConfigDirPath, $"{AppConstants.SettingsConfigFileName}.json");

        if (!File.Exists(configurationFilePath)) {
            var defaultConfig = new Settings();

            var json = JsonSerializer.Serialize(new { Settings = defaultConfig });

            Directory.CreateDirectory(AppConstants.ConfigDirPath);
            File.WriteAllText(configurationFilePath, json);
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configurationFilePath, optional: false, reloadOnChange: true)
            .Build();

        app.Services.Configure<Settings>(configuration.GetSection(nameof(Settings)));

        return app;
    }
}
