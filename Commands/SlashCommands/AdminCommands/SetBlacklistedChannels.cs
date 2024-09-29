using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetBlacklistedChannels(ApiService apiService, ILogger<SetBlacklistedChannels> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-blacklisted-channels", "Set channels the bot will not work in. add channel ids followed by a comma.")]
    public async Task Handle([Summary(description: "Channel ids to blacklist")] string channelIds)
    {
        await DeferAsync();
        
        if (string.IsNullOrEmpty(channelIds))
        {
            await FollowupAsync("No channelIds provided.");
            return;
        }

        var channelIdsArray = channelIds.Split(',');

        var isBlacklisted = await apiService.SetBlacklistedChannels(Context.Guild.Id, channelIdsArray);

        if (isBlacklisted)
        {
            await FollowupAsync("Channels blacklisted successfully.");
        }
        else
        {
            await FollowupAsync("An error occurred while trying to blacklist channels.");
        }
    }
}