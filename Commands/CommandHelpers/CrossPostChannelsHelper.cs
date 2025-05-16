using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace NorDevBestOfBot.Commands.CommandHelpers;

public class CrossPostChannelsHelper
{
    public static async Task PostToChannels(List<ulong> channels, List<Embed> embeds, string nominatedMessageLink,
        IMessage nominatedMessage, SocketInteractionContext<SocketMessageCommand> context)
    {
        var generalChannel = context.Client.GetChannel(channels[0]) as ITextChannel;

        if (generalChannel is not null && context.Channel.Id != generalChannel.Id)
        {
            var messageLinkButton = new ComponentBuilder()
                .WithButton(
                    "Take me to the post 📫",
                    style: ButtonStyle.Link,
                    url: nominatedMessageLink,
                    row: 0)
                .WithButton(
                    "👍🏻",
                    $"vote:true,{nominatedMessageLink}",
                    ButtonStyle.Success,
                    row: 0)
                .WithButton(
                    "💩",
                    $"vote:false,{nominatedMessageLink}",
                    ButtonStyle.Danger,
                    row: 0);

            await generalChannel.SendMessageAsync(
                Helpers.GeneralChannelGreeting(context.Interaction.Channel, context.Interaction.User,
                    nominatedMessage),
                allowedMentions: AllowedMentions.All,
                components: messageLinkButton.Build(),
                embeds: embeds.ToArray());
        }
    }
    
    public static async Task PostToChannel(List<Embed> embeds, string nominatedMessageLink,
        IMessage nominatedMessage, ITextChannel channel, SocketUser nominator)
    {
            var messageLinkButton = new ComponentBuilder()
                .WithButton(
                    "Take me to the post 📫",
                    style: ButtonStyle.Link,
                    url: nominatedMessageLink,
                    row: 0)
                .WithButton(
                    "👍🏻",
                    $"vote:true,{nominatedMessageLink}",
                    ButtonStyle.Success,
                    row: 0)
                .WithButton(
                    "💩",
                    $"vote:false,{nominatedMessageLink}",
                    ButtonStyle.Danger,
                    row: 0);

            await channel.SendMessageAsync(
                Helpers.GeneralChannelGreeting(channel, nominator,
                    nominatedMessage),
                allowedMentions: AllowedMentions.All,
                components: messageLinkButton.Build(),
                embeds: embeds.ToArray());
    }
}