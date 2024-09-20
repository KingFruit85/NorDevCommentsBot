using Amazon.S3;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NorDevBestOfBot;
using NorDevBestOfBot.BatchJobs;
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
            .Configure<ApiOptions>(builderContext.Configuration.GetSection("ApiOptions"))
            .Configure<AmazonS3Options>(builderContext.Configuration.GetSection("AmazonS3Options"))
            .Configure<CloudinaryOptions>(builderContext.Configuration.GetSection("CloudinaryOptions"))
            .AddDefaultAWSOptions(builderContext.Configuration.GetAWSOptions())
            .AddAWSService<IAmazonS3>();

        services
            .AddSingleton(new HttpClient())
            .AddSingleton(new DiscordSocketClient(config)) // Add the discord client to services
            .AddSingleton<DiscordLogger>()
            .AddSingleton(sp =>
                new InteractionService(sp.GetRequiredService<DiscordSocketClient>(),
                    interactionServiceConfig))
            .AddSingleton<ApiService>()
            .AddSingleton<AmazonS3Service>()
            .AddSingleton<CloudinaryService>()
            .AddHostedService<InteractionHandlingService>()
            .AddSingleton<BackgroundScheduler>()
            .AddSingleton<PostTopMonthCommentsScheduledTask>()
            .AddSingleton<BulkImageUpload>()
            .AddSingleton<Helpers>()
            .AddHostedService<Startup>();
    })
    .Build();

await host.RunAsync();