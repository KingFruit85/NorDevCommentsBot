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
        var message = await channel.GetMessageAsync(command.Data.Message.Id);
        string refMessageLink = string.Empty;
        IUserMessage? refrencedMessage = null;

        // Have to cast here to get the ReferencedMessage
        if (message is IUserMessage userMessage)
        {
            Console.WriteLine("Message is IUserMessage");
            if (userMessage.ReferencedMessage is not null)
            {
                refrencedMessage = userMessage.ReferencedMessage;
                refMessageLink = refrencedMessage.GetJumpUrl().Trim();
            }
        }
        string messageLink = message.GetJumpUrl().Trim();

        Comment? MessageAlreadyPersisted = await Helpers.CheckIfMessageAlreadyPersistedAsync(messageLink, httpClient);

        if (MessageAlreadyPersisted is not null)
        {
            await command.FollowupAsync(text: $"This message has already been added to the best of list", ephemeral: true);
            return;
        }

        var voteButtons = new ComponentBuilder()
            .WithButton(
                label: "I Agree 👍🏻",
                customId: $"yes - {messageLink}",
                style: ButtonStyle.Success,
                row: 0)

            .WithButton(
                label: "I Disagree 💩",
                customId: $"no - {messageLink}",
                style: ButtonStyle.Danger,
                row: 0);

        // Create a list of embeds that we will include with the response
        List<Embed> embeds = new ();
        // Check if the message refrences another message, if it does we'll want to post that first
        if (refrencedMessage is not null)
        {

            var refrencedMessageEmbed = new EmbedBuilder()
            .WithAuthor(refrencedMessage.Author)
            .WithDescription(refrencedMessage.Content)
            .WithUrl(refMessageLink);

            // if ther is just one image then just set the image url of the embed to the image url of the reffed message
            if (refrencedMessage.Embeds != null && refrencedMessage.Embeds.Count == 1)
            {
                var refEmbed = refrencedMessage.Embeds.FirstOrDefault();

                if (refEmbed != null)
                {
                    if (refEmbed.Image.HasValue)
                    {
                        refrencedMessageEmbed.ImageUrl = refEmbed.Url;
                    }
                }
            }

            // multiple image handling 
            if (refrencedMessage.Embeds != null && refrencedMessage.Embeds.Count > 1)
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

            // same with attachments

            if (refrencedMessage.Attachments != null && refrencedMessage.Attachments.Count == 1)
            {
                var refAttach = refrencedMessage.Attachments.FirstOrDefault();

                if (refAttach != null)
                {
                    if (refAttach.Width > 0 && refAttach.Height > 0)
                    {
                        refrencedMessageEmbed.ImageUrl = refAttach.Url;
                    }
                }

                embeds.Add(refrencedMessageEmbed.Build());
            }

            if (refrencedMessage.Attachments != null && refrencedMessage.Attachments.Count > 1)
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

                embeds.Add(refrencedMessageEmbed.Build());

        }

        // Create nominated message embed

        Console.WriteLine($"Creating main embed for nominated message");
        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(command.Data.Message.Author)
            .WithDescription(command.Data.Message.Content)
            .WithUrl(messageLink);

        var embed = command.Data.Message.Embeds.FirstOrDefault();

        if (embed != null)
        {
            if (embed.Image.HasValue)
            {
                nominatedMessageEmbed.ImageUrl = embed.Url;
            }
        }

        var attach = command.Data.Message.Attachments.FirstOrDefault();

        if (attach != null)
        {
            if (attach.Width > 0 && attach.Height > 0)
            {
                nominatedMessageEmbed.ImageUrl = attach.Url;
            }
        }

        embeds.Add(nominatedMessageEmbed.Build());

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
        bool IsChrisNominating = command.User.Id == 317070992339894273;

        if (!IsChrisNominating && generalChannel is not null && command.Channel.Id != generalChannel.Id && sendToGeneralChannel)
            {
                var messageLinkButton = voteButtons
                    .WithButton(
                        label: "Take me to the post 📫",
                        style: ButtonStyle.Link,
                        url: messageLink,
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
