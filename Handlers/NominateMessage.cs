using Discord;
using Discord.WebSocket;
using NorDevBestOfBot.Models;

namespace NorDevBestOfBot.Handlers;

internal class NominateMessage
{
    public static async Task HandleNominateMessageAsync(SocketMessageCommand command, DiscordSocketClient client, HttpClient httpClient, SocketGuild? guild)
    {
        Console.WriteLine("Entered HandleNominateMessageAsync");

        Console.WriteLine("Checking if nominated message was created by a bot");
        if (command.Data.Message.Author.IsBot)
        {
            await command.FollowupAsync(text: $"Sorry, you can't nominate a bot message", ephemeral: true);
            return;
        }

        Console.WriteLine("Checking if user nominated own message");
        if (command.User.Id == command.Data.Message.Author.Id)
        {
            var interactionUser = guild!.GetUser(command.User.Id);
            await command.FollowupAsync(text: Helpers.UserNominatingOwnComment(interactionUser), ephemeral: false);
            return;
        }

        var server = client.GetGuild(command.GuildId!.Value);
        var channel = server.GetTextChannel(command.ChannelId!.Value);
        var message = await channel.GetMessageAsync(command.Data.Message.Id);

        string messageLink = string.Empty;
        IUserMessage? refrencedMessage = null;

        if (message is IUserMessage userMessage)
        {
            Console.WriteLine("Message is IUserMessage");
            if (userMessage.ReferencedMessage is not null)
            {
                refrencedMessage = userMessage.ReferencedMessage;
            }
        }
        messageLink = message.GetJumpUrl().Trim();

        Comment? MessageAlreadyPersisted = await Helpers.CheckIfMessageAlreadyPersistedAsync(messageLink, httpClient);

        if (MessageAlreadyPersisted is not null)
        {
            await command.FollowupAsync(text: $"This message has already been added to the best of list", ephemeral: true);
            return;
        }

        string authorName;
        if (command.Data.Message.Author is IGuildUser user)
        {
            Console.WriteLine("Author is IGuildUser, attempting to get the nickname");
            authorName = user.DisplayName;
        }
        else
        {
            Console.WriteLine("Author is NOT IGuildUser, just using the username");
            authorName = command.Data.Message.Author.Username;
        }

        Console.WriteLine($"Creating main embed for nominated message");
        var embed = new EmbedBuilder()
            .WithUrl(messageLink)
            .WithAuthor(name: authorName, iconUrl: command.Data.Message.Author.GetAvatarUrl())
            .WithTimestamp(command.Data.Message.Timestamp)
            .WithDescription(description: command.Data.Message.Content)
            .WithColor(color: new Color(76, 175, 80))
            .Build();


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

        // Post to original channel
        var willCheck = command.User.Id == 136293146647724032 ? "The ACTUAL poo-poo head " : "";
        Console.WriteLine("Posting message to channel message was nominated in");

        List<Embed> embeds = new ();

        // Create nominated message embed

        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(command.Data.Message.Author)
            .WithDescription(command.Data.Message.Content)
            .Build();

        embeds.Add(nominatedMessageEmbed);

        if (refrencedMessage is not null)
        {
            var refrencedMessageEmbed = new EmbedBuilder()
            .WithAuthor(refrencedMessage.Author)
            .WithDescription(refrencedMessage.Content)
            .Build();

            embeds.Add(refrencedMessageEmbed);
        }

        await command.FollowupAsync(
                text: $"**{willCheck}{command.User.Mention}** has nominated **{command.Data.Message.Author.Mention}'s** message to be added to the best of list",
                components: voteButtons.Build(),
                embeds: embeds.ToArray()
            );


        ulong GeneralChannelId = ulong.Parse(Environment.GetEnvironmentVariable("GeneralChannelId")!);

        // Post to the general channel if the nominated message didn't orginate in the general channel
        var generalChannel = client.GetChannel(GeneralChannelId) as ITextChannel;
        bool sendToGeneralChannel = false;


        if (generalChannel is not null && command.Channel.Id != generalChannel.Id && sendToGeneralChannel)
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
