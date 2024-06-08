using Discord.Interactions;
using Discord.WebSocket;

namespace NorDevBestOfBot.Commands.MessageComponents;

public class InfoButton : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("info_button")]
    public async Task Handle()
    {
        await DeferAsync();

        await FollowupAsync(
            "You can nominate a message to be added to the best of list by right clicking on the message, then selecting Apps -> nominate-message",
            ephemeral: true);
    }
}