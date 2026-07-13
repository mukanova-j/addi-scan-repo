namespace AddiScan.Web.Services;

/// <summary>Holds the current circuit's JWT in memory. Lost on browser refresh — fine for a test client.</summary>
public class AuthState
{
    public string? Token { get; private set; }
    public string? Email { get; private set; }

    public bool IsAuthenticated => Token is not null;

    public event Action? Changed;

    public void SignIn(string email, string token)
    {
        Email = email;
        Token = token;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Email = null;
        Token = null;
        Changed?.Invoke();
    }
}
