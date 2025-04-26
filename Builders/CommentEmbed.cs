using Discord;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Builders;

internal class CommentEmbed
{
    public static async Task<List<EmbedBuilder>> CreateEmbedAsync(Comment comment, Color postColour)
    {
        List<EmbedBuilder> embeds = [];
        var replyHint = string.Empty;
        if (!string.IsNullOrWhiteSpace(comment.quotedMessageAuthor))
        {
            replyHint = $"(replying to {comment.quotedMessageAuthor})";
            var quotedMessage = new EmbedBuilder()
                .WithAuthor(comment.quotedMessageAuthor,
                    await Helpers.TryGetAvatarAsync(comment.quotedMessageAvatarLink!))
                .WithDescription(comment.quotedMessage)
                .WithUrl(comment.messageLink)
                .WithColor(postColour);

            if (!string.IsNullOrWhiteSpace(comment.quotedMessageImage))
                quotedMessage.ImageUrl = comment.quotedMessageImage;

            embeds.Add(quotedMessage);
        }

        var message = new EmbedBuilder()
            .WithAuthor($"{comment.userName} {replyHint}", await Helpers.TryGetAvatarAsync(comment.iconUrl!))
            .WithDescription(comment.comment)
            .WithColor(postColour)
            .WithUrl(comment.messageLink)
            .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}");

        if (!string.IsNullOrWhiteSpace(comment.imageUrl)) message.ImageUrl = comment.imageUrl;

        embeds.Add(message);

        return embeds;
    }
    
    public static async Task<EmbedBuilder> CreateSingleEmbedAsync(Comment comment, Color postColour)
    {
        var description = "";

        if (!string.IsNullOrWhiteSpace(comment.quotedMessageAuthor) && !string.IsNullOrWhiteSpace(comment.quotedMessage))
        {
            // Add quoted message in blockquote style
            description += $"> **{comment.quotedMessageAuthor}** said:\n> {comment.quotedMessage}\n\n";
        }

        // Now add the main comment text
        description += comment.comment;

        var embed = new EmbedBuilder()
            .WithAuthor($"{comment.userName}", await Helpers.TryGetAvatarAsync(comment.iconUrl!))
            .WithDescription(description)
            .WithColor(postColour)
            .WithUrl(comment.messageLink)
            .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}");

        // If either the main comment or the quoted message has an image, use the main image
        if (!string.IsNullOrWhiteSpace(comment.imageUrl))
            embed.ImageUrl = comment.imageUrl;
        else if (!string.IsNullOrWhiteSpace(comment.quotedMessageImage))
            embed.ImageUrl = comment.quotedMessageImage;

        return embed;
    }
}