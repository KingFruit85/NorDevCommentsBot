using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Models;
using Quartz;

namespace NorDevBestOfBot.Services.ScheduledJobs.TestJobs;

public class TestMonthlyRecapJob(DiscordSocketClient client, ApiService apiService, ILogger<TestMonthlyRecapJob> logger)
    : IJob
{
    private readonly List<string> _monthlyMessages =
    [
        "Here's a recap of the month! 🎉",
        "Can you believe it's already the end of the month? Here's what happened! 📅",
        "As we wrap up the month, let's take a look at some highlights! 🌟",
        "The month has flown by! Check out these memorable moments! 🕒",
        "Time flies when you're having fun! Here's a recap of the month! ⏰",
        "What a month it's been! Let's relive some of the best moments! 🏆",
        "As we close out the month, let's celebrate some of our best moments! 🎊",
        "The month is coming to an end! Here's a look back at some highlights! 📸",
        "It's time for our monthly recap! Let's see what we've accomplished! 📈",
        "The month has come to a close! Here's a look at some of our best moments! 🏅",
        "As we bid farewell to the month, let's celebrate some of our best moments! 🎉",
        "The month has flown by! Let's take a look at some of our best moments! 🕒",
    ];

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Executing PostMonthlyRecapJob job at {time}", DateTime.Now);

            foreach (var guild in client.Guilds)
            {
                if (guild.Id != 1054500338947858522) continue;

                var response = await apiService.GetThisMonthsComments(guild.Id);

                if (response == null || response.Count == 0)
                {
                    logger.LogWarning("No comments found for this month");
                    continue;
                }

                var chan = client.GetChannel(1054500340063555606) as ITextChannel;
                await chan!.SendMessageAsync(
                    text: _monthlyMessages[new Random().Next(_monthlyMessages.Count)]);
                
                foreach (var comment in response)
                {
                    var (linkButton, embeds) = await PostCommentsHelper.GetMultipleCommentEmbeds(client, [comment]);
                    await chan!.SendMessageAsync(
                        embeds: embeds.ToArray(),
                        components: linkButton.Build());
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while posting random comment");
        }
    }
}