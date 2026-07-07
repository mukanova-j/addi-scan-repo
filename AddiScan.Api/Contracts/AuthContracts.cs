namespace AddiScan.Api.Contracts;

public record RegisterRequest(string Email, string Password, bool ConsentGiven);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token);
