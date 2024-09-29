using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services;

namespace NorDevBestOfBot.Commands.MessageCommands;

public class NominateMessage(
    ApiService apiService,
    IOptions<ServerOptions> serverOptions,
    AmazonS3Service amazonS3Service,
    Helpers helpers)
    : InteractionModuleBase<SocketInteractionContext<SocketMessageCommand>>
{
    private const ulong ChrisUserId = 317070992339894273;

    [MessageCommand("nominate-message")]
    public async Task Handle(IMessage nominatedMessage)
    {
        Console.WriteLine(@"Entered HandleNominateMessageAsync");
        await DeferAsync();
        
        var nominatedMessageLink = nominatedMessage.GetJumpUrl().Trim();
        var guildId = Context.Guild!.Id;

        var messageAlreadyPersisted =
            await apiService.CheckIfMessageAlreadyPersistedAsync(nominatedMessageLink, guildId);

        if (messageAlreadyPersisted is not null)
        {
            await FollowupAsync("This message has already been added to the best of list", ephemeral: true);
            return;
        }

        Console.WriteLine(@"Checking if user nominated own message");
        if (Context.Interaction.User.Id == nominatedMessage.Author.Id && Context.Interaction.User.Id != ChrisUserId)
        {
            var interactionUser = Context.Guild!.GetUser(Context.Interaction.User.Id);
            await FollowupAsync(helpers.UserNominatingOwnComment(interactionUser), ephemeral: false);
            return;
        }

        Console.WriteLine($@"got message {nominatedMessage.Id} - {nominatedMessage.Content}");
        var refMessageLink = string.Empty;
        IUserMessage? refrencedMessage = null;

        if (nominatedMessage.Reference is not null)
        {
            var channel = Context.Guild.GetTextChannel(nominatedMessage.Reference.ChannelId);
            refrencedMessage = await channel.GetMessageAsync(nominatedMessage.Reference.MessageId.Value) as IUserMessage;
            
            if (refrencedMessage is not null)
            {
                refMessageLink = refrencedMessage.GetJumpUrl().Trim();
                Console.WriteLine($@"Manually fetched referenced message: {refrencedMessage.Content}");
            }
            else
            {
                Console.WriteLine(@"Manually fetching referenced message failed");
            }
        }

        var voteButtons = new ComponentBuilder()
            // .WithButton(
            //     "👍🏻",
            //     $"vote:true,{nominatedMessageLink}",
            //     ButtonStyle.Success,
            //     row: 0)
            // .WithButton(
            //     "💩",
            //     $"vote:false,{nominatedMessageLink}",
            //     ButtonStyle.Danger,
            //     row: 0)
            .WithButton(
                "❔",
                "info_button",
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
                url: "https://ephemeral-dieffenbachia-1b47c2.netlify.app/?guildId=" + guildId,
                row: 0);

        // Create a list of embeds that we will include with the response
        List<Embed> embeds = [];

        // Check if the message references another message, if it does we'll want to post that first
        if (refrencedMessage is not null)
        {
            Console.WriteLine(@"Creating embeds for referenced message");
            Console.WriteLine(
                $@"The referenced message has {refrencedMessage.Embeds.Count} embeds and {refrencedMessage.Attachments.Count} attachments");

            var referencedMessageEmbed = new EmbedBuilder()
                .WithAuthor(refrencedMessage.Author)
                .WithDescription(refrencedMessage.Content)
                .WithUrl(refMessageLink);

            if (!refrencedMessage.Embeds.Any() || !refrencedMessage.Attachments.Any())
                embeds.Add(referencedMessageEmbed.Build());

            // Singles
            if (refrencedMessage.Embeds.Count == 1)
            {
                var refEmbed = refrencedMessage.Embeds.FirstOrDefault();
                var e = referencedMessageEmbed;

                if (refEmbed!.Image.HasValue)
                {
                    Console.WriteLine(@$"Attempting to upload image embed {refEmbed.Image.Value.Url} to s3");
                    amazonS3Service.UploadImageToS3FromUrlInBackground(refEmbed.Image.Value.Url);

                    e.WithImageUrl(refEmbed.Image.Value.Url);
                    embeds.Add(e.Build());
                }
            }

            if (refrencedMessage.Attachments.Count == 1)
            {
                var refAttach = refrencedMessage.Attachments.FirstOrDefault();
                var e = referencedMessageEmbed;

                if (refAttach!.Width > 0 && refAttach.Height > 0)
                {
                    Console.WriteLine(@$"Attempting to upload image embed {e.Url} to s3");
                    amazonS3Service.UploadImageToS3FromUrlInBackground(refAttach.Url);

                    e.WithImageUrl(refAttach.Url);
                    embeds.Add(e.Build());
                }
            }

            // multiples
            if (refrencedMessage.Embeds.Count > 1)
                foreach (var e in refrencedMessage.Embeds)
                {
                    if (e.Image?.Url is null) continue;
                    Console.WriteLine(@$"Attempting to upload image embed {e.Image.Value.Url} to s3");
                    amazonS3Service.UploadImageToS3FromUrlInBackground(e.Image.Value.Url);
                    var em = new EmbedBuilder()
                        .WithUrl(refMessageLink)
                        .WithImageUrl(e.Url)
                        .Build();

                    embeds.Add(em);
                }

            if (refrencedMessage.Attachments.Count > 1)
                foreach (var a in refrencedMessage.Attachments)
                    if (a.Width > 0 && a.Height > 0)
                    {
                        amazonS3Service.UploadImageToS3FromUrlInBackground(a.Url);
                        var at = new EmbedBuilder()
                            .WithUrl(refMessageLink)
                            .WithImageUrl(a.Url)
                            .Build();

                        embeds.Add(at);
                    }
        }

        // Create nominated message embed
        Console.WriteLine(@"Creating main embed for nominated message");
        Console.WriteLine(
            $@"The message has {nominatedMessage.Embeds.Count} embeds and {nominatedMessage.Attachments.Count} attachments");
        var nominatedMessageEmbed = new EmbedBuilder()
            .WithAuthor(nominatedMessage.Author)
            .WithDescription(nominatedMessage.Content)
            .WithUrl(nominatedMessageLink);

        if (!nominatedMessage.Embeds.Any() || !nominatedMessage.Attachments.Any())
        {
            Console.WriteLine(@"found no embeds or attachments");
            embeds.Add(nominatedMessageEmbed.Build());
        }

        if (nominatedMessage.Embeds.Count == 1 || nominatedMessage.Attachments.Count == 1)
        {
            var embed = nominatedMessage.Embeds.FirstOrDefault();
            if (embed is not null && embed.Image.HasValue)
            {
                Console.WriteLine(@"found 1 embed");
                Console.WriteLine(@$"Attempting to upload image embed {embed.Url} to s3");
                amazonS3Service.UploadImageToS3FromUrlInBackground(embed.Image.Value.Url);
                nominatedMessageEmbed.WithImageUrl(embed.Image.Value.Url);
                embeds.Add(nominatedMessageEmbed.Build());
            }

            var attachment = nominatedMessage.Attachments.FirstOrDefault();

            if (attachment is not null && attachment.Width > 0 && attachment.Height > 0)
            {
                Console.WriteLine(@"found 1 attachment");
                Console.WriteLine(@$"Attempting to upload image attachment {attachment.Url} to s3");
                amazonS3Service.UploadImageToS3FromUrlInBackground(attachment.Url);

                var a = nominatedMessageEmbed;
                a.WithImageUrl(attachment.Url);
                embeds.Add(a.Build());
            }
        }

        if (nominatedMessage.Embeds.Count > 1 || nominatedMessage.Attachments.Count > 1)
        {
            if (nominatedMessage.Embeds is not null)
            {
                Console.WriteLine($@"found {nominatedMessage.Embeds.Count} embeds");
                foreach (var embed in nominatedMessage.Embeds)
                    if (embed!.Image.HasValue)
                    {
                        var em = new EmbedBuilder()
                            .WithUrl(nominatedMessageLink)
                            .WithImageUrl(embed.Image.Value.Url)
                            .Build();

                        embeds.Add(em);
                    }
            }

            if (nominatedMessage.Attachments is not null)
            {
                Console.WriteLine($@"found {nominatedMessage.Attachments.Count} attachments");
                foreach (var attachment in nominatedMessage.Attachments)
                    if (attachment.Width > 0 && attachment.Height > 0)
                    {
                        Console.WriteLine(@$"Attempting to upload image attachment {attachment.Url} to s3");
                        amazonS3Service.UploadImageToS3FromUrlInBackground(attachment.Url);
                        var e = nominatedMessageEmbed;
                        e.WithImageUrl(attachment.Url);
                        embeds.Add(e.Build());
                    }
            }
        }


        // Post to original channel
        Console.WriteLine(@"Posting message to channel message was nominated in");

        await FollowupAsync(
            $"**The {Helpers.GetUserNameAdjective()} {Context.Interaction.User.Mention}** has nominated **{nominatedMessage.Author.Mention}'s** message to be added to the best of list",
            components: voteButtons.Build(),
            embeds: embeds.ToArray()
        );

        var generalChannelId = serverOptions.Value.ChannelId;

        // Post to the general channel if the nominated message didn't originate in the general channel
        var generalChannel = Context.Client.GetChannel(generalChannelId) as ITextChannel;

        if (generalChannel is not null && Context.Channel.Id != generalChannel.Id &&
            Context.Interaction.User.Id != ChrisUserId)
        {
            var messageLinkButton = new ComponentBuilder()
                .WithButton(
                    "Take me to the post 📫",
                    style: ButtonStyle.Link,
                    url: nominatedMessageLink,
                    row: 0);
                
            Console.WriteLine(@"Posting message to Lobby");

            await generalChannel.SendMessageAsync(
                Helpers.GeneralChannelGreeting(Context.Interaction.Channel, Context.Interaction.User,
                    nominatedMessage),
                allowedMentions: AllowedMentions.All,
                components: messageLinkButton.Build(),
                embeds: embeds.ToArray());
        }
    }
}