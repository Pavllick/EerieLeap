using EerieLeap.Services;
using EerieLeap.Utilities.Converters;
using System.Text.Json.Serialization;
using EerieLeap.Utilities.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EerieLeap.Hardware;

namespace EerieLeap;

public static class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services
            .AddControllers(options => options.Filters.Add<ModelStateValidationFilter>())
            .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

        // Register services
        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AdcFactory>();
        builder.Services.AddSingleton<ValidationJsonConverterFactory>();

        // Register SensorReadingService as both ISensorReadingService and IHostedService
        builder.Services.AddSingleton<SensorReadingService>();
        builder.Services.AddSingleton<ISensorReadingService>(sp => sp.GetRequiredService<SensorReadingService>());
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SensorReadingService>());

        // Configure base JSON options
        builder.Services.Configure<JsonOptions>(options => {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.IncludeFields = true;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.WriteIndented = true;
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure logging
        builder.Services.AddLogging();
        builder.Services.AddSingleton(sp => {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger("EerieLeap");
        });

        var app = builder.Build();

        // Configure JSON converters after app is built
        var jsonOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
        var converterFactory = app.Services.GetRequiredService<ValidationJsonConverterFactory>();

        // Add converters for all types that need validation in our assembly
        var assembly = typeof(Program).Assembly;
        foreach (var converter in converterFactory.CreateConvertersForAssembly(assembly))
            jsonOptions.JsonSerializerOptions.Converters.Add(converter);

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
