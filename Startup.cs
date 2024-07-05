using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services.Scheduling;

namespace NorDevBestOfBot;

public class Startup : IHostedService
{
    private readonly BackgroundScheduler _backgroundScheduler;
    private readonly IOptions<BotOptions> _botOptions;
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<DiscordSocketClient> _logger;
    private readonly PostTopMonthCommentsScheduledTask _postTopMonthCommentsScheduledTask;

    public Startup(DiscordSocketClient discord,
        IOptions<BotOptions> botOptions,
        ILogger<DiscordSocketClient> logger,
        BackgroundScheduler backgroundScheduler,
        PostTopMonthCommentsScheduledTask postTopMonthCommentsScheduledTask)
    {
        _discord = discord;
        _botOptions = botOptions;
        _logger = logger;
        _backgroundScheduler = backgroundScheduler;
        _postTopMonthCommentsScheduledTask = postTopMonthCommentsScheduledTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging in...");
        await _discord.LoginAsync(TokenType.Bot, _botOptions.Value.Token);
        await _discord.StartAsync();

        _logger.LogDebug("Starting background scheduler...");
        _backgroundScheduler.ScheduleJob("month-top-comments", "0 12 28 * *",
            async () => await _postTopMonthCommentsScheduledTask.ExecuteAsync());

        await Task.Delay(-1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }
}