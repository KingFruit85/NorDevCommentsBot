using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class GetRandomComment(ApiService apiService, ILogger<GetRandomComment> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("get-random-comment", "Gets a random comment from the database.")]
    public async Task Handle([Summary(description: "Hide this post?")] bool isEphemeral = true)
    {
        
        try
        {
            var response = await apiService.GetRandomComment(Context.Guild.Id);
            
            if (response is null)
            {
                await RespondAsync("No comments found for this guild.", ephemeral: isEphemeral);
                return;
            }
            
            List<Embed> embeds = [];
            
            var (_, channelId, messageId) = ParseMessageLink.Parse(response.messageLink!);
            var message = await Context.Guild.GetTextChannel(channelId).GetMessageAsync(messageId);
            if (message is null)
            {
                await RespondAsync("Comment was not found in the discord database.", ephemeral: isEphemeral);
                return;
            }
            
            // This order means the embeds will display in the correct order original message first, then the quoted message
            if (message.Reference != null)
            {
                var quotedMessage = await Context.Guild.GetTextChannel(channelId).GetMessageAsync(message.Reference!.MessageId.Value);
                if (quotedMessage is null)
                {
                    return;
                }
                embeds.Add(Create.Embed(quotedMessage));
            }
            embeds.Add(Create.Embed(message));

            
            var voteButtons = new ComponentBuilder()
                .WithButton(
                    "Take me to the post 📫",
                    style: ButtonStyle.Link,
                    url: response.messageLink,
                    row: 1);

            await RespondAsync(embeds: embeds.ToArray(), components: voteButtons.Build(), ephemeral: isEphemeral);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await RespondAsync("An error occurred while trying to get a random comment.", ephemeral: isEphemeral);
        }
    }
}