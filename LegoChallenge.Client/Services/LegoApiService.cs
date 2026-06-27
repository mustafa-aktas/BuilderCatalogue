using System.Net.Http.Json;
using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public class LegoApiService(HttpClient http)
{
    private record UsersResponse(List<UserSummary> Users);
    private record SetsResponse(List<SetSummary> Sets);
    private record ColoursResponse(List<Colour> Colours);

    private List<UserSummary>?             _usersCache;
    private List<SetSummary>?             _setsCache;
    private readonly Dictionary<string, LegoSet> _setCache = [];

    public async Task<List<UserSummary>> GetUsersAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _usersCache is not null) return _usersCache;
        return _usersCache = (await http.GetFromJsonAsync<UsersResponse>("api/users"))!.Users;
    }

    public async Task<User> GetUserByIdAsync(string id) =>
        (await http.GetFromJsonAsync<User>($"api/user/by-id/{id}"))!;

    public async Task<List<SetSummary>> GetSetsAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _setsCache is not null) return _setsCache;
        return _setsCache = (await http.GetFromJsonAsync<SetsResponse>("api/sets"))!.Sets;
    }

    public async Task<LegoSet> GetSetByIdAsync(string id, bool forceRefresh = false)
    {
        if (!forceRefresh && _setCache.TryGetValue(id, out var cached)) return cached;
        var set = (await http.GetFromJsonAsync<LegoSet>($"api/set/by-id/{id}"))!;
        return _setCache[id] = set;
    }

    public async Task<List<Colour>> GetColoursAsync() =>
        (await http.GetFromJsonAsync<ColoursResponse>("api/colours"))!.Colours;
}
