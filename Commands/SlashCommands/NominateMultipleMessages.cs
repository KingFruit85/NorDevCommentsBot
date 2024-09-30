using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Builders;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class NominateMultipleMessages()
    : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("nominate-multiple-messages", "Nominate a selection of messages.")]
    public async Task Handle([Summary(description: "Start message Id")] ulong startMessageId, [Summary(description: "Number of messages to include")] int numberOfMessages, bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);
        const Direction direction = Direction.After;
        const CacheMode cacheMode = CacheMode.AllowDownload;
        // get channel slash command was used in
        var channel = Context.Channel;
        
        // need to find all the messages in the time range
        var messages = await channel.GetMessagesAsync(startMessageId, direction, numberOfMessages, cacheMode).FlattenAsync();
        
        List<Embed> comments = new();

        foreach (var message in messages)
        {
            var comment = new Comment
            {
                messageLink = message.GetJumpUrl(),
                userName = message.Author.Username,
                comment = message.Content,
                iconUrl = message.Author.GetAvatarUrl(),
                voteCount = 0,
            };
            
            var embeds =
                await CommentEmbed.CreateEmbedAsync(comment, new Color(244, 67, 54));

            comments.AddRange(embeds.Select(embed => embed.Build()));
        }
        await FollowupAsync(embeds: comments.ToArray());
    }
    
}