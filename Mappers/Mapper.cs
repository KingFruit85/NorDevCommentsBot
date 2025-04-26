using Discord;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Mappers;

public static class Mapper
{
    public static Comment MapToComment(IMessage message, ulong guildId)
    {
        return new Comment
        {
            messageLink = message.GetJumpUrl(),
            messageId = message.Id.ToString(),
            serverId = guildId.ToString(),
            userName = message.Author.GlobalName ?? message.Author.Username,
            comment = message.Content,
            voteCount = 0,
            iconUrl = message.Author.GetAvatarUrl(),
            dateOfSubmission = DateTime.UtcNow,
            imageUrl = message?.Attachments.Count > 0 ? message.Attachments.First().Url : string.Empty,
        };
    }
}