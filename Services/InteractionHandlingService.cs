using System.Reflection;
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

        // _discord.InteractionCreated += OnInteractionAsync;
        _discord.SlashCommandExecuted += SlashCommandExecuted;
        _discord.ButtonExecuted += ButtonComponentExecuted;
        _discord.MessageCommandExecuted += MessageCommandExecuted;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _interactions.Dispose();
        return Task.CompletedTask;
    }

    private async Task SlashCommandExecuted(SocketSlashCommand interaction)
    {
        var context = new SocketInteractionContext<SocketSlashCommand>(_discord, interaction);
        try
        {
            var result = await _interactions.ExecuteCommandAsync(context, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ToString());
        }
        catch (Exception ex)
        {
            if (!context.Interaction.HasResponded)
                await context.Interaction.RespondAsync($"An error occoured: {ex.Message}", ephemeral: true);
        }
    }

    private async Task ButtonComponentExecuted(SocketMessageComponent interaction)
    {
        var context = new SocketInteractionContext<SocketMessageComponent>(_discord, interaction);
        try
        {
            var result = await _interactions.ExecuteCommandAsync(context, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ToString());
        }
        catch (Exception ex)
        {
            if (!context.Interaction.HasResponded)
                await context.Interaction.RespondAsync($"An error occoured: {ex.Message}", ephemeral: true);
        }
    }

    private async Task MessageCommandExecuted(SocketMessageCommand interaction)
    {
        var context = new SocketInteractionContext<SocketMessageCommand>(_discord, interaction);
        try
        {
            var result = await _interactions.ExecuteCommandAsync(context, _services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ToString());
        }
        catch (Exception ex)
        {
            if (!context.Interaction.HasResponded)
                await context.Interaction.RespondAsync($"An error occoured: {ex.Message}", ephemeral: true);
        }
    }

    // private async Task OnInteractionAsync(SocketInteraction interaction)
    // {
    //     try
    //     {
    //         var context = new SocketInteractionContext(_discord, interaction);
    //         var result = await _interactions.ExecuteCommandAsync(context, _services);
    //
    //         if (!result.IsSuccess)
    //             await context.Channel.SendMessageAsync(result.ToString());
    //     }
    //     catch
    //     {
    //         if (interaction.Type == InteractionType.ApplicationCommand)
    //             await interaction.GetOriginalResponseAsync()
    //                 .ContinueWith(msg => msg.Result.DeleteAsync());
    //     }
    // }
}