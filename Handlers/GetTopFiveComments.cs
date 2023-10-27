using Discord.WebSocket;
using Discord;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

public class GetTopFiveComments
{
    public static async Task HandleGetTopFiveComments(SocketSlashCommand command, HttpClient httpClient)
    {
        bool isEphemeral = (bool)command.Data.Options.First().Value;

        await command.DeferAsync(ephemeral: isEphemeral);

        // Post to the general channel if the nominated message didn't orginate in the general channel
        var channel = await command.GetChannelAsync() as ITextChannel;

        // colours are a visual cue that two posts are related
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

                    List<Embed> embeds = new();
                    string replyHint = string.Empty;

                    // check if quote exists
                    if (!string.IsNullOrWhiteSpace(comment.quotedMessageAuthor))
                    {
                        replyHint = $"(replying to {comment.quotedMessageAuthor})";
                        var quotedMessage = new EmbedBuilder()
                            .WithAuthor(comment.quotedMessageAuthor, await Helpers.TryGetAvatarAsync(comment.quotedMessageAvatarLink!))
                            .WithDescription(comment.quotedMessage)
                            .WithColor(postColours[counter]);

                        if (!string.IsNullOrWhiteSpace(comment.quotedMessageImage))
                        {
                            quotedMessage.ImageUrl = comment.quotedMessageImage;
                        }

                        embeds.Add(quotedMessage.Build());
                    }

                    // create nominated post
                    var message = new EmbedBuilder()
                        .WithAuthor($"{comment.userName} {replyHint}", await Helpers.TryGetAvatarAsync(comment.iconUrl!))
                        .WithDescription(comment.comment)
                        .WithColor(postColours[counter])
                        .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}");

                    if (!string.IsNullOrWhiteSpace(comment.imageUrl))
                    {
                        message.ImageUrl = comment.imageUrl;
                    }

                    embeds.Add(message.Build());

                    // post
                    var linkButton = new ComponentBuilder()
                        .WithButton(
                            label: "Take me to the post 📫",
                            url: comment.messageLink,
                            style: ButtonStyle.Link,
                            row: 0);

                    // Post for everyone to see
                    if(!isEphemeral)
                    {
                        await channel!.SendMessageAsync(
                            components: linkButton.Build(),
                            embeds: embeds.ToArray());
                    }

                    // post just to user
                    if (isEphemeral)
                    {
                        await command.FollowupAsync(
                            components: linkButton.Build(),
                            embeds: embeds.ToArray(),
                            ephemeral: isEphemeral);
                    }
                    counter++;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
