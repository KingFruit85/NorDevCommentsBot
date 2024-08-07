﻿using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.MessageComponents;

public class VoteButton(
    DiscordSocketClient client, 
    ApiService apiservice) : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    private readonly ApiService _apiService = apiservice;
    private readonly DiscordSocketClient _client = client;

    [ComponentInteraction("vote:*,*")]
    public async Task Handle(bool isVote, string nominatedMessageLink)
    {
        await DeferAsync();
        
        var parts = nominatedMessageLink.Split('/');
        
        var serverId = parts[4];
        var channelId = parts[5];
        var messageId = parts[6];

        var server = _client.GetGuild(ulong.Parse(serverId));
        var channel = server.GetTextChannel(ulong.Parse(channelId));
        var message = await channel.GetMessageAsync(ulong.Parse(messageId));

        var voteCountToAdd = isVote ? 1 : -1;

        var persistedMessage = await _apiService.CheckIfMessageAlreadyPersistedAsync(nominatedMessageLink);

        if (persistedMessage is not null && persistedMessage.voters!.Contains(Context.User.Username))
        {
            Console.WriteLine(@"message found in the database");
            await FollowupAsync(
                $"You've already voted for this message!, it currently has {persistedMessage.voteCount} votes",
                ephemeral: true);
            return;
        }

        // Message has already been persisted, just adjust the vote count and add the voter
        if (persistedMessage is not null && !persistedMessage.voters!.Contains(Context.User.Username))
            try
            {
                Console.WriteLine(@"preparing message to POST");
                Console.WriteLine(@"sending addvotetomessage POST");

                var isVoteAdded =
                    await _apiService.AddVoteToMessage(nominatedMessageLink.Trim(), Context.Interaction.User.Username, isVote);

                if (isVoteAdded)
                {
                    Console.WriteLine(@"addvotetomessage Success");

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
            messageLink = nominatedMessageLink.Trim(),
            messageId = messageId,
            serverId = serverId,
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

            Console.WriteLine($@"creating new record with following content {data}");
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