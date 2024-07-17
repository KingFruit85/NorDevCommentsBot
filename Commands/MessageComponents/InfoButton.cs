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
            "This bot is allows users to nominate discord comments they find notable, You can nominate a message to be added to the best of list by right clicking on the message, " +
            "then selecting Apps -> nominate-message. You can use various slash commands to retrieve these comments. try typing /get-random-comment to get a random comment.",
            ephemeral: true);
    }
}