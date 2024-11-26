using EerieLeap.Services;
using EerieLeap.Hardware;
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

        // Register SensorReadingService as both ISensorReadingService and IHostedService
        builder.Services.AddSingleton<SensorReadingService>();
        builder.Services.AddSingleton<ISensorReadingService>(sp => sp.GetRequiredService<SensorReadingService>());
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SensorReadingService>());

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
