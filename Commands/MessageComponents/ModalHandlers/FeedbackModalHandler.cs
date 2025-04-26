using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.MessageComponents.ModalHandlers;

public class FeedbackModalHandler(ApiService apiService) : InteractionModuleBase<SocketInteractionContext<SocketModal>>
{
    const string feedbackGuildId
 = "1054500338947858522";
    
    [ModalInteraction("bot_feedback")]
    public async Task HandleFoodModal(FeedbackModal modal)
    {
        if (!string.IsNullOrWhiteSpace(modal.Feedback))
        {
            var feedback = new BotFeedback
            {
                Feedback = modal.Feedback,
                GuildId = Context.Guild.Id.ToString(),
                UserName = Context.User.Username,
                UserTag = Context.User.Username,
                DateOfSubmission = DateTime.UtcNow
                
            };
            var response = await apiService.AddBotFeedback(feedback);
            if (response)
            {
                await RespondAsync($"Thanks for your feedback, I really appreciate it!", ephemeral: true);
                
                // Post feedback to the #botfeedback channel
                var guild = Context.Client.GetGuild(ulong.Parse(feedbackGuildId));
                var channel = guild?.TextChannels.FirstOrDefault(c => c.Name == "botfeedback");

                if (channel != null)
                {
                    await channel.SendMessageAsync($"New feedback from {Context.User.Username}:\n{modal.Feedback}");
                }
            }
            
        }
        else
        {
            await RespondAsync("I'll take your lack of feedback as a glowing endorsement, cheers!.");
        }
        await RespondAsync("There was an error saving your feedback, sorry!.");

    }
}

public class FeedbackModal : IModal
{
    public string Title => "feedback";

    [InputLabel("Feedback")]
    [ModalTextInput("feedback", TextInputStyle.Paragraph)]
    public required string Feedback { get; set; }
}