using Discord;
using Discord.Interactions;
using Discord.WebSocket;


namespace NorDevBestOfBot.Commands.MessageComponents;

public class FeedbackButton : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("feedback_button")]
    public async Task Handle()
    {

        var mb = new ModalBuilder()
            .WithTitle("Bot Feedback")
            .WithCustomId("bot_feedback")
            .AddTextInput("Feedback", "feedback", TextInputStyle.Paragraph, placeholder: "Enter your feedback here");

    
    await Context.Interaction.RespondWithModalAsync(mb.Build());
    }
}