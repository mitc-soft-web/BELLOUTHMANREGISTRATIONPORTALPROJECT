using Microsoft.EntityFrameworkCore;
using Mitc_Registration_Portal.Models.Entities;
using Mitc_Registration_Portal.Persistence.Context;

namespace Mitc_Registration_Portal.Implementation.Services
{
    // Periodically deletes expired registration drafts to keep the DB bounded even with retries.
    // Named *Worker* so it doesn't get accidentally registered by the Scrutor "EndsWith(Service)" scan.
    public class RegistrationDraftCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RegistrationDraftCleanupWorker> _logger;

        public RegistrationDraftCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<RegistrationDraftCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Registration draft cleanup failed.");
                }

                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task CleanupOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();

            var now = DateTime.UtcNow;
            var expired = await db.Set<RegistrationDraft>()
                .Where(d => d.ExpiresAtUtc < now)
                .OrderBy(d => d.ExpiresAtUtc)
                .Take(500)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0) return;

            db.RemoveRange(expired);
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted {Count} expired registration drafts.", expired.Count);
        }
    }
}

