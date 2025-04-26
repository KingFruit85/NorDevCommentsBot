using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.SlashCommands;

public class AddBotFeedback(ApiService apiService, ILogger<AddBotFeedback> logger) : InteractionModuleBase<SocketInteractionContext<SocketSlashCommand>>
{
    [SlashCommand("add-bot-feedback", "Let me know if you have any feature requests.")]
    public async Task Handle([Summary(description: "Feedback?")] string feedback = "", [Summary(description: "Hide this feedback?")] bool isEphemeral = true)
    {
        await DeferAsync(isEphemeral);

        try
        {
            var fb = new BotFeedback
            {
                Feedback = feedback,
                GuildId = Context.Guild.Id.ToString(),
                UserName = Context.User.Username,
                UserTag = Context.User.Username,
                DateOfSubmission = DateTime.UtcNow
                
            };
            var response = await apiService.AddBotFeedback(fb);

            if (response is false)
            {
                await FollowupAsync("There was an error saving your feedback, sorry!.");
            }
            else
            {
                await FollowupAsync("Thanks for your feedback! I really appreciate it.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            await FollowupAsync("An error occurred while trying to save your feedback.{errorMessage}");
        }
    }
}