using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.BatchJobs;

public class BulkImageUpload(
    ApiService apiService,
    CloudinaryService cloudinaryService,
    AmazonS3Service amazonS3Service,
    ILogger<BulkImageUpload> logger,
    Helpers helpers)
{
    private async Task UploadAllImagesFromCommentsWithImageUrls(DiscordSocketClient client)
    {
        try
        {
            logger.LogInformation("Fetching all comments with images.");
            var response = await apiService.GetAllComments();
            if (response != null)
            {
                var numberOfMessages = response.Count.ToString();
                logger.LogInformation("Fetched {numberOfMessages} all comments with images.", numberOfMessages);
                var commentsWithImages = response
                    .Where(x => x.imageUrl != string.Empty || x.quotedMessageImage != string.Empty)
                    .ToList();
                logger.LogInformation("Found {commentsWithImages} comments with images.", commentsWithImages.Count);

                if (commentsWithImages.Count == 0)
                {
                    logger.LogInformation("No comments with images found.");
                    return;
                }

                // iterate through the comments and upload the images
                foreach (var comment in commentsWithImages)
                {
                    logger.LogInformation("reading comment {MessageLink}", comment.messageLink);
                    var timestamp = comment.dateOfSubmission.ToString("dd-MM-yyyyTHH-mm-ss");
                    if (!string.IsNullOrWhiteSpace(comment.imageUrl) && comment.messageLink is not null)
                    {
                        // get the message object
                        var originalComment = await helpers.GetCommentFromMessageLinkAsync(client, comment.messageLink);

                        if (originalComment is null)
                        {
                            logger.LogInformation("Original comment not found for {MessageLink}", comment.messageLink);
                            continue;
                        }

                        logger.LogInformation("Attempting to get imageUrl from {MessageLink}", comment.messageLink);
                        var imageUrl = await helpers.GetImageUrlFromMessage(client, comment.messageLink);
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            var result = await cloudinaryService.UploadImageAndReturnCompressedImageUrl(imageUrl);
                            
                            var imageUrlToUse = result ?? imageUrl;

                            logger.LogInformation("decided to use the following URL: {ImageUrl}", imageUrlToUse);
                            
                            var s3ImageUrl = await amazonS3Service.UploadImageViaUrlAsync(imageUrlToUse, timestamp);
                            
                            // call the upset endpoint to update the comment with the s3ImageUrl
                            await apiService.UpsertMessageAsync(comment with
                            {
                                s3ImageUrl = s3ImageUrl,
                                imageUrl = originalComment.Attachments.First().Url,
                                userName = originalComment.Author.Username,
                                userTag = originalComment.Author.Discriminator,
                                iconUrl = originalComment.Author.GetAvatarUrl(),
                                nickname = originalComment.Author.Username,
                                comment = originalComment.Content
                            });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(comment.quotedMessage))
                    {
                        // This is required because the database had a bug where the quoted message details were not saved
                        // need to retrieve the quoted message link, I think it's probably more reliable to get this from the original comment

                        if (comment.messageLink != null)
                        {
                            var originalComment =
                                await helpers.GetCommentFromMessageLinkAsync(client, comment.messageLink);

                            if (originalComment is null)
                            {
                                logger.LogInformation("Original comment not found for {MessageLink}",
                                    comment.messageLink);
                                continue;
                            }

                            var referenceMessageId = originalComment.Reference.MessageId;
                            var refMessage = await originalComment.Channel.GetMessageAsync((ulong)referenceMessageId);
                            var refMessageLink = refMessage.GetJumpUrl();
                            logger.LogInformation(
                                "Attempting to get the quoted message imageUrl from {QuotedMessageMessageLink}",
                                refMessageLink);

                            var imageUrl =
                                await helpers.GetImageUrlFromMessage(client, refMessageLink);

                            if (!string.IsNullOrWhiteSpace(imageUrl))
                            {
                                var result = await cloudinaryService.UploadImageAndReturnCompressedImageUrl(imageUrl);
                            
                                var imageUrlToUse = result ?? imageUrl;

                                logger.LogInformation("decided to use the following URL: {ImageUrl}", imageUrlToUse);
                                
                                var s3ImageUrl = await amazonS3Service.UploadImageViaUrlAsync(imageUrlToUse, timestamp);
                                await apiService.UpsertMessageAsync(comment with
                                {
                                    s3QuotedMessageImageUrl = s3ImageUrl,
                                    quotedMessageAuthor = refMessage.Author.Username,
                                    quotedMessageAvatarLink = refMessage.Author.GetAvatarUrl(),
                                    quotedMessageMessageLink = refMessageLink,
                                    quotedMessage = refMessage.Content
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        logger.LogInformation("Bulk uploader finished.");
    }

    public void BuckUploadInBackground(DiscordSocketClient client)
    {
        Task.Run(async () =>
        {
            try
            {
                await UploadAllImagesFromCommentsWithImageUrls(client);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error uploading image to S3 from URL in background");
            }
        });
    }
}