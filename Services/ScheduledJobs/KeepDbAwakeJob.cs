using Microsoft.Extensions.Logging;
using Quartz;

namespace NorDevBestOfBot.Services.ScheduledJobs;

// This job is used to keep the database awake by executing a simple query every 5 minutes.
public class KeepDbAwakeJob(ApiService apiService,
    ILogger<KeepDbAwakeJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {   logger.LogInformation("firing Keep db awake job at {time}", DateTime.Now);
            await apiService.KeepDatabaseAwake();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return;
        }
        logger.LogInformation("finished keep db awake job at {time}", DateTime.Now);
    }
}