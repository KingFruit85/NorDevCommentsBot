using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetCrosspostChannel(ApiService apiService) : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    // [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-crosspost-channel", "Set the channel(s) the bot will crosspost to. (separate multiple channels with a comma)")]
    public async Task Handle([Summary(description: "Channel id to crosspost to")] string channelIds, bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);
        
        if (string.IsNullOrEmpty(channelIds))
        {
            await FollowupAsync("No channelId provided.");
            return;
        }
        
        var channelIdsAsUlongList = channelIds.Split(',').Select(ulong.Parse).ToList();

        var isCrosspostChannelSet = await apiService.SetCrosspostChannels(channelIdsAsUlongList, Context.Guild.Id);

        if (isCrosspostChannelSet)
        {
            await FollowupAsync("Crosspost channel set successfully.");
        }
        else
        {
            await FollowupAsync("An error occurred while trying to set the crosspost channel.");
        }
    }   
}