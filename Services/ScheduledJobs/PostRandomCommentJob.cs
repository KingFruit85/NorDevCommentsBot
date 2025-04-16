using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Quartz;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Extensions;

namespace NorDevBestOfBot.Services.ScheduledJobs;

public class PostRandomCommentJob : IJob
{
    private readonly DiscordSocketClient _client;
    private readonly ApiService _apiService;
    private readonly ILogger<PostRandomCommentJob> _logger;

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


    public PostRandomCommentJob(DiscordSocketClient client, ApiService apiService, ILogger<PostRandomCommentJob> logger)
    {
        _client = client;
        _apiService = apiService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Executing scheduled job at {time}", DateTime.Now);

            foreach (var guild in _client.Guilds)
            {
                var channels = guild.TextChannels;

                var channelId = channels
                    .Where(c => c.Name is "lobby"
                        or "general") // TODO: This sucks, think of a way to pull this value from the server settings in mongo.
                    .Select(c => c.Id)
                    .FirstOrDefault();
                _logger.LogInformation("channel {channelId}", channelId);

                if (channelId == 0)
                {
                    _logger.LogWarning("No channel found");
                    continue;
                }

                var response = await _apiService.GetRandomComment(guild.Id);

                if (response is null) continue;

                _logger.LogInformation("attempting to send message {msgId}", response.messageId);
                var randomColour = ColourExtensions.GetRandomColour();
                var reply = await CommentEmbed.CreateEmbedAsync(response, randomColour);
                var builtEmbed = reply.First().Build();

                var voteButtons = new ComponentBuilder()
                    .WithButton(
                        "Take me to the post 📫",
                        style: ButtonStyle.Link,
                        url: response.messageLink,
                        row: 1);

                var chan = _client.GetChannel(channelId) as IMessageChannel;
                if (chan != null)
                {
                    
                    var randomMessage = _dailyMessages[new Random().Next(_dailyMessages.Count)];
                    
                    await chan.SendMessageAsync(text: randomMessage,
                        embed: builtEmbed, components: voteButtons.Build());
                }
                else
                {
                    _logger.LogError("Channel was not a message channel");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while posting random comment");
        }
    }
}