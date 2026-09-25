using GameHub.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameHub.Api.HealthChecks;

// Readiness: the API can reach PostgreSQL. Reports only the status, never
// connection details.
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly GameHubDbContext _dbContext;

    public DatabaseHealthCheck(GameHubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy();
    }
}
