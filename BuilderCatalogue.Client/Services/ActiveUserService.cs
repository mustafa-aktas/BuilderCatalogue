using BuilderCatalogue.Client.Models;

namespace BuilderCatalogue.Client.Services;

public class ActiveUserService
{
    public UserSummary? CurrentUser { get; private set; }

    public event Action? OnUserChanged;

    public void Login(UserSummary user)
    {
        CurrentUser = user;
        OnUserChanged?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        OnUserChanged?.Invoke();
    }
}
