using System.Net;

namespace AddiScan.Web.Services;

public record RegisterRequest(string Email, string Password, bool ConsentGiven);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token);

public record ApiResult(bool Success, string? Token, string? Error);

public class AddiScanApiClient(HttpClient httpClient)
{
    public async Task<ApiResult> RegisterAsync(string email, string password, bool consentGiven)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, password, consentGiven));
        return await ToResultAsync(response);
    }

    public async Task<ApiResult> LoginAsync(string email, string password)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));
        return await ToResultAsync(response);
    }

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.OK)
        {
            var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
            return new ApiResult(true, body?.Token, null);
        }

        var error = await response.Content.ReadAsStringAsync();
        return new ApiResult(false, null, string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
    }
}
