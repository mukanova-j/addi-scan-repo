using System.Net;
using System.Net.Http.Headers;

namespace AddiScan.Web.Services;

public record RegisterRequest(string Email, string Password, bool ConsentGiven);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token);

public record ApiResult(bool Success, string? Token, string? Error);

public record ScanUploadResponse(bool Accepted, string? Message, string? DetectedFormat, string? ExtractedText);

public record AdditiveSummary(int Id, string? ENumber, string Name, bool Graded, decimal? FinalScore, string? RiskBand);

public record CriterionScore(int? Score, string? Note);

public record SafetyGradingDetail(
    bool Graded,
    decimal? FinalScore,
    int? RawPoints,
    string? RiskBand,
    CriterionScore Carcinogenicity,
    CriterionScore BanStatus,
    CriterionScore AllergicReactions,
    CriterionScore CumulativeRisk,
    CriterionScore Origin,
    CriterionScore ChildrenAndVulnerable,
    CriterionScore FunctionalNecessity);

public record AdditiveDetail(
    int Id,
    string? ENumber,
    string Name,
    string NameVerbatim,
    string? RegulatoryNoteFromSource,
    string? Purpose,
    string? FoundIn,
    string? SourceOrigin,
    string? HealthConcerns,
    string? SideEffects,
    string? BannedAnywhere,
    IReadOnlyList<string> CommonNamesAndSynonyms,
    IReadOnlyList<string> EvidenceSources,
    DateOnly? LastResearched,
    SafetyGradingDetail? Grading);

public class AddiScanApiClient(HttpClient httpClient)
{
    public async Task<List<AdditiveSummary>> GetAdditivesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<AdditiveSummary>>("api/additives") ?? [];
    }

    public async Task<AdditiveDetail?> GetAdditiveAsync(int id)
    {
        var response = await httpClient.GetAsync($"api/additives/{id}");
        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AdditiveDetail>();
    }

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

    public async Task<ScanUploadResponse> UploadScanImageAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync("api/scan/upload", content, cancellationToken);

        if (response.StatusCode is HttpStatusCode.TooManyRequests)
        {
            return new ScanUploadResponse(false, "Too many upload attempts. Please wait a minute and try again.", null, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ScanUploadResponse(false, string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error, null, null);
        }

        var body = await response.Content.ReadFromJsonAsync<ScanUploadResponse>(cancellationToken: cancellationToken);
        return body ?? new ScanUploadResponse(false, "Unexpected empty response from server.", null, null);
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
