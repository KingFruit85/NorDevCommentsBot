using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.MessageComponents;

public class VoteButton : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    private readonly ApiService _apiService;
    private readonly DiscordSocketClient _client;

    public VoteButton(DiscordSocketClient client, ApiService apiservice)
    {
        _client = client;
        _apiService = apiservice;
    }

    [ComponentInteraction("vote_button_*")]
    public async Task Handle(bool isVote)
    {
        await DeferAsync();

        var messageLink = Context.Interaction.Message.GetJumpUrl();

        var serverId = Context.Interaction.GuildId!.Value;
        var server = _client.GetGuild(serverId);

        var channelId = Context.Interaction.ChannelId!.Value;
        var channel = server.GetTextChannel(channelId);

        var messageId = Context.Interaction.Message.Id;
        var message = await channel.GetMessageAsync(messageId);

        var voteCountToAdd = isVote ? 1 : -1;

        var persistedMessage = await _apiService.CheckIfMessageAlreadyPersistedAsync(messageLink.Trim());

        if (persistedMessage is not null && persistedMessage.voters!.Contains(Context.User.Username))
        {
            Console.WriteLine("message found in the database");
            await FollowupAsync(
                $"You've already voted for this message!, it currently has {persistedMessage.voteCount} votes",
                ephemeral: true);
            return;
        }

        // Message has already been persisted, just adjust the vote count and add the voter
        if (persistedMessage is not null && !persistedMessage.voters!.Contains(Context.User.Username))
            try
            {
                Console.WriteLine("preparing message to POST");
                Console.WriteLine("sending addvotetomessage POST");

                var isVoteAdded =
                    await _apiService.AddVoteToMessage(messageLink.Trim(), Context.Interaction.User.Username, isVote);

                if (isVoteAdded)
                {
                    Console.WriteLine("addvotetomessage Success");

                    var isPlural = persistedMessage.voteCount + voteCountToAdd == 1 ? "" : "s";

                    await FollowupAsync
                    (
                        $"Thanks for voting!, {message.Author.Username}'s comment now has {persistedMessage.voteCount + voteCountToAdd} vote{isPlural}!",
                        null, false, true
                    );
                    return;
                }

                await FollowupAsync(
                    "Oops something isn't working correctly!");
                return;
            }
            catch (Exception ex)
            {
                await FollowupAsync($"Something unexpected happened: {ex.Message}");
                return;
            }

        // If first time voted on
        var comment = new Comment
        {
            messageLink = messageLink.Trim(),
            messageId = messageId.ToString(),
            serverId = serverId.ToString(),
            userName = message.Author.Username,
            userTag = message.Author.Username,
            comment = message.Content,
            voteCount = voteCountToAdd,
            iconUrl = message.Author.GetAvatarUrl(),
            dateOfSubmission = DateTime.UtcNow,
            voters = new List<string> { Context.Interaction.User.Username },
            imageUrl = "",
            quotedMessage = "",
            quotedMessageAuthor = "",
            quotedMessageAvatarLink = "",
            quotedMessageImage = "",
            nickname = message.Author.Username,
            quotedMessageAuthorNickname = "",
            quotedMessageMessageLink = ""
        };

        // Check for attachments, add the urls to the comment
        var attachmentUrls = new List<string>();

        if (message.Attachments.Count > 0 || message.Embeds.Count > 0)
        {
            attachmentUrls.AddRange(message.Attachments.Select(attachment => attachment.Url));

            attachmentUrls.AddRange(message.Embeds.Select(embed => embed.Url));

            if (attachmentUrls.Any()) comment.imageUrl = string.Join(",", attachmentUrls);
        }

        // Do the same if the message refs another message
        if (message is IUserMessage userMessage)
        {
            var refrencedMessage = userMessage.ReferencedMessage;

            if (refrencedMessage != null)
            {
                comment.quotedMessage = refrencedMessage.Content;
                comment.quotedMessageAuthor = refrencedMessage.Author.Username;
                comment.quotedMessageAvatarLink = refrencedMessage.Author.GetAvatarUrl();
                comment.quotedMessageAuthorNickname = "";
                comment.quotedMessageMessageLink = refrencedMessage.GetJumpUrl().Trim();

                var quotedMessageAttachmentUrls = new List<string>();

                if (refrencedMessage.Attachments.Count > 0 || refrencedMessage.Embeds.Count > 0)
                {
                    quotedMessageAttachmentUrls.AddRange(
                        refrencedMessage.Attachments.Select(attachment => attachment.Url));

                    quotedMessageAttachmentUrls.AddRange(refrencedMessage.Embeds.Select(embed => embed.Url));

                    if (quotedMessageAttachmentUrls.Any())
                        comment.quotedMessageImage = string.Join(",", quotedMessageAttachmentUrls);
                }
            }
        }


        try
        {
            var data = JsonSerializer.Serialize(comment);
            var content = new StringContent(data, Encoding.UTF8, "application/json");

            Console.WriteLine($"creating new record with following content {data}");
            var isCommentSavedSuccessfully = await _apiService.SaveComment(content);

            if (isCommentSavedSuccessfully)
            {
                await FollowupAsync
                (
                    $"Thanks for voting!, {message.Author.Username}'s comment now has {voteCountToAdd} vote!",
                    ephemeral: true
                );
                return;
            }

            await FollowupAsync("Something unexpected happened!, Chris isn't very good at this is he?");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await FollowupAsync("Something unexpected happened!, Chris isn't very good at this is he?");
        }
    }
}