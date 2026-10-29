# NPI Registry — PowerToys Command Palette extension

Searches the public [NPPES NPI Registry API (v2.1)](https://npiregistry.cms.hhs.gov/api-page) from Command Palette. No API key required.

## Search syntax

| Input | Meaning |
|---|---|
| `1700016003` | NPI lookup (check digit validated locally) |
| `smith` | last name starts with "smith" |
| `jo smith` / `smith, jo` | first name starts with "jo", last name starts with "smith" |
| `org:mayo clinic` | organization name starts with "mayo clinic" |
| `... in WA` | restrict to a two-letter state |

Results show primary taxonomy and city/state; the details pane shows all taxonomies, licenses and addresses.
Enter copies the NPI; the context menu (Ctrl+K) opens the registry page or copies the name, address or phone.

## Build & install (Windows 11, PowerToys with Command Palette, Developer Mode on)

1. Open `NpiRegistry.sln` in Visual Studio 2022 (Windows App SDK / .NET desktop workloads).
2. **Build → Deploy NpiRegistry** (building alone doesn't register the package).
3. In Command Palette run **Reload** (the "Reload Command Palette extensions" one), then open **NPI Registry**.

NPI data is public provider-directory information; do not use it for unsolicited marketing where prohibited by the NPPES data-dissemination terms.
