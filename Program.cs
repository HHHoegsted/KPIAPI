using KPIAPI.Data;
using KPIAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register AppDbContext (adjust options as needed for your environment)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services for dependency injection
builder.Services.AddScoped<RobotService>();
builder.Services.AddScoped<RunsService>();
builder.Services.AddScoped<RunEventsService>();
builder.Services.AddScoped<MetaService>();
builder.Services.AddScoped<KpiDefinitionsService>();

// Register the heartbeat timeout service as a hosted service
builder.Services.AddHostedService<RunHeartbeatTimeoutService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
