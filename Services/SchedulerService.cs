using System.Collections.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services.Factories;
using NorDevBestOfBot.Services.ScheduledJobs;
using Quartz;
using Quartz.Impl;

namespace NorDevBestOfBot.Services;

public class SchedulerService(ILogger<SchedulerService> logger, IServiceProvider serviceProvider)
    : IHostedService
{
    private IScheduler? _scheduler;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Scheduler Service");
        
        // Create scheduler with minimal configuration
        var properties = new NameValueCollection
        {
            ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz",
            ["quartz.threadPool.threadCount"] = "1"
        };
        
        var factory = new StdSchedulerFactory(properties);
        _scheduler = await factory.GetScheduler(cancellationToken);
        _scheduler.JobFactory = new JobFactory(serviceProvider);
        
        // Initialize the scheduler but don't start it yet
        await SetupJob(cancellationToken);
        
        // Set up a timer to manage scheduler activation
        
        await _scheduler.Start(cancellationToken);
        logger.LogInformation("Scheduler service initialized");
    }

    private async Task SetupJob(CancellationToken cancellationToken)
    {
        // Test jobs
        var dailyRandomCommentJobTEST = JobBuilder.Create<PostRandomCommentJob>()
            .WithIdentity("dailyCommentJobTEST", "discordBotGroup")
            .Build();
        
        var dailyRandomCommentJobTriggerTEST = TriggerBuilder.Create()
            .WithIdentity("dailyCommentTriggerTEST", "discordBotGroup")
            .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(1))
            .Build();
        
        // Define the job
        var dailyRandomCommentJob = JobBuilder.Create<PostRandomCommentJob>()
            .WithIdentity("dailyCommentJob", "discordBotGroup")
            .Build();
        
        var dailyRandomCommentJobTrigger = TriggerBuilder.Create()
            .WithIdentity("dailyCommentTrigger", "discordBotGroup")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(09, 30).InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")))
            .Build();
        
        var monthlyRecapJob = JobBuilder.Create<PostMonthlyRecapJob>()
            .WithIdentity("monthlyRecapJob", "discordBotGroup")
            .Build();
        
        var monthlyRecapJobTrigger = TriggerBuilder.Create()
            .WithIdentity("monthlyRecapTrigger", "discordBotGroup")
            .WithSchedule(CronScheduleBuilder.CronSchedule("0 20 9 L * ?").InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")))
            .Build();
        
        var keepDbAwakeJob = JobBuilder.Create<KeepDbAwakeJob>()
            .WithIdentity("keepDbAwakeJob", "discordBotGroup")
            .Build();
        
        var keepDbAwakeJobTrigger = TriggerBuilder.Create()
            .WithIdentity("keepDbAwakeTrigger", "discordBotGroup")
            .WithSimpleSchedule(x => x.RepeatForever().WithIntervalInMinutes(5))
            .Build();
        
        // Schedule the jobs
        if (_scheduler == null)
        {
            throw new InvalidOperationException("Scheduler is not initialized.");
        }
        // TODO can probably use a batch method to schedule jobs
        await _scheduler.ScheduleJob(dailyRandomCommentJob, dailyRandomCommentJobTrigger, cancellationToken);
        await _scheduler.ScheduleJob(monthlyRecapJob, monthlyRecapJobTrigger, cancellationToken);
        await _scheduler.ScheduleJob(keepDbAwakeJob, keepDbAwakeJobTrigger, cancellationToken);
        
        // Test jobs
        await _scheduler.ScheduleJob(dailyRandomCommentJobTEST, dailyRandomCommentJobTriggerTEST, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Scheduler Service");
        
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }
}