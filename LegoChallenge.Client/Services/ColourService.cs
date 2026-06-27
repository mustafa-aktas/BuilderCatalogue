using LegoChallenge.Client.Models;

namespace LegoChallenge.Client.Services;

public class ColorService(LegoApiService api)
{
    private Dictionary<int, string> _names = [];
    private bool _initialized;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        var colours = await api.GetColoursAsync();
        _names = colours.ToDictionary(c => c.Code, c => c.Name);
        _initialized = true;
    }

    public string GetName(int code) =>
        _names.TryGetValue(code, out var name) ? name : $"#{code}";

    public string GetHint(int code) => code switch
    {
        1  => "#ffffff", 11 => "#1b1b1b", 5  => "#d40000", 7  => "#006cb7",
        6  => "#00852b", 3  => "#ffd700", 4  => "#fe8a18", 2  => "#dab571",
        8  => "#6b3f1b", 9  => "#9b9b9b", 10 => "#595959", 34 => "#a3ce40",
        _  => "#cccccc"
    };
}
