﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace NorDevBestOfBot;

public class Helpers(ILogger<Helpers> logger)
{
    
    public ulong GetGuildIdFromMessageLink(string messageLink)
    {
        var parts = messageLink.Split('/');
        if (parts.Length < 7)
        {
            logger.LogError("Invalid message link format: {MessageLink}", messageLink);
            return 0;
        }

        if (ulong.TryParse(parts[4], out var guildId)) return guildId;
        logger.LogError("Failed to parse guild ID from message link: {MessageLink}", messageLink);
        return 0;
    }
    
    public async Task<IMessage?> GetCommentFromMessageLinkAsync(DiscordSocketClient client, string messageLink)
    {
        var (_, _, message) = await GetObjectsFromMessageLinkPartsAsync(client, messageLink);
        if (message is not null)
        {
            return message;
        }

        logger.LogInformation("no message found");
        return null;
    }

    public async Task<string> GetImageUrlFromMessage(DiscordSocketClient client, string messageLink)
    {
        var (_, _, message) = await GetObjectsFromMessageLinkPartsAsync(client, messageLink);
        if (message is null)
        {
            logger.LogInformation("no message found");
            return string.Empty;
        }

        try
        {
            return message.Attachments.First().Url;
        }
        catch (Exception e)
        {
            logger.LogInformation("no image found {e}", e.Message);
            logger.LogInformation("message attachment count: {attachmentCount}", message.Attachments.Count);
            logger.LogInformation("message embed count: {embedCount}", message.Embeds.Count);
            return string.Empty;
        }
    }

    private async Task<(SocketGuild? server, SocketTextChannel? channel, IMessage? message)>
        GetObjectsFromMessageLinkPartsAsync(DiscordSocketClient client, string messageLink)
    {
        try
        {
            var parts = messageLink.Split('/');
            if (parts.Length < 7)
            {
                logger.LogError("Invalid message link format: {MessageLink}", messageLink);
                return (null, null, null);
            }

            if (!ulong.TryParse(parts[4], out var guildId) ||
                !ulong.TryParse(parts[5], out var channelId) ||
                !ulong.TryParse(parts[6], out var messageId))
            {
                logger.LogError("Failed to parse IDs from message link: {MessageLink}", messageLink);
                return (null, null, null);
            }

            var guild = client.GetGuild(guildId);
            if (guild == null)
            {
                logger.LogError("Guild with ID {GuildId} not found", guildId);
                return (null, null, null);
            }

            var channel = guild.GetTextChannel(channelId);
            if (channel == null)
            {
                logger.LogError("Channel with ID {ChannelId} not found in server {ServerName}", channelId, guild.Name);
                return (null, null, null);
            }

            var message = await channel.GetMessageAsync(messageId);
            if (message == null)
            {
                logger.LogError("Message with ID {MessageId} not found in channel {ChannelName}", messageId,
                    channel.Name);
                return (null, null, null);
            }

            return (guild, channel, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing message link: {MessageLink}", messageLink);
            return (null, null, null);
        }
    }

    public static string GetFileNameFromDiscordUrl(string url)
    {
        // Remove query parameters
        var urlWithoutQuery = url.Split('?')[0];

        // Get the file name from the path
        var fileName = Path.GetFileName(urlWithoutQuery);

        return fileName;
    }

    public static List<Embed> GetEmbedsAndattachments(IUserMessage message)
    {
        List<Embed> embeds = new();

        if (message.Embeds is not null || message.Embeds?.Count > 0)
            foreach (var embed in message.Embeds)
            {
                var em = new EmbedBuilder()
                    .WithUrl(message.GetJumpUrl().Trim())
                    .WithImageUrl(embed.Url)
                    .Build();

                embeds.Add(em);
            }

        if (message.Attachments != null && message.Attachments.Count > 0)
            foreach (var attachment in message.Attachments)
                if (attachment.Width > 0 && attachment.Height > 0)
                {
                    var at = new EmbedBuilder()
                        .WithUrl(message.GetJumpUrl().Trim())
                        .WithImageUrl(attachment.Url)
                        .Build();

                    embeds.Add(at);
                }

        return embeds;
    }

    public static string GetUserNameAdjective()
    {
        List<string> positiveAdjectives = new()
        {
            "Happy",
            "Radiant",
            "Joyful",
            "Sunny",
            "Brilliant",
            "Gleaming",
            "Sparkling",
            "Vibrant",
            "Lively",
            "Cheerful",
            "Breezy",
            "Ecstatic",
            "Blissful",
            "Enchanting",
            "Dazzling",
            "Energetic",
            "Spirited",
            "Dynamic",
            "Optimistic",
            "Glorious",
            "Exuberant",
            "Shimmering",
            "Buoyant",
            "Vivid",
            "Jubilant",
            "Zesty",
            "Playful",
            "Resplendent",
            "Glowing",
            "Fantastic",
            "Marvelous",
            "Splendid",
            "Fabulous",
            "Wonderful",
            "Magical",
            "Amazing",
            "Delightful",
            "Charming",
            "Enthusiastic",
            "Radiant",
            "Thriving",
            "Sparkling",
            "Charismatic",
            "Invigorating",
            "Captivating",
            "Dynamic",
            "Flourishing",
            "Refreshing",
            "Alluring",
            "Captivating"
        };

        Random random = new();

        var randomIndex = random.Next(positiveAdjectives.Count);

        var randomAdjective = positiveAdjectives[randomIndex];

        return randomAdjective;
    }

    public static async Task<string> TryGetAvatarAsync(string url)
    {
        var avatarImage = "https://www.publicdomainpictures.net/pictures/40000/velka/question-mark.jpg";

        using (HttpClient client = new())
        {
            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode) avatarImage = url!;
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
        if (ext.Contains('?')) ext = ext.Split('?')[0];
        Console.WriteLine($@"Checking to see if attachment is image, ext is {ext}");

        return imageExtensions.Contains(ext);
    }

    public static bool IsAudioAttachment(string filename)
    {
        string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?')) ext = ext.Split('?')[0];

        return audioExtensions.Contains(ext);
    }

    public static bool IsVideoAttachment(string filename)
    {
        string[] videoExtensions = { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?')) ext = ext.Split('?')[0];

        return videoExtensions.Contains(ext);
    }

    // Function to check if an embed is an image
    public static bool IsImageEmbed(IEmbed embed)
    {
        // Check the embed type and its URL
        return embed.Type == EmbedType.Image && Uri.IsWellFormedUriString(embed.Url, UriKind.Absolute);
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

    public static string GeneralChannelGreeting(IChannel channel, SocketUser user, IMessage message)
    {
        List<string> replies = new()
        {
            $"Hey there, Nordevians! 🌟 {user.Mention} just nominated a gem from {channel.Name} by {message.Author.Mention} for our 🏆best of list🏆. What's your take on it?",
            $"Looks like {user.Mention} is on the prowl for greatness! They've nominated {message.Author.Mention}'s post in {channel.Name} for our 🏆best of list🏆. What's your verdict?",
            $"It's nomination time! 🏅 {user.Mention} thinks {message.Author.Mention}'s message in {channel.Name} deserves a spot in the 🏆best of list🏆. What's your say?",
            $"{user.Mention} is playing judge today! They've nominated {message.Author.Mention}'s post in {channel.Name} for our prestigious 🏆best of list🏆. Share your thoughts!",
            $"Attention, everyone! 📢 {user.Mention} believes that {message.Author.Mention}'s message in {channel.Name} is worthy of our 🏆best of list🏆. What's your opinion?",
            $"🔔 Nomination alert! {user.Mention} has singled out a message from {message.Author.Mention} in {channel.Name} for the 🏆best of list🏆. What do you think?",
            $"{user.Mention} has nominated a contender! 🌟 Check out {message.Author.Mention}'s post in {channel.Name} and tell us if it deserves a spot in our 🏆best of list🏆.",
            $"It's nomination time, and {user.Mention} is leading the way! They've nominated {message.Author.Mention}'s post in {channel.Name} for the prestigious 🏆best of list🏆. What's your verdict?",
            $"🌠 {user.Mention} just nominated a message from {message.Author.Mention} over in {channel.Name}. Is it worthy of a place in the 🏆best of list🏆?",
            $"Big news! 📢 {user.Mention} has nominated a message by {message.Author.Mention} from {channel.Name} for our 🏆best of list🏆. What's your take on this nomination?",
            $"{user.Mention} has spotlighted {message.Author.Mention}'s post in {channel.Name} as a potential champion for our 🏆best of list🏆. Share your thoughts!",
            $"Attention all! 📣 {user.Mention} has nominated {message.Author.Mention}'s message in {channel.Name} for our esteemed 🏆best of list🏆. What's your verdict?",
            $"🌟 {user.Mention} brings exciting news! They've nominated {message.Author.Mention}'s post in {channel.Name} for our distinguished 🏆best of list🏆. What do you think?",
            $"{user.Mention} just ignited the nomination fire! They've put forward {message.Author.Mention}'s post in {channel.Name} for our coveted 🏆best of list🏆. Share your opinion!",
            $"🔥 {user.Mention} believes {message.Author.Mention}'s message in {channel.Name} has the spark for our 🏆best of list🏆. What's your take on this nomination?",
            $"{user.Mention} is raising the bar! They've selected {message.Author.Mention}'s post in {channel.Name} for potential inclusion in our 🏆best of list🏆. What's your verdict?",
            $"✨ Big announcement! {user.Mention} has nominated {message.Author.Mention}'s message in {channel.Name} for our prestigious 🏆best of list🏆. What's your opinion?",
            $"🏅 {user.Mention} just nominated a standout from {message.Author.Mention} in {channel.Name} for our coveted 🏆best of list🏆. What do you think?",
            $"Breaking news! 📰 {user.Mention} has championed {message.Author.Mention}'s post in {channel.Name} for our renowned 🏆best of list🏆. Share your thoughts!",
            $"{user.Mention} just proposed {message.Author.Mention}'s message in {channel.Name} as a contender for our esteemed 🏆best of list🏆. What's your verdict?"
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
            Console.WriteLine(@"Message is IUserMessage");
            var refrencedMessage = userMessage.ReferencedMessage;

            if (refrencedMessage != null)
            {
                var messageLink = userMessage.ReferencedMessage.GetJumpUrl().Trim();

                Console.WriteLine(@"Referenced message is not null");

                Console.WriteLine($@"Referenced message attachment count:{refrencedMessage.Attachments.Count}");

                if (refrencedMessage.Attachments.Count == 0)
                {
                    var refEmbed = new EmbedBuilder()
                        .WithAuthor(refrencedMessage.Author)
                        .WithDescription(refrencedMessage.Content)
                        .WithTimestamp(refrencedMessage.Timestamp)
                        .WithColor(new Color(0, 100, 0))
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
                        .WithColor(new Color(0, 100, 0))
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
                        Console.WriteLine(@"Checking refrenced message attachments");

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
                        .WithColor(new Color(0, 100, 0))
                        .WithUrl(messageLink)
                        .Build();

                    embeds.Add(refEmbed);
                }
            }
            else
            {
                Console.WriteLine(@"Refrenced Message is null");
            }
        }

        return embeds;
    }
}