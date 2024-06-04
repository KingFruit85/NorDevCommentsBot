using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot.Services;

public class InteractionHandlingService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly DiscordSocketClient _discord;
    private readonly InteractionService _interactions;
    private readonly ILogger<InteractionService> _logger;
    private readonly IOptions<ServerOptions> _serverOptions;
    private readonly IServiceProvider _services;

    public InteractionHandlingService(
        DiscordSocketClient discord,
        InteractionService interactions,
        IServiceProvider services,
        IConfiguration config,
        ILogger<InteractionService> logger,
        IOptions<ServerOptions> serverOptions)
    {
        _discord = discord;
        _interactions = interactions;
        _services = services;
        _config = config;
        _logger = logger;
        _serverOptions = serverOptions;

        _interactions.Log += msg =>
        {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.Ready += () =>
        {
            _logger.LogInformation("Bot is connected and ready.");
            return _interactions.RegisterCommandsToGuildAsync(_serverOptions.Value.GuildId);
        };

        _discord.InteractionCreated += OnInteractionAsync;
        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _interactions.Dispose();
        return Task.CompletedTask;
    }

    private async Task OnInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_discord, interaction);
            var result = await _interactions.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ToString());
        }
        catch
        {
            if (interaction.Type == InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(msg => msg.Result.DeleteAsync());
        }
    }
}