using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;
using System.Net;
using System.Net.Http.Json;

namespace NorDevBestOfBot;

public class Helpers
{
    public static async Task<string> TryGetAvatarAsync(string url)
    {
        string avatarImage = "https://www.publicdomainpictures.net/pictures/40000/velka/question-mark.jpg";

        using (HttpClient client = new())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    avatarImage = url!;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        return avatarImage;
    }

    // Function to check if an attachment URL is an image
    public static bool IsImageAttachment(string filename)
    {
        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?'))
        {
            ext = ext.Split('?')[0];
        }
        Console.WriteLine($"Checking to see if attachment is image, ext is {ext}");

        return imageExtensions.Contains(ext);
    }

    public static bool IsAudioAttachment(string filename)
    {
        string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?'))
        {
            ext = ext.Split('?')[0];
        }

        return audioExtensions.Contains(ext);
    }

    public static bool IsVideoAttachment(string filename)
    {
        string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?'))
        {
            ext = ext.Split('?')[0];
        }

        return videoExtensions.Contains(ext);
    }

    // Function to check if an embed is an image
    public static bool IsImageEmbed(IEmbed embed)
    {
        // Check the embed type and its URL
        return embed.Type == EmbedType.Image && Uri.IsWellFormedUriString(embed.Url, UriKind.Absolute);
    }

    public static async Task<Comment?> CheckIfMessageAlreadyPersistedAsync(string messageLink, HttpClient httpClient)
    {
        var PotentalPersistedMessageLink = $"https://nordevcommentsbackend.fly.dev/api/messages/GetMessageByMessageLink?id={messageLink}";

        try
        {
            Console.WriteLine("Checking if message has already been persisted");
            var response = await httpClient.GetFromJsonAsync<Comment?>(PotentalPersistedMessageLink);
            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("message not found");
            return null;
        }
    }

    public static string UserNominatingOwnComment(SocketGuildUser user)
    {
        List<string> replies = new()
        {
            $"{user.Mention} don't you think nominating your own comment is a bit cringe?",
            $"@everyone Everyone look, {user.Mention} just tried to nominate their own comment 🤣🤣🤣"
        };

         Random r = new();

        return replies[r.Next(replies.Count)];
    }

    public static string GeneralChannelGreeting(IChannel channel, SocketUser user, SocketMessage message)
    {
        List<string> replies = new()
        {
            $"Hey there, Nordeveans! 🌟 {user.Mention} just nominated a gem from {channel.Name} by {message.Author.Mention} for our 🏆best of list🏆. What's your take on it?",
            $"Looks like {user.Mention} is on the prowl for greatness! They've nominated {message.Author.Mention}'s post in {channel.Name} for our 🏆best of list🏆. What's your verdict?",
            $"It's nomination time! 🏅 {user.Mention} thinks {message.Author.Mention}'s message in {channel.Name} deserves a spot in the 🏆best of list🏆. What's your say?",
            $"{user.Mention} is playing judge today! They've nominated {message.Author.Mention}'s post in {channel.Name} for our prestigious 🏆best of list🏆. Share your thoughts!",
            $"Attention, everyone! 📢 {user.Mention} believes that {message.Author.Mention}'s message in {channel.Name} is worthy of our 🏆best of list🏆. What's your opinion?",
            $"🔔 Nomination alert! {user.Mention} has singled out a message from {message.Author.Mention} in {channel.Name} for the 🏆best of list🏆. What do you think?",
            $"{user.Mention} has nominated a contender! 🌟 Check out {message.Author.Mention}'s post in {channel.Name} and tell us if it deserves a spot in our 🏆best of list🏆.",
            $"It's nomination time, and {user.Mention} is leading the way! They've nominated {message.Author.Mention}'s post in {channel.Name} for the prestigious 🏆best of list🏆. What's your verdict?",
            $"🌠 {user.Mention} just nominated a message from {message.Author.Mention} over in {channel.Name}. Is it worthy of a place in the 🏆best of list🏆?",
            $"Big news! 📢 {user.Mention} has nominated a message by {message.Author.Mention} from {channel.Name} for our 🏆best of list🏆. What's your take on this nomination?",
        };

        Random r = new();

        return replies[r.Next(replies.Count)];
    }

    public static List<Embed> GetRefrencedMessage(IMessage message) // Surely this can be refactored for both messages
    {
        var embeds = new List<Embed>();

        Console.Write("Entering GetReferencedMessage");
        if (message is IUserMessage userMessage)
        {
            Console.WriteLine("Message is IUserMessage");
            var refrencedMessage = userMessage.ReferencedMessage;

            if (refrencedMessage != null)
            {
                var messageLink = userMessage.ReferencedMessage.GetJumpUrl().Trim();

                Console.WriteLine("Referenced message is not null");

                Console.WriteLine($"Referenced message attachment count:{refrencedMessage.Attachments.Count}");

                if (refrencedMessage.Attachments.Count == 0)
                {
                    var refEmbed = new EmbedBuilder()
                        .WithAuthor(refrencedMessage.Author)
                        .WithDescription(refrencedMessage.Content)
                        .WithTimestamp(refrencedMessage.Timestamp)
                        .WithColor(color: new Color(0, 100, 0))
                        .WithUrl(messageLink)
                        .Build();

                    embeds.Add(refEmbed);
                    return embeds;
                }

                if (refrencedMessage.Attachments.Count == 1)
                {
                    var refEmbed = new EmbedBuilder()
                        .WithAuthor(refrencedMessage.Author)
                        .WithImageUrl(refrencedMessage.Attachments.First().Url)
                        .WithDescription(refrencedMessage.Content)
                        .WithTimestamp(refrencedMessage.Timestamp)
                        .WithColor(color: new Color(0, 100, 0))
                        .WithUrl(messageLink)
                        .Build();

                    embeds.Add(refEmbed);
                    return embeds;
                }

                if (refrencedMessage.Attachments.Count > 1)
                {
                    // Check message attachments
                    foreach (var attachment in refrencedMessage.Attachments.Skip(1))
                    {

                        Console.WriteLine("Checking refrenced message attachments");

                        if (IsImageAttachment(attachment.Url))
                        {
                            var e = new EmbedBuilder()
                                .WithUrl(messageLink)
                                .WithImageUrl(attachment.Url)
                                .Build();
                            embeds.Add(e);
                        }
                        else if (IsAudioAttachment(attachment.Url) || IsVideoAttachment(attachment.Url))
                        {
                            var e = new EmbedBuilder()
                                .WithUrl(messageLink)
                                .WithDescription(attachment.Url)
                                .Build();
                            embeds.Add(e);
                        }
                    }
                    var refEmbed = new EmbedBuilder()
                        .WithAuthor(refrencedMessage.Author)
                        .WithImageUrl(refrencedMessage.Attachments.First().Url)
                        .WithDescription(refrencedMessage.Content)
                        .WithTimestamp(refrencedMessage.Timestamp)
                        .WithColor(color: new Color(0, 100, 0))
                        .WithUrl(messageLink)
                        .Build();

                    embeds.Add(refEmbed);
                }
            }
            else
            {
                Console.WriteLine("Refrenced Message is null");
            }
        }
        return embeds;
    }
}
