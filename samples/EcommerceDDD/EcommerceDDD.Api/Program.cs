using EcommerceDDD.Infrastructure.DependencyInjection;
using EcommerceDDD.Web.Infrastructure.Generated;
using Monbsoft.BrilliantMediator.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
 .WriteTo.File("logs/ecommerce-.txt", rollingInterval: RollingInterval.Day)
   .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
   options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
  {
     Title = "Ecommerce DDD API",
        Version = "v1",
        Description = "API pour la gestion des commandes e-commerce avec BrilliantMediator et DDD"
    });
});

// Infrastructure : repositories et domain services
builder.Services.AddEcommerceDDD();

// BrilliantMediator avec handlers générés à la compilation (zéro réflexion)
builder.Services.AddBrilliantMediator()
    .AddGeneratedHandlers()
    .Build();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
 options.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce DDD API v1");
  });
}

app.Services.UseBrilliantMediator();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
 Log.Information("🚀 Démarrage de l'application Ecommerce DDD API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ L'application s'est arrêtée de manière inattendue");
}
finally
{
Log.CloseAndFlush();
}
