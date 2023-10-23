using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using System.Text;
using System.Text.Json;

namespace NorDevBestOfBot.Handlers;

internal static class VoteButtonHandler
{
    public static async Task<bool> Vote(
        DiscordSocketClient client, 
        SocketMessageComponent component, 
        HttpClient httpClient)
    {
        await component.DeferAsync();

        var buttonId = component.Data.CustomId.Split('-');
        var buttonChoice = buttonId[0].TrimEnd();
        var messageLink = buttonId[1];

        string[] parts = messageLink.Split('/');

        // Extract the IDs
        string serverId = parts[4];
        string channelId = parts[5];
        string messageId = parts[6];
        int voteCount = (buttonChoice == "yes") ? 1 : -1;

        var server = client.GetGuild(ulong.Parse(serverId));
        var channel = server.GetTextChannel(ulong.Parse(channelId));
        var message = await channel.GetMessageAsync(ulong.Parse(messageId));

        Comment? persistedMessage = await Helpers.CheckIfMessageAlreadyPersistedAsync(messageLink.Trim(), httpClient);
        
        if (persistedMessage is not null && persistedMessage.voters!.Contains(component.User.Username))
        {
            Console.WriteLine("message found in the database");
            await component.FollowupAsync(text: $"You've already voted for this message!",ephemeral: true);
            return false;
        }

        // Message has already been persisted, just adjust the vote count and add the voter
        if (persistedMessage is not null && !persistedMessage.voters!.Contains(component.User.Username))
        {
            bool votedYes = buttonChoice == "yes";

            try
            {
                string url = "https://nordevcommentsbackend.fly.dev/api/messages/addvotetomessage";
                var content = new StringContent(
                    $"?messageLink={messageLink.Trim()}&username={component.User.Username}&votedYes={votedYes}", Encoding.UTF8, "application/json");
                
                Console.WriteLine("sending addvotetomessage POST");
                Console.WriteLine($"Request: {messageLink.Trim()}&username={message.Author.Username}&votedYes={votedYes}");
                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("addvotetomessage Success");

                    string multipleVotes = persistedMessage.voteCount + voteCount > 1 ? "s" : "";

                    await component.FollowupAsync
                    (
                        text: $"Thanks for voting!, {message.Author.Username}'s comment now has {persistedMessage.voteCount + voteCount} vote{multipleVotes}!", null, false, ephemeral: true, null, null, null, null
                    );
                    return true;
                }
                else
                {
                    await component.FollowupAsync(text: $"Opps something isn't working correctly! {response.StatusCode} - {response.ReasonPhrase} - {response.Content}");
                    return false;
                }

            }
            catch (Exception ex)
            {
                await component.FollowupAsync(text: $"Something unexpected happened: {ex.Message}");
                return true;
            }
        }

        // If first time voted on
        var comment = new Comment()
        {
            messageLink = messageLink.Trim(),
            messageId = messageId,
            serverId = serverId,
            userName = component.User.Username,
            userTag  = component.User.Username,
            comment = message.Content,
            voteCount = voteCount,
            iconUrl = message.Author.GetAvatarUrl(),
            dateOfSubmission = DateTime.UtcNow,
            voters = new List<string> { component.User.Username },
            imageUrl = "",
            quotedMessage = "",
            quotedMessageAuthor = "",
            quotedMessageAvatarLink = "",
            quotedMessageImage = "",
            nickname = component.User.Username,
            quotedMessageAuthorNickname = ""
        };

        // Check for attachments, add the urls to the comment
        var attachmentUrls = new List<string>();

        if (message.Attachments.Count > 0)
        {
            foreach (var attachment in message.Attachments)
            {
                attachmentUrls.Add(attachment.Url);
            }

            if (attachmentUrls.Any())
            {
                comment.imageUrl = string.Join(",", attachmentUrls);
            }
        }

        // Do the same if the message refs another message
        if (message is IUserMessage userMessage)
        {
            var refrencedMessage = userMessage.ReferencedMessage;

            if (refrencedMessage != null)
            {

                comment.quotedMessage = refrencedMessage.Content;
                comment.quotedMessageAuthor = refrencedMessage.Author.Username;
                comment.quotedMessageAvatarLink = refrencedMessage.Author.GetAvatarUrl();
                comment.quotedMessageAuthorNickname = "";

                var quotedMessageAttachmentUrls = new List<string>();

                if (refrencedMessage.Attachments.Count > 0)
                {
                    foreach (var attachment in refrencedMessage.Attachments)
                    {
                        quotedMessageAttachmentUrls.Add(attachment.Url);
                    }

                    if (quotedMessageAttachmentUrls.Any())
                    {
                        comment.quotedMessageImage = string.Join(",", quotedMessageAttachmentUrls);
                    }
                }
            }
        }
        string apiUrl = "https://nordevcommentsbackend.fly.dev/api/messages/savecomment";
        var data = JsonSerializer.Serialize(comment);

        try
        {
            var content = new StringContent(data, Encoding.UTF8, "application/json");
            Console.WriteLine("creating new record");
            var response = await httpClient.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"");
                await component.FollowupAsync
                    (
                        text: $"Thanks for voting!, {message.Author.Username}'s comment now has {voteCount} vote!", null, false, ephemeral: true, null, null, null, null
                    );
                    return false;
            }
            else
            {
                Console.WriteLine($"POST request failed with status code: {response.StatusCode}");
                await component.FollowupAsync(text: $"Something unexpected happened!");
                return false;
            }
        }
        catch (Exception ex )
        {
            Console.WriteLine(ex.Message);
            await component.FollowupAsync(text: $"Something unexpected happened!");
        }
        return true;
    }
}
