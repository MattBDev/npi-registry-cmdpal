using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NpiRegistry;

public partial class NpiRegistryCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public NpiRegistryCommandsProvider()
    {
        DisplayName = "NPI Registry";
        Icon = new IconInfo(""); // Search
        _commands =
        [
            new CommandItem(new Pages.NpiSearchPage())
            {
                Title = DisplayName,
                Subtitle = "Search the NPPES NPI Registry for providers and organizations",
            },
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;
}
