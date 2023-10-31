using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NorDevBestOfBot;

internal class DatabaseCreator
{

    public static async Task BuildNewDatabase (HttpClient httpClient, DiscordSocketClient client)
    {

        var response = await httpClient.GetFromJsonAsync<List<Comment>>("https://nordevcommentsbackend.fly.dev/api/messages");

        if (response != null)
        {
            foreach (var message in response)
            {
                string[] messageLinkParts = message.messageLink!.Split('/');
                ulong guildId = ulong.Parse(messageLinkParts[4]);
                ulong channelId = ulong.Parse(messageLinkParts[5]);
                ulong nominatedMessageId = ulong.Parse(messageLinkParts[6]);
                var guild = client.GetGuild(guildId);
                var originChannel = guild.GetTextChannel(channelId);
                var nominatedMessage = await originChannel.GetMessageAsync(nominatedMessageId);

                var nominatedMessageEmbedsAndAttachmentUrls = new List<string>();

                foreach (var embed in nominatedMessage.Embeds)
                {
                    nominatedMessageEmbedsAndAttachmentUrls.Add(embed.Url);
                }
                foreach (var attachment in nominatedMessage.Attachments)
                {
                    nominatedMessageEmbedsAndAttachmentUrls.Add(attachment.Url);
                }

                IMessage? refedMessage = null;
                var refedMessageEmbedsAndAttachmentUrls = new List<string>();

                if (nominatedMessage is IUserMessage userMessage)
                {
                    refedMessage = userMessage.ReferencedMessage;

                    foreach (var embed in refedMessage.Embeds)
                    {
                        refedMessageEmbedsAndAttachmentUrls.Add(embed.Url);
                    }
                    foreach (var attachment in refedMessage.Attachments)
                    {
                        refedMessageEmbedsAndAttachmentUrls.Add(attachment.Url);
                    }
                }

                DiscordMessage messageToPersist = new ()
                {
                    DateOfSubmission = nominatedMessage.Timestamp.DateTime,
                    NominatedMessageLink = nominatedMessage.GetJumpUrl(),
                    NominatedMessageAuthorUserName = nominatedMessage.Author.Username,
                    NominatedMessageAuthorDisplayName = (nominatedMessage.Author as IGuildUser)?.Nickname ?? nominatedMessage.Author.GlobalName,
                    NominatedMessageComment = nominatedMessage.Content,
                    NominatedMessageAuthorAvatarUrl = nominatedMessage.Author.GetAvatarUrl(),
                    NominatedMessageEmbedAndAttachmentUrls = nominatedMessageEmbedsAndAttachmentUrls,
                    QuotedMessageMessageLink = refedMessage.GetJumpUrl() ?? null,
                    QuotedMessageComment = refedMessage.Content ?? null,
                    QuotedMessageAuthorUserName = refedMessage.Author.Username ?? null,
                    QuotedMessageAvatarLink = refedMessage.Author.GetAvatarUrl() ?? null,
                    QuotedMessageEmbedAndAttachmentUrls = refedMessageEmbedsAndAttachmentUrls ?? null,
                    QuotedMessageAuthorDisplayname = (refedMessage.Author as IGuildUser)?.Nickname ?? refedMessage.Author.GlobalName,
                    VoteCount = message.voteCount,
                    Voters = message.voters
                };

                // Save comment

                string apiUrl = "https://nordevcommentsbackend.fly.dev/api/messages/savenewcomment";
                var data = JsonSerializer.Serialize(messageToPersist);

                try
                {
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var postResponse = await httpClient.PostAsync(apiUrl, content);

                    if (postResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Persisted message! : {messageToPersist.NominatedMessageLink}");
                    }
                    else
                    {
                        Console.WriteLine($"POST request failed for message : {messageToPersist.NominatedMessageLink},  with status code: {postResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
