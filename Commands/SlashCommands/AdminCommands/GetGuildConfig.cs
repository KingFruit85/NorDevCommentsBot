using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class GetGuildConfig(ApiService apiService, ILogger<GetGuildConfig> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    // [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("bot-config", "Get the guild configuration.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        logger.LogInformation("Trying to get guild config for {GuildId}", Context.Guild.Id);
        try
        {
            var response = await apiService.GetGuildConfigAsync(Context.Guild.Id);

            logger.LogInformation("Got guild config : {GuildId}", response.ToString());
            Dictionary<ulong, string> crosspostChannelsIdsAndNames = new();
            Dictionary<ulong, string> blacklistChannelsIdsAndNames = new();
            if (response.CrosspostChannels is { Count: > 0 })
            {
                foreach (var c in response.CrosspostChannels)
                {
                    var channel = Context.Guild.GetChannel(c);
                    if (channel is not null)
                    {
                        crosspostChannelsIdsAndNames.Add(c, channel.Name);
                    }
                }
            }
            if (response.BlacklistedChannels is { Count: > 0 })
            {
                foreach (var c in response.BlacklistedChannels)
                {
                    var channel = Context.Guild.GetChannel(c);
                    if (channel is not null)
                    {
                        blacklistChannelsIdsAndNames.Add(c, channel.Name);
                    }
                }
            }
            var reply = $"\nAllow crosspost: {response.AllowCrosspost}";
            // refactor this to use the dictionaries above for the channel names and ids
            foreach(var entry in crosspostChannelsIdsAndNames)
            {
                reply += $"\nCrosspost channel: {entry.Value} ({entry.Key})";
            }
            foreach(var entry in blacklistChannelsIdsAndNames)
            {
                reply += $"\nBlacklisted channel: {entry.Value} ({entry.Key})";
            }            

            await FollowupAsync(reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await FollowupAsync("An error occurred while trying to get the guild config.");
        }
    }
}