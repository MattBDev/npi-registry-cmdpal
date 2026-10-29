using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace NpiRegistry;

public class Program
{
    [MTAThread]
    public static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            global::WinRT.ComWrappersSupport.InitializeComWrappers();
            await using var server = new ComServer();

            var extensionDisposedEvent = new ManualResetEvent(false);
            var extensionInstance = new NpiRegistry(extensionDisposedEvent);

            server.RegisterClass<NpiRegistry, IExtension>(() => extensionInstance);
            server.Start();

            extensionDisposedEvent.WaitOne();
            server.Stop();
            server.UnsafeDispose();
        }
        else
        {
            Console.WriteLine("Not being launched as an extension... exiting.");
        }
    }
}
