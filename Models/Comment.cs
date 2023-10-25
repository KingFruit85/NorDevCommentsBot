
namespace NorDevBestOfBot.Models;

public class Comment
{
    public string? messageLink { get; set; }
    public string? messageId { get; set; }
    public string? serverId { get; set; }
    public string? userName { get; set; }
    public string? userTag { get; set; }
    public string? comment { get; set; }
    public int voteCount { get; set; }
    public string? iconUrl { get; set; }
    public DateTime dateOfSubmission { get; set; }
    public List<string>? voters { get; set; }
    public string? imageUrl { get; set; }
    public string? quotedMessage { get; set; }
    public string? quotedMessageAuthor { get; set; }
    public string? quotedMessageAvatarLink { get; set; }
    public string? quotedMessageImage { get; set; }
    public string? nickname { get; set; }
    public string? quotedMessageAuthorNickname { get; set; }
    public string? quotedMessageMessageLink {  get; set; }
}
