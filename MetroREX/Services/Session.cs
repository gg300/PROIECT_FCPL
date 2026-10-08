using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class Session
{
    public User? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

    public Cart Cart { get; } = new();

    public event EventHandler? Changed;

    public void SignIn(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        Cart.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
