using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TheRichScript_WinUI.Services;

public interface IFxRateService
{
    Task<double?> GetExchangeRateAsync(string fromCurrency, string toCurrency);
    Task<(double convertedAmount, double rate)> ConvertAmountAsync(double amount, string fromCurrency, string toCurrency, double? customRate = null);
    double GetExchangeRate(string? fromCurrency, string? toCurrency);
    double ConvertAmount(double amount, string? fromCurrency, string? toCurrency);
    Task PreloadRatesAsync(IEnumerable<string> fromCurrencies, string toCurrency);
}

public class FxRateService : IFxRateService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, (double rate, DateTime expiresAt)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    // Static base rates against USD (mirrors TheRichScript-Backend fx.service.js for offline resilience)
    private static readonly Dictionary<string, double> BaseRatesAgainstUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1.0,
        ["EUR"] = 0.92,
        ["INR"] = 83.50,
        ["GBP"] = 0.79,
        ["CAD"] = 1.36,
        ["AUD"] = 1.52,
        ["JPY"] = 155.0,
        ["CHF"] = 0.90,
        ["CNY"] = 7.23,
        ["SGD"] = 1.35,
        ["AED"] = 3.673,
        ["SAR"] = 3.751,
        ["THB"] = 36.5,
        ["PHP"] = 58.0,
        ["MYR"] = 4.70,
        ["NZD"] = 1.63,
        ["HKD"] = 7.82,
        ["KRW"] = 1370.0,
        ["BRL"] = 5.25,
        ["ZAR"] = 18.5,
        ["KWD"] = 0.307,
        ["BHD"] = 0.377,
        ["OMR"] = 0.385,
        ["QAR"] = 3.64
    };

    public FxRateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
    }

    public double GetExchangeRate(string? fromCurrency, string? toCurrency)
    {
        var from = string.IsNullOrWhiteSpace(fromCurrency) ? "INR" : fromCurrency.Trim().ToUpperInvariant();
        var to = string.IsNullOrWhiteSpace(toCurrency) ? "INR" : toCurrency.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        var cacheKey = $"{from}_{to}";
        if (_cache.TryGetValue(cacheKey, out var entry) && entry.expiresAt > DateTime.UtcNow)
        {
            return entry.rate;
        }

        return GetFallbackRate(from, to);
    }

    public double ConvertAmount(double amount, string? fromCurrency, string? toCurrency)
    {
        var from = string.IsNullOrWhiteSpace(fromCurrency) ? "INR" : fromCurrency.Trim().ToUpperInvariant();
        var to = string.IsNullOrWhiteSpace(toCurrency) ? "INR" : toCurrency.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        double rate = GetExchangeRate(from, to);
        return Math.Round(amount * rate, 2);
    }

    public async Task PreloadRatesAsync(IEnumerable<string> fromCurrencies, string toCurrency)
    {
        var to = string.IsNullOrWhiteSpace(toCurrency) ? "INR" : toCurrency.Trim().ToUpperInvariant();
        var tasks = fromCurrencies
            .Where(c => !string.IsNullOrWhiteSpace(c) && !string.Equals(c, to, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(c => GetExchangeRateAsync(c, to));

        await Task.WhenAll(tasks);
    }

    public async Task<double?> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        var from = string.IsNullOrWhiteSpace(fromCurrency) ? "INR" : fromCurrency.Trim().ToUpperInvariant();
        var to = string.IsNullOrWhiteSpace(toCurrency) ? "INR" : toCurrency.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        var cacheKey = $"{from}_{to}";
        if (_cache.TryGetValue(cacheKey, out var entry) && entry.expiresAt > DateTime.UtcNow)
        {
            return entry.rate;
        }

        // Attempt live rate fetch from Frankfurter API (dev endpoint first, app endpoint fallback)
        double? liveRate = await FetchFromFrankfurterAsync($"https://api.frankfurter.dev/v1/latest?base={from}&symbols={to}", to);
        if (!liveRate.HasValue)
        {
            liveRate = await FetchFromFrankfurterAsync($"https://api.frankfurter.app/latest?from={from}&to={to}", to);
        }

        if (liveRate.HasValue && liveRate.Value > 0)
        {
            _cache[cacheKey] = (liveRate.Value, DateTime.UtcNow.Add(CacheDuration));
            return liveRate.Value;
        }

        // Fallback to static offline rates
        var fallbackRate = GetFallbackRate(from, to);
        _cache[cacheKey] = (fallbackRate, DateTime.UtcNow.AddMinutes(15));
        return fallbackRate;
    }

    public async Task<(double convertedAmount, double rate)> ConvertAmountAsync(
        double amount,
        string fromCurrency,
        string toCurrency,
        double? customRate = null)
    {
        var from = string.IsNullOrWhiteSpace(fromCurrency) ? "INR" : fromCurrency.Trim().ToUpperInvariant();
        var to = string.IsNullOrWhiteSpace(toCurrency) ? "INR" : toCurrency.Trim().ToUpperInvariant();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return (amount, 1.0);
        }

        double rate;
        if (customRate.HasValue && customRate.Value > 0)
        {
            rate = customRate.Value;
        }
        else
        {
            var fetched = await GetExchangeRateAsync(from, to);
            rate = fetched ?? GetFallbackRate(from, to);
        }

        var convertedAmount = Math.Round(amount * rate, 4);
        return (convertedAmount, rate);
    }

    private async Task<double?> FetchFromFrankfurterAsync(string url, string targetCurrency)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("rates", out var ratesElement) &&
                ratesElement.TryGetProperty(targetCurrency, out var rateElement))
            {
                if (rateElement.TryGetDouble(out var rateVal))
                {
                    return rateVal;
                }
            }
        }
        catch
        {
            // Network timeout, offline, or transient error
        }

        return null;
    }

    private static double GetFallbackRate(string from, string to)
    {
        if (BaseRatesAgainstUsd.TryGetValue(from, out var fromRate) &&
            BaseRatesAgainstUsd.TryGetValue(to, out var toRate) &&
            fromRate > 0)
        {
            return Math.Round(toRate / fromRate, 6);
        }

        return 1.0;
    }
}
