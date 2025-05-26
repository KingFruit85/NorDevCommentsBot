using System.Net.Http.Json;
using System.Text.Json;
using Discord;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NorDevBestOfBot.Models;
using NorDevBestOfBot.Models.Options;

namespace NorDevBestOfBot.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient httpClient, IOptions<ApiOptions> apiOptions, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(apiOptions.Value.BaseUrl);
        _logger = logger;
    }

    public async Task<bool> AddBotFeedback(BotFeedback feedback)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("BotFeedback/SaveFeedback", feedback);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Bot feedback added successfully");
            return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("Add bot feedback failed: {Message}", e.Message);
        }
        return false;
    }
    
    public async Task KeepDatabaseAwake()
    {
        try
        {
            var response = await _httpClient.GetAsync("messages/keepDbAwake");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Keep Database Awake request sent successfully");
            }
            else
            {
                _logger.LogError("Keep Database Awake failed: {Reason}", response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keep Database Awake failed: {Message}", ex.Message);
        }
    }

    public async Task<bool> SetBlacklistedChannels(List<ulong> channelIds, ulong guildId)
    {
        var url = "messages/setBlacklistedChannels?guildId=" + guildId;

        var response = await _httpClient.PostAsJsonAsync(url, channelIds);
        _logger.LogInformation("Response: {response}", response);

        if (response.IsSuccessStatusCode) return true;
        _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
        _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,
            response.Content.Headers);
        return false;
    }

    public async Task<BotGuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        var url = $"messages/guildconfig?guildId={guildId}";
        try
        {
            var response = await _httpClient.GetFromJsonAsync<BotGuildConfig>(url);
            if (response is not null) return response;
            throw new HttpRequestException($"No guild config found for guild {guildId}", null,
                System.Net.HttpStatusCode.NotFound);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Error getting guild config for guild {GuildId}. Status code: {StatusCode}", guildId,
                e.StatusCode);
            throw;
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Error deserializing guild config for guild {GuildId}", guildId);
            throw;
        }
    }

    public async Task<bool> UpsertMessageAsync(Comment comment)
    {
        // Retrieve the API key from environment variables
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("API key is not set in environment variables");
            return false;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "messages/upsertmessage");

        // Add the API key to the request headers
        request.Headers.Add("X-API-Key", apiKey);

        // Set the content of the request
        request.Content = JsonContent.Create(comment);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode) return response.IsSuccessStatusCode;
        
        _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
        _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,
            response.Content.Headers);

        return response.IsSuccessStatusCode;
    }

    public async Task<Comment?> GetRandomComment(ulong guildId, bool checkIfAlreadyPosted)
    {
        _logger.LogInformation("Fetching random comment for guild {guildId} with checkIfAlreadyPosted={checkIfAlreadyPosted}", guildId, checkIfAlreadyPosted);
        return await GetFromJsonAsync<Comment?>("messages/GetRandomGuildMessage?guildId=" + guildId + "&checkIfAlreadyPosted=" + checkIfAlreadyPosted);
    }
    
    public async Task<RandomAlreadyPosted> AddAlreadyPostedForGuild(ulong messageId, ulong guildId)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("messages/AddAlreadyPostedForGuild?messageId=" + messageId + "&guildId=" + guildId, new { });
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully added message to already posted list for guild {guildId}", guildId);
                return await response.Content.ReadFromJsonAsync<RandomAlreadyPosted>();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return null;
    }

    public async Task<List<Comment>?> GetAllComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetThisMonthsComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/GetTopTenGuildCommentsForCurrentMonth?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetTopTenComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/GetTopTenGuildCommentsByVoteCount?guildId=" + guildId);
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByPostCount(ulong guildId)
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/GetTopTenUsersByPostCount?guildId=" +
                                                                guildId);
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByVoteCount(ulong guildId)
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/GetGuildTopTenUsersByVoteCount?guildId=" +
                                                                guildId);
    }

    public async Task<List<Comment>?> GetUsersTopTenComments(IUser user, ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>($"messages/GetUsersTopTenCommentsByVoteCount?user={user}&guildId={guildId}");
    }

    public async Task<Comment?> CheckIfMessageAlreadyPersistedAsync(string messageLink, ulong guildId)
    {
        _logger.LogInformation("Checking if message has already been persisted");
        var response =
            await GetFromJsonAsync<Comment?>($"messages/GetMessageByMessageLink?messageLink={messageLink}");
        if (response is null) _logger.LogInformation("message not found in database");

        return response ?? null;
    }

    public async Task<bool> AddVoteToMessage(string messageLink, string username, bool isVote, ulong guildId)
    {
        var url =
            $"messages/AddVoteToMessage?messageLink={Uri.EscapeDataString(messageLink)}&username={Uri.EscapeDataString(username)}&votedYes={isVote}&guildId={guildId}";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            if (!response.IsSuccessStatusCode)
                _logger.LogError(
                    "something went wrong Request message: {message}, headers: {headers}", response.RequestMessage,
                    response.Content.Headers);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to add vote to message: {messageLink}, exception message : {ex}", messageLink,
                ex.Message);

            return false;
        }
    }

    public async Task<bool> SaveComment(StringContent content, ulong guildId)
    {
        try
        {
            var response = await _httpClient.PostAsync("Messages/SaveMessage?guildId=" + guildId, content);
            _logger.LogInformation("Response: {response}", response);

            if (response.IsSuccessStatusCode) return response.IsSuccessStatusCode;
            _logger.LogError("POST request failed with status code: {code}", response.StatusCode);
            _logger.LogError(
                "something went wrong Request message: {message}, headers: {headers}", response.RequestMessage,
                response.Content.Headers);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save comment, exception message : {ex}", ex.Message);
            return false;
        }
    }

    private async Task<T?> GetFromJsonAsync<T>(string endpoint)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(endpoint) ?? default;
        }
        catch (Exception)
        {
            _logger.LogError("Failed to get data from endpoint: {endpoint}", endpoint);
            return default;
        }
    }

    public async Task<bool> SetCrosspostChannels(List<ulong> channelIds, ulong guildId)
    {
        var url = $"messages/setCrosspostChannels?guildId={guildId}";

        var response = await _httpClient.PostAsJsonAsync(url, channelIds);

        if (response.IsSuccessStatusCode) return true;
        _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
        _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,
            response.Content.Headers);

        return false;
    }

    public async Task<bool> SetAllowCrosspost(ulong guildId, bool allowCrosspost)
    {
        var url = $"messages/setAllowCrossposts?allow={allowCrosspost}&guildId={guildId}";

        var response = await _httpClient.PostAsJsonAsync(url, allowCrosspost);
        _logger.LogInformation("SetAllowCrosspost response: {response}", response);

        if (response.IsSuccessStatusCode) return true;
        _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
        _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,
            response.Content.Headers);

        return false;
    }
}