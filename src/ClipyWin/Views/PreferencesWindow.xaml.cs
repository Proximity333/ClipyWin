using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClipyWin.Environments;
using ClipyWin.Storage;
using ClipyWin.Utilities;

namespace ClipyWin.Views;

public partial class PreferencesWindow : Window
{
    private static PreferencesWindow? _instance;

    private readonly Settings _settings;
    private bool _loading;

    public PreferencesWindow()
    {
        InitializeComponent();
        _settings = AppEnvironment.Current.Settings;
        Load();
        WireHandlers();
        VersionText.Text = $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
        LoadExcluded();
        LoadHotkeys();
        LoadUpdates();
        LoadLanguages();
        ApplyLocalization();
    }

    private void LoadLanguages()
    {
        LanguageCombo.ItemsSource = Loc.SupportedLanguages.Select(x => new { Code = x.Code, Name = x.Name }).ToArray();
        LanguageCombo.SelectedValue = _settings.GetString(Constants.Settings.Culture, "en");
        LanguageCombo.SelectionChanged += (_, _) =>
        {
            if (_loading) return;
            var code = LanguageCombo.SelectedValue as string;
            if (!string.IsNullOrEmpty(code))
            {
                _settings.Set(Constants.Settings.Culture, code);
                Loc.Set(code);
                ApplyLocalization();
                SnippetsWindow.RefreshLocalization();
            }
        };
    }

    public void ApplyLocalization()
    {
        Title = Loc.T("prefs.title");
        GeneralTab.Header = Loc.T("prefs.tab.general");
        MenuTab.Header = Loc.T("prefs.tab.menu");
        ExcludedTab.Header = Loc.T("prefs.tab.excluded");
        ShortcutsTab.Header = Loc.T("prefs.tab.shortcuts");
        UpdatesTab.Header = Loc.T("prefs.tab.updates");
        LanguageLabel.Content = Loc.T("prefs.language");
        MaxHistoryLabel.Content = Loc.T("prefs.maxHistorySize");
        ReorderCheck.Content = Loc.T("prefs.reorder");
        LoginItemCheck.Content = Loc.T("prefs.loginItem");
        OverwriteCheck.Content = Loc.T("prefs.overwriteSame");
        CopySameCheck.Content = Loc.T("prefs.copySame");
        LanguageRestartHintText.Text = Loc.T("prefs.languageRestartHint");
        ItemsShownInlineLabel.Content = Loc.T("prefs.itemsShownInline");
        ItemsPerSubFolderLabel.Content = Loc.T("prefs.itemsPerSubFolder");
        MaxTitleLengthLabel.Content = Loc.T("prefs.maxTitleLength");
        TooltipMaxLengthLabel.Content = Loc.T("prefs.tooltipMaxLength");
        MarkedCheck.Content = Loc.T("prefs.numberMenuItems");
        StartZeroCheck.Content = Loc.T("prefs.startNumberFromZero");
        ShowTooltipCheck.Content = Loc.T("prefs.showTooltip");
        AddClearCheck.Content = Loc.T("prefs.showClearHistoryItem");
        AlertClearCheck.Content = Loc.T("prefs.confirmBeforeClear");
        ExcludedHelpText.Text = Loc.T("prefs.excludedHelp");
        ExcludeAddBtn.Content = Loc.T("common.add");
        ExcludeRemoveBtn.Content = Loc.T("common.remove");
        ShortcutHelpText.Text = Loc.T("prefs.shortcutHelp");
        ShowHistoryMenuLabel.Content = Loc.T("prefs.showHistoryMenu");
        ShowSnippetsMenuLabel.Content = Loc.T("prefs.showSnippetsMenu");
        UpdateHelpText.Text = Loc.T("prefs.updateHelp");
        UpdateFeedUrlLabel.Content = Loc.T("prefs.updateFeedUrl");
        CheckUpdateBtn.Content = Loc.T("prefs.checkForUpdates");
        ApplyUpdateBtn.Content = Loc.T("prefs.downloadAndRestart");
        CloseBtn.Content = Loc.T("prefs.close");
    }

    private void LoadUpdates()
    {
        UpdateUrlBox.Text = _settings.GetString(Constants.Settings.UpdateFeedUrl, "");
        UpdateUrlBox.LostFocus += (_, _) =>
        {
            if (_loading) return;
            _settings.Set(Constants.Settings.UpdateFeedUrl, UpdateUrlBox.Text.Trim());
        };
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatusText.Text = Loc.T("prefs.updateChecking");
        ApplyUpdateBtn.IsEnabled = false;
        _settings.Set(Constants.Settings.UpdateFeedUrl, UpdateUrlBox.Text.Trim());
        var svc = new Services.UpdateService(UpdateUrlBox.Text.Trim());
        var result = await svc.CheckAsync();
        UpdateStatusText.Text = result.Message;
        ApplyUpdateBtn.IsEnabled = result.NewVersion != null;
        ApplyUpdateBtn.Tag = svc;
    }

    private async void ApplyUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (ApplyUpdateBtn.Tag is not Services.UpdateService svc) return;
        UpdateStatusText.Text = Loc.T("prefs.updateDownloading");
        ApplyUpdateBtn.IsEnabled = false;
        var ok = await svc.DownloadAndApplyAsync();
        UpdateStatusText.Text = ok ? Loc.T("prefs.updateRestarting") : Loc.T("prefs.updateFailed");
    }

    private void LoadHotkeys()
    {
        var main = _settings.GetString(Constants.HotKey.MainKeyCombo, "Ctrl+Shift+V");
        HistoryHotkeyBox.Text = string.IsNullOrEmpty(main) ? Loc.T("hotkey.unassigned") : main;

        var snippets = _settings.GetString(Constants.HotKey.SnippetKeyCombo, "");
        SnippetHotkeyBox.Text = string.IsNullOrEmpty(snippets) ? Loc.T("hotkey.unassigned") : snippets;
    }

    private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        e.Handled = true;

        if (e.Key == Key.Escape || e.Key == Key.Back || e.Key == Key.Delete)
        {
            SetHotkey(box, HotKeyCombo.Empty);
            return;
        }

        var actual = e.Key == Key.System ? e.SystemKey : e.Key;
        if (HotKeyCombo.IsModifier(actual)) return;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None) return;

        SetHotkey(box, new HotKeyCombo(mods, actual));
    }

    private void SetHotkey(TextBox box, HotKeyCombo combo)
    {
        var key = box.Tag as string;
        var settingKey = key == "snippets" ? Constants.HotKey.SnippetKeyCombo : Constants.HotKey.MainKeyCombo;
        _settings.Set(settingKey, combo.ToString());
        box.Text = combo.IsEmpty ? Loc.T("hotkey.unassigned") : combo.ToString();
        App.ApplyHotKeys();
    }

    public static void ShowSingleton()
    {
        if (_instance == null || !_instance.IsLoaded)
        {
            _instance = new PreferencesWindow();
            _instance.Closed += (_, _) => _instance = null;
            _instance.Show();
        }
        else
        {
            if (_instance.WindowState == WindowState.Minimized) _instance.WindowState = WindowState.Normal;
            _instance.Activate();
        }
    }

    private void Load()
    {
        _loading = true;
        try
        {
            MaxHistoryBox.Text  = _settings.GetInt(Constants.Settings.MaxHistorySize, 30).ToString();
            ReorderCheck.IsChecked    = _settings.GetBool(Constants.Settings.ReorderClipsAfterPasting, true);
            LoginItemCheck.IsChecked  = _settings.GetBool(Constants.Settings.LoginItem, false);
            OverwriteCheck.IsChecked  = _settings.GetBool(Constants.Settings.OverwriteSameHistory, true);
            CopySameCheck.IsChecked   = _settings.GetBool(Constants.Settings.CopySameHistory, true);

            InlineBox.Text     = _settings.GetInt(Constants.Settings.NumberOfItemsPlaceInline, 0).ToString();
            InFolderBox.Text   = _settings.GetInt(Constants.Settings.NumberOfItemsPlaceInsideFolder, 10).ToString();
            MaxTitleBox.Text   = _settings.GetInt(Constants.Settings.MaxMenuItemTitleLength, 20).ToString();
            MaxTooltipBox.Text = _settings.GetInt(Constants.Settings.MaxLengthOfToolTip, 200).ToString();

            MarkedCheck.IsChecked      = _settings.GetBool(Constants.Settings.MenuItemsAreMarkedWithNumbers, true);
            StartZeroCheck.IsChecked   = _settings.GetBool(Constants.Settings.MenuItemsTitleStartWithZero, false);
            ShowTooltipCheck.IsChecked = _settings.GetBool(Constants.Settings.ShowToolTipOnMenuItem, true);
            AddClearCheck.IsChecked    = _settings.GetBool(Constants.Settings.AddClearHistoryMenuItem, true);
            AlertClearCheck.IsChecked  = _settings.GetBool(Constants.Settings.ShowAlertBeforeClearHistory, true);
        }
        finally { _loading = false; }
    }

    private void WireHandlers()
    {
        MaxHistoryBox.LostFocus  += (_, _) => TrySetInt(Constants.Settings.MaxHistorySize, MaxHistoryBox.Text, 30, 1);
        InlineBox.LostFocus      += (_, _) => TrySetInt(Constants.Settings.NumberOfItemsPlaceInline, InlineBox.Text, 0, 0);
        InFolderBox.LostFocus    += (_, _) => TrySetInt(Constants.Settings.NumberOfItemsPlaceInsideFolder, InFolderBox.Text, 10, 1);
        MaxTitleBox.LostFocus    += (_, _) => TrySetInt(Constants.Settings.MaxMenuItemTitleLength, MaxTitleBox.Text, 20, 1);
        MaxTooltipBox.LostFocus  += (_, _) => TrySetInt(Constants.Settings.MaxLengthOfToolTip, MaxTooltipBox.Text, 200, 1);

        ReorderCheck.Click       += (_, _) => BoolChanged(Constants.Settings.ReorderClipsAfterPasting, ReorderCheck);
        LoginItemCheck.Click     += (_, _) => { BoolChanged(Constants.Settings.LoginItem, LoginItemCheck); ApplyLoginItem(); };
        OverwriteCheck.Click     += (_, _) => BoolChanged(Constants.Settings.OverwriteSameHistory, OverwriteCheck);
        CopySameCheck.Click      += (_, _) => BoolChanged(Constants.Settings.CopySameHistory, CopySameCheck);
        MarkedCheck.Click        += (_, _) => BoolChanged(Constants.Settings.MenuItemsAreMarkedWithNumbers, MarkedCheck);
        StartZeroCheck.Click     += (_, _) => BoolChanged(Constants.Settings.MenuItemsTitleStartWithZero, StartZeroCheck);
        ShowTooltipCheck.Click   += (_, _) => BoolChanged(Constants.Settings.ShowToolTipOnMenuItem, ShowTooltipCheck);
        AddClearCheck.Click      += (_, _) => BoolChanged(Constants.Settings.AddClearHistoryMenuItem, AddClearCheck);
        AlertClearCheck.Click    += (_, _) => BoolChanged(Constants.Settings.ShowAlertBeforeClearHistory, AlertClearCheck);
    }

    private void BoolChanged(string key, CheckBox cb)
    {
        if (_loading) return;
        _settings.Set(key, cb.IsChecked == true);
    }

    private void TrySetInt(string key, string text, int fallback, int min)
    {
        if (_loading) return;
        if (!int.TryParse(text.Trim(), out var v) || v < min)
        {
            v = Math.Max(min, fallback);
        }
        _settings.Set(key, v);
    }

    private void LoadExcluded()
    {
        ExcludeList.Items.Clear();
        foreach (var name in AppEnvironment.Current.ExcludeAppService.Excluded)
            ExcludeList.Items.Add(name);
    }

    private void ExcludeAdd_Click(object sender, RoutedEventArgs e)
    {
        var text = ExcludeInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        if (text.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            text = text[..^4];
        AppEnvironment.Current.ExcludeAppService.Add(text);
        ExcludeInput.Clear();
        LoadExcluded();
    }

    private void ExcludeRemove_Click(object sender, RoutedEventArgs e)
    {
        if (ExcludeList.SelectedItem is not string s) return;
        AppEnvironment.Current.ExcludeAppService.Remove(s);
        LoadExcluded();
    }

    private void ApplyLoginItem()
    {
        try
        {
            var enabled = LoginItemCheck.IsChecked == true;
            Utilities.LoginItem.Apply(enabled);
        }
        catch (Exception ex)
        {
            Utilities.Log.Error("ApplyLoginItem", ex);
            MessageBox.Show(string.Format(Loc.T("error.loginItem"), ex.Message), "Clipy", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
