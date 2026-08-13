using System.Security.Claims;
using AddiScan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Api.Auth;

/// <summary>
/// Resolves whether functional necessity should count toward an additive's grading total for
/// the caller. Anonymous callers and users who never opted in get the default (include it),
/// matching the behavior grading had before the per-user setting existed.
/// </summary>
public static class GradingPreference
{
    public static async Task<bool> IncludeFunctionalNecessityAsync(
        ClaimsPrincipal user, AddiScanDbContext dbContext, CancellationToken cancellationToken)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
        {
            return true;
        }

        var userId = Guid.Parse(userIdClaim);
        var ignoreFunctionalNecessity = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.IgnoreFunctionalNecessity)
            .FirstOrDefaultAsync(cancellationToken);

        return !ignoreFunctionalNecessity;
    }
}
