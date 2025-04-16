using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot;

public class Startup(
    DiscordSocketClient discord,
    IOptions<BotOptions> botOptions,
    ILogger<DiscordSocketClient> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Logging in...");
        await discord.LoginAsync(TokenType.Bot, botOptions.Value.Token);
        await discord.StartAsync();

        await Task.Delay(-1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await discord.LogoutAsync();
        await discord.StopAsync();
    }
}