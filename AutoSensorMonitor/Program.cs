using AutoSensorMonitor.Service.Hardware;
using AutoSensorMonitor.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add our services
builder.Services.AddSingleton<AdcFactory>();
builder.Services.AddSingleton<ISensorReadingService, SensorReadingService>();
builder.Services.AddHostedService<SensorReadingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
