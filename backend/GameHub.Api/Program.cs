using GameHub.Application.Interfaces;
using GameHub.Application.Services;
using GameHub.Infrastructure.Data;
using GameHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Security;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GameHub.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Required configuration. Nothing secret is versioned: in Development these
// come from appsettings.Development.json (local Docker database) and
// dotnet user-secrets (Jwt:Key); anywhere else from environment variables
// (ConnectionStrings__DefaultConnection, Jwt__Key). Fail at startup with a
// clear message instead of on the first request.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured. " +
        "Set ConnectionStrings__DefaultConnection.");
}

var jwtKey = builder.Configuration["Jwt:Key"];

// HS256 requires a key of at least 256 bits.
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with at least 32 bytes. " +
        "Use 'dotnet user-secrets set Jwt:Key <value>' in Development " +
        "or the Jwt__Key environment variable.");
}

// Browser origins allowed to call the API from another origin
// (Cors__AllowedOrigins__0, __1, ...). Empty means same-origin only, which is
// all the Vite dev proxy or a same-domain reverse proxy needs.
var corsOrigins = (builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [])
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => origin.Length > 0)
    .ToArray();

builder.Services.AddOpenApi();

builder.Services.AddControllers();

// Consistent RFC 7807 bodies for unhandled errors (see UseExceptionHandler).
builder.Services.AddProblemDetails();

builder.Services.AddSignalR();

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // The SignalR browser client sends credentials on negotiate.
            .AllowCredentials()));
}

builder.Services.AddDbContext<GameHubDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IChannelRepository, ChannelRepository>();

builder.Services.AddScoped<IChannelService, ChannelService>();

builder.Services.AddScoped<IUnitOfWork>(sp =>
    sp.GetRequiredService<GameHubDbContext>());

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IMessageRepository, MessageRepository>();

builder.Services.AddScoped<IMessageService, MessageService>();

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // Preserve the original JWT claim names (e.g. "sub")
        // instead of mapping them to ClaimTypes.*.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            // Only accept the algorithm JwtService signs with.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Browsers cannot send headers on WebSocket/SSE connections,
        // so the SignalR client sends the token in the query string.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // The developer exception page is enabled automatically here.
    app.MapOpenApi();
}
else
{
    // Generic ProblemDetails without exception details, stack traces or
    // connection strings.
    app.UseExceptionHandler();
    app.UseHsts();
}

// Basic hardening headers for every response.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "no-referrer";

    await next();
});

app.UseHttpsRedirection();

if (corsOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();