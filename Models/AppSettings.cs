namespace TheRichScript_WinUI.Models;

public class AppSettings
{
    public const string DefaultProductionApiUrl = "https://therichscript-backend.onrender.com/api";
    public const string LocalApiUrl = "http://localhost:5000/api";

    public string ApiBaseUrl { get; set; } = DefaultProductionApiUrl;
    public string AppTheme { get; set; } = "Default"; // Default, Light, Dark
    public string PreferredCurrency { get; set; } = "INR"; // INR, USD, EUR, GBP, JPY, CAD, AUD
    public string CurrencySymbol { get; set; } = "₹";
}
