using System;
using System.IO;
using System.Text.Json;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void SaveSettings();
    string GetCurrencySymbol(string currencyCode);
    event Action<string>? ThemeChanged;
    event Action<string>? CurrencyChanged;
}

public class SettingsService : ISettingsService
{
    private static string SettingsFilePath => AppStorageHelper.GetFilePath("appsettings.json");

    public AppSettings Settings { get; private set; } = new();

    public event Action<string>? ThemeChanged;
    public event Action<string>? CurrencyChanged;

    public SettingsService()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var path = SettingsFilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                    Settings.CurrencySymbol = GetCurrencySymbol(Settings.PreferredCurrency);
                    return;
                }
            }
        }
        catch
        {
            // Ignore and use default
        }

        Settings = new AppSettings();
        Settings.CurrencySymbol = GetCurrencySymbol(Settings.PreferredCurrency);
    }

    public void SaveSettings()
    {
        try
        {
            Settings.CurrencySymbol = GetCurrencySymbol(Settings.PreferredCurrency);
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);

            ThemeChanged?.Invoke(Settings.AppTheme);
            CurrencyChanged?.Invoke(Settings.PreferredCurrency);
        }
        catch
        {
            // Ignore
        }
    }

    public string GetCurrencySymbol(string currencyCode)
    {
        return currencyCode.ToUpperInvariant() switch
        {
            "INR" => "₹",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "CAD" => "CA$",
            "AUD" => "A$",
            "AED" => "AED ",
            _ => currencyCode + " "
        };
    }
}
