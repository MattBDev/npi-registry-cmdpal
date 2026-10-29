using System.Globalization;
using System.Text;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NpiRegistry.Models;

namespace NpiRegistry.Pages;

internal sealed partial class NpiSearchPage : DynamicListPage
{
    private const int DebounceMs = 350;

    private static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;

    private IListItem[] _items = [Hint("Search the NPI Registry", HelpText)];
    private CancellationTokenSource? _cts;

    private const string HelpText =
        "NPI number, \"last\", \"first last\", \"last, first\", or \"org:name\". Add \"in WA\" to filter by state.";

    public NpiSearchPage()
    {
        Icon = new IconInfo("");
        Title = "NPI Registry";
        Name = "Search";
        PlaceholderText = "NPI, provider name, or org:organization  (add \"in WA\" for a state)";
        ShowDetails = true;
    }

    public override IListItem[] GetItems() => _items;

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _cts?.Cancel();
        _cts?.Dispose();

        var query = NpiQuery.Parse(newSearch, out var problem);
        if (query is null)
        {
            _cts = null;
            IsLoading = false;
            _items = string.IsNullOrWhiteSpace(newSearch)
                ? [Hint("Search the NPI Registry", HelpText)]
                : [Hint(problem ?? "Keep typing…", HelpText)];
            RaiseItemsChanged();
            return;
        }

        var cts = _cts = new CancellationTokenSource();
        IsLoading = true;
        _ = RunSearchAsync(query, cts.Token);
    }

    private async Task RunSearchAsync(NpiQuery query, CancellationToken ct)
    {
        try
        {
            await Task.Delay(DebounceMs, ct);
            var response = await NpiClient.SearchAsync(query, ct);
            ct.ThrowIfCancellationRequested();

            if (response.Errors is { Count: > 0 } errors)
            {
                _items = [Hint("The registry rejected the search", string.Join("; ", errors.Select(e => e.Description)))];
            }
            else if (response.Results is not { Count: > 0 } results)
            {
                _items = [Hint("No matches", "Try fewer words, a different spelling, or remove the state filter.")];
            }
            else
            {
                _items = results.Select(ToItem).ToArray();
            }
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer search
        }
        catch (Exception ex)
        {
            _items = [Hint("Could not reach the NPI Registry", ex.Message)];
        }

        if (!ct.IsCancellationRequested)
        {
            IsLoading = false;
            RaiseItemsChanged();
        }
    }

    private static ListItem Hint(string title, string subtitle) =>
        new(new NoOpCommand()) { Title = title, Subtitle = subtitle, Icon = new IconInfo("") };

    private static ListItem ToItem(NpiResult r)
    {
        var name = DisplayName(r);
        var location = r.Addresses?.FirstOrDefault(a => a.Purpose == "LOCATION") ?? r.Addresses?.FirstOrDefault();
        var primary = r.Taxonomies?.FirstOrDefault(t => t.Primary) ?? r.Taxonomies?.FirstOrDefault();
        var address = FormatAddress(location);
        var cityState = location is null ? null : $"{Cap(location.City)}, {location.State}";

        var more = new List<IContextItem>
        {
            new CommandContextItem(new OpenUrlCommand($"https://npiregistry.cms.hhs.gov/provider-view/{r.Number}") { Name = "Open in NPI Registry" }),
            new CommandContextItem(new CopyTextCommand(name) { Name = "Copy name" }),
        };
        if (address.Length > 0)
        {
            more.Add(new CommandContextItem(new CopyTextCommand(address) { Name = "Copy address" }));
        }

        if (!string.IsNullOrWhiteSpace(location?.Phone))
        {
            more.Add(new CommandContextItem(new CopyTextCommand(location!.Phone!) { Name = "Copy phone number" }));
        }

        var tags = new List<ITag> { new Tag(r.IsOrganization ? "Organization" : "Individual") };
        if (r.Basic.Status is { } status && status != "A")
        {
            tags.Add(new Tag("Inactive"));
        }

        return new ListItem(new CopyTextCommand(r.Number) { Name = "Copy NPI" })
        {
            Title = name,
            Subtitle = string.Join(" · ", new[] { r.Number, primary?.Description, cityState }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Icon = new IconInfo(r.IsOrganization ? "" : ""), // Org: Org, Individual: Contact
            Tags = tags.ToArray(),
            MoreCommands = more.ToArray(),
            Details = new Details { Title = name, Body = BuildMarkdown(r, name) },
        };
    }

    private static string DisplayName(NpiResult r)
    {
        var b = r.Basic;
        if (r.IsOrganization)
        {
            return Cap(b.OrganizationName);
        }

        var parts = new[] { b.NamePrefix, b.FirstName, b.MiddleName, b.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p) && p != "--")
            .Select(Cap);
        var name = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(b.Credential) ? name : $"{name}, {b.Credential!.Trim()}";
    }

    private static string Cap(string? s) =>
        string.IsNullOrWhiteSpace(s) ? "" : Text.ToTitleCase(s.Trim().ToLowerInvariant());

    private static string FormatAddress(NpiAddress? a)
    {
        if (a is null)
        {
            return "";
        }

        var zip = a.PostalCode is { Length: 9 } z ? $"{z[..5]}-{z[5..]}" : a.PostalCode;
        var line1 = string.Join(", ", new[] { Cap(a.Address1), Cap(a.Address2) }.Where(s => s.Length > 0));
        var line2 = $"{Cap(a.City)}, {a.State} {zip}".Trim();
        return $"{line1}, {line2}";
    }

    private static string BuildMarkdown(NpiResult r, string name)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## {name}");
        sb.AppendLine($"**NPI:** {r.Number} ({(r.IsOrganization ? "NPI-2, organization" : "NPI-1, individual")})  ");
        sb.AppendLine($"**Status:** {(r.Basic.Status == "A" ? "Active" : r.Basic.Status ?? "Unknown")}  ");
        if (!string.IsNullOrWhiteSpace(r.Basic.EnumerationDate))
        {
            sb.AppendLine($"**Enumerated:** {r.Basic.EnumerationDate}  ");
        }

        if (!string.IsNullOrWhiteSpace(r.Basic.LastUpdated))
        {
            sb.AppendLine($"**Last updated:** {r.Basic.LastUpdated}  ");
        }

        if (r.IsOrganization && !string.IsNullOrWhiteSpace(r.Basic.AuthorizedOfficialLastName))
        {
            var official = Cap($"{r.Basic.AuthorizedOfficialFirstName} {r.Basic.AuthorizedOfficialLastName}");
            sb.AppendLine($"**Authorized official:** {official} {Cap(r.Basic.AuthorizedOfficialTitle)}  ".TrimEnd() + "  ");
        }

        if (r.Taxonomies is { Count: > 0 })
        {
            sb.AppendLine().AppendLine("### Taxonomies");
            foreach (var t in r.Taxonomies)
            {
                var license = string.IsNullOrWhiteSpace(t.License) ? "" : $" — license {t.License}{(string.IsNullOrWhiteSpace(t.State) ? "" : $" ({t.State})")}";
                sb.AppendLine($"- {(t.Primary ? "**" : "")}{t.Description}{(t.Primary ? "** (primary)" : "")} `{t.Code}`{license}");
            }
        }

        if (r.Addresses is { Count: > 0 })
        {
            foreach (var a in r.Addresses)
            {
                sb.AppendLine().AppendLine($"### {Cap(a.Purpose)} address");
                sb.AppendLine(FormatAddress(a) + "  ");
                if (!string.IsNullOrWhiteSpace(a.Phone))
                {
                    sb.AppendLine($"Phone: {a.Phone}  ");
                }

                if (!string.IsNullOrWhiteSpace(a.Fax))
                {
                    sb.AppendLine($"Fax: {a.Fax}  ");
                }
            }
        }

        return sb.ToString();
    }
}
