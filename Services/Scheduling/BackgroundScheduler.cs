using Cronos;
using Timer = System.Timers.Timer;

namespace NorDevBestOfBot.Services.Scheduling;

public class BackgroundScheduler
{
    public static readonly Dictionary<string, Timer> Timers = new();

    public void ScheduleJob(string taskName, string cronExpression, Func<Task> taskToRun)
    {
        var timer = new Timer();
        var cron = CronExpression.Parse(cronExpression);
        var nextOccurrence = cron.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
        var timespan = nextOccurrence - DateTimeOffset.Now;

        timer.Interval = timespan.Value.TotalMilliseconds;
        timer.Elapsed += async (_, _) =>
        {
            try
            {
                await taskToRun();
                var nextTime = cron.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
                timer.Interval = (nextTime! - DateTimeOffset.Now).Value.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"An error occurred while running task {taskName}: {ex.Message}");
            }
        };
        timer.Start();
        Timers.Add(taskName, timer);
    }

    //TODO implement slash command to stop job
    public static void StopJob(string taskName)
    {
        if (!Timers.TryGetValue(taskName, out var value)) return;
        value.Stop();
        Timers.Remove(taskName);
    }
}