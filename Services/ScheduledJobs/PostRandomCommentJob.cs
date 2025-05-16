using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Quartz;
using NorDevBestOfBot.Commands.CommandHelpers;

namespace NorDevBestOfBot.Services.ScheduledJobs;

public class PostRandomCommentJob(
    DiscordSocketClient client,
    ApiService apiService,
    ILogger<PostRandomCommentJob> logger)
    : IJob
{
    private readonly List<string> _dailyMessages =
    [
        "Here's a gem I've dug up from the archives for your enjoyment today! ✨",
        "Rise and shine with today's blast from the past! 🌅",
        "Your daily dose of Nordev brilliance has arrived! 🏆",
        "Starting the day with this memorable moment from our history! 📚",
        "Good morning! Enjoy this highlight from our community's greatest hits! 🌟",
        "Today's featured comment from the Nordev vault has arrived! 📫",
        "Kicking off the day with this standout moment from our archives! 🚀",
        "Look what I found in our treasure trove of great messages! 💎",
        "Your daily Nordev nostalgia is served! 🍽️",
        "Time for your daily reminder of why this community is awesome! 💯",
        "Coming at you with today's handpicked community highlight! 🔍",
        "Breaking up the workday with this gem from our collection! ⏰",
        "Today's featured message is quite the treat! Enjoy! 🎁",
        "And now, for your daily moment of Nordev brilliance... ✨",
        "I've searched through the archives to bring you today's highlight! 🗂️",
        "Another day, another fantastic message from our history! 📆",
        "Drop what you're doing and check out today's featured comment! 📱",
        "Your daily reminder of the amazing conversations happening here! 💬",
        "Hot off the archives: today's featured community moment! 🔥",
        "Behold! Today's gem from the Nordev collection has arrived! 🏅",
        "Start your day right with this classic moment from our community! ☕",
        "The algorithm has spoken! Here's today's featured message! 🤖",
        "My daily treasure hunt through the archives yielded this gem! 🗺️",
        "Community spotlight time! Check out today's featured message! 🔦",
        "Surprise! Here's your daily dose of community excellence! 🎉",
        "Looking for inspiration? Here's today's message from the archives! 💡",
        "The message of the day has been chosen! Feast your eyes on this one! 👀",
        "Nordev time capsule: Bringing you this classic from our archives! ⏳",
        "Daily highlight incoming! Prepare to be entertained! 🎭",
        "Attention everyone! Today's featured message has arrived! 📢"
    ];


    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            logger.LogInformation("Executing PostRandomCommentJob job at {time}", DateTime.Now);

            foreach (var guild in client.Guilds)
            {
                var channels = guild.TextChannels;

                var channelToPostMessage = channels
                    .Where(c => c.Id is 680873189106384988
                        or 1054500340063555606) // TODO: This sucks, think of a way to pull this value from the server settings in mongo.
                    .Select(c => c.Id)
                    .FirstOrDefault();

                if (channelToPostMessage == 0)
                {
                    logger.LogWarning("No channel found");
                    continue;
                }

                var response = await apiService.GetRandomComment(guild.Id);
                
                if (response is null) continue;

                logger.LogInformation("attempting to send message {msgId}", response.messageId);
                List<Embed> embeds = [];
            
                var (_, channel, messageId) = ParseMessageLink.Parse(response.messageLink!);
                logger.LogInformation("attempting to get message {msgId} from channel {channel} in guild {guildId}", messageId, channel, guild.Id);
                var message = await guild.GetTextChannel(channel).GetMessageAsync(messageId);
                if (message is null)
                {
                    logger.LogWarning("message is null");
                    continue;
                }
            
                // This order means the embeds will display in the correct order original message first, then the quoted message
                if (message.Reference != null)
                {
                    var quotedMessage = await guild.GetTextChannel(channel).GetMessageAsync(message.Reference!.MessageId.Value);
                    if (quotedMessage is null)
                    {
                        return;
                    }
                    embeds.Add(Create.Embed(quotedMessage));
                }
                embeds.Add(Create.Embed(message));

                var voteButtons = new ComponentBuilder()
                    .WithButton(
                        "Take me to the post 📫",
                        style: ButtonStyle.Link,
                        url: response.messageLink,
                        row: 1)
                    ;

                if (client.GetChannel(channelToPostMessage) is IMessageChannel chan)
                {
                    
                    var randomMessage = _dailyMessages[new Random().Next(_dailyMessages.Count)];
                    
                    await chan.SendMessageAsync(text: randomMessage,
                        embeds: embeds.ToArray(), components: voteButtons.Build());
                }
                else
                {
                    logger.LogError("Channel was not a message channel");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while posting random comment");
        }
    }
}