using AddiScan.Core.Auth;
using BC = BCrypt.Net.BCrypt;

namespace AddiScan.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BC.EnhancedHashPassword(password);

    public bool Verify(string password, string hash) => BC.EnhancedVerify(password, hash);
}
