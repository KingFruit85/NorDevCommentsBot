namespace NorDevBestOfBot.Models;

public class RandomAlreadyPosted
{
    public Id? Id { get; init; }
    public string? GuildId { get; init; }
    public string? MessageId { get; init; }
}

public class Id
{
    public long Timestamp { get; init; }
    public DateTime CreationTime { get; init; }
}