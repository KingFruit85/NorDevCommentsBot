using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;


namespace NorDevBestOfBot.Services;

public class AmazonS3Service(
    IOptions<AmazonS3Options> awsS3Options,
    ILogger<AmazonS3Service> logger,
    HttpClient httpClient)
{
    private readonly AmazonS3Client _client = new();
    private readonly AmazonS3Options _awsS3Options = awsS3Options.Value;

    public async Task<string?> UploadImageViaUrlAsync(string? imageUrl, string overrideDate = "")
    {
        if (imageUrl is null)
        {
            logger.LogError("No image URL provided to upload to S3");
            return null;
        }
        
        var s3Url = "";
        try
        {
            logger.LogInformation("Starting upload for image URL: {ImageUrl}", imageUrl);

            var transferUtility = new TransferUtility(_client);

            var fileName = Helpers.GetFileNameFromDiscordUrl(imageUrl);
            logger.LogInformation("Parsed filename as: {fileName}", fileName);
            var commentDateTime = string.IsNullOrEmpty(overrideDate)
                ? DateTime.UtcNow.ToString("dd-MM-yyyyTHH-mm-ss")
                : overrideDate;

            // A pretty clunky way of handling duplicate file names, it would be better to use an uuid as the key, but I don't want to update the db model right now to hold the uuids
            var key = $"{commentDateTime}_{fileName}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _awsS3Options.BucketName,
                Key = key,
            };

            // Stream the content from the URL to S3
            using (var response = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
                uploadRequest.InputStream = streamToReadFrom;
                await transferUtility.UploadAsync(uploadRequest);
            }

            // Construct the URL of the uploaded file
            s3Url = $"https://{_awsS3Options.BucketName}.s3.{_awsS3Options.Region}.amazonaws.com/{key}";
            logger.LogInformation(
                "Successfully uploaded {fileName} to S3 bucket {bucketName} with file name {uuidString}. Access with this link {s3Url} ",
                fileName, _awsS3Options.BucketName, key, s3Url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading image to S3 from URL: {ImageUrl}", imageUrl);
        }

        return s3Url;
    }

    public void UploadImageToS3FromUrlInBackground(string imageUrl, string overrideDate = "")
    {
        Task.Run(async () =>
        {
            try
            {
                await UploadImageViaUrlAsync(imageUrl, overrideDate);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading image to S3 from URL in background: {ImageUrl}", imageUrl);
            }

        });
    }
}