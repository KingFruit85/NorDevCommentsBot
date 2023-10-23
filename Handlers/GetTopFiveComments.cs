using Discord.WebSocket;
using Discord;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

public class GetTopFiveComments
{
    public static async Task HandleGetTopFiveComments(SocketSlashCommand command, HttpClient httpClient)
    {
        await command.DeferAsync();

        List<Color> postColours = new()
        {
                new Color(244, 67, 54),   // #F44336 (Red)
                new Color(0, 188, 212),   // #00BCD4 (Cyan)
                new Color(156, 39, 176),  // #9C27B0 (Purple)
                new Color(255, 193, 7),   // #FFC107 (Amber)
                new Color(76, 175, 80)    // #4CAF50 (Green)
        };
        int counter = 0;

        List<Embed> comments = new();
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Comment>>("https://nordevcommentsbackend.fly.dev/api/messages/gettopfivecomments");

            if (response is not null)
            {
                foreach (var comment in response)
                {
                    var embeds = await CommentEmbed.CreateEmbedAsync(comment, postColours[counter]);
                    foreach (var embed in embeds)
                    {
                        comments.Add(embed.Build());
                    }
                    counter++;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        await command.FollowupAsync(embeds: comments.ToArray());
    }
}
