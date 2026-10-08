using SARE.Api.Extensions;
using SARE.Application.Extensions;
using SARE.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiPipeline();
await app.SeedDatabaseAsync();

app.Run();
