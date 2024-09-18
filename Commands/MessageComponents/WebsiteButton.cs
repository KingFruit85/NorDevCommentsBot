using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace NorDevBestOfBot.Commands.MessageComponents;

public class WebsiteButton : InteractionModuleBase<SocketInteractionContext<SocketMessageComponent>>
{
    [ComponentInteraction("website_button")]
    public async Task Handle()
    {
        var builder = new ComponentBuilder()
            .WithButton("🌐", style: ButtonStyle.Link, url: "https://ephemeral-dieffenbachia-1b47c2.netlify.app/");

        await RespondAsync("Click the button below to visit the website:", components: builder.Build(), ephemeral: true);
    }
}