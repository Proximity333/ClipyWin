using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ClipyWin.Interop;
using ClipyWin.Storage;

namespace ClipyWin.Services;

public sealed class ExcludeAppService
{
    private const string SettingsKey = "excludeApplications";

    private readonly Settings _settings;
    private List<string> _excludedExecutables;

    public ExcludeAppService(Settings settings)
    {
        _settings = settings;
        _excludedExecutables = _settings.Get<List<string>>(SettingsKey) ?? new List<string>();
    }

    public IReadOnlyList<string> Excluded => _excludedExecutables;

    public bool IsForegroundExcluded()
    {
        if (_excludedExecutables.Count == 0) return false;
        try
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return false;
            using var proc = Process.GetProcessById((int)pid);
            var name = proc.ProcessName;
            return _excludedExecutables.Any(e => string.Equals(e, name, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public void Add(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;
        if (_excludedExecutables.Contains(processName, StringComparer.OrdinalIgnoreCase)) return;
        _excludedExecutables.Add(processName);
        Save();
    }

    public void Remove(string processName)
    {
        _excludedExecutables.RemoveAll(e => string.Equals(e, processName, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private void Save() => _settings.Set(SettingsKey, _excludedExecutables);
}
