using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace NpiRegistry;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            ComServer server = new();

            ManualResetEvent extensionDisposedEvent = new(false);

            // One extension instance, returned every time the host asks for IExtension.
            NpiRegistry extensionInstance = new(extensionDisposedEvent);
            server.RegisterClass<NpiRegistry, IExtension>(() => extensionInstance);
            server.Start();

            // Block until the extension class signals that it was disposed.
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
