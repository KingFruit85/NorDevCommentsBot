
namespace NorDevBestOfBot.Models;

public class Message
{
    public string? MessageLink { get; set; }
    public int VoteCount { get; set; }
    public Dictionary<string, string>? Voters {  get; set; }   
}
