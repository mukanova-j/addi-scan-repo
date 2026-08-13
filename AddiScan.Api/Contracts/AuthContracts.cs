namespace AddiScan.Api.Contracts;

public record RegisterRequest(string Email, string Password, bool ConsentGiven);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token);

/// <summary>A registered user's grading preferences.</summary>
public record UserSettingsResponse(bool IgnoreFunctionalNecessity);

public record UpdateUserSettingsRequest(bool IgnoreFunctionalNecessity);
