using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DialogueDown.Visualization.Live;

/// <summary>
/// Opens targets with the operating system's default handler — <c>open</c> on
/// macOS, the shell on Windows, and <c>xdg-open</c> on Linux.
/// </summary>
internal sealed class BrowserLauncher : IBrowserLauncher
{
    public void Open(string target)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", target);
        }
        else
        {
            Process.Start("xdg-open", target);
        }
    }
}
