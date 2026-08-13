using System.Security.Claims;
using AddiScan.Api.Contracts;
using AddiScan.Core.Auth;
using AddiScan.Core.Entities;
using AddiScan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AddiScan.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AddiScanDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>
    /// Registers a new user account. The user must provide consent to create an account. 
    /// If the email is already taken, a conflict response is returned.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        // Validate consent
        if (!request.ConsentGiven)
        {
            return BadRequest("Consent is required to create an account.");
        }

        // Check if the email is already taken
        var emailTaken = await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailTaken)
        {
            return Conflict("An account with this email already exists.");
        }

        // Create a new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            ConsentGivenAt = DateTime.UtcNow,
        };

        // Save the user to the database
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AuthResponse(jwtTokenService.IssueToken(user)));
    }

    /// <summary>
    /// Logins an existing user. If the email or password is incorrect, an unauthorized response is returned.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        // Find the user by email
        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(new AuthResponse(jwtTokenService.IssueToken(user)));
    }

    /// <summary>
    /// Returns the caller's grading preferences, such as whether functional necessity is
    /// dropped from the total when grading additives.
    /// </summary>
    [HttpGet("settings")]
    [Authorize]
    public async Task<ActionResult<UserSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);

        return Ok(new UserSettingsResponse(user.IgnoreFunctionalNecessity));
    }

    /// <summary>
    /// Updates the caller's grading preferences.
    /// </summary>
    [HttpPut("settings")]
    [Authorize]
    public async Task<ActionResult<UserSettingsResponse>> UpdateSettings(
        UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await dbContext.Users.SingleAsync(u => u.Id == userId, cancellationToken);

        user.IgnoreFunctionalNecessity = request.IgnoreFunctionalNecessity;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UserSettingsResponse(user.IgnoreFunctionalNecessity));
    }
}
