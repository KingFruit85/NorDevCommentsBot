using Quartz.Spi;
using Quartz;

namespace NorDevBestOfBot.Services.Factories;

public class JobFactory(IServiceProvider serviceProvider) : IJobFactory
{
    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        return (IJob)serviceProvider.GetService(jobType)!;
    }

    public void ReturnJob(IJob job) { }
}