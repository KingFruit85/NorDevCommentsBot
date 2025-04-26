using Discord;

namespace NorDevBestOfBot.Commands.CommandHelpers;

public class Create
{
    public static Embed Embed(IMessage message)
    {
        var embed = new EmbedBuilder()
            .WithAuthor(message.Author)
            .WithDescription(message.Content)
            .WithUrl(message.GetJumpUrl());

        switch (message.Attachments.Count)
        {
            // Return early if there are no attachments or embeds
            case 0:
                return embed.Build();
            // Message has a single attachment
            case 1:
            {
                var refAttach = message.Attachments.FirstOrDefault();

                if (refAttach!.Width > 0 && refAttach.Height > 0)
                {
                    // amazonS3Service.UploadImageToS3FromUrlInBackground(refAttach.Url);
                    embed.WithImageUrl(refAttach.Url);
                }

                break;
            }
            // Message has multiple attachments
            case > 1:
            {
                foreach (var attachItem in message.Attachments)
                {
                    if (attachItem.Width <= 0 || attachItem.Height <= 0) continue;
                    // amazonS3Service.UploadImageToS3FromUrlInBackground(attachItem.Url);
                    embed.WithImageUrl(attachItem.Url);
                }
                return embed.Build();
            }
        }

        switch (message.Embeds.Count)
        {
            // Message has single embed
            case 1:
            {
                var refEmbed = message.Embeds.FirstOrDefault();

                if (!refEmbed!.Image.HasValue) return embed.Build();
                // amazonS3Service.UploadImageToS3FromUrlInBackground(refEmbed.Image.Value.Url);
                embed.WithImageUrl(refEmbed.Image.Value.Url);
                return embed.Build();
            }
            // Message has multiple embeds
            case > 1:
            {
                foreach (var embedItem in message.Embeds)
                {
                    if (!embedItem.Image.HasValue) continue;
                    // amazonS3Service.UploadImageToS3FromUrlInBackground(embedItem.Image.Value.Url);
                    embed.WithImageUrl(embedItem.Image.Value.Url);
                }
                break;
            }
        }

        return embed.Build();
    }
}