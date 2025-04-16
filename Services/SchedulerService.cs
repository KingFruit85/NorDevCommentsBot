using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services.Factories;
using NorDevBestOfBot.Services.ScheduledJobs;
using Quartz;
using Quartz.Impl;

namespace NorDevBestOfBot.Services;

public class SchedulerService : IHostedService
{
    private readonly ILogger<SchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IScheduler _scheduler;

    public SchedulerService(ILogger<SchedulerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Scheduler Service");
        
        // Create a scheduler factory
        var factory = new StdSchedulerFactory();
        _scheduler = await factory.GetScheduler(cancellationToken);
        
        // Use our custom job factory with DI
        _scheduler.JobFactory = new JobFactory(_serviceProvider);
        
        // Define the job
        var job = JobBuilder.Create<PostRandomCommentJob>()
            .WithIdentity("dailyCommentJob", "discordBotGroup")
            .Build();
        
        // Create a trigger that fires daily at 9:00 AM
        var trigger = TriggerBuilder.Create()
            .WithIdentity("dailyCommentTrigger", "discordBotGroup")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(09, 00))
            .Build();
        
        // Schedule the job
        await _scheduler.ScheduleJob(job, trigger, cancellationToken);
        
        // Start the scheduler
        await _scheduler.Start(cancellationToken);
        
        _logger.LogInformation("Scheduler started, job will execute at 9:00 AM daily");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Scheduler Service");
        
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}