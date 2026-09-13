using System;
using System.Collections.Generic;
using System.Linq;
using TheRichScript_WinUI.Models;

namespace TheRichScript_WinUI.Services;

public static class CountryCurrencyData
{
    private static readonly Lazy<List<CountryInfo>> _countries = new(InitializeCountries);
    private static readonly Lazy<List<CurrencyInfo>> _currencies = new(InitializeCurrencies);
    private static readonly Lazy<Dictionary<string, string>> _currencySymbols = new(() =>
        _currencies.Value.ToDictionary(c => c.Code, c => c.Symbol, StringComparer.OrdinalIgnoreCase));
    private static readonly Lazy<Dictionary<string, CountryInfo>> _countryByCode = new(() =>
        _countries.Value.ToDictionary(c => c.Code, c => c, StringComparer.OrdinalIgnoreCase));
    private static readonly Lazy<Dictionary<string, CurrencyInfo>> _currencyByCode = new(() =>
        _currencies.Value.ToDictionary(c => c.Code, c => c, StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<CountryInfo> AllCountries => _countries.Value;
    public static IReadOnlyList<CurrencyInfo> AllCurrencies => _currencies.Value;

    public static CountryInfo? FindCountry(string? codeOrName)
    {
        if (string.IsNullOrWhiteSpace(codeOrName)) return null;
        var trimmed = codeOrName.Trim();

        if (_countryByCode.Value.TryGetValue(trimmed, out var country))
        {
            return country;
        }

        return _countries.Value.FirstOrDefault(c =>
            string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Code, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static CurrencyInfo? FindCurrency(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var trimmed = code.Trim();

        if (_currencyByCode.Value.TryGetValue(trimmed, out var currency))
        {
            return currency;
        }

        return _currencies.Value.FirstOrDefault(c =>
            string.Equals(c.Code, trimmed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetCurrencySymbol(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return "₹";
        var code = currencyCode.Trim().ToUpperInvariant();

        if (_currencySymbols.Value.TryGetValue(code, out var symbol) && !string.IsNullOrWhiteSpace(symbol))
        {
            return symbol;
        }

        return code + " ";
    }

    private static List<CountryInfo> InitializeCountries()
    {
        var rawList = new (string Code, string Name, string Currency)[]
        {
            ("AF", "Afghanistan", "AFN"),
            ("AL", "Albania", "ALL"),
            ("DZ", "Algeria", "DZD"),
            ("AD", "Andorra", "EUR"),
            ("AO", "Angola", "AOA"),
            ("AG", "Antigua and Barbuda", "XCD"),
            ("AR", "Argentina", "ARS"),
            ("AM", "Armenia", "AMD"),
            ("AU", "Australia", "AUD"),
            ("AT", "Austria", "EUR"),
            ("AZ", "Azerbaijan", "AZN"),
            ("BS", "Bahamas", "BSD"),
            ("BH", "Bahrain", "BHD"),
            ("BD", "Bangladesh", "BDT"),
            ("BB", "Barbados", "BBD"),
            ("BY", "Belarus", "BYN"),
            ("BE", "Belgium", "EUR"),
            ("BZ", "Belize", "BZD"),
            ("BJ", "Benin", "XOF"),
            ("BT", "Bhutan", "BTN"),
            ("BO", "Bolivia", "BOB"),
            ("BA", "Bosnia and Herzegovina", "BAM"),
            ("BW", "Botswana", "BWP"),
            ("BR", "Brazil", "BRL"),
            ("BN", "Brunei", "BND"),
            ("BG", "Bulgaria", "BGN"),
            ("BF", "Burkina Faso", "XOF"),
            ("BI", "Burundi", "BIF"),
            ("CV", "Cabo Verde", "CVE"),
            ("KH", "Cambodia", "KHR"),
            ("CM", "Cameroon", "XAF"),
            ("CA", "Canada", "CAD"),
            ("CF", "Central African Republic", "XAF"),
            ("TD", "Chad", "XAF"),
            ("CL", "Chile", "CLP"),
            ("CN", "China", "CNY"),
            ("CO", "Colombia", "COP"),
            ("KM", "Comoros", "KMF"),
            ("CG", "Congo", "XAF"),
            ("CD", "Congo (DRC)", "CDF"),
            ("CR", "Costa Rica", "CRC"),
            ("CI", "Côte d'Ivoire", "XOF"),
            ("HR", "Croatia", "EUR"),
            ("CU", "Cuba", "CUP"),
            ("CY", "Cyprus", "EUR"),
            ("CZ", "Czech Republic", "CZK"),
            ("DK", "Denmark", "DKK"),
            ("DJ", "Djibouti", "DJF"),
            ("DM", "Dominica", "XCD"),
            ("DO", "Dominican Republic", "DOP"),
            ("EC", "Ecuador", "USD"),
            ("EG", "Egypt", "EGP"),
            ("SV", "El Salvador", "USD"),
            ("GQ", "Equatorial Guinea", "XAF"),
            ("ER", "Eritrea", "ERN"),
            ("EE", "Estonia", "EUR"),
            ("SZ", "Eswatini", "SZL"),
            ("ET", "Ethiopia", "ETB"),
            ("FJ", "Fiji", "FJD"),
            ("FI", "Finland", "EUR"),
            ("FR", "France", "EUR"),
            ("GA", "Gabon", "XAF"),
            ("GM", "Gambia", "GMD"),
            ("GE", "Georgia", "GEL"),
            ("DE", "Germany", "EUR"),
            ("GH", "Ghana", "GHS"),
            ("GR", "Greece", "EUR"),
            ("GD", "Grenada", "XCD"),
            ("GT", "Guatemala", "GTQ"),
            ("GN", "Guinea", "GNF"),
            ("GW", "Guinea-Bissau", "XOF"),
            ("GY", "Guyana", "GYD"),
            ("HT", "Haiti", "HTG"),
            ("HN", "Honduras", "HNL"),
            ("HK", "Hong Kong", "HKD"),
            ("HU", "Hungary", "HUF"),
            ("IS", "Iceland", "ISK"),
            ("IN", "India", "INR"),
            ("ID", "Indonesia", "IDR"),
            ("IR", "Iran", "IRR"),
            ("IQ", "Iraq", "IQD"),
            ("IE", "Ireland", "EUR"),
            ("IL", "Israel", "ILS"),
            ("IT", "Italy", "EUR"),
            ("JM", "Jamaica", "JMD"),
            ("JP", "Japan", "JPY"),
            ("JO", "Jordan", "JOD"),
            ("KZ", "Kazakhstan", "KZT"),
            ("KE", "Kenya", "KES"),
            ("KI", "Kiribati", "AUD"),
            ("KP", "North Korea", "KPW"),
            ("KR", "South Korea", "KRW"),
            ("KW", "Kuwait", "KWD"),
            ("KG", "Kyrgyzstan", "KGS"),
            ("LA", "Laos", "LAK"),
            ("LV", "Latvia", "EUR"),
            ("LB", "Lebanon", "LBP"),
            ("LS", "Lesotho", "LSL"),
            ("LR", "Liberia", "LRD"),
            ("LY", "Libya", "LYD"),
            ("LI", "Liechtenstein", "CHF"),
            ("LT", "Lithuania", "EUR"),
            ("LU", "Luxembourg", "EUR"),
            ("MO", "Macao", "MOP"),
            ("MG", "Madagascar", "MGA"),
            ("MW", "Malawi", "MWK"),
            ("MY", "Malaysia", "MYR"),
            ("MV", "Maldives", "MVR"),
            ("ML", "Mali", "XOF"),
            ("MT", "Malta", "EUR"),
            ("MH", "Marshall Islands", "USD"),
            ("MR", "Mauritania", "MRU"),
            ("MU", "Mauritius", "MUR"),
            ("MX", "Mexico", "MXN"),
            ("FM", "Micronesia", "USD"),
            ("MD", "Moldova", "MDL"),
            ("MC", "Monaco", "EUR"),
            ("MN", "Mongolia", "MNT"),
            ("ME", "Montenegro", "EUR"),
            ("MA", "Morocco", "MAD"),
            ("MZ", "Mozambique", "MZN"),
            ("MM", "Myanmar", "MMK"),
            ("NA", "Namibia", "NAD"),
            ("NR", "Nauru", "AUD"),
            ("NP", "Nepal", "NPR"),
            ("NL", "Netherlands", "EUR"),
            ("NZ", "New Zealand", "NZD"),
            ("NI", "Nicaragua", "NIO"),
            ("NE", "Niger", "XOF"),
            ("NG", "Nigeria", "NGN"),
            ("MK", "North Macedonia", "MKD"),
            ("NO", "Norway", "NOK"),
            ("OM", "Oman", "OMR"),
            ("PK", "Pakistan", "PKR"),
            ("PW", "Palau", "USD"),
            ("PS", "Palestine", "ILS"),
            ("PA", "Panama", "PAB"),
            ("PG", "Papua New Guinea", "PGK"),
            ("PY", "Paraguay", "PYG"),
            ("PE", "Peru", "PEN"),
            ("PH", "Philippines", "PHP"),
            ("PL", "Poland", "PLN"),
            ("PT", "Portugal", "EUR"),
            ("QA", "Qatar", "QAR"),
            ("RO", "Romania", "RON"),
            ("RU", "Russia", "RUB"),
            ("RW", "Rwanda", "RWF"),
            ("KN", "Saint Kitts and Nevis", "XCD"),
            ("LC", "Saint Lucia", "XCD"),
            ("VC", "Saint Vincent and the Grenadines", "XCD"),
            ("WS", "Samoa", "WST"),
            ("SM", "San Marino", "EUR"),
            ("ST", "Sao Tome and Principe", "STN"),
            ("SA", "Saudi Arabia", "SAR"),
            ("SN", "Senegal", "XOF"),
            ("RS", "Serbia", "RSD"),
            ("SC", "Seychelles", "SCR"),
            ("SL", "Sierra Leone", "SLE"),
            ("SG", "Singapore", "SGD"),
            ("SK", "Slovakia", "EUR"),
            ("SI", "Slovenia", "EUR"),
            ("SB", "Solomon Islands", "SBD"),
            ("SO", "Somalia", "SOS"),
            ("ZA", "South Africa", "ZAR"),
            ("SS", "South Sudan", "SSP"),
            ("ES", "Spain", "EUR"),
            ("LK", "Sri Lanka", "LKR"),
            ("SD", "Sudan", "SDG"),
            ("SR", "Suriname", "SRD"),
            ("SE", "Sweden", "SEK"),
            ("CH", "Switzerland", "CHF"),
            ("SY", "Syria", "SYP"),
            ("TW", "Taiwan", "TWD"),
            ("TJ", "Tajikistan", "TJS"),
            ("TZ", "Tanzania", "TZS"),
            ("TH", "Thailand", "THB"),
            ("TL", "Timor-Leste", "USD"),
            ("TG", "Togo", "XOF"),
            ("TO", "Tonga", "TOP"),
            ("TT", "Trinidad and Tobago", "TTD"),
            ("TN", "Tunisia", "TND"),
            ("TR", "Turkey", "TRY"),
            ("TM", "Turkmenistan", "TMT"),
            ("TV", "Tuvalu", "AUD"),
            ("UG", "Uganda", "UGX"),
            ("UA", "Ukraine", "UAH"),
            ("AE", "United Arab Emirates", "AED"),
            ("GB", "United Kingdom", "GBP"),
            ("US", "United States", "USD"),
            ("UY", "Uruguay", "UYU"),
            ("UZ", "Uzbekistan", "UZS"),
            ("VU", "Vanuatu", "VUV"),
            ("VA", "Vatican City", "EUR"),
            ("VE", "Venezuela", "VES"),
            ("VN", "Vietnam", "VND"),
            ("YE", "Yemen", "YER"),
            ("ZM", "Zambia", "ZMW"),
            ("ZW", "Zimbabwe", "ZWL")
        };

        return rawList
            .Select(item => new CountryInfo
            {
                Code = item.Code,
                Name = item.Name,
                DefaultCurrency = item.Currency,
                FlagEmoji = CountryInfo.GetFlagEmoji(item.Code)
            })
            .OrderBy(c => c.Name)
            .ToList();
    }

    private static List<CurrencyInfo> InitializeCurrencies()
    {
        // Major / common global currencies listed first
        var prioritizedCodes = new[] { "USD", "EUR", "GBP", "INR", "JPY", "CAD", "AUD", "AED", "CHF", "SGD", "CNY", "SAR", "BRL", "ZAR", "NZD" };

        var rawCurrencies = new (string Code, string Name, string Symbol)[]
        {
            ("USD", "US Dollar", "$"),
            ("EUR", "Euro", "€"),
            ("GBP", "British Pound", "£"),
            ("INR", "Indian Rupee", "₹"),
            ("JPY", "Japanese Yen", "¥"),
            ("CAD", "Canadian Dollar", "CA$"),
            ("AUD", "Australian Dollar", "A$"),
            ("AED", "United Arab Emirates Dirham", "AED"),
            ("CHF", "Swiss Franc", "CHF"),
            ("SGD", "Singapore Dollar", "S$"),
            ("CNY", "Chinese Yuan", "¥"),
            ("SAR", "Saudi Riyal", "SAR"),
            ("BRL", "Brazilian Real", "R$"),
            ("ZAR", "South African Rand", "R"),
            ("NZD", "New Zealand Dollar", "NZ$"),

            // All other currencies in alphabetical order
            ("AFN", "Afghan Afghani", "؋"),
            ("ALL", "Albanian Lek", "L"),
            ("AMD", "Armenian Dram", "֏"),
            ("ANG", "Netherlands Antillean Guilder", "ƒ"),
            ("AOA", "Angolan Kwanza", "Kz"),
            ("ARS", "Argentine Peso", "$"),
            ("AWG", "Aruban Florin", "ƒ"),
            ("AZN", "Azerbaijani Manat", "₼"),
            ("BAM", "Bosnia-Herzegovina Mark", "KM"),
            ("BBD", "Barbadian Dollar", "Bds$"),
            ("BDT", "Bangladeshi Taka", "৳"),
            ("BGN", "Bulgarian Lev", "лв"),
            ("BHD", "Bahraini Dinar", "BD"),
            ("BIF", "Burundian Franc", "FBu"),
            ("BND", "Brunei Dollar", "B$"),
            ("BOB", "Bolivian Boliviano", "Bs."),
            ("BSD", "Bahamian Dollar", "B$"),
            ("BTN", "Bhutanese Ngultrum", "Nu."),
            ("BWP", "Botswana Pula", "P"),
            ("BYN", "Belarusian Ruble", "Br"),
            ("BZD", "Belize Dollar", "BZ$"),
            ("CDF", "Congolese Franc", "FC"),
            ("CLP", "Chilean Peso", "$"),
            ("COP", "Colombian Peso", "$"),
            ("CRC", "Costa Rican Colón", "₡"),
            ("CUP", "Cuban Peso", "₱"),
            ("CVE", "Cape Verdean Escudo", "Esc"),
            ("CZK", "Czech Koruna", "Kč"),
            ("DJF", "Djiboutian Franc", "Fdj"),
            ("DKK", "Danish Krone", "kr"),
            ("DOP", "Dominican Peso", "RD$"),
            ("DZD", "Algerian Dinar", "DA"),
            ("EGP", "Egyptian Pound", "E£"),
            ("ERN", "Eritrean Nakfa", "Nfk"),
            ("ETB", "Ethiopian Birr", "Br"),
            ("FJD", "Fijian Dollar", "FJ$"),
            ("GEL", "Georgian Lari", "₾"),
            ("GHS", "Ghanaian Cedi", "GH₵"),
            ("GIP", "Gibraltar Pound", "£"),
            ("GMD", "Gambian Dalasi", "D"),
            ("GNF", "Guinean Franc", "FG"),
            ("GTQ", "Guatemalan Quetzal", "Q"),
            ("GYD", "Guyanese Dollar", "GY$"),
            ("HKD", "Hong Kong Dollar", "HK$"),
            ("HNL", "Honduran Lempira", "L"),
            ("HTG", "Haitian Gourde", "G"),
            ("HUF", "Hungarian Forint", "Ft"),
            ("IDR", "Indonesian Rupiah", "Rp"),
            ("ILS", "Israeli New Shekel", "₪"),
            ("IQD", "Iraqi Dinar", "د.ع"),
            ("IRR", "Iranian Rial", "﷼"),
            ("ISK", "Icelandic Króna", "kr"),
            ("JMD", "Jamaican Dollar", "J$"),
            ("JOD", "Jordanian Dinar", "JD"),
            ("KES", "Kenyan Shilling", "KSh"),
            ("KGS", "Kyrgyzstani Som", "с"),
            ("KHR", "Cambodian Riel", "៛"),
            ("KMF", "Comorian Franc", "CF"),
            ("KPW", "North Korean Won", "₩"),
            ("KRW", "South Korean Won", "₩"),
            ("KWD", "Kuwaiti Dinar", "KD"),
            ("KZT", "Kazakhstani Tenge", "₸"),
            ("LAK", "Lao Kip", "₭"),
            ("LBP", "Lebanese Pound", "L£"),
            ("LKR", "Sri Lankan Rupee", "Rs"),
            ("LRD", "Liberian Dollar", "L$"),
            ("LSL", "Lesotho Loti", "L"),
            ("LYD", "Libyan Dinar", "LD"),
            ("MAD", "Moroccan Dirham", "DH"),
            ("MDL", "Moldovan Leu", "L"),
            ("MGA", "Malagasy Ariary", "Ar"),
            ("MKD", "Macedonian Denar", "ден"),
            ("MMK", "Myanmar Kyat", "K"),
            ("MNT", "Mongolian Tögrög", "₮"),
            ("MOP", "Macanese Pataca", "MOP$"),
            ("MRU", "Mauritanian Ouguiya", "UM"),
            ("MUR", "Mauritian Rupee", "Rs"),
            ("MVR", "Maldivian Rufiyaa", "Rf"),
            ("MWK", "Malawian Kwacha", "MK"),
            ("MXN", "Mexican Peso", "Mex$"),
            ("MYR", "Malaysian Ringgit", "RM"),
            ("MZN", "Mozambican Metical", "MT"),
            ("NAD", "Namibian Dollar", "N$"),
            ("NGN", "Nigerian Naira", "₦"),
            ("NIO", "Nicaraguan Córdoba", "C$"),
            ("NOK", "Norwegian Krone", "kr"),
            ("NPR", "Nepalese Rupee", "Rs"),
            ("OMR", "Omani Rial", "OMR"),
            ("PAB", "Panamanian Balboa", "B/."),
            ("PEN", "Peruvian Sol", "S/"),
            ("PGK", "Papua New Guinean Kina", "K"),
            ("PHP", "Philippine Peso", "₱"),
            ("PKR", "Pakistani Rupee", "₨"),
            ("PLN", "Polish Złoty", "zł"),
            ("PYG", "Paraguayan Guaraní", "₲"),
            ("QAR", "Qatari Riyal", "QR"),
            ("RON", "Romanian Leu", "lei"),
            ("RSD", "Serbian Dinar", "din"),
            ("RUB", "Russian Ruble", "₽"),
            ("RWF", "Rwandan Franc", "RF"),
            ("SBD", "Solomon Islands Dollar", "SI$"),
            ("SCR", "Seychellois Rupee", "SR"),
            ("SDG", "Sudanese Pound", "SDG"),
            ("SEK", "Swedish Krona", "kr"),
            ("SOS", "Somali Shilling", "S"),
            ("SRD", "Surinamese Dollar", "Sr$"),
            ("SSP", "South Sudanese Pound", "SSP"),
            ("STN", "São Tomé and Príncipe Dobra", "Db"),
            ("SYP", "Syrian Pound", "S£"),
            ("SZL", "Swazi Lilangeni", "E"),
            ("THB", "Thai Baht", "฿"),
            ("TJS", "Tajikistani Somoni", "SM"),
            ("TMT", "Turkmenistani Manat", "T"),
            ("TND", "Tunisian Dinar", "DT"),
            ("TOP", "Tongan Paʻanga", "T$"),
            ("TRY", "Turkish Lira", "₺"),
            ("TTD", "Trinidad and Tobago Dollar", "TT$"),
            ("TWD", "New Taiwan Dollar", "NT$"),
            ("TZS", "Tanzanian Shilling", "TSh"),
            ("UAH", "Ukrainian Hryvnia", "₴"),
            ("UGX", "Ugandan Shilling", "USh"),
            ("UYU", "Uruguayan Peso", "$U"),
            ("UZS", "Uzbekistani Som", "so'm"),
            ("VES", "Venezuelan Bolívar", "Bs."),
            ("VND", "Vietnamese Đồng", "₫"),
            ("VUV", "Vanuatu Vatu", "VT"),
            ("WST", "Samoan Tālā", "WS$"),
            ("XAF", "Central African CFA Franc", "FCFA"),
            ("XCD", "East Caribbean Dollar", "EC$"),
            ("XOF", "West African CFA Franc", "CFA"),
            ("YER", "Yemeni Rial", "﷼"),
            ("ZMW", "Zambian Kwacha", "K"),
            ("ZWL", "Zimbabwean Dollar", "Z$")
        };

        var map = rawCurrencies.ToDictionary(c => c.Code, c => new CurrencyInfo { Code = c.Code, Name = c.Name, Symbol = c.Symbol }, StringComparer.OrdinalIgnoreCase);

        var list = new List<CurrencyInfo>();

        // Add prioritized currencies first
        foreach (var pCode in prioritizedCodes)
        {
            if (map.TryGetValue(pCode, out var cInfo))
            {
                list.Add(cInfo);
            }
        }

        // Add the rest sorted alphabetically by code
        foreach (var kvp in map.OrderBy(k => k.Key))
        {
            if (!prioritizedCodes.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(kvp.Value);
            }
        }

        return list;
    }
}
