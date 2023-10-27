using Discord.WebSocket;
using Discord;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

internal class GetUsersTopFiveComments
{
    public static async Task HandleGetUsersTopFiveComments(SocketSlashCommand command, HttpClient httpClient)
    {
        bool isEphemeral = ((bool?)command.Data.Options.First().Value) ?? true;

        await command.DeferAsync(ephemeral: isEphemeral);

        List<Color> postColours = new()
        {
                new Color(244, 67, 54),   // #F44336 (Red)
                new Color(0, 188, 212),   // #00BCD4 (Cyan)
                new Color(156, 39, 176),  // #9C27B0 (Purple)
                new Color(255, 193, 7),   // #FFC107 (Amber)
                new Color(76, 175, 80)    // #4CAF50 (Green)
        };
        int counter = 0;

        try
        {
            var user = command.Data.Options.First().Value;
            string endpoint = $"https://nordevcommentsbackend.fly.dev/api/messages/getalluserscomments?user={user}";

            var response = await httpClient.GetFromJsonAsync<List<Comment>>(endpoint);

            if (response?.Count < 1 || response is null)
            {
                await command.FollowupAsync($"The user {user} was not found or does not have any nominated comments");
            }

            List<Embed> comments = new();

            var limitOfFive = 0;
            foreach (var comment in response!)
            {
                var embeds = await CommentEmbed.CreateEmbedAsync(comment, postColours[counter]);
                foreach (var embed in embeds)
                {
                    comments.Add(embed.Build());
                }
                counter++;
                limitOfFive++;
                if (limitOfFive == 4)
                {
                    break;
                }
            }

            await command.FollowupAsync(embeds: comments.ToArray());
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

}
