using Microsoft.EntityFrameworkCore;
using Rise.Persistence;
using Rise.Persistence.Triggers;
using Rise.Services.Products;
using Rise.Shared.Products;
using Serilog.Events;

try
{
    Log.Information("Starting up Server");
    var builder = WebApplication.CreateBuilder(args);

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.ApplicationInsights(connectionString: builder.Configuration.GetConnectionString("ApplicationInsights"), TelemetryConverter.Traces)
        .CreateLogger();

    builder.Services.AddSerilog();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
        options.UseTriggers(options => options.AddTrigger<EntityBeforeSaveTrigger>());
    });

    builder.Services.AddScoped<IProductService, ProductService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    // After StaticFiles else we'll log that stuff too.
    app.UseSerilogIngestion();//.UseSerilogRequestLogging();

    app.UseRouting();

    app.MapControllers();
    app.MapFallbackToFile("index.html");

    using (var scope = app.Services.CreateScope())
    { // Require a DbContext from the service provider and seed the database.
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Seeder seeder = new(dbContext);
        seeder.Seed();
    }

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
