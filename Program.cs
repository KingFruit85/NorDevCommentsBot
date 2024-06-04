using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NorDevBestOfBot;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services;
using Serilog;

using var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((_, loggerConfiguration) => loggerConfiguration
        .MinimumLevel.Verbose()
        .Enrich.FromLogContext()
        .WriteTo.Console())
    .ConfigureAppConfiguration(config => { })
    .ConfigureServices((builderContext, services) =>
    {
        var config = new DiscordSocketConfig
        {
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages,
            MaxWaitBetweenGuildAvailablesBeforeReady = 100000
        };

        var interactionServiceConfig = new InteractionServiceConfig
        {
            LogLevel = LogSeverity.Debug
        };

        // Bind options to models
        services
            .Configure<BotOptions>(builderContext.Configuration.GetSection("BotOptions"))
            .Configure<ServerOptions>(builderContext.Configuration.GetSection("ServerOptions"))
            .Configure<ApiOptions>(builderContext.Configuration.GetSection("ApiOptions"));

        services
            .AddSingleton(new HttpClient())
            .AddSingleton(new DiscordSocketClient(config)) // Add the discord client to services
            .AddSingleton<DiscordLogger>()
            .AddSingleton(sp =>
                new InteractionService(sp.GetRequiredService<DiscordSocketClient>(),
                    interactionServiceConfig))
            .AddSingleton<ApiService>()
            .AddHostedService<InteractionHandlingService>()
            .AddHostedService<Startup>();
        // .AddSingleton<BackgroundScheduler>();
    })
    .Build();

await host.RunAsync();

//     public static async Task MessageCommandHandler(SocketMessageCommand command)
//     {
//         await command.DeferAsync();
//
//         Console.WriteLine("Entering MessageCommandHandler");
//
//         try
//         {
//             Console.WriteLine($"received {command.Data.Name} command from {command.User}");
//
//             if (command.Data.Name == "nominate-message")
//             {
//                 try
//                 {
//                     await NominateMessage.HandleNominateMessageAsync(command, DiscordClient!, HttpClient, Guild);
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine(ex.ToString());
//                     throw;
//                 }
//             }
//             else
//             {
//                 Console.WriteLine($"Received unknown context command: {command.Data.Name}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"An error occurred in MessageCommandHandler: {ex.Message}");
//         }
//
//         Console.WriteLine("Exiting MessageCommandHandler");
//     }
//
//     public static async Task MyButtonHandler(SocketMessageComponent component)
//     {
//         Console.WriteLine($"Button interaction received, triggered by {component.User}");
//         await VoteButtonHandler.Vote(DiscordClient!, component, HttpClient);
//         return;
//     }
//
//     public void Dispose()
//     {
//         throw new NotImplementedException();
//     }
// }