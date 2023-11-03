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
            .WithUrl(refMessageLink)
            .Build();

            embeds.Add(refrencedMessageEmbed);

            Console.WriteLine($"found {refrencedMessage.Attachments.Count} ref message attachments");

            foreach (var attachment in refrencedMessage.Attachments)
            {
                Console.WriteLine(attachment.Url, attachment.ContentType, attachment.Description);
                if (attachment.Width.HasValue && attachment.Height.HasValue)
                {
                    embeds.Add(
                        new EmbedBuilder()
                            .WithUrl(messageLink)
                            .WithImageUrl(attachment.Url)
                            .Build());
                }
            }

            Console.WriteLine($"found {refrencedMessage.Embeds.Count} ref message embeds");
            foreach (var embed in refrencedMessage.Embeds)
            {
                embeds.Add((Embed)embed);
            }
        }

        // Create nominated message embed
        Console.WriteLine($"Creating main embed for nominated message");
        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(command.Data.Message.Author)
            .WithDescription(command.Data.Message.Content)
            .WithUrl (messageLink)
            .Build();

        embeds.Add(nominatedMessageEmbed);

        Console.WriteLine($"found {message.Attachments.Count}  message attachments");
        foreach (var attachment in message.Attachments)
        {
            Console.WriteLine(attachment.Url, attachment.ContentType, attachment.Description);

            if (attachment.Width.HasValue && attachment.Height.HasValue)
            {
                embeds.Add(
                    new EmbedBuilder()
                        .WithUrl(messageLink)
                        .WithImageUrl(attachment.Url)
                        .Build());
            }
        }

        Console.WriteLine($"found {message.Embeds.Count} message embeds");
        foreach (var embed in message.Embeds)
        {
            embeds.Add((Embed)embed);
        }

        // Post to original channel
        var willCheck = command.User.Id == 136293146647724032 ? "The ACTUAL poo-poo head " : ""; // lol 💩
        Console.WriteLine("Posting message to channel message was nominated in");

        await command.FollowupAsync(
                text: $"**{willCheck}{command.User.Mention}** has nominated **{command.Data.Message.Author.Mention}'s** message to be added to the best of list",
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
