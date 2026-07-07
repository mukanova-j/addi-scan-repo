namespace AddiScan.Web.Services;

/// <summary>Holds the current circuit's JWT in memory. Lost on browser refresh — fine for a test client.</summary>
public class AuthState
{
    public string? Token { get; private set; }
    public string? Email { get; private set; }

    public bool IsAuthenticated => Token is not null;

    public void SignIn(string email, string token)
    {
        Email = email;
        Token = token;
    }

    public void SignOut()
    {
        Email = null;
        Token = null;
    }
}
