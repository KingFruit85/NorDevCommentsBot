using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot;

public class Startup : IHostedService
{
    // private readonly BackgroundScheduler _backgroundScheduler;
    private readonly IOptions<BotOptions> _botOptions;
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<DiscordSocketClient> _logger;

    public Startup(DiscordSocketClient discord,
        IOptions<BotOptions> botOptions,
        ILogger<DiscordSocketClient> logger)
    {
        _discord = discord;
        _botOptions = botOptions;
        _logger = logger;
        // _backgroundScheduler = backgroundScheduler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging in...");
        await _discord.LoginAsync(TokenType.Bot, _botOptions.Value.Token);
        await _discord.StartAsync();

        _logger.LogDebug("Starting background scheduler...");
        // JobManager.Initialize(_backgroundScheduler);
        await Task.Delay(-1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }
}