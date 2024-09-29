using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class GetGuildConfig(ApiService apiService, ILogger<GetRandomComment> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("bot-config", "Get the guild configuration.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        try
        {
            logger.LogInformation("Getting guild config for {GuildId}", Context.Guild.Id);
            var response = await apiService.GetGuildConfigAsync(Context.Guild.Id);

            logger.LogInformation("Got guild config : {GuildId}", response.ToString());

            var reply = $"Blacklisted channels: {string.Join(", ", response.BlacklistedChannels!)}";
            reply += $"\nCrosspost channel: {response.CrosspostChannel}";
            reply += $"\nAllow crosspost: {response.AllowCrosspost}";
            
            await FollowupAsync(reply);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await FollowupAsync("An error occurred while trying to get the guild config.");
        }
    }
}