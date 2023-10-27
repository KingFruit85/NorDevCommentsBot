using Discord.WebSocket;
using System.Net.Http.Json;
using Discord;

namespace NorDevBestOfBot.Handlers;

internal class GetTopTenUsersByVoteCount
{
    internal static async Task HandleGetTopTenUsersByVoteCount(SocketSlashCommand command, HttpClient httpClient)
    {
        bool isEphemeral = ((bool?)command.Data.Options.First().Value) ?? true;

        await command.DeferAsync(ephemeral: isEphemeral);
        try
		{
            var response = await httpClient.GetFromJsonAsync<Dictionary<string, int>>("https://nordevcommentsbackend.fly.dev/api/messages/gettoptenusersbyvotecount");

            if (response is null || response!.Count < 1)
            {
                await command.FollowupAsync("No user voting history was found for this server");
            }

            var embed = new EmbedBuilder()
                .WithTitle("The top ten users by total ✅ vote ✅ count");

            foreach (var user in response!)
            {
                embed.AddField(user.Key, user.Value, inline:false);
            }

            await command.FollowupAsync(embed: embed.Build());

        }
        catch (Exception)
		{

			throw;
		}


    }
}
