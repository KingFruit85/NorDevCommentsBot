using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetUsersTopFiveComments : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    private readonly ApiService _apiService;
    private readonly ILogger<GetUsersTopFiveComments> _logger;

    public GetUsersTopFiveComments(ApiService apiService, ILogger<GetUsersTopFiveComments> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [SlashCommand("get-users-top-five-comments", "Gets the top five comments for a user.")]
    public async Task Handle(
        [Summary(description: "The user to get the top five comments for.")]
        IUser user,
        [Summary(description: "Hide this post?")]
        bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var availableColours = ColourExtensions.AllowedColours();

        try
        {
            var response = await _apiService.GetUsersTopFiveComments(user);

            if (response?.Count < 1 || response is null)
                await FollowupAsync($"The user {user} was not found or does not have any nominated comments");

            List<Embed> comments = new();

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