using System;
using System.Threading;
using System.Windows;
using ClipyWin.Environments;
using ClipyWin.Utilities;

namespace ClipyWin;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;
    private bool _ownsMutex;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error("DispatcherUnhandledException", args.Exception);
            MessageBox.Show(args.Exception.ToString(), "ClipyWin unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Error("AppDomain.UnhandledException", args.ExceptionObject as Exception);
            MessageBox.Show(args.ExceptionObject?.ToString() ?? "unknown", "ClipyWin fatal exception", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        Log.Info("=== Startup ===");

        _singleInstanceMutex = new Mutex(initiallyOwned: false, "Global\\ClipyWin.SingleInstance");
        try { _ownsMutex = _singleInstanceMutex.WaitOne(0, false); }
        catch (AbandonedMutexException) { _ownsMutex = true; }

        if (!_ownsMutex)
        {
            MessageBox.Show("ClipyWin is already running.", "ClipyWin", MessageBoxButton.OK, MessageBoxImage.Information);
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        RunStep("AppEnvironment.Initialize", () => AppEnvironment.Initialize());
        RunStep("ClipService.StartMonitoring", () => AppEnvironment.Current.ClipService.StartMonitoring());
        RunStep("DataCleanService.StartMonitoring", () => AppEnvironment.Current.DataCleanService.StartMonitoring());
        RunStep("HotKeyService.Initialize", () => AppEnvironment.Current.HotKeyService.Initialize());
        RunStep("HotKeyService.ApplyBindings", () => ApplyHotKeys());

        Log.Info("=== Startup complete ===");
    }

    public static void ApplyHotKeys()
    {
        AppEnvironment.Current.HotKeyService.ApplyBindings(
            AppEnvironment.Current.Settings,
            () => AppEnvironment.Current.MenuManager.ShowClipMenu(),
            () => AppEnvironment.Current.MenuManager.ShowSnippetsMenu());
    }

    private static void RunStep(string name, Action step)
    {
        try
        {
            Log.Info($"Start: {name}");
            step();
            Log.Info($"OK: {name}");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed: {name}", ex);
            throw;
        }
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        try { AppEnvironment.Current?.Dispose(); }
        catch (Exception ex) { Log.Error("AppEnvironment.Dispose", ex); }

        if (_singleInstanceMutex != null)
        {
            if (_ownsMutex)
            {
                try { _singleInstanceMutex.ReleaseMutex(); }
                catch (Exception ex) { Log.Error("ReleaseMutex", ex); }
            }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }
}
