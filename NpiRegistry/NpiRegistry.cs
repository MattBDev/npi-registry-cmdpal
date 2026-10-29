using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace NpiRegistry;

[ComVisible(true)]
[Guid("0f8f0e0f-6f3e-4a7a-944d-23f4741a09a1")]
[ComDefaultInterface(typeof(IExtension))]
public sealed partial class NpiRegistry : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly NpiRegistryCommandsProvider _provider = new();

    public NpiRegistry(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType) => providerType switch
    {
        ProviderType.Commands => _provider,
        _ => null,
    };

    public void Dispose() => _extensionDisposedEvent.Set();
}
