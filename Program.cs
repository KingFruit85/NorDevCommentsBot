using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using NorDevBestOfBot.Handlers;

public class Program : IDisposable
{
    public static Task Main(string[] args) => new Program().MainAsync();
    private static DiscordSocketClient? DiscordClient { get; set; }
    private static readonly HttpClient HttpClient = new();
    private static SocketGuild? Guild;

    public async Task MainAsync()
    {
        var _config = new DiscordSocketConfig 
        { 
            MessageCacheSize = 100 ,
            GatewayIntents = GatewayIntents.AllUnprivileged ^ GatewayIntents.GuildScheduledEvents ^ GatewayIntents.GuildInvites
        };
        DiscordClient = new DiscordSocketClient(_config);

        DiscordClient.Log += Log;

        var discordToken = Environment.GetEnvironmentVariable("DiscordToken");


        await DiscordClient.LoginAsync(TokenType.Bot, discordToken);
        await DiscordClient.StartAsync();

        DiscordClient.MessageUpdated += MessageUpdated;
        DiscordClient.Ready += Client_Ready;
        DiscordClient.SlashCommandExecuted += SlashCommandHandler;
        DiscordClient.MessageCommandExecuted += MessageCommandHandler;

        DiscordClient.ButtonExecuted += MyButtonHandler;

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message} -> {after}");
    }

    public static async Task Client_Ready()
    {
        ulong guildId = ulong.MinValue;
        try
        {
            guildId = ulong.Parse(Environment.GetEnvironmentVariable("NorDevGuildId")!);
        }
        catch (FormatException)
        {
            Console.WriteLine("The input string is not a valid ulong.");
        }
        catch (OverflowException)
        {
            Console.WriteLine("The input string represents a value that is too large for ulong.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Something unexpected went wrong when attempting to access the guildId: ", ex);
        }

        Guild = DiscordClient!.GetGuild(guildId);

        var nominate = new MessageCommandBuilder()
            .WithName("nominate-message");

        var getRandom = new SlashCommandBuilder()
        .WithName("get-random-comment")
        .WithDescription("Gets a random comment from the database.");

        var getTopFive = new SlashCommandBuilder()
        .WithName("get-top-five-comments")
        .WithDescription("Gets the top five comments of all time from the server.")
        .AddOption("isephemeral", ApplicationCommandOptionType.Boolean,"Keep this post hidden?", isRequired:true);

        var getUsersTopFive = new SlashCommandBuilder()
        .WithName("get-users-top-five-comments")
        .WithDescription("Gets a users top five comments.")
        .AddOption("user", ApplicationCommandOptionType.User, "The user", isRequired: true);

        var getTopTenUsersByVoteCount = new SlashCommandBuilder()
        .WithName("get-top-ten-users-by-vote-count")
        .WithDescription("Gets the top ten users ordered by the sum of their vote counts.");

        var getTopTenUsersByPopstCount = new SlashCommandBuilder()
        .WithName("get-top-ten-users-by-post-count")
        .WithDescription("Gets the top ten users ordered by the sum of their vote counts.");

        try
        {
            Console.WriteLine("Started Commands");
            await Guild.BulkOverwriteApplicationCommandAsync(new ApplicationCommandProperties[]
                {
                    getRandom.Build(),
                    getTopFive.Build(),
                    getUsersTopFive.Build(),
                    getTopTenUsersByVoteCount.Build(),
                    getTopTenUsersByPopstCount.Build(),
                    nominate.Build(),
                });
            Console.WriteLine("Finished Commands");
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        Console.WriteLine($"received {command.Data.Name} slash command");
        switch (command.Data.Name)
        {
            case "get-random-comment":
                await GetRandomComment.HandleGetRandomComment(command, HttpClient);
                break;
            case "get-top-five-comments":
                await GetTopFiveComments.HandleGetTopFiveComments(command, HttpClient);
                break;
            case "get-users-top-five-comments":
                await GetUsersTopFiveComments.HandleGetUsersTopFiveComments(command, HttpClient);
                break;
            case "get-top-ten-users-by-vote-count":
                await GetTopTenUsersByVoteCount.HandleGetTopTenUsersByVoteCount(command, HttpClient);
                break;
            case "get-top-ten-users-by-post-count":
                await GetTopTenUsersByPostCount.HandleGetTopTenUsersByPostCount(command, HttpClient);
                break;
        }
    }

    public static async Task MessageCommandHandler(SocketMessageCommand command)
    {
        await command.DeferAsync();

        Console.WriteLine("Entering MessageCommandHandler");

        try
        {
            Console.WriteLine($"received {command.Data.Name} command from {command.User}");

            if (command.Data.Name == "nominate-message")
            {
                try
                {
                    await NominateMessage.HandleNominateMessageAsync(command, DiscordClient!, HttpClient, Guild);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }
            else
            {
                Console.WriteLine($"Received unknown context command: {command.Data.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred in MessageCommandHandler: {ex.Message}");
        }

        Console.WriteLine("Exiting MessageCommandHandler");
    }


    public static async Task MyButtonHandler(SocketMessageComponent component)
    {
        Console.WriteLine($"Button interaction received, triggered by {component.User}");
        await VoteButtonHandler.Vote(DiscordClient!, component, HttpClient);
        return;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}