using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

[SuppressMessage("Performance", "CA1860:Avoid using \'Enumerable.Any()\' extension method")]
public class GetThisMonthsComments(
    ApiService apiService,
    ILogger<GetThisMonthsComments> logger,
    DiscordSocketClient client)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-this-months-comments", "Gets this month's comments.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        List<Comment> comments = [];
        try
        {
            var response = await apiService.GetThisMonthsComments(Context.Guild.Id);
            if (response is not null)
                comments = response.ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get this-month's comments.");
            throw new Exception(ex.Message);
        }

        if (comments.Any())
        {
            foreach (var comment in comments)
            {
                var (linkButton, embeds) = await PostCommentsHelper.GetMultipleCommentEmbeds(client, [comment]);
                await FollowupAsync(
                    components: linkButton.Build(),
                    embeds: embeds.ToArray(),
                    ephemeral: isEphemeral);
            }

            await FollowupAsync(
                "I hope you enjoyed reading though this month's comments as much as I did 🤗",
                ephemeral: isEphemeral);
        }
        else
        {
            await FollowupAsync(
                "No comments found for this month. Please try again later.",
                ephemeral: isEphemeral);
        }
    }
}