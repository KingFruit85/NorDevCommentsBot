using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using NorDevBestOfBot.Commands.CommandHelpers;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot;

public class Helpers(
    ILogger<Helpers> logger,
    ApiService apiService,
    AmazonS3Service amazonS3Service)
{
    public async Task<string> GetCompressedMessageImageUrls(IMessage message)
    {
        var s3ImageUrls = string.Empty;

        if (message.Attachments.Count <= 0) return s3ImageUrls;
        foreach (var attachment in message.Attachments)
        {
            if (!(attachment.Width > 0) || !(attachment.Height > 0)) continue;
            var url = await amazonS3Service.UploadImageViaUrlAsync(attachment.Url);
            s3ImageUrls += url;
            s3ImageUrls += ",";
        }

        // remove the last comma
        if (s3ImageUrls.Length > 0) s3ImageUrls = s3ImageUrls.Remove(s3ImageUrls.Length - 1);

        return s3ImageUrls;
    }

    public async Task NominateMessage(IMessage nominatedMessage, IUser nominator)
    {
        const ulong chrisUserId = 317070992339894273;
        var channel = nominatedMessage.Channel as ITextChannel;
        var guild = channel!.Guild;

        Console.WriteLine(@"Entered NominateMessage Method");

        if (nominator.Id == nominatedMessage.Author.Id && nominator.Id != chrisUserId)
        {
            await channel.SendMessageAsync(UserNominatingOwnComment(nominator));
            return;
        }

        var nominatedMessageLink = nominatedMessage.GetJumpUrl().Trim();
        var messageAlreadyPersisted =
            await apiService.CheckIfMessageAlreadyPersistedAsync(nominatedMessageLink, guild.Id);

        // Check if the message has already been nominated
        if (messageAlreadyPersisted is not null)
        {
            var voters = messageAlreadyPersisted?.voters;
            Console.WriteLine($@"votes: {voters}");
            if (voters != null && !voters.Contains(nominator.Username))
            {
                await apiService.AddVoteToMessage(nominatedMessage.GetJumpUrl(), nominator.Username, true, guild.Id);
                var voteCount = messageAlreadyPersisted?.voteCount + 1;
                await nominator.SendMessageAsync(
                    @$"This message has already been nominated, but I've added your vote to it, it now has {voteCount} votes, Cheers!");
                return;
            }
        }


        var referencedMessageLink = string.Empty;
        IUserMessage? referencedMessage = null;

        if (nominatedMessage.Reference is not null)
        {
            Console.WriteLine(@"Nominated message has a reference message");
            var refMessage = await channel.GetMessageAsync(nominatedMessage.Reference.MessageId.Value) as IUserMessage;
            if (refMessage is not null)
            {
                referencedMessageLink = refMessage.GetJumpUrl().Trim();
                referencedMessage = refMessage;
            }
            else
            {
                Console.WriteLine(@"Referenced message not found");
            }
        }

        var voteButtons = new ComponentBuilder()
            .WithButton(
                "👍🏻",
                $"vote:true,{nominatedMessageLink}",
                ButtonStyle.Success,
                row: 0)
            .WithButton(
                "💩",
                $"vote:false,{nominatedMessageLink}",
                ButtonStyle.Danger,
                row: 0)
            .WithButton(
                "ℹ️",
                "info_button",
                ButtonStyle.Secondary,
                row: 0
            )
            .WithButton(
                "📤",
                style: ButtonStyle.Link,
                url: nominatedMessageLink,
                row: 0)
            .WithButton(
                "🌐",
                style: ButtonStyle.Link,
                url: "https://ephemeral-dieffenbachia-1b47c2.netlify.app/?guildId=" + guild.Id,
                row: 0);

        // Create a list of embeds that we will include with the response
        List<Embed> embeds = [];

        if (referencedMessage is not null)
        {
            Console.WriteLine(@"Creating embed for referenced message");
            var referencedMessageEmbed = new EmbedBuilder()
                .WithAuthor(referencedMessage.Author)
                .WithDescription(referencedMessage.Content)
                .WithUrl(referencedMessageLink);

            if (referencedMessage.Attachments.Count == 0)
            {
                embeds.Add(referencedMessageEmbed.Build());
            }

            if (referencedMessage.Attachments.Count == 1)
            {
                var refAttach = referencedMessage.Attachments.FirstOrDefault();
                if (refAttach is { Width: > 0, Height: > 0 })
                {
                    Console.WriteLine(@$"Attempting to upload image embed {referencedMessageEmbed.Url} to s3");
                    amazonS3Service.UploadImageToS3FromUrlInBackground(refAttach.Url);

                    referencedMessageEmbed.WithImageUrl(refAttach.Url);
                    embeds.Add(referencedMessageEmbed.Build());
                }
            }

            if (referencedMessage.Attachments.Count > 1)
            {
                foreach (var attachment in referencedMessage.Attachments)
                {
                    if (!(attachment.Width > 0) || !(attachment.Height > 0)) continue;
                    amazonS3Service.UploadImageToS3FromUrlInBackground(attachment.Url);
                    var attach
                        = new EmbedBuilder()
                            .WithUrl(referencedMessage.GetJumpUrl().Trim())
                            .WithImageUrl(attachment.Url)
                            .Build();

                    embeds.Add(attach);
                }
            }
        }

        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(nominatedMessage.Author)
            .WithDescription(nominatedMessage.Content)
            .WithUrl(nominatedMessageLink);

        if (nominatedMessage.Attachments.Count == 0)
        {
            embeds.Add(nominatedMessageEmbed.Build());
        }

        if (nominatedMessage.Attachments.Count == 1)
        {
            var messageAttachment = nominatedMessage.Attachments.FirstOrDefault();
            if (messageAttachment is { Width: > 0, Height: > 0 })
            {
                amazonS3Service.UploadImageToS3FromUrlInBackground(messageAttachment.Url);
                nominatedMessageEmbed.WithImageUrl(messageAttachment.Url);
                embeds.Add(nominatedMessageEmbed.Build());
            }
        }

        if (nominatedMessage.Attachments.Count > 1)
        {
            foreach (var attachment in nominatedMessage.Attachments)
            {
                if (!(attachment.Width > 0) || !(attachment.Height > 0)) continue;
                amazonS3Service.UploadImageToS3FromUrlInBackground(attachment.Url);
                var e = nominatedMessageEmbed;
                e.WithImageUrl(attachment.Url);
                embeds.Add(e.Build());
            }
        }

        Console.WriteLine("Sending message to channel -------------------------");
        await channel.SendMessageAsync(
            text:
            $"**The {GetUserNameAdjective()} {nominator.Mention}** has nominated **{nominatedMessage.Author.Mention}'s** message to be added to the best of list",
            components: voteButtons.Build(), embeds: embeds.ToArray());

        // Post to the general channel if the nominated message didn't originate in the general channel

        var generalChannelId = guild.Id == 680873189106384900 ? 680873189106384988 : 1054500340063555606;
        var crossPostChannel = await guild.GetChannelAsync((ulong)generalChannelId) as ITextChannel;
        if (crossPostChannel is null) return;
        Console.WriteLine($"channel Id {channel.Id}, crossPostChannel Id {crossPostChannel.Id}");

        if (channel.Id != crossPostChannel.Id)
        {
            Console.WriteLine("Posting to general");
            await CrossPostChannelsHelper.PostToChannel(embeds, nominatedMessageLink, nominatedMessage, crossPostChannel, (SocketUser)nominator, channel);
        }
    }

    public static List<Embed> GetMessageAttachments(IMessage message, AmazonS3Service amazonS3Service,
        IUserMessage? referencedMessage = null)
    {
        Console.WriteLine(referencedMessage is not null);
        List<Embed> messageAttachments = [];

        if (referencedMessage is not null)
        {
            Console.WriteLine(@"Creating embed for referenced message)");
            Console.WriteLine(
                $@"The referenced message has {referencedMessage.Attachments.Count} attachments");

            var referencedMessageEmbed = new EmbedBuilder()
                .WithAuthor(referencedMessage.Author)
                .WithDescription(referencedMessage.Content)
                .WithUrl(referencedMessage.GetJumpUrl().Trim());

            switch (referencedMessage.Attachments.Count)
            {
                case 0:
                    messageAttachments.Add(referencedMessageEmbed.Build());
                    break;
                case 1:
                {
                    var refAttach = referencedMessage.Attachments.FirstOrDefault();

                    if (refAttach is { Width: > 0, Height: > 0 })
                    {
                        Console.WriteLine(@$"Attempting to upload image embed {referencedMessageEmbed.Url} to s3");
                        amazonS3Service.UploadImageToS3FromUrlInBackground(refAttach.Url);

                        referencedMessageEmbed.WithImageUrl(refAttach.Url);
                        messageAttachments.Add(referencedMessageEmbed.Build());
                    }

                    break;
                }
                case > 1:
                {
                    foreach (var a in referencedMessage.Attachments)
                        if (a.Width > 0 && a.Height > 0)
                        {
                            Console.WriteLine(@$"Attempting to upload image embed {a.Url} to s3");
                            amazonS3Service.UploadImageToS3FromUrlInBackground(a.Url);
                            var at = new EmbedBuilder()
                                .WithUrl(referencedMessage.GetJumpUrl().Trim())
                                .WithImageUrl(a.Url)
                                .Build();

                            messageAttachments.Add(at);
                        }

                    break;
                }
            }
        }

        Console.WriteLine(@"Creating embeds the nominated message)");
        Console.WriteLine(
            $@"The nominated message has {message.Attachments.Count} attachments and {message.Embeds.Count} embeds");

        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(message.Author)
            .WithDescription(message.Content)
            .WithUrl(message.GetJumpUrl().Trim());

        switch (message.Attachments.Count)
        {
            case 0:
                messageAttachments.Add(nominatedMessageEmbed.Build());
                break;
            case 1:
            {
                var messageAttachment = message.Attachments.FirstOrDefault();

                if (messageAttachment is { Width: > 0, Height: > 0 })
                {
                    Console.WriteLine(@$"Attempting to upload image attachment {messageAttachment.Url} to s3");
                    amazonS3Service.UploadImageToS3FromUrlInBackground(messageAttachment.Url);

                    nominatedMessageEmbed.WithImageUrl(messageAttachment.Url);
                    messageAttachments.Add(nominatedMessageEmbed.Build());
                }

                break;
            }
            case > 1:
            {
                Console.WriteLine($@"found {message.Attachments.Count} attachments and {message.Embeds.Count} embeds");
                foreach (var attachment in message.Attachments)
                    if (attachment.Width > 0 && attachment.Height > 0)
                    {
                        Console.WriteLine(@$"Attempting to upload image attachment {attachment.Url} to s3");
                        amazonS3Service.UploadImageToS3FromUrlInBackground(attachment.Url);
                        var e = nominatedMessageEmbed;
                        e.WithImageUrl(attachment.Url);
                        messageAttachments.Add(e.Build());
                    }

                break;
            }
        }

        return messageAttachments;
    }

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
        List<string> positiveAdjectives =
        [
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
        ];

        Random random = new();

        var randomIndex = random.Next(positiveAdjectives.Count);

        var randomAdjective = positiveAdjectives[randomIndex];

        return randomAdjective;
    }

    public static async Task<string> TryGetAvatarAsync(string url)
    {
        var avatarImage = "https://www.publicdomainpictures.net/pictures/40000/velka/question-mark.jpg";

        using HttpClient client = new();
        try
        {
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode) avatarImage = url!;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $@"Error fetching avatar image from URL: {url}, using default avatar image. {ex.Message}");
        }

        return avatarImage;
    }

// Function to check if an attachment URL is an image
    private static bool IsImageAttachment(string filename)
    {
        string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?')) ext = ext.Split('?')[0];
        Console.WriteLine($@"Checking to see if attachment is image, ext is {ext}");

        return imageExtensions.Contains(ext);
    }

    private static bool IsAudioAttachment(string filename)
    {
        string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".flac", ".m4a", ".aac" };
        var parts = filename.Split('.');
        var ext = '.' + parts.Last().ToLower();
        if (ext.Contains('?')) ext = ext.Split('?')[0];

        return audioExtensions.Contains(ext);
    }

    private static bool IsVideoAttachment(string filename)
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

    public string UserNominatingOwnComment(IUser user)
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
                Console.WriteLine(@"Referenced Message is null");
            }
        }

        return embeds;
    }
}