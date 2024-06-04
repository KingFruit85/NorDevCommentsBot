using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.SlashCommands;

public class GetTopTenUsersByPostCount : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ApiService _apiService;
    private readonly ILogger<GetTopTenUsersByPostCount> _logger;

    public GetTopTenUsersByPostCount(ApiService apiService, ILogger<GetTopTenUsersByPostCount> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    [SlashCommand("get-top-ten-users-by-post-count", "Gets the top ten users ordered by the sum of their vote counts.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var embed = new EmbedBuilder()
            .WithTitle("The top ten users by total 📫 post 📫 count");
        var response = await _apiService.GetTopTenUsersByPostCount();

        if (response is null || response!.Count < 1)
            await FollowupAsync("No user voting history was found for this server");

        foreach (var user in response!) embed.AddField(user.Key, user.Value);

        await FollowupAsync(embed: embed.Build());
    }
}