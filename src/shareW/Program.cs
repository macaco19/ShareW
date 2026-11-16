// SPDX-License-Identifier: GPL-3.0-or-later
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ShareX.AvaloniaUI.Integration;
using ShareX.AvaloniaUI.Theming;
using ShareX.ImageEditor.Integration;

namespace ShareW;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("shareW requiere Windows.");

        // No updater, network client, browser integration or clipboard watcher.
        AvaloniaBootstrapper.Initialize(args, () =>
        {
            // Shared control colors live in the ShareXDark theme dictionary.
            // Activate it before creating any host controls or editor windows.
            ThemeManager.Configure(new ApplicationThemeOptions());
            ImageEditorIntegration.Initialize();
            EditorServices.Clipboard = new LocalClipboardService();
            var window = new MainWindow();
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = window;
            window.Show();
            return Task.CompletedTask;
        }, () => { });

        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            // Do not suppress an unknown failure or submit diagnostic data anywhere.
            System.Diagnostics.Debug.WriteLine(e.Exception);
        };
        AvaloniaBootstrapper.Run();
    }
}
