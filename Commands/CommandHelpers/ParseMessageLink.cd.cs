namespace NorDevBestOfBot.Commands.CommandHelpers;

public static class ParseMessageLink
{
    public static (ulong guildId, ulong channelId, ulong messageId) Parse(string messageLink)
    {
        var parts = messageLink.Split('/');

        return (ulong.Parse(parts[4]), ulong.Parse(parts[5]), ulong.Parse(parts[6]));
    }
}