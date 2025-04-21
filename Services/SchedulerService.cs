using System.Collections.Specialized;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services.Factories;
using NorDevBestOfBot.Services.ScheduledJobs;
using Quartz;
using Quartz.Impl;

namespace NorDevBestOfBot.Services;

public class SchedulerService(ILogger<SchedulerService> logger, IServiceProvider serviceProvider)
    : IHostedService, IDisposable
{
    private IScheduler? _scheduler;
    private Timer? _activationTimer;
    private bool _schedulerActive = false;

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
        SetupActivationTimer();
        
        logger.LogInformation("Scheduler service initialized");
    }

    private async Task SetupJob(CancellationToken cancellationToken)
    {
        // Define the job
        var dailyRandomCommentJob = JobBuilder.Create<PostRandomCommentJob>()
            .WithIdentity("dailyCommentJob", "discordBotGroup")
            .Build();
        
        // Create a trigger that fires daily at 9:00 AM
        var dailyRandomCommentJobTrigger = TriggerBuilder.Create()
            .WithIdentity("dailyCommentTrigger", "discordBotGroup")
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(09, 20).InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")))
            .Build();
        
        var monthlyRecapJob = JobBuilder.Create<PostMonthlyRecapJob>()
            .WithIdentity("monthlyRecapJob", "discordBotGroup")
            .Build();
        
        var monthlyRecapJobTrigger = TriggerBuilder.Create()
            .WithIdentity("monthlyRecapTrigger", "discordBotGroup")
            .WithSchedule(CronScheduleBuilder.CronSchedule("0 20 9 L * ?").InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")))
            .Build();
        
        // Schedule the jobs
        if (_scheduler == null)
        {
            throw new InvalidOperationException("Scheduler is not initialized.");
        }
        // TODO can probably use a batch method to schedule jobs
        await _scheduler.ScheduleJob(dailyRandomCommentJob, dailyRandomCommentJobTrigger, cancellationToken);
        await _scheduler.ScheduleJob(monthlyRecapJob, monthlyRecapJobTrigger, cancellationToken);
    }

    private void SetupActivationTimer()
    {
        // Check every hour to see if we need to activate or deactivate the scheduler
        _activationTimer = new Timer(CheckSchedulerActivation, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
    }

    private async void CheckSchedulerActivation(object? state)
    {
        try
        {
            // Get the current time in GMT
            var gmtZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var nowGmt = TimeZoneInfo.ConvertTime(DateTime.Now, gmtZone);
            
            logger.LogInformation("Firing CheckSchedulerActivation, Current GMT time: {nowGmt}", nowGmt);
        
            // Only activate the scheduler between 8:50 AM and 9:50 AM GMT
            var shouldBeActive = nowGmt is { Hour: 8, Minute: >= 50 } or { Hour: 9, Minute: <= 50 };
        
            if (_scheduler is not null && shouldBeActive && !_schedulerActive)
            {
                logger.LogInformation("Activating scheduler for the 9:20 AM GMT window");
                await _scheduler.Start();
                _schedulerActive = true;
            }
            else if (_scheduler is not null && !shouldBeActive && _schedulerActive)
            {
                logger.LogInformation("Deactivating scheduler until next scheduled window");
                await _scheduler.Standby();
                _schedulerActive = false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in scheduler activation check");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Scheduler Service");
        
        _activationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(cancellationToken);
        }
    }

    public void Dispose()
    {
        _activationTimer?.Dispose();
    }
}