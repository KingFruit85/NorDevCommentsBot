using Discord;
using Discord.WebSocket;
using FluentScheduler;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.SlashCommands;

namespace NorDevBestOfBot.Services;

public class BackgroundScheduler : Registry
{
    private readonly DiscordSocketClient _client;
    private readonly GetThisMonthsComments _getThisMonthsComments;
    private readonly HttpClient _httpClient;
    private readonly IOptions<ServerOptions> _serverOptions;

    public BackgroundScheduler(DiscordSocketClient client,
        IOptions<ServerOptions> serverOptions,
        HttpClient httpClient,
        GetThisMonthsComments getThisMonthsComments)
    {
        _client = client;
        _serverOptions = serverOptions;
        _httpClient = httpClient;
        _getThisMonthsComments = getThisMonthsComments;

        ScheduleJobs();
    }

    private async void ScheduleJobs()
    {
        Schedule(() =>
            {
                var guild = _client.GetGuild(_serverOptions.Value.GuildId);
                ITextChannel? channel = guild.GetTextChannel(_serverOptions.Value.GuildChannelId);

                Task.Run(() => _getThisMonthsComments.PostThisMonthsComments(channel, false, true));
            })
            .ToRunEvery(1).Months()
            .OnTheLast(DayOfWeek.Friday)
            .At(12, 0);
    }
}