using Discord;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Builders;

internal class CommentEmbed
{
    public static async Task<List<EmbedBuilder>> CreateEmbedAsync(Comment comment, Color postColour)
    {
        List<EmbedBuilder> embeds = new();
        var replyHint = string.Empty;

        if (!string.IsNullOrWhiteSpace(comment.quotedMessageAuthor))
        {
            replyHint = $"(replying to {comment.quotedMessageAuthor})";
            var questedMessage = new EmbedBuilder()
                .WithAuthor(comment.quotedMessageAuthor,
                    await Helpers.TryGetAvatarAsync(comment.quotedMessageAvatarLink!))
                .WithDescription(comment.quotedMessage)
                .WithColor(postColour);

            if (!string.IsNullOrWhiteSpace(comment.quotedMessageImage))
                questedMessage.ImageUrl = comment.quotedMessageImage;

            embeds.Add(questedMessage);
        }

        var message = new EmbedBuilder()
            .WithAuthor($"{comment.userName} {replyHint}", await Helpers.TryGetAvatarAsync(comment.iconUrl!))
            .WithDescription(comment.comment)
            .WithColor(postColour)
            .WithFooter(footer => footer.Text = $"Votes: {comment.voteCount}");

        if (!string.IsNullOrWhiteSpace(comment.imageUrl)) message.ImageUrl = comment.imageUrl;

        embeds.Add(message);

        return embeds;
    }
}