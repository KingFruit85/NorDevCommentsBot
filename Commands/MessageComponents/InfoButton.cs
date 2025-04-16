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
            "This bot allows users to nominate discord comments they find notable, You can nominate a message to be added to the best of list by adding a 🏆 emoji to the message, " +
            "you can also right click, or long press the message, select Apps -> nominate-message. The bot also provides various slash commands to interact with nominated comments. try typing /get-random-comment to get a random comment.",
            ephemeral: true);
    }
}