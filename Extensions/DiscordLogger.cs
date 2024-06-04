using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace NorDevBestOfBot.Extensions;

public class DiscordLogger
{
    private readonly ILogger<DiscordLogger> _logger;

    public DiscordLogger(DiscordSocketClient discord,
        ILogger<DiscordLogger> logger)
    {
        _logger = logger;
        discord.Log += LogDiscordMessage;
    }

    private Task LogDiscordMessage(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                _logger.LogCritical(message.Exception, message.Message);
                break;
            case LogSeverity.Error:
                _logger.LogError(message.Exception, message.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning(message.Message);
                break;
            case LogSeverity.Info:
                _logger.LogInformation(message.Message);
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                _logger.LogDebug(message.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return Task.CompletedTask;
    }
}