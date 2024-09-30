
namespace NorDevBestOfBot.Models;

public record BotGuildConfig
{
    public ulong GuildId { get; set; }
    
    public List<ulong>? BlacklistedChannels { get; set; }
    
    public List<ulong>? CrosspostChannels { get; set; }
    
    public bool AllowCrosspost { get; set; }
}