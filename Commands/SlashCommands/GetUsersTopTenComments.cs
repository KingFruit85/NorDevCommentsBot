using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetUsersTopTenComments(ApiService apiService)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-users-top-ten-comments", "Gets the top ten comments for a user in this guild.")]
    public async Task Handle(
        [Summary(description: "The user to get the top ten comments for.")]
        IUser user,
        [Summary(description: "Hide this post?")]
        bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var availableColours = ColourExtensions.AllowedColours();

        try
        {
            var response = await apiService.GetUsersTopTenComments(user, Context.Guild.Id);

            if (response?.Count < 1 || response is null)
                await FollowupAsync($"The user {user} was not found or does not have any nominated comments");

            List<Embed> comments = [];

            foreach (var (comment, index) in response!.Take(5).Select((comment, index) => (comment, index)))
            {
                var colourToUse = availableColours[index % availableColours.Count];

                var embeds =
                    await CommentEmbed.CreateEmbedAsync(comment, colourToUse);

                comments.AddRange(embeds.Select(embed => embed.Build()));
            }

            await FollowupAsync(embeds: comments.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}