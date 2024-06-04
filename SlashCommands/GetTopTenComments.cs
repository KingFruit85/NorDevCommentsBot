using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.SlashCommands;

public class GetTopTenComments : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ApiService _apiService;
    private readonly DiscordSocketClient _client;
    private readonly ILogger<GetTopTenComments> _logger;

    public GetTopTenComments(ApiService apiService,
        ILogger<GetTopTenComments> logger,
        DiscordSocketClient client)
    {
        _apiService = apiService;
        _logger = logger;
        _client = client;
    }

    [SlashCommand("get-top-ten-comments", "Gets the top ten comments of all time from the server.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        var colours = ColourExtensions.AllowedColours();


        try
        {
            var response = await _apiService.GetTopTenComments();

            if (response is not null)
            {
                foreach (var (comment, index) in response.Select((comment, index) => (comment, index)))
                {
                    List<Embed> embeds = new();
                    var replyHint = string.Empty;
                    var colourToUse = colours[index % colours.Count];

                    var (guildId, channelId, messageId) = ParseMessageLink(comment.messageLink);
                    var guild = _client.GetGuild(guildId);
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

                        embeds.AddRange(nominatedMessage.Attachments.Select(atchmt =>
                                new EmbedBuilder()
                                    .WithUrl(nominatedMessage.GetJumpUrl())
                                    .WithImageUrl(atchmt.Url)
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

                    switch (isEphemeral)
                    {
                        // Post for everyone to see
                        case false:
                            await Context.Channel.SendMessageAsync(
                                components: linkButton.Build(),
                                embeds: embeds.ToArray());
                            break;
                        // post just to user
                        case true:
                            await FollowupAsync(
                                components: linkButton.Build(),
                                embeds: embeds.ToArray(),
                                ephemeral: isEphemeral);
                            break;
                    }
                }

                await FollowupAsync(
                    "I hope you enjoyed reading though the server's top ten comments as much as I did 🤗",
                    ephemeral: true);
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