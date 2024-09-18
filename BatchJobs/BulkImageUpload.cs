using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.BatchJobs;

public class BulkImageUpload(
    ApiService apiService,
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
                    if (comment.imageUrl is not null && comment.messageLink is not null)
                    {
                        logger.LogInformation("Attempting to get imageUrl from {MessageLink}", comment.messageLink);
                        var imageUrl = await helpers.GetImageUrlFromMessage(client, comment.messageLink);
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            var s3ImageUrl = await amazonS3Service.UploadImageViaUrlAsync(imageUrl, timestamp);
                            // call the upset endpoint to update the comment with the s3ImageUrl
                            await apiService.UpsertMessageAsync(comment with { s3ImageUrl = s3ImageUrl });
                        }
                    }

                    if (comment.quotedMessageImage is not null && comment.quotedMessageMessageLink is not null)
                    {
                        logger.LogInformation(
                            "Attempting to get the quoted message imageUrl from {QuotedMessageMessageLink}",
                            comment.quotedMessageMessageLink);
                        var imageUrl =
                            await helpers.GetImageUrlFromMessage(client, comment.quotedMessageMessageLink);
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                        {
                            var s3ImageUrl = await amazonS3Service.UploadImageViaUrlAsync(imageUrl, timestamp);
                            await apiService.UpsertMessageAsync(comment with { s3QuotedMessageImageUrl = s3ImageUrl });
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