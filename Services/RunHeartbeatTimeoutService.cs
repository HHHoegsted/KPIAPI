using KPIAPI.Data;
using KPIAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KPIAPI.Services;

public sealed class RunHeartbeatTimeoutService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;

    public RunHeartbeatTimeoutService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var cutoff = now - Timeout;

            var staleRuns = await db.RobotRuns
                .Where(r => r.Outcome == null)
                .Where(r => (r.LastHeartbeatUtc ?? r.StartTimeUtc) <= cutoff)
                .ToListAsync(stoppingToken);

            if (staleRuns.Count == 0) continue;

            foreach (var run in staleRuns)
            {
                run.Outcome = RunOutcome.Failed;
                run.EndTimeUtc = now;
                run.ErrorCode = "heartbeat_timeout";
                run.ErrorMessage = "No heartbeat received for 5 minutes.";
            }

            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
