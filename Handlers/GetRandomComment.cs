using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

public class GetRandomComment
{
    public static async Task HandleGetRandomComment(SocketSlashCommand command, HttpClient httpClient)
    {
        var firstOption = command.Data.Options.FirstOrDefault();
        bool isEphemeral = (firstOption != null) ? (bool)firstOption.Value : true;

        await command.DeferAsync(ephemeral: isEphemeral);

        List<Color> postColours = new()
        {
                new Color(244, 67, 54),   // #F44336 (Red)
                new Color(0, 188, 212),   // #00BCD4 (Cyan)
                new Color(156, 39, 176),  // #9C27B0 (Purple)
                new Color(255, 193, 7),   // #FFC107 (Amber)
                new Color(76, 175, 80)    // #4CAF50 (Green)
        };

        Embed random = new EmbedBuilder().WithDescription("blah").Build();

        int counter = 0;
        try
        {
            var response = await httpClient.GetFromJsonAsync<Comment>("https://nordevcommentsbackend.fly.dev/api/messages/random");

            if (response is not null)
            {
                // Try and get the nickname here



                var reply = await CommentEmbed.CreateEmbedAsync(response, postColours[counter]);
                random = reply.First().Build();
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        await command.FollowupAsync(embed: random);

    }
}
