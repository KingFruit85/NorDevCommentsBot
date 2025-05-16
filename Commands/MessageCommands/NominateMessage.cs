using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NorDevBestOfBot.Commands.MessageCommands;

public class NominateMessage(
    ApiService apiService,
    IOptions<ServerOptions> serverOptions,
    AmazonS3Service amazonS3Service,
    Helpers helpers,
    ILogger<NominateMessage> logger)
    : InteractionModuleBase<SocketInteractionContext<SocketMessageCommand>>
{
    [MessageCommand("nominate-message")]
    public async Task Handle(IMessage nominatedMessage)
    {
        // await DeferAsync();

        var nominatedMessageLink = nominatedMessage.GetJumpUrl().Trim();
        var guildId = Context.Guild!.Id;
        var channel = Context.Guild.GetTextChannel(nominatedMessage.Channel.Id);

        // Check if we should return early
        var shouldReturnEarly = await EarlyReturn.Checks(nominatedMessage, apiService, Context);

        switch (shouldReturnEarly)
        {
            case EarlyReturnReason.AuthorIsBot:
                await RespondAsync("You cannot nominate bot messages", ephemeral: true);
                return;
            case EarlyReturnReason.MessageIsAlreadyPersisted:
                await RespondAsync("This message has already been nominated!", ephemeral: true);
                return;
            case EarlyReturnReason.UserNominatedOwnMessage:
                await RespondAsync(helpers.UserNominatingOwnComment(Context.Interaction.User), ephemeral: true);
                return;
            default:
                logger.LogDebug(
                    "The EarlyReturn.Checks method returned null or an unexpected enum value: {shouldReturnEarly}",
                    shouldReturnEarly);
                break;
        }

        // Create the base comment object
        var comment = Mappers.Mapper.MapToComment(nominatedMessage, guildId) with
        {
            voteCount = 1,
            voters = [Context.User.Username],
        };

        // Check if the message is a reply to another message
        // If it is, we need to update the comment object with the quoted message details
        IUserMessage? quotedMessage = null;

        if (nominatedMessage.Reference is not null)
        {
            quotedMessage =
                await channel.GetMessageAsync(nominatedMessage.Reference.MessageId.Value) as IUserMessage;
            comment.quotedMessageMessageLink =
                quotedMessage?.GetJumpUrl().Trim();
            comment.quotedMessageAuthor = quotedMessage?.Author.GlobalName ?? quotedMessage?.Author.Username;
            comment.quotedMessage = quotedMessage?.Content;
            comment.quotedMessageAvatarLink = quotedMessage?.Author.GetAvatarUrl();
            comment.quotedMessageImage = string.Join(",", quotedMessage?.Attachments.Select(x => x.Url) ?? []);
        }

        // Create a list of embeds that we will include with the response
        List<Embed> embeds = [];

        if (quotedMessage is not null)
        {
            var quotedMessageEmbed = Create.Embed(quotedMessage);
            embeds.Add(quotedMessageEmbed);
        }

        var nominatedMessageEmbed = Create.Embed(nominatedMessage);
        embeds.Add(nominatedMessageEmbed);

        // Add the comment to the database
        var data = JsonSerializer.Serialize(comment);
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        var isCommentSavedSuccessfully = await apiService.SaveComment(content, Context.Guild.Id);
        if (!isCommentSavedSuccessfully)
        {
            logger.LogCritical("Failed to save comment to the database");
        }

        var voteButtons = new ComponentBuilder()
                .WithButton(
                    "👍🏻",
                    $"vote:true,{nominatedMessageLink}",
                    ButtonStyle.Success,
                    row: 0)
                .WithButton(
                    "💩",
                    $"vote:false,{nominatedMessageLink}",
                    ButtonStyle.Danger,
                    row: 0)
                .WithButton(
                    "ℹ️",
                    "info_button",
                    ButtonStyle.Secondary,
                    row: 0
                )
                .WithButton(
                    "📤",
                    style: ButtonStyle.Link,
                    url: nominatedMessageLink,
                    row: 0)
                .WithButton(
                    "🌐",
                    style: ButtonStyle.Link,
                    url: "https://ephemeral-dieffenbachia-1b47c2.netlify.app/?guildId=" + guildId,
                    row: 0)
                .WithButton(
                    "🗳️",
                    customId: "feedback_button",
                    row: 0)
            ;
        
        var generalChannelId = guildId == 680873189106384900 ? 680873189106384988 : 1054500340063555606;

        // Post to the general channel if the nominated message didn't originate in the general channel
        await CrossPostChannelsHelper.PostToChannels([(ulong)generalChannelId], embeds, nominatedMessageLink,
            nominatedMessage, Context);

        // Post to an original channel
        logger.LogDebug(@"Posting message to channel message was nominated in");
        await RespondAsync(
            $"**The {Helpers.GetUserNameAdjective()} {Context.Interaction.User.Mention}** has nominated **{nominatedMessage.Author.Mention}'s** message to be added to the best of list",
            components: voteButtons.Build(),
            embeds: embeds.ToArray()
        );
    }
}