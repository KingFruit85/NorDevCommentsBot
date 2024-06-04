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

    public async Task<Comment?> GetRandomComment()
    {
        return await GetFromJsonAsync<Comment?>("messages/random");
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