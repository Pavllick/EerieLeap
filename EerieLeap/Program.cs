using EerieLeap.Services;
using EerieLeap.Utilities.Converters;
using System.Text.Json.Serialization;
using EerieLeap.Utilities.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using EerieLeap.Hardware;

namespace EerieLeap;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services
            .AddControllers(options => {
                options.Filters.Add<ModelStateValidationFilter>();
                // options.Filters.Add<ValidationExceptionFilter>();
            })
            .AddJsonOptions(options => {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.IncludeFields = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.WriteIndented = true;

                options.JsonSerializerOptions.Converters.Clear();

                // Add validation converters for the current assembly
                var converterFactory = new ValidationJsonConverterFactory(new ActionContextAccessor());
                foreach (var converter in converterFactory.CreateConvertersForAssembly(typeof(Program).Assembly)) {
                    options.JsonSerializerOptions.Converters.Add(converter);
                }
            })
            .ConfigureApiBehaviorOptions(options => {
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressInferBindingSourcesForParameters = true;
                options.SuppressModelStateInvalidFilter = true;
                options.SuppressMapClientErrors = true;
            });

        // Register services
        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<AdcFactory>();
        builder.Services.AddSingleton<ValidationJsonConverterFactory>();

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
            return loggerFactory.CreateLogger("EerieLeap");
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
