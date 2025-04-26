namespace NorDevBestOfBot.Models;

public class BotFeedback
{
    public string? UserName { get; set; }
    public string? UserTag { get; set; }
    public string? GuildId { get; set; }
    public string? Feedback { get; set; }
    public DateTime DateOfSubmission { get; set; }
}