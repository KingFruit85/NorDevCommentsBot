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
            _logger.LogError($"POST request failed with status code: {response.StatusCode}");
            _logger.LogError($"Request message: {response.RequestMessage}, headers: {response.Content.Headers}");
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<Comment?> GetRandomComment()
    {
        return await GetFromJsonAsync<Comment?>("messages/random");
    }

    public async Task<List<Comment>?> GetAllComments()
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/");
    }

    public async Task<List<Comment>?> GetThisMonthsComments()
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/getthismonthscomments");
    }

    public async Task<List<Comment>?> GetTopTenComments()
    {
        return await GetFromJsonAsync<List<Comment>?>("messages/gettoptencomments");
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByPostCount()
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/gettoptenusersbypostcount");
    }

    public async Task<Dictionary<string, int>?> GetTopTenUsersByVoteCount()
    {
        return await GetFromJsonAsync<Dictionary<string, int>?>("messages/gettoptenusersbyvotecount");
    }

    public async Task<List<Comment>?> GetUsersTopFiveComments(IUser user)
    {
        return await GetFromJsonAsync<List<Comment>?>($"messages/getalluserscomments?user={user}");
    }

    public async Task<Comment?> CheckIfMessageAlreadyPersistedAsync(string messageLink)
    {
        Console.WriteLine(@"Checking if message has already been persisted");
        var response = await GetFromJsonAsync<Comment?>($"messages/GetMessageByMessageLink?id={messageLink}");

        if (response != null) return response;

        Console.WriteLine(@"message not found");
        return null;
    }

    public async Task<bool> AddVoteToMessage(string messageLink, string username, bool isVote)
    {
        var url =
            $"messages/addvotetomessage?messageLink={Uri.EscapeDataString(messageLink)}&username={Uri.EscapeDataString(username)}&votedYes={isVote}";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            if (!response.IsSuccessStatusCode)
                _logger.LogError(
                    $"something went wrong Request message: {response.RequestMessage}, headers: {response.Content.Headers}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            return false;
        }
    }

    public async Task<bool> SaveComment(StringContent content)
    {
        try
        {
            var response = await _httpClient.PostAsync("messages/savecomment", content);
            Console.WriteLine($@"Response: {response}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"POST request failed with status code: {response.StatusCode}");
                _logger.LogError(
                    $"something went wrong Request message: {response.RequestMessage}, headers: {response.Content.Headers}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            return false;
        }
    }

    private async Task<T?> GetFromJsonAsync<T>(string endpoint)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return default;
        }
    }
}