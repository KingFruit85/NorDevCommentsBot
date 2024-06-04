using Discord;
using Discord.Interactions;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.SlashCommands;

public class GetTopTenUsersByVoteCount : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ApiService _apiService;

    public GetTopTenUsersByVoteCount(ApiService apiService)
    {
        _apiService = apiService;
    }

    [SlashCommand("get-top-ten-users-by-vote-count", "Gets the top ten users ordered by the sum of their vote counts.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var response = await _apiService.GetTopTenUsersByVoteCount();

        if (response is null || response!.Count < 1)
            await FollowupAsync("No user voting history was found for this server");

        var embed = new EmbedBuilder()
            .WithTitle("The top ten users by total ✅ vote ✅ count");

        foreach (var user in response!) embed.AddField(user.Key, user.Value);

        await FollowupAsync(embed: embed.Build());
    }
}