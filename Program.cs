﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NorDevBestOfBot;
using NorDevBestOfBot.Extensions;
using NorDevBestOfBot.Models.Options;
using NorDevBestOfBot.Services;
using NorDevBestOfBot.Services.Scheduling;
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
                             GatewayIntents.GuildMessages
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
            .AddSingleton<BackgroundScheduler>()
            .AddSingleton<PostTopMonthCommentsScheduledTask>()
            .AddHostedService<Startup>();
    })
    .Build();

await host.RunAsync();