using System.Net.Http.Json;
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
    
    public async Task<List<string>?> GetBlacklistedChannels(ulong guildId)
    {
        return await GetFromJsonAsync<List<string>>($"guildconfig/getblacklistedchannels?guildId=" + guildId);
    }
    
    public async Task<bool> SetBlacklistedChannels(ulong guildId, string[] channelIds)
    {
        var url = $"guildconfig/setblacklistedchannels?guildId={guildId}";
        var response = await _httpClient.PostAsJsonAsync(url, channelIds);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
            _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,response.Content.Headers );
        }
        
        return response.IsSuccessStatusCode;
    }
    
    public async Task<BotGuildConfig> GetGuildConfigAsync(ulong guildId)
    {
        var url = $"messages/guildconfig?guildId={guildId}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<BotGuildConfig?>(url);
            if (response is null)
            {
                _logger.LogError("Something went wrong retrieving the guild config for guild: {guildId}", guildId);
                throw new Exception("No guild config found");
            }
            return response;
            
        }
        catch (Exception e)
        {
            _logger.LogError("Error getting guild config for guild {guildId}: {Message}", guildId, e.Message);
            throw;
        }
    }
    
    public async Task<bool> UpsertGuildConfigAsync(BotGuildConfig config)
    {
        // Retrieve the API key from environment variables
        var apiKey = Environment.GetEnvironmentVariable("API_KEY");

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("API key is not set in environment variables");
            return false;
        }

        // Create a new HttpRequestMessage
        var request = new HttpRequestMessage(HttpMethod.Post, "messages/upsertGuildConfig");

        // Add the API key to the request headers
        request.Headers.Add("X-API-Key", apiKey);

        // Set the content of the request
        request.Content = JsonContent.Create(config);
        // Send the request
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
            _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,response.Content.Headers );
        }

        return response.IsSuccessStatusCode;
        
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

        // Create a new HttpRequestMessage
        var request = new HttpRequestMessage(HttpMethod.Post, "messages/upsertmessage");

        // Add the API key to the request headers
        request.Headers.Add("X-API-Key", apiKey);

        // Set the content of the request
        request.Content = JsonContent.Create(comment);

        // Send the request
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("POST request failed with status code: {statusCode}", response.StatusCode);
            _logger.LogError("Request message: {message}, headers: {headers}", response.RequestMessage,response.Content.Headers );
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<Comment?> GetRandomComment(ulong guildId)
    {
        return await GetFromJsonAsync<Comment?>("messages/random?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetAllComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetThisMonthsComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/getthismonthscomments?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetTopTenComments(ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/gettoptencomments?guildId=" + guildId);
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByPostCount(ulong guildId)
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/gettoptenusersbypostcount?guildId=" + guildId);
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByVoteCount(ulong guildId)
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/gettoptenusersbyvotecount?guildId=" + guildId);
    }

    public async Task<List<Comment>?> GetUsersTopFiveComments(IUser user, ulong guildId)
    {
        return await GetFromJsonAsync<List<Comment>?>($"messages/getalluserscomments?user={user}&guildId={guildId}");
    }

    public async Task<Comment?> CheckIfMessageAlreadyPersistedAsync(string messageLink, ulong guildId)
    {
        _logger.LogInformation("Checking if message has already been persisted");
        var response = await GetFromJsonAsync<Comment?>($"messages/GetMessageByMessageLink?id={messageLink}&guildId={guildId}");
        if (response is null) _logger.LogInformation("message not found in database");

        return response ?? null;
    }

    public async Task<bool> AddVoteToMessage(string messageLink, string username, bool isVote, ulong guildId)
    {
        var url =
            $"messages/addvotetomessage?messageLink={Uri.EscapeDataString(messageLink)}&username={Uri.EscapeDataString(username)}&votedYes={isVote}&guildId={guildId}";

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
            _logger.LogError("Failed to add vote to message: {messageLink}, exception message : {ex}", messageLink, ex.Message);

            return false;
        }
    }

    public async Task<bool> SaveComment(StringContent content, ulong guildId)
    {
        try
        {
            var response = await _httpClient.PostAsync("messages/savecomment?guildId=" + guildId, content);
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

    public async Task<bool> SetCrosspostChannel(ulong guildId, string channelId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SetAllowCrosspost(ulong guildId, bool allowCrosspost)
    {
        throw new NotImplementedException();
    }
}