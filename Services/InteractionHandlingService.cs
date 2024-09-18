using System.Reflection;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.BatchJobs;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot.Services;

public class InteractionHandlingService : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly InteractionService _interactions;
    private readonly ILogger<InteractionService> _logger;
    private readonly IOptions<ServerOptions> _serverOptions;
    private readonly IServiceProvider _services;
    private readonly BulkImageUpload bulkImageUpload;

    public InteractionHandlingService(
        DiscordSocketClient discord,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<InteractionService> logger,
        IOptions<ServerOptions> serverOptions,
        BulkImageUpload bulkImageUpload)
    {
        _discord = discord;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _serverOptions = serverOptions;
        this.bulkImageUpload = bulkImageUpload;

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
            // _logger.LogDebug("Starting bulk image upload to s3...");
            // try
            // {
            //     bulkImageUpload.BuckUploadInBackground(_discord);
            // }
            // catch (Exception e)
            // {
            //     _logger.LogError("unable to run bulk upload: {}", e.Message);
            // }
            return _interactions.RegisterCommandsToGuildAsync(_serverOptions.Value.GuildId);
        };

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
                await context.Interaction.RespondAsync($"An error occurred: {ex.Message}", ephemeral: true);
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
                await context.Interaction.RespondAsync($"An error occurred: {ex.Message}", ephemeral: true);
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
                await context.Interaction.RespondAsync($"An error occurred: {ex.Message}", ephemeral: true);
        }
    }
}