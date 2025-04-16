using System.Reflection;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.BatchJobs;
using NorDevBestOfBot.Commands.MessageCommands;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot.Services;

public class InteractionHandlingService : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly InteractionService _interactions;
    private readonly ILogger<InteractionService> _logger;
    private readonly IServiceProvider _services;
    private readonly Helpers _helpers;
    private readonly ApiService _apiService;

    public InteractionHandlingService(
        DiscordSocketClient discord,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<InteractionService> logger,
        Helpers helpers,
        ApiService apiService)
    {
        _discord = discord;
        _interactions = interactions;
        _services = services;
        _logger = logger;
        _helpers = helpers;
        _apiService = apiService;

        _interactions.Log += msg =>
        {
            Console.WriteLine(msg);
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.Ready += async () =>
        {
            _logger.LogInformation("Bot is connected and ready.");
            
            // // Delete all global commands
            // await _discord.Rest.DeleteAllGlobalCommandsAsync();
            // _logger.LogInformation("Deleted all global commands");
            //
            // // First, delete guild-specific commands
            // foreach (var guild in _discord.Guilds)
            // {
            //     await guild.DeleteApplicationCommandsAsync();
            //     _logger.LogInformation($"Deleted all commands for guild {guild.Id}");
            // }
            
            foreach (var guild in _discord.Guilds)
            {
                _logger.LogInformation("Guild found {id}", guild);
                _logger.LogInformation("Registering all commands for guild {id}", guild.Id);
                try
                {
                    await _interactions.RegisterCommandsToGuildAsync(guild.Id, true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to register commands for guild {id}", guild.Id);
                    throw;
                }
            }

            // // Then, register commands globally
            // await _interactions.RegisterCommandsGloballyAsync(true);
            // _logger.LogInformation("Registered commands globally");
        };

        _discord.SlashCommandExecuted += SlashCommandExecuted;
        _discord.ButtonExecuted += ButtonComponentExecuted;
        _discord.MessageCommandExecuted += MessageCommandExecuted;
        _discord.ReactionAdded += HandleReactionAdded;
        _discord.ReactionRemoved += HandleReactionRemoved;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _interactions.Dispose();
        return Task.CompletedTask;
    }

    private async Task SlashCommandExecuted(SocketSlashCommand interaction)
    {
        _logger.LogInformation("Received slash command {command}", interaction.Data.Name);
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
            {
                await context.Interaction.RespondAsync($"An error occurred: {ex.Message}", ephemeral: true);
            }
            _logger.LogInformation("Some error");

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

    private async Task HandleReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
    {
        // Ignore bot reactions
        if (reaction.User.Value.IsBot) return;
        if (reaction.Message.Value.Author.IsBot) return;

        var message = await cachedMessage.GetOrDownloadAsync();
        var channel = await originChannel.GetOrDownloadAsync();
        var guildId = _helpers.GetGuildIdFromMessageLink(message.GetJumpUrl());
        var guild = _discord.GetGuild(guildId);
        var user = reaction.User.Value;

        // Check if the reaction is the one you're interested in
        if (reaction.Emote.Name == "🏆") // Replace with the emoji you're interested in
        {
            try
            {
                var isPersisted = await _apiService.CheckIfMessageAlreadyPersistedAsync(message.GetJumpUrl(),guildId);
                
                // if message is not null, add vote to message
                if (isPersisted?.voters != null)
                {
                    var userHasVoted = isPersisted.voters.Contains(user.Username);
                    if (userHasVoted) return;
                    // add vote to message
                    await _apiService.AddVoteToMessage(message.GetJumpUrl(), user.Username, true, guildId);
                    var voteCount = isPersisted.voteCount + 1;
                    
                    // if (channel is ITextChannel textChannel)
                    // {
                    //     var ephemeralMessage = await textChannel.SendMessageAsync(
                    //         text: $"{reaction.User.Value.Mention}, thanks for voting for {message.Author.Username}'s message! It now has {isPersisted?.voteCount} votes.",
                    //         flags: MessageFlags.Ephemeral);
                    //
                    //     // Optionally, delete the ephemeral message after a short delay
                    //     _ = Task.Delay(TimeSpan.FromSeconds(3))
                    //         .ContinueWith(_ => ephemeralMessage.DeleteAsync());
                    // }
                }
                else
                
                
                if (isPersisted is null)
                {
                    Console.WriteLine($"message not persisted, persisting message and adding vote");
                    var s3Urls = await _helpers.GetCompressedMessageImageUrls(message);
                    
                    var referencedMessageLink = string.Empty;
                    IUserMessage? referencedMessage = null;
                    var referencedMessageS3ImageUrls = string.Empty;
                    
                    if (message.Reference is not null)
                    {
                        referencedMessage = await channel.GetMessageAsync(message.Reference.MessageId.Value) as IUserMessage;
                        if (referencedMessage is null)
                        {
                            _logger.LogError("Unable to retrieve Referenced message");
                            return;
                        }
                        referencedMessageLink = referencedMessage.GetJumpUrl().Trim();
                        referencedMessageS3ImageUrls = await _helpers.GetCompressedMessageImageUrls(referencedMessage);
                    }
                    
                    
                    // persist message
                    var comment = new Comment
                    {
                        messageLink = message.GetJumpUrl().Trim(),
                        messageId = message.Id.ToString(),
                        serverId = guildId.ToString(),
                        userName = message.Author.Username,
                        comment = message.Content,
                        voteCount = 1,
                        iconUrl = message.Author.GetAvatarUrl(),
                        dateOfSubmission = DateTime.UtcNow,
                        voters = [user.Username],
                        userTag = message.Author.Username,
                        imageUrl = "",
                        s3ImageUrl = s3Urls,
                        quotedMessage = referencedMessage?.Content ?? "",
                        quotedMessageAuthor = referencedMessage?.Author.Username ?? "",
                        quotedMessageAvatarLink = referencedMessage?.Author.GetAvatarUrl() ?? "",
                        quotedMessageImage = "",
                        s3QuotedMessageImageUrl = referencedMessageS3ImageUrls,
                        nickname = message.Author.Username,
                        quotedMessageAuthorNickname = "",
                        quotedMessageMessageLink = referencedMessageLink
                    };
                    
                    var data = JsonSerializer.Serialize(comment);
                    var content = new StringContent(data, Encoding.UTF8, "application/json");

                    await _apiService.SaveComment(content, guildId);
                    
                    await _helpers.NominateMessage(message, user);
                }
                
                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to nominate message");
                throw;
            }
        }
    }

    private async Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> originChannel, SocketReaction reaction)
    {
        // Ignore bot reactions
        if (reaction.User.Value.IsBot) return;

        var channel = await originChannel.GetOrDownloadAsync();
        var message = await cachedMessage.GetOrDownloadAsync();

        // Check if the reaction is the one you're interested in
        if (reaction.Emote.Name == "🏆") // Replace with the emoji you're interested in
        {
            // check if message is persisted
            var isPersisted = await _apiService.CheckIfMessageAlreadyPersistedAsync(message.GetJumpUrl(), _helpers.GetGuildIdFromMessageLink(message.GetJumpUrl()));
            if (isPersisted?.voters != null)
            {
                var userHasVoted = isPersisted.voters.Contains(reaction.User.Value.Username);
                if (!userHasVoted) return;
                // remove vote from message
                await _apiService.AddVoteToMessage(message.GetJumpUrl(), reaction.User.Value.Username, false, _helpers.GetGuildIdFromMessageLink(message.GetJumpUrl()));
                var voteCount = isPersisted.voteCount - 1 ;
                
                // if (channel is ITextChannel textChannel)
                // {
                //     var ephemeralMessage = await textChannel.SendMessageAsync(
                //         text: $"{reaction.User.Value.Mention}, thanks for voting for {message.Author.Username}'s message! It now has {isPersisted?.voteCount} votes.",
                //         flags: MessageFlags.Ephemeral);
                //
                //     // Optionally, delete the ephemeral message after a short delay
                //     _ = Task.Delay(TimeSpan.FromSeconds(3))
                //         .ContinueWith(_ => ephemeralMessage.DeleteAsync());
                // }
            }
        }
    }
}