using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ClipyWin.Utilities;

public static class LoginItem
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = Constants.Application.Name;

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("HKCU Run key not accessible");

        if (enabled)
        {
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exe)) throw new InvalidOperationException("Cannot resolve executable path");
            key.SetValue(ValueName, $"\"{exe}\"", RegistryValueKind.String);
        }
        else
        {
            if (key.GetValue(ValueName) != null)
                key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) != null;
    }
}
