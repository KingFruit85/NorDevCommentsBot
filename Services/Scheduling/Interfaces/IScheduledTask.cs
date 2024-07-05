namespace NorDevBestOfBot.Services.Scheduling.Interfaces;

public interface IScheduledTask
{
    Task ExecuteAsync();
}