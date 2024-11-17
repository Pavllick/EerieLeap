using EerieLeap.Hardware;
using EerieLeap.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add our services
builder.Services.AddSingleton<AdcFactory>();

// Register SensorReadingService as both ISensorReadingService and IHostedService
builder.Services.AddSingleton<SensorReadingService>();
builder.Services.AddSingleton<ISensorReadingService>(sp => sp.GetRequiredService<SensorReadingService>());
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SensorReadingService>());

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
