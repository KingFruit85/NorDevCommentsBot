using Discord;
using Discord.WebSocket;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

internal class GetTopTenUsersByPostCount
{
    internal static async Task HandleGetTopTenUsersByPostCount(SocketSlashCommand command, HttpClient httpClient)
    {
        bool isEphemeral = ((bool?)command.Data.Options.First().Value) ?? true;

        await command.DeferAsync(ephemeral: isEphemeral);

        var embed = new EmbedBuilder()
            .WithTitle("The top ten users by total 📫 post 📫 count");
        try
        {
            var response = await httpClient.GetFromJsonAsync<Dictionary<string, int>>("https://nordevcommentsbackend.fly.dev/api/messages/gettoptenusersbypostcount");

            if (response is null || response!.Count < 1)
            {
                await command.FollowupAsync("No user voting history was found for this server");
            }

            foreach (var user in response!)
            {
                embed.AddField(user.Key, user.Value, inline: false);
            }
        }
        catch (Exception)
        {
            throw;
        }

        await command.FollowupAsync(embed: embed.Build());
    }
}
