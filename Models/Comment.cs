namespace NorDevBestOfBot.Models;

public record Comment
{
    public string? messageLink { get; init; }
    public string? messageId { get; init; }
    public string? serverId { get; init; }
    public string? userName { get; init; }
    public string? userTag { get; set; }
    public string? comment { get; init; }
    public int voteCount { get; init; }
    public string? iconUrl { get; init; }
    public DateTime dateOfSubmission { get; init; }
    public List<string>? voters { get; init; }
    public string? imageUrl { get; set; }
    
    public string? s3ImageUrl { get; set; }
    public string? quotedMessage { get; set; }
    public string? quotedMessageAuthor { get; set; }
    public string? quotedMessageAvatarLink { get; set; }
    public string? quotedMessageImage { get; set; }
    
    public string? s3QuotedMessageImageUrl { get; set; }
    public string? nickname { get; set; }
    public string? quotedMessageAuthorNickname { get; set; }
    public string? quotedMessageMessageLink { get; set; }
}