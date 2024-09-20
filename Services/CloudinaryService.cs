using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot.Services;

public class CloudinaryService(IOptions<CloudinaryOptions> cloudinaryOptions, ILogger<CloudinaryService> logger)
{
    readonly Cloudinary _cloudinary = new Cloudinary(cloudinaryOptions.Value.CLOUDINARY_URL)
    {
        Api =
        {
            Secure = true
        }
    };

    public async Task<string?> UploadImageAndReturnCompressedImageUrl(string imageUrl)
    {
        logger.LogInformation("Starting upload for image URL: {ImageUrl}", imageUrl);
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(imageUrl),
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };
        try
        {
            var uploadResults = await _cloudinary.UploadAsync(uploadParams);

            logger.LogInformation(
                "Successfully uploaded {fileName} to Cloudinary with file name", imageUrl);

            return uploadResults.SecureUrl.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error uploading image to Cloudinary from URL: {ImageUrl}", imageUrl);
        }

        return default;
    }
}