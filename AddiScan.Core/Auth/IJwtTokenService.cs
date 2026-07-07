using AddiScan.Core.Entities;

namespace AddiScan.Core.Auth;

public interface IJwtTokenService
{
    string IssueToken(User user);
}
