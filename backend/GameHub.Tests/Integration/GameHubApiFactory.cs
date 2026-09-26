using System.Net.Http.Headers;
using System.Security.Cryptography;
using GameHub.Application.Interfaces;
using GameHub.Domain.Entities;
using GameHub.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace GameHub.Tests.Integration;

// The real API (controllers, JWT authentication, ChatHub, EF Core
// repositories) hosted in memory, on an in-memory SQLite database instead of
// PostgreSQL. No external database or secret is needed.
public class GameHubApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Required by Program.cs at startup. The key is random per run; the
        // connection string only passes the startup check (the database is
        // replaced below).
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused");
        builder.UseSetting("Jwt:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Jwt:Issuer", "gamehub-tests");
        builder.UseSetting("Jwt:Audience", "gamehub-tests");
        builder.UseSetting("AllowedHosts", "localhost");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<GameHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GameHubDbContext>>();

            // One open connection keeps the in-memory database alive.
            _connection.Open();
            services.AddDbContext<GameHubDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<GameHubDbContext>()
            .Database
            .EnsureCreated();

        return host;
    }

    // Inserts a user directly and issues a real token with the app's
    // JwtService (the auth endpoints are rate limited and covered elsewhere).
    public async Task<TestUser> CreateUserAsync(string prefix)
    {
        using var scope = Services.CreateScope();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"{prefix}-{Guid.NewGuid():N}"[..20],
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = "not-used"
        };

        var db = scope.ServiceProvider.GetRequiredService<GameHubDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = scope.ServiceProvider
            .GetRequiredService<IJwtService>()
            .GenerateToken(user);

        return new TestUser(user.Id, user.Username, token);
    }

    public HttpClient CreateClient(TestUser? user)
    {
        var client = CreateClient();

        if (user is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", user.Token);
        }

        return client;
    }

    // A SignalR client connected through the in-memory server.
    public HubConnection CreateHubConnection(TestUser? user)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, "hubs/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;

                if (user is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
                }
            })
            .Build();
    }

    public async Task<T> QueryDbAsync<T>(Func<GameHubDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();

        return await query(scope.ServiceProvider.GetRequiredService<GameHubDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public record TestUser(Guid Id, string Username, string Token);
