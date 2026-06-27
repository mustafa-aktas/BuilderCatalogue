using System.Net.Http.Json;
using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public class LegoApiService(HttpClient http)
{
    public async Task<List<UserSummary>> GetUsersAsync() =>
        (await http.GetFromJsonAsync<UsersResponse>("api/users"))!.Users;

    public async Task<UserSummary> GetUserByUsernameAsync(string username) =>
        (await http.GetFromJsonAsync<UserSummary>($"api/user/by-username/{username}"))!;

    public async Task<User> GetUserByIdAsync(string id) =>
        (await http.GetFromJsonAsync<User>($"api/user/by-id/{id}"))!;

    public async Task<List<SetSummary>> GetSetsAsync() =>
        (await http.GetFromJsonAsync<SetsResponse>("api/sets"))!.Sets;

    public async Task<LegoSet> GetSetByIdAsync(string id) =>
        (await http.GetFromJsonAsync<LegoSet>($"api/set/by-id/{id}"))!;

    public async Task<List<Colour>> GetColoursAsync() =>
        (await http.GetFromJsonAsync<ColoursResponse>("api/colours"))!.Colours;
}
