using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetTopTenComments(
    ApiService apiService,
    DiscordSocketClient client) : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-top-ten-comments", "Gets the top ten comments of all time from the server.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        try
        {
            var response = await apiService.GetTopTenComments(Context.Guild.Id);

            if (response is null || response.Count == 0)
            {
                await FollowupAsync(
                    "No nominated comments were found for this guild.",
                    ephemeral: isEphemeral);
                return;
            }
            
            var comments = response.ToList();
            
            var (linkButton, embeds) = await PostCommentsHelper.GetMultipleCommentEmbeds(client, comments);

            await FollowupAsync(
                components: linkButton.Build(),
                embeds: embeds.ToArray(),
                ephemeral: isEphemeral);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}