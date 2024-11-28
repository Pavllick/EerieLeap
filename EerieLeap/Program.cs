using EerieLeap.Services;
using EerieLeap.Hardware;
using EerieLeap.Repositories;
using System.Text.Json.Serialization;

namespace EerieLeap;

public sealed class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services
            .AddControllers()
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.IncludeFields = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        // Register services
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AdcFactory>();

        // Register configuration repository
        builder.Services.AddSingleton<IConfigurationRepository, JsonConfigurationRepository>();

        // Register configuration services
        builder.Services.AddSingleton<IAdcConfigurationService, AdcConfigurationService>();
        builder.Services.AddSingleton<ISensorConfigurationService, SensorConfigurationService>();
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
