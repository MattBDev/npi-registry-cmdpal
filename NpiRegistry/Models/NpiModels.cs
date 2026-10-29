using System.Text.Json.Serialization;

namespace NpiRegistry.Models;

public sealed class NpiResponse
{
    [JsonPropertyName("result_count")] public int ResultCount { get; set; }
    [JsonPropertyName("results")] public List<NpiResult>? Results { get; set; }
    [JsonPropertyName("Errors")] public List<NpiError>? Errors { get; set; }
}

public sealed class NpiError
{
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("field")] public string? Field { get; set; }
}

public sealed class NpiResult
{
    [JsonPropertyName("number")] public string Number { get; set; } = "";
    [JsonPropertyName("enumeration_type")] public string EnumerationType { get; set; } = "";
    [JsonPropertyName("basic")] public NpiBasic Basic { get; set; } = new();
    [JsonPropertyName("addresses")] public List<NpiAddress>? Addresses { get; set; }
    [JsonPropertyName("taxonomies")] public List<NpiTaxonomy>? Taxonomies { get; set; }

    public bool IsOrganization => EnumerationType == "NPI-2";
}

public sealed class NpiBasic
{
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("middle_name")] public string? MiddleName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("name_prefix")] public string? NamePrefix { get; set; }
    [JsonPropertyName("credential")] public string? Credential { get; set; }
    [JsonPropertyName("organization_name")] public string? OrganizationName { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("sex")] public string? Sex { get; set; }
    [JsonPropertyName("enumeration_date")] public string? EnumerationDate { get; set; }
    [JsonPropertyName("last_updated")] public string? LastUpdated { get; set; }
    [JsonPropertyName("authorized_official_first_name")] public string? AuthorizedOfficialFirstName { get; set; }
    [JsonPropertyName("authorized_official_last_name")] public string? AuthorizedOfficialLastName { get; set; }
    [JsonPropertyName("authorized_official_title_or_position")] public string? AuthorizedOfficialTitle { get; set; }
}

public sealed class NpiAddress
{
    [JsonPropertyName("address_purpose")] public string? Purpose { get; set; }
    [JsonPropertyName("address_1")] public string? Address1 { get; set; }
    [JsonPropertyName("address_2")] public string? Address2 { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("postal_code")] public string? PostalCode { get; set; }
    [JsonPropertyName("telephone_number")] public string? Phone { get; set; }
    [JsonPropertyName("fax_number")] public string? Fax { get; set; }
}

public sealed class NpiTaxonomy
{
    [JsonPropertyName("code")] public string? Code { get; set; }
    [JsonPropertyName("desc")] public string? Description { get; set; }
    [JsonPropertyName("primary")] public bool Primary { get; set; }
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("license")] public string? License { get; set; }
}

[JsonSerializable(typeof(NpiResponse))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class NpiJsonContext : JsonSerializerContext
{
}
