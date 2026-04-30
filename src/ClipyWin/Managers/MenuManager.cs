using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ClipyWin.Interop;
using ClipyWin.Models;
using ClipyWin.Services;
using ClipyWin.Storage;
using ClipyWin.Utilities;
using ClipyWin.Views;

namespace ClipyWin.Managers;

public sealed class MenuManager : IDisposable
{
    private readonly ClipyDb _db;
    private readonly Settings _settings;
    private readonly PasteService _pasteService;

    private ContextMenu? _openMenu;
    private Window? _anchor;
    private IntPtr _previousForeground;
    private readonly List<MenuItem> _clipItemsByOrder = new();

    public MenuManager(ClipyDb db, Settings settings, PasteService pasteService)
    {
        _db = db;
        _settings = settings;
        _pasteService = pasteService;
    }

    public void ShowClipMenu()
    {
        Log.Info("ShowClipMenu invoked");

        if (_openMenu != null && _openMenu.IsOpen)
        {
            _openMenu.IsOpen = false;
            CloseAnchor();
            _openMenu = null;
        }

        _previousForeground = NativeMethods.GetForegroundWindow();
        OpenAnchor();

        var menu = BuildClipMenu();
        menu.Placement = PlacementMode.MousePoint;
        menu.PlacementTarget = _anchor;
        menu.StaysOpen = false;

        if (_settings.GetBool(Constants.Settings.AddNumericKeyEquivalents, false))
        {
            menu.PreviewKeyDown += OnMenuPreviewKeyDown;
        }

        menu.Closed += (_, _) =>
        {
            if (_openMenu == menu) _openMenu = null;
            RestoreForeground();
            CloseAnchor();
        };

        _openMenu = menu;
        menu.IsOpen = true;

        Log.Info($"ShowClipMenu: IsOpen={menu.IsOpen}, items={menu.Items.Count}");
    }

    public void ShowSnippetsMenu()
    {
        Log.Info("ShowSnippetsMenu invoked");

        if (_openMenu != null && _openMenu.IsOpen)
        {
            _openMenu.IsOpen = false;
            CloseAnchor();
            _openMenu = null;
        }

        _previousForeground = NativeMethods.GetForegroundWindow();
        OpenAnchor();

        var menu = BuildSnippetsMenu();
        menu.Placement = PlacementMode.MousePoint;
        menu.PlacementTarget = _anchor;
        menu.StaysOpen = false;
        menu.Closed += (_, _) =>
        {
            if (_openMenu == menu) _openMenu = null;
            RestoreForeground();
            CloseAnchor();
        };

        _openMenu = menu;
        menu.IsOpen = true;
    }

    private void OnMenuPreviewKeyDown(object sender, KeyEventArgs e)
    {
        int idx = DigitIndex(e.Key);
        if (idx < 0) return;
        if (idx >= _clipItemsByOrder.Count) return;
        _clipItemsByOrder[idx].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        e.Handled = true;
    }

    private static int DigitIndex(Key k)
    {
        if (k >= Key.D1 && k <= Key.D9) return k - Key.D1;
        if (k >= Key.NumPad1 && k <= Key.NumPad9) return k - Key.NumPad1;
        if (k == Key.D0 || k == Key.NumPad0) return 9;
        return -1;
    }

    private void OpenAnchor()
    {
        CloseAnchor();
        _anchor = new Window
        {
            Width = 1,
            Height = 1,
            Left = -32000,
            Top = -32000,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = null,
            Opacity = 0,
            ShowInTaskbar = false,
            ShowActivated = true,
            Topmost = true,
            Focusable = false
        };
        _anchor.Show();
        _anchor.Activate();
    }

    private void CloseAnchor()
    {
        if (_anchor != null)
        {
            try { _anchor.Close(); } catch { }
            _anchor = null;
        }
    }

    private void RestoreForeground()
    {
        if (_previousForeground == IntPtr.Zero) return;
        try
        {
            var ours = NativeMethods.GetCurrentThreadId();
            var target = NativeMethods.GetWindowThreadProcessId(_previousForeground, out _);

            bool attached = false;
            if (target != 0 && target != ours)
                attached = NativeMethods.AttachThreadInput(ours, target, true);

            NativeMethods.BringWindowToTop(_previousForeground);
            NativeMethods.SetForegroundWindow(_previousForeground);

            if (attached)
                NativeMethods.AttachThreadInput(ours, target, false);
        }
        catch (Exception ex) { Log.Error("RestoreForeground", ex); }
        _previousForeground = IntPtr.Zero;
    }

    private ContextMenu BuildClipMenu()
    {
        _clipItemsByOrder.Clear();
        var menu = new ContextMenu();

        var maxHistory = _settings.GetInt(Constants.Settings.MaxHistorySize, 30);
        var inline = _settings.GetInt(Constants.Settings.NumberOfItemsPlaceInline, 0);
        var inFolder = _settings.GetInt(Constants.Settings.NumberOfItemsPlaceInsideFolder, 10);
        var maxTitle = _settings.GetInt(Constants.Settings.MaxMenuItemTitleLength, 20);
        var marked = _settings.GetBool(Constants.Settings.MenuItemsAreMarkedWithNumbers, true);
        var startFromZero = _settings.GetBool(Constants.Settings.MenuItemsTitleStartWithZero, false);

        menu.Items.Add(new MenuItem { Header = Loc.T("menu.history"), IsEnabled = false });

        var clips = _db.GetClips(newestFirst: true, take: maxHistory).ToList();

        if (clips.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = Loc.T("menu.noHistory"), IsEnabled = false });
        }
        else
        {
            MenuItem? currentSubmenu = null;
            int subCount = 0;
            int totalAdded = 0;

            for (int i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];

                if (inline > 0 && (i == inline || (i > inline && subCount >= inFolder)))
                {
                    currentSubmenu = new MenuItem { Header = BuildSubmenuTitle(i, clips.Count, inFolder) };
                    menu.Items.Add(currentSubmenu);
                    subCount = 0;
                }

                ItemCollection target = (inline > 0 && i >= inline && currentSubmenu != null)
                    ? currentSubmenu.Items
                    : menu.Items;

                var listNumber = startFromZero ? totalAdded : totalAdded + 1;
                var item = BuildClipMenuItem(clip, listNumber, maxTitle, marked);
                target.Add(item);
                _clipItemsByOrder.Add(item);

                totalAdded++;
                if (target != menu.Items) subCount++;
            }
        }

        // Snippets submenu (if any folders exist)
        var folders = _db.Folders.FindAll().OrderBy(f => f.Index).ToList();
        if (folders.Count > 0)
        {
            menu.Items.Add(new Separator());
            var snippetsRoot = new MenuItem { Header = Loc.T("menu.snippets") };
            foreach (var folder in folders.Where(f => f.Enable))
            {
                var folderItem = new MenuItem { Header = string.IsNullOrWhiteSpace(folder.Title) ? Loc.T("menu.untitled") : folder.Title };
                foreach (var snippet in folder.Snippets.Where(s => s.Enable).OrderBy(s => s.Index))
                {
                    var snippetItem = new MenuItem { Header = string.IsNullOrWhiteSpace(snippet.Title) ? Loc.T("menu.untitled") : snippet.Title };
                    var captured = snippet;
                    snippetItem.Click += (_, _) =>
                    {
                        if (_openMenu != null) _openMenu.IsOpen = false;
                        RestoreForeground();
                        CloseAnchor();
                        _pasteService.PasteString(captured.Content);
                    };
                    folderItem.Items.Add(snippetItem);
                }
                if (folderItem.Items.Count == 0)
                    folderItem.Items.Add(new MenuItem { Header = Loc.T("menu.empty"), IsEnabled = false });
                snippetsRoot.Items.Add(folderItem);
            }
            menu.Items.Add(snippetsRoot);

            var editSnippets = new MenuItem { Header = Loc.T("menu.editSnippets") };
            editSnippets.Click += (_, _) => OpenWindowFromMenu(SnippetsWindow.ShowSingleton);
            menu.Items.Add(editSnippets);
        }
        else
        {
            menu.Items.Add(new Separator());
            var editSnippets = new MenuItem { Header = Loc.T("menu.editSnippets") };
            editSnippets.Click += (_, _) => OpenWindowFromMenu(SnippetsWindow.ShowSingleton);
            menu.Items.Add(editSnippets);
        }

        menu.Items.Add(new Separator());

        if (_settings.GetBool(Constants.Settings.AddClearHistoryMenuItem, true))
        {
            var clear = new MenuItem { Header = Loc.T("menu.clearHistory") };
            clear.Click += (_, _) =>
            {
                if (_settings.GetBool(Constants.Settings.ShowAlertBeforeClearHistory, true))
                {
                    var result = MessageBox.Show(
                        Loc.T("confirm.clearHistoryBody"),
                        Loc.T("confirm.clearHistoryTitle"),
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Question);
                    if (result != MessageBoxResult.OK) return;
                }
                Environments.AppEnvironment.Current.ClipService.ClearAll();
            };
            menu.Items.Add(clear);
        }

        var prefs = new MenuItem { Header = Loc.T("menu.preferences") };
        prefs.Click += (_, _) => OpenWindowFromMenu(PreferencesWindow.ShowSingleton);
        menu.Items.Add(prefs);

        menu.Items.Add(new Separator());

        var quit = new MenuItem { Header = Loc.T("menu.quit") };
        quit.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(quit);

        return menu;
    }

    private ContextMenu BuildSnippetsMenu()
    {
        var menu = new ContextMenu();
        menu.Items.Add(new MenuItem { Header = Loc.T("menu.snippets"), IsEnabled = false });

        var folders = _db.Folders.FindAll().OrderBy(f => f.Index).Where(f => f.Enable).ToList();
        if (folders.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = Loc.T("menu.noSnippets"), IsEnabled = false });
        }
        else
        {
            foreach (var folder in folders)
            {
                var folderItem = new MenuItem { Header = string.IsNullOrWhiteSpace(folder.Title) ? Loc.T("menu.untitled") : folder.Title };
                foreach (var snippet in folder.Snippets.Where(s => s.Enable).OrderBy(s => s.Index))
                {
                    var snippetItem = new MenuItem { Header = string.IsNullOrWhiteSpace(snippet.Title) ? Loc.T("menu.untitled") : snippet.Title };
                    var captured = snippet;
                    snippetItem.Click += (_, _) =>
                    {
                        if (_openMenu != null) _openMenu.IsOpen = false;
                        RestoreForeground();
                        CloseAnchor();
                        _pasteService.PasteString(captured.Content);
                    };
                    folderItem.Items.Add(snippetItem);
                }
                if (folderItem.Items.Count == 0)
                    folderItem.Items.Add(new MenuItem { Header = Loc.T("menu.empty"), IsEnabled = false });
                menu.Items.Add(folderItem);
            }
        }

        menu.Items.Add(new Separator());
        var edit = new MenuItem { Header = Loc.T("menu.editSnippets") };
        edit.Click += (_, _) => OpenWindowFromMenu(SnippetsWindow.ShowSingleton);
        menu.Items.Add(edit);

        return menu;
    }

    private MenuItem BuildClipMenuItem(ClipEntry clip, int listNumber, int maxTitle, bool marked)
    {
        var trimmed = TrimTitle(clip.Title, maxTitle);
        var display = string.IsNullOrEmpty(trimmed) ? DisplayFallback(clip) : trimmed;
        var header = marked ? $"{listNumber}. {display}" : display;

        var item = new MenuItem
        {
            Header = header,
            ToolTip = BuildTooltip(clip)
        };

        if (_settings.GetBool(Constants.Settings.ShowIconInTheMenu, true))
        {
            var icon = BuildIcon(clip);
            if (icon != null) item.Icon = icon;
        }

        item.Click += (_, _) =>
        {
            if (_openMenu != null) _openMenu.IsOpen = false;
            RestoreForeground();
            CloseAnchor();

            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
                _pasteService.PasteClipPlain(clip);
            else
                _pasteService.PasteClip(clip);
        };
        return item;
    }

    private void OpenWindowFromMenu(Action showWindow)
    {
        _previousForeground = IntPtr.Zero;
        if (_openMenu != null) _openMenu.IsOpen = false;
        Application.Current.Dispatcher.BeginInvoke(showWindow, DispatcherPriority.Background);
    }

    private object? BuildIcon(ClipEntry clip)
    {
        var size = Math.Max(12, _settings.GetInt(Constants.Settings.MenuIconSize, 16));

        if (clip.IsColorCode && !string.IsNullOrWhiteSpace(clip.Title))
        {
            if (TryParseColor(clip.Title.Trim(), out var color))
            {
                return new Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    StrokeThickness = 1
                };
            }
        }

        if (clip.PrimaryType == nameof(ClipDataType.Image) && !string.IsNullOrEmpty(clip.DataPath) && File.Exists(clip.DataPath))
        {
            try
            {
                var data = ClipData.ReadFromFile(clip.DataPath);
                if (data?.ImagePng is { Length: > 0 })
                {
                    using var ms = new MemoryStream(data.ImagePng);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = size;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    return new Image { Source = bmp, Width = size, Height = size };
                }
            }
            catch (Exception ex) { Log.Error("BuildIcon image", ex); }
        }

        return null;
    }

    private static bool TryParseColor(string s, out Color color)
    {
        color = default;
        if (string.IsNullOrEmpty(s)) return false;
        if (s.StartsWith('#')) s = s[1..];

        try
        {
            if (s.Length == 3)
            {
                byte r = ParseHex($"{s[0]}{s[0]}");
                byte g = ParseHex($"{s[1]}{s[1]}");
                byte b = ParseHex($"{s[2]}{s[2]}");
                color = Color.FromRgb(r, g, b);
                return true;
            }
            if (s.Length == 6)
            {
                color = Color.FromRgb(ParseHex(s[..2]), ParseHex(s.Substring(2, 2)), ParseHex(s.Substring(4, 2)));
                return true;
            }
            if (s.Length == 8)
            {
                color = Color.FromArgb(ParseHex(s[..2]), ParseHex(s.Substring(2, 2)), ParseHex(s.Substring(4, 2)), ParseHex(s.Substring(6, 2)));
                return true;
            }
        }
        catch { }
        return false;
    }

    private static byte ParseHex(string s) => Convert.ToByte(s, 16);

    private static string DisplayFallback(ClipEntry clip) => clip.PrimaryType switch
    {
        nameof(ClipDataType.Image) => Loc.T("menu.fallback.image"),
        nameof(ClipDataType.FileDrop) => Loc.T("menu.fallback.files"),
        _ => Loc.T("menu.fallback.empty")
    };

    private string BuildTooltip(ClipEntry clip)
    {
        if (!_settings.GetBool(Constants.Settings.ShowToolTipOnMenuItem, true)) return string.Empty;
        var max = _settings.GetInt(Constants.Settings.MaxLengthOfToolTip, 200);
        var s = clip.Title ?? string.Empty;
        return s.Length > max ? s[..max] : s;
    }

    private static string TrimTitle(string? title, int max)
    {
        if (string.IsNullOrEmpty(title)) return string.Empty;
        var line = title.Replace("\r", "").Split('\n', 2)[0].Trim();
        if (line.Length <= max) return line;
        const string ell = "...";
        var cut = Math.Max(0, max - ell.Length);
        return line[..cut] + ell;
    }

    private static string BuildSubmenuTitle(int from, int total, int inFolder)
    {
        var last = Math.Min(from + inFolder, total);
        return $"{from + 1} - {last}";
    }

    public void Dispose()
    {
        if (_openMenu != null)
        {
            _openMenu.IsOpen = false;
            _openMenu = null;
        }
        CloseAnchor();
    }
}
