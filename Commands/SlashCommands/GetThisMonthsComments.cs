using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

[SuppressMessage("Performance", "CA1860:Avoid using \'Enumerable.Any()\' extension method")]
public class GetThisMonthsComments(
    ApiService apiService,
    ILogger<GetThisMonthsComments> logger,
    DiscordSocketClient client)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-this-months-comments", "Gets this month's comments.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var channel = Context.Channel as ITextChannel;

        List<Comment> comments = [];
        try
        {
            var response = await apiService.GetThisMonthsComments(Context.Guild.Id);
            logger.LogInformation("Retrieved {count} comments", response?.Count);
            if (response is not null)
                comments = response.ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get this-month's comments.");
            throw new Exception(ex.Message);
        }

        if (comments.Any())
        {
            await PostThisMonthsComments(comments, isEphemeral);

            await FollowupAsync(
                "I hope you enjoyed reading though this month's comments as much as I did 🤗",
                ephemeral: isEphemeral);
        }
        else
        {
            await FollowupAsync(
                "No comments found for this month. Please try again later.",
                ephemeral: isEphemeral);
        }
    }

    private async Task PostThisMonthsComments(List<Comment> comments, bool isEphemeral
    )
    {
        var colours = ColourExtensions.AllowedColours();

        try
        {
            foreach (var (comment, index) in comments.Select((comment, index) => (comment, index)))
            {
                List<Embed> embeds = [];
                var replyHint = string.Empty;
                var colourToUse = colours[index % colours.Count];

                var (guildId, channelId, messageId) = ParseMessageLink(comment.messageLink!);
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

                await FollowupAsync(
                    components: linkButton.Build(),
                    embeds: embeds.ToArray(),
                    ephemeral: isEphemeral);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private static (ulong guildId, ulong channelId, ulong messageId) ParseMessageLink(string messageLink)
    {
        var parts = messageLink.Split('/');

        return (ulong.Parse(parts[4]), ulong.Parse(parts[5]), ulong.Parse(parts[6]));
    }
}