using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SARE.Domain.Catalog;
using SARE.Infrastructure.Persistence;

namespace SARE.Api.Tests;

public sealed class CatalogApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "sare-catalog-tests";
    private const string Audience = "sare-test-api";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly SymmetricSecurityKey _signingKey = new(RandomNumberGenerator.GetBytes(32));
    public bool SimulateDeleteRace { get; set; }
    public bool SimulateProductCategoryRace { get; set; }

    public CatalogApiFactory()
    {
        _connection.Open();
        _connection.CreateFunction("now", () => DateTime.UtcNow.ToString("O"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection)
                .AddInterceptors(new DeleteRaceInterceptor(this)));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.ConfigurationManager = null;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = Issuer,
                    ValidateAudience = true, ValidAudience = Audience,
                    ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true, IssuerSigningKey = _signingKey,
                    RoleClaimType = ClaimTypes.Role
                };
            });
        });
    }

    public string CreateToken(string role = "admin", bool expired = false, bool invalidSignature = false)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(Issuer, Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()), new Claim("role", role)],
            notBefore: now.AddHours(-1), expires: expired ? now.AddMinutes(-1) : now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                invalidSignature ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) : _signingKey,
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task InDatabaseAsync(Func<AppDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class DeleteRaceInterceptor(CatalogApiFactory factory) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if ((factory.SimulateDeleteRace && eventData.Context!.ChangeTracker.Entries<Category>()
                    .Any(entry => entry.State == EntityState.Deleted))
                || (factory.SimulateProductCategoryRace && eventData.Context!.ChangeTracker.Entries<Product>()
                    .Any(entry => entry.State is EntityState.Added or EntityState.Modified)))
            {
                // Simulate PostgreSQL rejecting a delete after a concurrent product insert.
                throw new DbUpdateException("Concurrent product reference.", new PostgresException(
                    "Category is referenced by products.", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation,
                    constraintName: "fk_products_categories_category_id"));
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
