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
            await component.FollowupAsync(text: $"You've already voted for this message!, it currently has ${persistedMessage.voteCount} votes",ephemeral: true);
            return false;
        }

        // Message has already been persisted, just adjust the vote count and add the voter
        if (persistedMessage is not null && !persistedMessage.voters!.Contains(component.User.Username))
        {
            bool votedYes = buttonChoice == "yes";

            try
            {
                Console.WriteLine("preparing message to POST");

                string url = "https://nordevcommentsbackend.fly.dev/api/messages/addvotetomessage";
                string parameters = $"?messageLink={Uri.EscapeDataString(messageLink.Trim())}&username={Uri.EscapeDataString(component.User.Username)}&votedYes={votedYes}";

                Console.WriteLine("sending addvotetomessage POST");
                Console.WriteLine($"Request: {url}{parameters}");
                var response = await httpClient.PostAsync(url + parameters, null);

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
                    Console.WriteLine($"something went wrong Request message: {response.RequestMessage}, headers: {response.Content.Headers}");
                    await component.FollowupAsync(text: $"Opps something isn't working correctly! {response.StatusCode} - {response.ReasonPhrase} - {response.Content.Headers}");
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
            userName = message.Author.Username,
            userTag  = message.Author.Username,
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
            nickname = message.Author.Username,
            quotedMessageAuthorNickname = "",
            quotedMessageMessageLink = ""
        };

        // Check for attachments, add the urls to the comment
        var attachmentUrls = new List<string>();

        if (message.Attachments.Count > 0 || message.Embeds.Count > 0)
        {
            foreach (var attachment in message.Attachments)
            {
                attachmentUrls.Add(attachment.Url);
            }

            foreach (var embed in message.Embeds)
            {
                attachmentUrls.Add(embed.Url);
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
                comment.quotedMessageMessageLink = refrencedMessage.GetJumpUrl().Trim();

                var quotedMessageAttachmentUrls = new List<string>();

                if (refrencedMessage.Attachments.Count > 0 || refrencedMessage.Embeds.Count > 0)
                {
                    foreach (var attachment in refrencedMessage.Attachments)
                    {
                        quotedMessageAttachmentUrls.Add(attachment.Url);
                    }

                    foreach (var embed in refrencedMessage.Embeds)
                    {
                        quotedMessageAttachmentUrls.Add(embed.Url);
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
            Console.WriteLine($"creating new record with following content {data}");
            var response = await httpClient.PostAsync(apiUrl, content);
            Console.WriteLine($"Response: {response}");

            var voteComment = voteCount == 0 ? "zero votes 🎻" : $"{voteCount} vote";
            var s = (voteCount == 1 || voteCount == -1) ? "" : "s";

            if (response.IsSuccessStatusCode)
            {
                await component.FollowupAsync
                    (
                        text: $"Thanks for voting!, {message.Author.Username}'s comment now has {voteComment}{s}!", null, false, ephemeral: true, null, null, null, null
                    );
                    return false;
            }
            else
            {
                Console.WriteLine($"POST request failed with status code: {response.StatusCode}");
                await component.FollowupAsync(text: $"Something unexpected happened!, Chris isn't very good at this is he?");
                return false;
            }
        }
        catch (Exception ex )
        {
            Console.WriteLine(ex.Message);
            await component.FollowupAsync(text: $"Something unexpected happened!, Chris isn't very good at this is he?");
        }
        return true;
    }
}
