using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetAllowCrosspost(ApiService apiService)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-allow-crosspost", "Set whether the bot is allowed to crosspost.")]
    public async Task Handle([Summary(description:"Allow crosspost?")] bool allowCrosspost)
    {
        await DeferAsync();
        
        var isAllowCrosspostSet = await apiService.SetAllowCrosspost(Context.Guild.Id, allowCrosspost);

        if (isAllowCrosspostSet)
        {
            await FollowupAsync("Allow crosspost set successfully.");
        }
        else
        {
            await FollowupAsync("An error occurred while trying to set the allow crosspost.");
        }
    }
}