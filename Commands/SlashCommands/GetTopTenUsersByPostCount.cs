using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetTopTenUsersByPostCount : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    private readonly ApiService _apiService;

    public GetTopTenUsersByPostCount(ApiService apiService, ILogger<GetTopTenUsersByPostCount> logger)
    {
        _apiService = apiService;
    }

    [SlashCommand("get-top-ten-users-by-post-count", "Gets the top ten users ordered by the sum of their vote counts.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var embed = new EmbedBuilder()
            .WithTitle("The top ten users by total 📫 post 📫 count");
        var response = await _apiService.GetTopTenUsersByPostCount(Context.Guild.Id);

        if (response is null || response!.Count < 1)
            await FollowupAsync("No user voting history was found for this server");

        foreach (var user in response!) embed.AddField(user.Key, user.Value);

        await FollowupAsync(embed: embed.Build());
    }
}