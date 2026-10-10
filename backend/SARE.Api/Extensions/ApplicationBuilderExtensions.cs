using SARE.Api.Hubs;
using SARE.Infrastructure.Persistence;

namespace SARE.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<CartHub>("/hubs/cart");

        app.MapGet("/", () => "SARE API is running!");

        return app;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (!(app.Configuration.GetValue<bool?>("Database:SeedOnStartup") ?? app.Environment.IsDevelopment())) return;
        using var scope = app.Services.CreateScope();
        try
        {
            var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
            await initializer.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
            logger.LogWarning(ex, "Could not run database initializer on startup. Make sure the database exists and migrations are applied.");
        }
    }
}
