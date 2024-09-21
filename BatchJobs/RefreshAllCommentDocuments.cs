using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.BatchJobs;

public class RefreshAllCommentDocuments(
    ApiService apiService,
    CloudinaryService cloudinaryService,
    AmazonS3Service amazonS3Service,
    Helpers helpers,
    ILogger<RefreshAllCommentDocuments> logger)
{
    public async Task Refresh(DiscordSocketClient client)
    {
        logger.LogInformation("Fetching all comments.");
        var allComments = apiService.GetAllComments().Result;
        if (allComments is null)
        {
            logger.LogError("No comments found.");
            return;
        }

        foreach (var comment in allComments)
        {
            if (comment.messageLink is null)
            {
                logger.LogInformation("No message link found for comment {CommentId}", comment.messageLink);
                continue;
            }

            var originalComment =
                await helpers.GetCommentFromMessageLinkAsync(client, comment.messageLink);

            if (originalComment is null)
            {
                logger.LogInformation("Original comment not found for {MessageLink}", comment.messageLink);
                continue;
            }

            var attachmentLinks = new List<string>();
            var compressedAttachmentLinks = new List<string>();
            if (originalComment.Attachments.Count > 0)
            {
                foreach (var attachment in originalComment.Attachments)
                {
                    attachmentLinks.Add(attachment.Url);

                    // compress for s3 upload
                    var result = await cloudinaryService.UploadImageAndReturnCompressedImageUrl(attachment.Url);
                    if (result is null)
                    {
                        logger.LogError("Failed to compress image {AttachmentUrl}", attachment.Url);
                        continue;
                    }

                    var s3Url = await amazonS3Service.UploadImageViaUrlAsync(result);
                    if (s3Url is null)
                    {
                        logger.LogError("Failed to upload image to S3 {AttachmentUrl}", attachment.Url);
                        continue;
                    }

                    compressedAttachmentLinks.Add(s3Url);
                }
            }


            await apiService.UpsertMessageAsync(comment with
            {
                imageUrl = string.Join(",", attachmentLinks),
                s3ImageUrl = string.Join(",", compressedAttachmentLinks),
                messageLink = comment.messageLink,
                dateOfSubmission = comment.dateOfSubmission,
                comment = originalComment.Content,
                userName = originalComment.Author.Username,
                userTag = originalComment.Author.Discriminator,
                iconUrl = originalComment.Author.GetAvatarUrl(),
                nickname = originalComment.Author.Username
            });

            logger.LogInformation("Updated comment {CommentId}", comment.messageLink);
        }
    }

    public void RefreshInBackground(DiscordSocketClient client)
    {
        Task.Run(async () =>
        {
            try
            {
                await Refresh(client);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error refreshing comments");
            }
        });
    }
}