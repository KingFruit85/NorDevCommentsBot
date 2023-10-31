
namespace NorDevBestOfBot.Models;

public class DiscordMessage
{
    public DateTime DateOfSubmission { get; set; }
    public string? NominatedMessageLink { get; set; }
    public string? NominatedMessageAuthorUserName { get; set; }
    public string? NominatedMessageAuthorDisplayName { get; set; }
    public string? NominatedMessageComment { get; set; }
    public string? NominatedMessageAuthorAvatarUrl { get; set; }
    public List<string>? NominatedMessageEmbedAndAttachmentUrls { get; set; }
    public string? QuotedMessageMessageLink {  get; set; }
    public string? QuotedMessageComment { get; set; }
    public string? QuotedMessageAuthorUserName { get; set; }
    public string? QuotedMessageAvatarLink { get; set; }
    public List<string>? QuotedMessageEmbedAndAttachmentUrls { get; set; }
    public string? QuotedMessageAuthorDisplayname { get; set; }
    public int VoteCount { get; set; }
    public List<string>? Voters { get; set; }
}
