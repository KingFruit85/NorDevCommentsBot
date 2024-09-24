using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetRandomComment : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    private readonly ApiService _apiService;
    private readonly ILogger<GetRandomComment> _logger;

    public GetRandomComment(ApiService apiService, ILogger<GetRandomComment> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [SlashCommand("get-random-comment", "Gets a random comment from the database.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        try
        {
            var response = await _apiService.GetRandomComment(Context.Guild.Id);

            if (response is not null)
            {
                var randomColour = ColourExtensions.GetRandomColour();
                var reply = await CommentEmbed.CreateEmbedAsync(response, randomColour);
                var builtEmbed = reply.First().Build();

                var voteButtons = new ComponentBuilder()
                    .WithButton(
                        "Take me to the post 📫",
                        style: ButtonStyle.Link,
                        url: response.messageLink,
                        row: 1);

                await FollowupAsync(embed: builtEmbed, components: voteButtons.Build());
            }
            else
            {
                await FollowupAsync("No comments found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await FollowupAsync("An error occurred while trying to get a random comment.");
        }
    }
}