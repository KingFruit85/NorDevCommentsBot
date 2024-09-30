using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetBlacklistedChannels(ApiService apiService, ILogger<SetBlacklistedChannels> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    // [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-blacklisted-channels",
        "Set channels the bot will not work in. add channel ids followed by a comma.")]
    public async Task Handle([Summary(description: "Channel ids to blacklist")] string channelIds,
        bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        if (string.IsNullOrEmpty(channelIds))
        {
            await FollowupAsync("No channelIds provided.");
            return;
        }

        var channelIdsAsUlongList = channelIds.Split(',').Select(ulong.Parse).Distinct().ToList();
        logger.LogInformation("Channel ids: {channelIds}", channelIdsAsUlongList);

        var isBlacklisted = await apiService.SetBlacklistedChannels(channelIdsAsUlongList, Context.Guild.Id);
        logger.LogInformation("Is blacklisted: {isBlacklisted}", isBlacklisted);

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