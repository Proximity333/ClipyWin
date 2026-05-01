using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using ClipyWin.Utilities;
using ClipyWin.Views;

namespace ClipyWin.Services;

public sealed class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;

    public void Initialize(Action showHistoryMenu, Action showSnippetsMenu)
    {
        Dispose();

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = Constants.Application.Name,
            Visible = true,
            ContextMenuStrip = BuildMenu(showHistoryMenu, showSnippetsMenu)
        };

        _notifyIcon.DoubleClick += (_, _) => showHistoryMenu();
    }

    private static ContextMenuStrip BuildMenu(Action showHistoryMenu, Action showSnippetsMenu)
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add(Loc.T("menu.history"), null, (_, _) => showHistoryMenu());
        menu.Items.Add(Loc.T("menu.snippets"), null, (_, _) => showSnippetsMenu());
        menu.Items.Add(Loc.T("menu.editSnippets"), null, (_, _) => SnippetsWindow.ShowSingleton());
        menu.Items.Add(Loc.T("menu.preferences"), null, (_, _) => PreferencesWindow.ShowSingleton());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Loc.T("menu.quit"), null, (_, _) => System.Windows.Application.Current.Shutdown());

        return menu;
    }

    private static Icon SystemFallbackIcon() => SystemIcons.Application;

    private static Icon LoadIcon()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null) return icon;
            }
        }
        catch { }

        return SystemFallbackIcon();
    }

    public void RefreshLocalization(Action showHistoryMenu, Action showSnippetsMenu)
    {
        if (_notifyIcon == null) return;

        var oldMenu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = BuildMenu(showHistoryMenu, showSnippetsMenu);
        oldMenu?.Dispose();
    }

    public void Dispose()
    {
        if (_notifyIcon == null) return;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }
}
