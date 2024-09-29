
namespace NorDevBestOfBot.Models;

public record BotGuildConfig
{
    public ulong GuildId { get; set; }
    
    public List<string>? BlacklistedChannels { get; set; }
    
    public string? CrosspostChannel { get; set; }
    
    public bool AllowCrosspost { get; set; }
}