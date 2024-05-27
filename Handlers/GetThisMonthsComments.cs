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

        var channel = await command.GetChannelAsync() as ITextChannel;
        await PostThisMonthsComments(channel, isEphemeral, httpClient, client);
    }

    public static async Task PostThisMonthsComments(ITextChannel? channel, bool isEphemeral, HttpClient httpClient, DiscordSocketClient client)
    {
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

        int colourCounter = 0;

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

                        string refUserNickname = (refedMessage.Author as IGuildUser)?.Nickname ?? refedMessage.Author.GlobalName;
                        string refAvatarUrl = refedMessage.Author.GetAvatarUrl();

                        var quotedMessage = new EmbedBuilder()
                            .WithAuthor(name: refUserNickname, iconUrl: refAvatarUrl)
                            .WithDescription(refedMessage.Content)
                            .WithColor(postColours[colourCounter])
                            .WithUrl(refedMessage.GetJumpUrl());

                        var embed = refedMessage.Embeds.FirstOrDefault();

                        if (embed != null)
                        {
                            if (embed.Image.HasValue)
                            {
                                quotedMessage.ImageUrl = embed.Url;
                            }
                        }

                        var attach = refedMessage.Attachments.FirstOrDefault();

                        if (attach != null)
                        {
                            if (attach.Width > 0 && attach.Height > 0)
                            {
                                quotedMessage.ImageUrl = attach.Url;
                            }
                        }
                        embeds.Add(quotedMessage.Build());
                    }

                    string nickname = (nominatedMessage.Author as IGuildUser)?.Nickname ?? nominatedMessage.Author.GlobalName;
                    string avatarUrl = nominatedMessage.Author.GetAvatarUrl();

                    // create nominated post
                    var message = new EmbedBuilder()
                        .WithAuthor($"{nickname} {replyHint}", avatarUrl)
                        .WithDescription(nominatedMessage.Content)
                        .WithColor(postColours[colourCounter])
                        .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}")
                        .WithUrl(nominatedMessage.GetJumpUrl())
                        .Build();

                    embeds.Add(message);

                    if (nominatedMessage.Embeds.Any() || nominatedMessage.Attachments.Any())
                    {
                        foreach (var embd in nominatedMessage.Embeds)
                        {
                            var newEmbed = new EmbedBuilder()
                                .WithUrl(nominatedMessage.GetJumpUrl())
                                .WithImageUrl(embd.Url)
                                .Build();
                            embeds.Add(newEmbed);
                        }

                        foreach (var atchmt in nominatedMessage.Attachments)
                        {
                            var newEmbed = new EmbedBuilder()
                                .WithUrl(nominatedMessage.GetJumpUrl())
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
                    if (!isEphemeral)
                    {
                        await channel!.SendMessageAsync(
                            components: linkButton.Build(),
                            embeds: embeds.ToArray());
                    }

                    colourCounter++;

                    if (colourCounter >= postColours.Count)
                    {
                        colourCounter = 0;
                    }
                }

                if (isEphemeral)
                {
                    await channel.SendMessageAsync(
                        text: "I hope you enjoyed reading though this month's comments as much as I did 🤗");
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
