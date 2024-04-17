using Microsoft.EntityFrameworkCore;
using WoG.Combat.Services.Api.Data;

namespace WoG.Combat.Services.Api.Services
{
    public class DuelCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly int maximumDuelDurationInSeconds;
        private readonly int runCleanupEveryXMinutes;
        public DuelCleanupService(IServiceScopeFactory serviceScopeFactory, IConfiguration appSettings)
        {
            this.serviceScopeFactory = serviceScopeFactory;

            this.maximumDuelDurationInSeconds = appSettings.GetValue<int>("CombatProperties:MaximumDuelDurationInSeconds");
            this.runCleanupEveryXMinutes = appSettings.GetValue<int>("CombatProperties:RunCleanupEveryXMinutes");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = this.serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var recordsToUpdate = await dbContext.Duels
                        .Where(x => 
                            x.DuelOutcome == 0 &&
                            x.Events.Any(y => 
                                y.Sequence == 1 && 
                                EF.Functions.DateDiffSecond(y.TimeWhenNextActionOfTypeAvailable, DateTime.Now) >= this.maximumDuelDurationInSeconds))
                        .ToListAsync(stoppingToken);

                    foreach (var record in recordsToUpdate)
                    {
                        record.DuelOutcome = Enums.DuelOutcome.Draw;
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(runCleanupEveryXMinutes), stoppingToken);
            }
        }
    }
}
