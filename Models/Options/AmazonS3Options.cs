namespace NorDevBestOfBot.Models.Options;

public class AmazonS3Options
{
    public string AWS_ACCESS_KEY_ID { get; set; } = string.Empty;
    public string AWS_SECRET_ACCESS_KEY { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}