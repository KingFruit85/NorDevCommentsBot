using Quartz.Spi;
using Quartz;

namespace NorDevBestOfBot.Services.Factories;

public class JobFactory : IJobFactory
{
    private readonly IServiceProvider _serviceProvider;

    public JobFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
    {
        var jobType = bundle.JobDetail.JobType;
        return (IJob)_serviceProvider.GetService(jobType);
    }

    public void ReturnJob(IJob job) { }
}