using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public class UserCacheService(LegoApiService api)
{
    private readonly Dictionary<string, User> _cache = [];

    public async Task<User> GetUserAsync(string userId, bool forceRefresh = false)
    {
        if (!forceRefresh && _cache.TryGetValue(userId, out var cached))
            return cached;

        var user = await api.GetUserByIdAsync(userId);
        _cache[userId] = user;
        return user;
    }

    public void Invalidate(string userId) => _cache.Remove(userId);
}
