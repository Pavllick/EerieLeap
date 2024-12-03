using EerieLeap.Repositories;
using System.Text.Json.Serialization;
using EerieLeap.Configuration;
using EerieLeap.Controllers.Filters;
using EerieLeap.Domain.AdcDomain.Services;
using EerieLeap.Domain.AdcDomain.Hardware;
using EerieLeap.Domain.SensorDomain.Services;
using EerieLeap.Domain.SensorDomain.Processing;
using EerieLeap.Domain.SensorDomain.Processing.SensorTypeProcessors;

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
            .ConfigureApiBehaviorOptions(options => {
                options.SuppressModelStateInvalidFilter = true;
            });

        // Register services
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AdcFactory>();

        // Configure and register configuration options
        builder.Services.Configure<ConfigurationOptions>(
            builder.Configuration.GetSection(nameof(ConfigurationOptions)));

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

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
