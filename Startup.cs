using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services.Scheduling;

namespace NorDevBestOfBot;

public class Startup(
    DiscordSocketClient discord,
    IOptions<BotOptions> botOptions,
    ILogger<DiscordSocketClient> logger,
    BackgroundScheduler backgroundScheduler,
    PostTopMonthCommentsScheduledTask postTopMonthCommentsScheduledTask)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Logging in...");
        await discord.LoginAsync(TokenType.Bot, botOptions.Value.Token);
        await discord.StartAsync();

        logger.LogDebug("Starting background scheduler...");
        backgroundScheduler.ScheduleJob("month-top-comments", "0 12 28 * *",
            async () => await postTopMonthCommentsScheduledTask.ExecuteAsync());

        await Task.Delay(-1, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await discord.LogoutAsync();
        await discord.StopAsync();
    }
}