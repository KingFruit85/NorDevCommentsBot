using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetCrosspostChannel(ApiService apiService) : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-crosspost-channel", "Set the channel the bot will crosspost to.")]
    public async Task Handle([Summary(description: "Channel id to crosspost to")] string channelId)
    {
        await DeferAsync();
        
        if (string.IsNullOrEmpty(channelId))
        {
            await FollowupAsync("No channelId provided.");
            return;
        }

        var isCrosspostChannelSet = await apiService.SetCrosspostChannel(Context.Guild.Id, channelId);

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