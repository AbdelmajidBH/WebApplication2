using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization.Conventions;
using System.Text;
using System.Text.Json;
using WebApplication2.Api.Controllers;
using WebApplication2.Infrastructure;
using WebApplication2.Infrastructure.Analytics;
using WebApplication2.Infrastructure.Analytics.Dtos;
using WebApplication2.Infrastructure.Books;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
      .AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.Configure<MongoDatabaseSettings>(
    builder.Configuration.GetSection("BookStoreDatabase"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("a-string-secret-at-least-256-bits-long")) // à mettre en config
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("IsAdminEditor", policy => policy.RequireClaim("AdminEditor", "true"));

var pack = new ConventionPack
{
    new CamelCaseElementNameConvention(),
    new IgnoreExtraElementsConvention(true),
};
ConventionRegistry.Register("MyConventions", pack, _ => true);

builder.Services.Configure<MySettings>(
    builder.Configuration.GetSection("MySettings"));


builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<BooksRepository>();
builder.Services.AddScoped<AnalyticsRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

app.MapGet("/friends", async (
    [AsParameters] FriendsQuery query,
    AnalyticsRepository service,
    CancellationToken ct) =>
{
    var result = await service.GetFriendsAsync(query, ct);
    return Results.Ok(result);
});

app.MapGet("/friends/cursor", async (
    string? cursor,
    int pageSize,
    AnalyticsRepository service,
    CancellationToken ct) =>
{
    var result = await service.GetFriendsByCursorAsync(cursor, pageSize, ct);
    return Results.Ok(result);
});

app.MapGet("/friends/cursor/header", async (
    HttpContext httpContext,
    string? cursor,
    int pageSize,
    AnalyticsRepository service,
    CancellationToken ct) =>
{
    if (pageSize <= 0)
        pageSize = 20;

    var page = await service.GetFriendsByCursorAsync(cursor, pageSize, ct);

    var request = httpContext.Request;
    var path = $"{request.Path}";

    string? nextUrl = null;
    string? prevUrl = null;

    if (!string.IsNullOrEmpty(page.NextCursor))
    {
        nextUrl = $"{path}?pageSize={pageSize}&cursor={page.NextCursor}";
    }

    // Pour prev, en pagination cursor-based réelle, il faudrait
    // un système "before" / historique côté client, mais ici on
    // se contente d'exposer le cursor courant comme "prev".
    if (!string.IsNullOrEmpty(cursor))
    {
        prevUrl = $"{path}?pageSize={pageSize}&cursor={cursor}";
    }

    // On construit la valeur du header Link
    var links = new List<string>();

    if (!string.IsNullOrEmpty(prevUrl))
    {
        links.Add($"<{prevUrl}>; rel=\"prev\"");
    }

    if (!string.IsNullOrEmpty(nextUrl))
    {
        links.Add($"<{nextUrl}>; rel=\"next\"");
    }

    if (links.Count > 0)
    {
        // Un seul header Link avec les deux valeurs séparées par une virgule
        httpContext.Response.Headers["Link"] = string.Join(", ", links);
    }

    return Results.Ok(page.Items);
});


app.Run();
