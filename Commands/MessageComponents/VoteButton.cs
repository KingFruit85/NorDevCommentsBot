using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;
using static System.Console;

namespace NorDevBestOfBot.Commands.MessageComponents;

public class VoteButton(
    DiscordSocketClient client,
    ApiService apiService,
    CloudinaryService cloudinaryService,
    AmazonS3Service amazonS3Service) : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("vote:*,*")]
    public async Task Handle(bool isVote, string nominatedMessageLink)
    {
        await DeferAsync();
        
        var (guildId, channelId, messageId) = ParseMessageLink.Parse(nominatedMessageLink);
        
        var guild = client.GetGuild(guildId);
        var channel = guild.GetTextChannel(channelId);
        var message = await channel.GetMessageAsync(messageId);

        var voteCountToAdd = isVote ? 1 : -1;

        var persistedMessage = await apiService.CheckIfMessageAlreadyPersistedAsync(nominatedMessageLink, Context.Guild.Id);

        if (persistedMessage is not null && persistedMessage.voters!.Contains(Context.User.Username))
        {
            WriteLine(@"message found in the database");
            await FollowupAsync(
                $"You've already voted for this message!, it currently has {persistedMessage.voteCount} votes",
                ephemeral: true);
            return;
        }

        // Message has already been persisted, just adjust the vote count and add the voter
        if (persistedMessage is not null && !persistedMessage.voters!.Contains(Context.User.Username))
            try
            {
                WriteLine(@"preparing message to POST");
                WriteLine(@"sending addvotetomessage POST");

                var isVoteAdded =
                    await apiService.AddVoteToMessage(nominatedMessageLink.Trim(), Context.Interaction.User.Username,
                        isVote, Context.Guild.Id);

                if (isVoteAdded)
                {
                    WriteLine(@"addvotetomessage Success");

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
            messageId = messageId.ToString(),
            serverId = guildId.ToString(),
            userName = message.Author.Username,
            userTag = message.Author.Username,
            comment = message.Content,
            voteCount = voteCountToAdd,
            iconUrl = message.Author.GetAvatarUrl(),
            dateOfSubmission = DateTime.UtcNow,
            voters = [Context.Interaction.User.Username],
            imageUrl = "",
            s3ImageUrl = "",
            quotedMessage = "",
            quotedMessageAuthor = "",
            quotedMessageAvatarLink = "",
            quotedMessageImage = "",
            s3QuotedMessageImageUrl = "",
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

            if (attachmentUrls.Count != 0)
            {
                // iterate over the attachments and call the image cloudinary service to get a compressed image url then upload to s3
                foreach (var url in attachmentUrls)
                {
                    var compressedImageUrl = await cloudinaryService.UploadImageAndReturnCompressedImageUrl(url);
                    var s3Url = await amazonS3Service.UploadImageViaUrlAsync(compressedImageUrl);

                    comment.s3ImageUrl = !string.IsNullOrEmpty(s3Url) ? s3Url : url;
                }

                comment.imageUrl = string.Join(",", attachmentUrls);
            }
        }

        // Do the same if the message refs another message
        if (message is IUserMessage userMessage)
        {
            var referencedMessage = userMessage.ReferencedMessage;

            if (referencedMessage != null)
            {
                comment.quotedMessage = referencedMessage.Content;
                comment.quotedMessageAuthor = referencedMessage.Author.Username;
                comment.quotedMessageAvatarLink = referencedMessage.Author.GetAvatarUrl();
                comment.quotedMessageAuthorNickname = "";
                comment.quotedMessageMessageLink = referencedMessage.GetJumpUrl().Trim();

                var quotedMessageAttachmentUrls = new List<string>();

                if (referencedMessage.Attachments.Count > 0 || referencedMessage.Embeds.Count > 0)
                {
                    quotedMessageAttachmentUrls.AddRange(
                        referencedMessage.Attachments.Select(attachment => attachment.Url));

                    quotedMessageAttachmentUrls.AddRange(referencedMessage.Embeds.Select(embed => embed.Url));

                    if (quotedMessageAttachmentUrls.Count != 0)
                    {
                        foreach (var url in quotedMessageAttachmentUrls)
                        {
                            var compressedImageUrl =
                                await cloudinaryService.UploadImageAndReturnCompressedImageUrl(url);

                            if (!string.IsNullOrEmpty(compressedImageUrl))
                            {
                                var s3Url = await amazonS3Service.UploadImageViaUrlAsync(compressedImageUrl);
                                comment.s3QuotedMessageImageUrl = s3Url;
                            }
                            else
                            {
                                comment.s3QuotedMessageImageUrl = url;
                            }
                        }

                        comment.quotedMessageImage = string.Join(",", quotedMessageAttachmentUrls);
                    }
                }
            }
        }

        try
        {
            var data = JsonSerializer.Serialize(comment);
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            WriteLine($@"the guildId from guild context is {Context.Guild.Id}");
            WriteLine($@"creating new record with following content {data}");
            var isCommentSavedSuccessfully = await apiService.SaveComment(content, Context.Guild.Id);

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
            WriteLine(ex.Message);
            await FollowupAsync("Something unexpected happened!, Chris isn't very good at this is he?");
        }
    }
}