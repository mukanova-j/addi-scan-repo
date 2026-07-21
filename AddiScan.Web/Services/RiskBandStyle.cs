namespace AddiScan.Web.Services;

// Shared Bootstrap badge styling for a risk band, so the additive list, additive
// detail page, and scan pages all render the same colors for the same band.
public static class RiskBandStyle
{
    public static string CssClass(string? riskBand) => riskBand switch
    {
        "Safe" => "bg-success",
        "LowRisk" => "bg-info text-dark",
        "Moderate" => "bg-warning text-dark",
        "HighRisk" => "bg-danger",
        "Avoid" => "bg-dark",
        _ => "bg-secondary",
    };

    public static string Label(string? riskBand) => riskBand switch
    {
        "LowRisk" => "Low risk",
        "HighRisk" => "High risk",
        null => "-",
        _ => riskBand,
    };
}
