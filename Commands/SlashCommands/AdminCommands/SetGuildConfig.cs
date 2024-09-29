using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands.AdminCommands;

public class SetGuildConfig(ApiService apiService, ILogger<GetRandomComment> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("set-guild-config", "Set the guild configuration.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        try
        {
            var response = await apiService.GetGuildConfigAsync(Context.Guild.Id);

            var reply = $"Blacklisted channels: {string.Join(", ", response.BlacklistedChannels)}";
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