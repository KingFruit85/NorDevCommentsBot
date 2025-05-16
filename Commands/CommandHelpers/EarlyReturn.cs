using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.CommandHelpers;
public enum EarlyReturnReason
{
    AuthorIsBot,
    MessageIsAlreadyPersisted,
    UserNominatedOwnMessage
}
public class EarlyReturn
{
    private const ulong ChrisUserId = 317070992339894273; 
    
    public static async Task<EarlyReturnReason?> Checks (IMessage message, ApiService apiService, SocketInteractionContext<SocketMessageCommand> context)
    {
        if (message.Author.IsBot)
        {
            return EarlyReturnReason.AuthorIsBot;
        }
        
        var guildId = context.Guild!.Id;
        
        var messageAlreadyPersisted =
            await apiService.CheckIfMessageAlreadyPersistedAsync(message.GetJumpUrl(), guildId);

        if (messageAlreadyPersisted is not null)
        {
            return EarlyReturnReason.MessageIsAlreadyPersisted;
        }
        
        if (context.Interaction.User.Id != ChrisUserId && context.Interaction.User.Id == message.Author.Id)
        {
            return EarlyReturnReason.UserNominatedOwnMessage;
        }
        
        return null;
    }
    
    public static async Task<EarlyReturnReason?> Checks (IMessage message, ApiService apiService, ulong guildId, ulong userId )
    {
        Console.WriteLine("guildId: " + guildId, " userId: " + userId);
        if (message.Author.IsBot)
        {
            return EarlyReturnReason.AuthorIsBot;
        }
        
        if (userId != ChrisUserId && userId == message.Author.Id)
        {
            return EarlyReturnReason.UserNominatedOwnMessage;
        }
        
        var messageAlreadyPersisted =
            await apiService.CheckIfMessageAlreadyPersistedAsync(message.GetJumpUrl(), guildId);

        if (messageAlreadyPersisted is not null)
        {
            return EarlyReturnReason.MessageIsAlreadyPersisted;
        }
        
        
        return null;
    }
}