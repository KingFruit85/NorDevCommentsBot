using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Commands.CommandHelpers;

public static class PostCommentsHelper
{
    public static async Task<(ComponentBuilder linkButton, List<Embed> embeds)> GetMultipleCommentEmbeds(
        DiscordSocketClient client,
        List<Comment> comments)
    {
        var colours = ColourExtensions.AllowedColours();

        try
        {
            foreach (var (comment, index) in comments.Select((comment, index) => (comment, index)))
            {
                List<Embed> embeds = [];
                var replyHint = string.Empty;
                var colourToUse = colours[index % colours.Count];

                var (guildId, channelId, messageId) = ParseMessageLink.Parse(comment.messageLink!);
                var guild = client.GetGuild(guildId);
                var originChannel = guild.GetTextChannel(channelId);

                var nominatedMessage = await originChannel.GetMessageAsync(messageId);

                IMessage? referencedMessage = nominatedMessage is IUserMessage userMessage
                    ? userMessage.ReferencedMessage
                    : null;

                if (referencedMessage != null)
                {
                    replyHint = $"(replying to {referencedMessage.Author.Username})";

                    var refUserNickname = (referencedMessage.Author as IGuildUser)?.Nickname ??
                                          referencedMessage.Author.GlobalName;
                    var refAvatarUrl = referencedMessage.Author.GetAvatarUrl();

                    var quotedMessage = new EmbedBuilder()
                        .WithAuthor(refUserNickname, refAvatarUrl)
                        .WithDescription(referencedMessage.Content)
                        .WithColor(colourToUse)
                        .WithUrl(referencedMessage.GetJumpUrl());

                    var embed = referencedMessage.Embeds.FirstOrDefault();

                    if (embed?.Image != null) quotedMessage.ImageUrl = embed.Url;

                    var attach = referencedMessage.Attachments.FirstOrDefault();

                    if (attach is { Width: > 0, Height: > 0 })
                        quotedMessage.ImageUrl = attach.Url;

                    embeds.Add(quotedMessage.Build());
                }

                var nickname = (nominatedMessage.Author as IGuildUser)?.Nickname ??
                               nominatedMessage.Author.GlobalName;
                var avatarUrl = nominatedMessage.Author.GetAvatarUrl();

                // create nominated post
                var message = new EmbedBuilder()
                    .WithAuthor($"{nickname} {replyHint}", avatarUrl)
                    .WithDescription(nominatedMessage.Content)
                    .WithColor(colourToUse)
                    .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}")
                    .WithUrl(nominatedMessage.GetJumpUrl())
                    .Build();

                embeds.Add(message);

                if (nominatedMessage.Embeds.Any() || nominatedMessage.Attachments.Any())
                {
                    embeds.AddRange(nominatedMessage.Embeds.Select(embed =>
                            new EmbedBuilder()
                                .WithUrl(nominatedMessage.GetJumpUrl())
                                .WithImageUrl(embed.Url)
                                .Build()
                        )
                    );

                    embeds.AddRange(nominatedMessage.Attachments.Select(attachment =>
                            new EmbedBuilder()
                                .WithUrl(nominatedMessage.GetJumpUrl())
                                .WithImageUrl(attachment.Url)
                                .Build()
                        )
                    );
                }

                var linkButton =
                    new ComponentBuilder().WithButton(
                        "Take me to the post 📫",
                        url: comment.messageLink,
                        style: ButtonStyle.Link,
                        row: 0);

                return (linkButton, embeds);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

        return (new ComponentBuilder(), []);
    }
}