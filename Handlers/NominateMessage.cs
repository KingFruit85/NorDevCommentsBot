using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Handlers;

internal class NominateMessage
{
    public static async Task HandleNominateMessageAsync(SocketMessageCommand command, DiscordSocketClient client, HttpClient httpClient, SocketGuild? guild)
    {
        Console.WriteLine("Entered HandleNominateMessageAsync");

        Console.WriteLine("Checking if user nominated own message");
        if (command.User.Id == command.Data.Message.Author.Id && command.User.Id != 317070992339894273)
        {
            var interactionUser = guild!.GetUser(command.User.Id);
            await command.FollowupAsync(text: Helpers.UserNominatingOwnComment(interactionUser), ephemeral: false);
            return;
        }

        var server = client.GetGuild(command.GuildId!.Value);
        var channel = server.GetTextChannel(command.ChannelId!.Value);
        var nominatedMessage = await channel.GetMessageAsync(command.Data.Message.Id); // <-- here
        Console.WriteLine($"got message {nominatedMessage.Id} - {nominatedMessage.Content}");
        string refMessageLink = string.Empty;
        IUserMessage? refrencedMessage = null;

        // Have to cast here to get the ReferencedMessage
        if (nominatedMessage is IUserMessage userMessage)
        {
            Console.WriteLine("Message is IUserMessage");
            if (userMessage.ReferencedMessage is not null)
            {
                refrencedMessage = userMessage.ReferencedMessage;
                refMessageLink = refrencedMessage.GetJumpUrl().Trim();
            }
        }
        string nominatedMessageLink = nominatedMessage.GetJumpUrl().Trim();

        Comment? MessageAlreadyPersisted = await Helpers.CheckIfMessageAlreadyPersistedAsync(nominatedMessageLink, httpClient);

        if (MessageAlreadyPersisted is not null)
        {
            await command.FollowupAsync(text: $"This message has already been added to the best of list", ephemeral: true);
            return;
        }

        var voteButtons = new ComponentBuilder()
            .WithButton(
                label: "I Agree 👍🏻",
                customId: $"yes - {nominatedMessageLink}",
                style: ButtonStyle.Success,
                row: 0)

            .WithButton(
                label: "I Disagree 💩",
                customId: $"no - {nominatedMessageLink}",
                style: ButtonStyle.Danger,
                row: 0);

        // Create a list of embeds that we will include with the response
        List<Embed> embeds = new ();

        // Check if the message refrences another message, if it does we'll want to post that first
        if (refrencedMessage is not null)
        {
            Console.WriteLine($"Creating embeds for refrenced message");
            Console.WriteLine($"The refrenced message has {refrencedMessage.Embeds.Count} embeds and {refrencedMessage.Attachments.Count} attachments");

            var refrencedMessageEmbed = new EmbedBuilder()
                .WithAuthor(refrencedMessage.Author)
                .WithDescription(refrencedMessage.Content)
                .WithUrl(refMessageLink);

            if (!refrencedMessage.Embeds.Any() || !refrencedMessage.Attachments.Any())
            {
                embeds.Add(refrencedMessageEmbed.Build());
            }
            
            // Singles
            if (refrencedMessage.Embeds.Count == 1)
            {
                var refEmbed = refrencedMessage.Embeds.FirstOrDefault();
                var e = refrencedMessageEmbed;

                if (refEmbed!.Image.HasValue)
                {
                    e.WithImageUrl(refEmbed.Image.Value.Url);
                    embeds.Add(e.Build());
                }
            }

            if (refrencedMessage.Attachments.Count == 1)
            {
                var refAttach = refrencedMessage.Attachments.FirstOrDefault();
                var e = refrencedMessageEmbed;

                if (refAttach!.Width > 0 && refAttach!.Height > 0)
                {
                    e.WithImageUrl(refAttach.Url);
                    embeds.Add(e.Build());
                }
            }

            // multiples
            if ( refrencedMessage.Embeds.Count > 1)
            {
                foreach (var e in refrencedMessage.Embeds)
                {
                    var em = new EmbedBuilder()
                        .WithUrl(refMessageLink)
                        .WithImageUrl(e.Url)
                        .Build();

                    embeds.Add(em);
                }
            }

            if (refrencedMessage.Attachments.Count > 1)
            {
                foreach (var a in refrencedMessage.Attachments)
                {
                    if (a.Width > 0 && a.Height > 0)
                    {
                        var at = new EmbedBuilder()
                            .WithUrl(refMessageLink)
                            .WithImageUrl(a.Url)
                            .Build();

                        embeds.Add(at);
                    }
                }
            }
        }

        // Create nominated message embed
        Console.WriteLine($"Creating main embed for nominated message");
        Console.WriteLine($"The message has {nominatedMessage.Embeds.Count} embeds and {nominatedMessage.Attachments.Count} attachments");
        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(nominatedMessage.Author)
            .WithDescription(nominatedMessage.Content)
            .WithUrl(nominatedMessageLink);

        if (!nominatedMessage.Embeds.Any() || !nominatedMessage.Attachments.Any())
        {
            Console.WriteLine($"found no embeds or attachments");
            embeds.Add(nominatedMessageEmbed.Build());
        }

        if (nominatedMessage.Embeds.Count == 1 || nominatedMessage.Attachments.Count == 1)
        {
            var embed = nominatedMessage.Embeds.FirstOrDefault();
            var e = nominatedMessageEmbed;
            if (embed is not null && embed!.Image.HasValue) 
            {
                Console.WriteLine($"found 1 embed");
                e.WithImageUrl(embed.Image.Value.Url);
                embeds.Add(e.Build());
            }
            
            var attachment = nominatedMessage.Attachments.FirstOrDefault();

            if (attachment is not null && attachment.Width > 0 && attachment.Height > 0)
            {
                Console.WriteLine($"found 1 attachment");
                var a = nominatedMessageEmbed;
                a.WithImageUrl(attachment.Url);
                embeds.Add(a.Build());
            }
        }

        if (nominatedMessage.Embeds.Count > 1 || nominatedMessage.Attachments.Count > 1)
        {
            if (nominatedMessage.Embeds is not null)
            {
                Console.WriteLine($"found {nominatedMessage.Embeds.Count} embeds");
                foreach (var embed in nominatedMessage.Embeds)
                {
                    if (embed!.Image.HasValue)
                    {
                        var em = new EmbedBuilder()
                                .WithUrl(nominatedMessageLink)
                                .WithImageUrl(embed.Image.Value.Url)
                                .Build();

                        embeds.Add(em);
                    }
                }
            }

            if(nominatedMessage.Attachments is not null)
            {
                Console.WriteLine($"found {nominatedMessage.Attachments.Count} attachments");
                foreach ( var attachment in nominatedMessage.Attachments)
                {
                    if (attachment.Width > 0 && attachment.Height > 0)
                    {
                        var e = nominatedMessageEmbed;
                        e.WithImageUrl(attachment.Url);
                        embeds.Add(e.Build());
                    }
                }
            }
        }


        // Post to original channel
        Console.WriteLine("Posting message to channel message was nominated in");

        await command.FollowupAsync(
                text: $"**The {Helpers.GetUserNameAdjective()} {command.User.Mention}** has nominated **{command.Data.Message.Author.Mention}'s** message to be added to the best of list",
                components: voteButtons.Build(),
                embeds: embeds.ToArray()
            );

        ulong GeneralChannelId = ulong.Parse(Environment.GetEnvironmentVariable("GeneralChannelId")!);

        // Post to the general channel if the nominated message didn't orginate in the general channel
        var generalChannel = client.GetChannel(GeneralChannelId) as ITextChannel;
        bool sendToGeneralChannel = true; // testing toggle, set to false to stop spamming lobby while testing in bottesting

        if (generalChannel is not null && command.Channel.Id != generalChannel.Id && sendToGeneralChannel)
            {
                var messageLinkButton = voteButtons
                    .WithButton(
                        label: "Take me to the post 📫",
                        style: ButtonStyle.Link,
                        url: nominatedMessageLink,
                        row: 1);
                Console.WriteLine("Posting message to Lobby");

                await generalChannel!.SendMessageAsync(
                    text: Helpers.GeneralChannelGreeting(command.Channel, command.User, command.Data.Message),
                    allowedMentions: AllowedMentions.All,
                    components: messageLinkButton.Build(),
                    embeds: embeds.ToArray());
                return;
            }
    }
}
