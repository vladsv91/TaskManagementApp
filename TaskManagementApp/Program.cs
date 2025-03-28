using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.ServiceBus;
using TaskManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure RabbitMQ
builder.Services.Configure<RabbitMqConfig>(
    builder.Configuration.GetSection("RabbitMQ"));

// Register services
builder.Services.AddSingleton<IServiceBusHandler, ServiceBusHandler>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddHostedService<TaskMessageProcessor>();
builder.Services.AddRouting(options => options.LowercaseUrls = true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Create database if it doesn't exist
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();