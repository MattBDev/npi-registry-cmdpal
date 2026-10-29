using System.Net.Http.Json;
using System.Text.RegularExpressions;
using NpiRegistry.Models;

namespace NpiRegistry;

/// <summary>A parsed search box value.</summary>
internal sealed record NpiQuery(
    string? Number = null,
    string? FirstName = null,
    string? LastName = null,
    string? OrganizationName = null,
    string? State = null)
{
    private static readonly Regex StateSuffix = new(@"\s+in\s+([A-Za-z]{2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Grammar:
    ///   1234567893              -> NPI lookup
    ///   smith | john smith | smith, john  -> individual provider
    ///   org:mayo clinic         -> organization
    ///   ... in WA               -> restrict to a state
    /// </summary>
    public static NpiQuery? Parse(string input, out string? problem)
    {
        problem = null;
        var text = input.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        if (text.Length == 10 && text.All(char.IsAsciiDigit))
        {
            if (!IsValidNpi(text))
            {
                problem = "That NPI fails the check-digit test (Luhn, prefix 80840).";
                return null;
            }

            return new NpiQuery(Number: text);
        }

        string? state = null;
        var m = StateSuffix.Match(text);
        if (m.Success)
        {
            state = m.Groups[1].Value.ToUpperInvariant();
            text = text[..m.Index].TrimEnd();
        }

        if (text.StartsWith("org:", StringComparison.OrdinalIgnoreCase))
        {
            var org = text[4..].Trim();
            if (org.Length < 2)
            {
                problem = "Type at least 2 characters of the organization name.";
                return null;
            }

            return new NpiQuery(OrganizationName: org, State: state);
        }

        string? first = null, last;
        if (text.Contains(','))
        {
            var parts = text.Split(',', 2, StringSplitOptions.TrimEntries);
            last = parts[0];
            first = parts[1].Length > 0 ? parts[1] : null;
        }
        else
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1)
            {
                last = tokens[0];
            }
            else
            {
                first = tokens[0];
                last = tokens[^1];
            }
        }

        if (last.Length < 2 || (first is { Length: 1 }))
        {
            problem = "Names need at least 2 characters.";
            return null;
        }

        return new NpiQuery(FirstName: first, LastName: last, State: state);
    }

    /// <summary>NPI check digit: Luhn over "80840" + the first nine digits.</summary>
    public static bool IsValidNpi(string npi)
    {
        var digits = "80840" + npi;
        var sum = 0;
        var alt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var d = digits[i] - '0';
            if (alt)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
            alt = !alt;
        }

        return sum % 10 == 0;
    }
}

internal sealed class NpiClient
{
    private const string BaseUrl = "https://npiregistry.cms.hhs.gov/api/";
    public const int PageSize = 25;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<NpiResponse> SearchAsync(NpiQuery query, CancellationToken ct)
    {
        var args = new List<string> { "version=2.1", $"limit={PageSize}" };

        void Add(string key, string? value, bool wildcard = false)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                args.Add($"{key}={Uri.EscapeDataString(value.Trim() + (wildcard ? "*" : ""))}");
            }
        }

        Add("number", query.Number);
        Add("first_name", query.FirstName, wildcard: true);
        Add("last_name", query.LastName, wildcard: true);
        Add("organization_name", query.OrganizationName, wildcard: true);
        Add("state", query.State);

        var url = BaseUrl + "?" + string.Join('&', args);
        var response = await Http.GetFromJsonAsync(url, NpiJsonContext.Default.NpiResponse, ct);
        return response ?? new NpiResponse();
    }
}
