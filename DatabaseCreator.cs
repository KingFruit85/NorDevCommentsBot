using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot;

internal class DatabaseCreator
{
    public static async Task BuildNewDatabase(HttpClient httpClient, DiscordSocketClient client)
    {
        var response =
            await httpClient.GetFromJsonAsync<List<Comment>>("https://nordevcommentsbackend.fly.dev/api/messages");

        if (response != null)
            foreach (var message in response)
            {
                Console.WriteLine($@"Creating message : {message.messageLink}");
                var messageLinkParts = message.messageLink!.Split('/');
                var guildId = ulong.Parse(message.serverId!);
                var channelId = ulong.Parse(messageLinkParts[5]);
                var nominatedMessageId = ulong.Parse(message.messageId!);
                var guild = client.GetGuild(guildId);
                var originChannel = guild.GetTextChannel(channelId);
                var nominatedMessage = await originChannel.GetMessageAsync(nominatedMessageId);

                if (nominatedMessage is null)
                {
                    throw new Exception($"Error retrieving nominated message from channel {channelId}");
                }

                var nominatedMessageEmbedsAndAttachmentUrls = nominatedMessage.Embeds.Select(embed => embed.Url).ToList();
                    nominatedMessageEmbedsAndAttachmentUrls.AddRange(nominatedMessage.Attachments.Select(attachment => attachment.Url));

                DiscordMessage messageToPersist = new()
                {
                    DateOfSubmission = nominatedMessage.Timestamp.DateTime,
                    NominatedMessageLink = nominatedMessage.GetJumpUrl(),
                    NominatedMessageAuthorUserName = nominatedMessage.Author.Username,
                    NominatedMessageAuthorDisplayName = (nominatedMessage.Author as IGuildUser)?.Nickname ??
                                                        nominatedMessage.Author.GlobalName,
                    NominatedMessageComment = nominatedMessage.Content,
                    NominatedMessageAuthorAvatarUrl = nominatedMessage.Author.GetAvatarUrl(),
                    NominatedMessageEmbedAndAttachmentUrls = nominatedMessageEmbedsAndAttachmentUrls,
                    QuotedMessageMessageLink = null,
                    QuotedMessageComment = null,
                    QuotedMessageAuthorUserName = null,
                    QuotedMessageAvatarLink = null,
                    QuotedMessageEmbedAndAttachmentUrls = null,
                    QuotedMessageAuthorDisplayname = null,
                    VoteCount = message.voteCount,
                    Voters = message.voters
                };

                if (nominatedMessage is IUserMessage { ReferencedMessage: not null } userMessage)
                {
                    IMessage? referencedMessage = userMessage.ReferencedMessage;
                    var referencedMessageEmbedsAndAttachmentUrls = referencedMessage.Embeds.Select(embed => embed.Url).ToList();
                    referencedMessageEmbedsAndAttachmentUrls.AddRange(referencedMessage.Attachments.Select(attachment => attachment.Url));

                    messageToPersist.QuotedMessageMessageLink = referencedMessage.GetJumpUrl() ?? null;
                    messageToPersist.QuotedMessageComment = referencedMessage.Content ?? null;
                    messageToPersist.QuotedMessageAuthorUserName = referencedMessage.Author.Username ?? null;
                    messageToPersist.QuotedMessageAvatarLink = referencedMessage.Author.GetAvatarUrl() ?? null;
                    messageToPersist.QuotedMessageEmbedAndAttachmentUrls =
                        referencedMessageEmbedsAndAttachmentUrls ?? null;
                    messageToPersist.QuotedMessageAuthorDisplayname =
                        (referencedMessage.Author as IGuildUser)?.Nickname ?? referencedMessage.Author.GlobalName;
                }

                // Save comment
                // TODO: use options value for base url
                const string apiUrl = "https://nordevcommentsbackend.fly.dev/api/messages/savenewcomment";
                var data = JsonSerializer.Serialize(messageToPersist);

                try
                {
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var postResponse = await httpClient.PostAsync(apiUrl, content);

                    Console.WriteLine(postResponse.IsSuccessStatusCode
                        ? $@"Persisted message! : {messageToPersist.NominatedMessageLink}"
                        : $@"POST request failed for message : {messageToPersist.NominatedMessageLink},  with status code: {postResponse.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
    }
}