using Discord.WebSocket;
using Discord;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;

namespace NorDevBestOfBot.Handlers;

public class GetThisMonthsComments
{
    public static async Task HandleGetThisMonthsComments(SocketSlashCommand command, HttpClient httpClient, DiscordSocketClient client)
    {
        var firstOption = command.Data.Options.FirstOrDefault();
        bool isEphemeral = firstOption == null || (bool)firstOption.Value;

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
            new Color(76, 175, 80),   // #4CAF50 (Green)
            new Color(233, 30, 99),   // #E91E63 (Pink)
            new Color(33, 150, 243),  // #2196F3 (Blue)
            new Color(255, 87, 34),   // #FF5722 (Deep Orange)
            new Color(63, 81, 181),   // #3F51B5 (Indigo)
            new Color(255, 152, 0),   // #FF9800 (Orange)
            new Color(205, 220, 57),  // #CDDC39 (Lime)
            new Color(158, 158, 158),  // #9E9E9E (Grey)
            new Color(255, 235, 59),  // #FFEB3B (Yellow)
            new Color(48, 79, 254),   // #304FFE (Blue)
            new Color(255, 64, 129),  // #FF4081 (Pink)
            new Color(63, 81, 181),   // #3F51B5 (Indigo)
            new Color(33, 150, 243),  // #2196F3 (Blue)
            new Color(255, 87, 34),   // #FF5722 (Deep Orange)
            new Color(255, 152, 0)    // #FF9800 (Orange)
        };

        int counter = 0;

        List<Embed> comments = new();
        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Comment>>("https://nordevcommentsbackend.fly.dev/api/messages/getthismonthscomments");
            if (response is not null)
            {
                foreach (var comment in response)
                {

                    List<Embed> embeds = new();
                    string replyHint = string.Empty;

                    string[] messageLinkParts = comment.messageLink!.Split('/');
                    ulong guildId = ulong.Parse(messageLinkParts[4]);
                    ulong channelId = ulong.Parse(messageLinkParts[5]);
                    ulong nominatedMessageId = ulong.Parse(messageLinkParts[6]);
                    var guild = client.GetGuild(guildId);
                    var originChannel = guild.GetTextChannel(channelId);

                    var nominatedMessage = await originChannel.GetMessageAsync(nominatedMessageId);
                    IMessage? refedMessage = null;

                    if (nominatedMessage is IUserMessage userMessage)
                    {
                        refedMessage = userMessage.ReferencedMessage;
                    }

                    if (refedMessage != null)
                    {
                        replyHint = $"(replying to {refedMessage.Author.Username})";

                        var quotedMessage = new EmbedBuilder()
                            .WithAuthor(refedMessage.Author)
                            .WithDescription(refedMessage.Content)
                            .WithColor(postColours[counter])
                            .WithUrl(refedMessage.GetJumpUrl())
                            .Build();

                        embeds.Add(quotedMessage);

                        if (refedMessage.Embeds.Any() || refedMessage.Attachments.Any())
                        {
                            foreach (var embd in refedMessage.Embeds)
                            {
                                var newEmbed = new EmbedBuilder()
                                    .WithUrl(refedMessage.GetJumpUrl())
                                    .WithImageUrl(embd.Url)
                                    .Build();
                                embeds.Add(newEmbed);
                            }

                            foreach (var atchmt in refedMessage.Attachments)
                            {
                                var newEmbed = new EmbedBuilder()
                                    .WithUrl(refedMessage.GetJumpUrl())
                                    .WithImageUrl(atchmt.Url)
                                    .Build();
                                embeds.Add(newEmbed);
                            }
                        }
                    }

                    // create nominated post
                    var message = new EmbedBuilder()
                        .WithAuthor($"{nominatedMessage.Author.Username} {replyHint}", nominatedMessage.Author.GetAvatarUrl())
                        .WithDescription(nominatedMessage.Content)
                        .WithColor(postColours[counter])
                        .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}")
                        .WithUrl(nominatedMessage.GetJumpUrl())
                        .Build();

                    embeds.Add(message);

                    if (nominatedMessage.Embeds.Any() || nominatedMessage.Attachments.Any())
                    {
                        foreach (var embd in nominatedMessage.Embeds)
                        {
                            var newEmbed = new EmbedBuilder()
                                .WithUrl(refedMessage.GetJumpUrl())
                                .WithImageUrl(embd.Url)
                                .Build();
                            embeds.Add(newEmbed);
                        }

                        foreach (var atchmt in nominatedMessage.Attachments)
                        {
                            var newEmbed = new EmbedBuilder()
                                .WithUrl(refedMessage.GetJumpUrl())
                                .WithImageUrl(atchmt.Url)
                                .Build();
                            embeds.Add(newEmbed);
                        }
                    }

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

                    if (counter >= postColours.Count)
                    {
                        counter = 0;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
