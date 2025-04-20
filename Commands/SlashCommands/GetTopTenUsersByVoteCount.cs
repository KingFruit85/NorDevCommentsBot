using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetTopTenUsersByVoteCount(ApiService apiService)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-top-ten-users-by-vote-count", "Gets the top ten users ordered by the sum of their vote counts.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var response = await apiService.GetTopTenUsersByVoteCount(Context.Guild.Id);

        if (response is null || response.Count < 1)
            await FollowupAsync("No user voting history was found for this server");

        var embed = new EmbedBuilder()
            .WithTitle("The top ten users by total ✅ vote ✅ count");

        foreach (var user in response!) embed.AddField(user.Key, user.Value);

        await FollowupAsync(embed: embed.Build());
    }
}