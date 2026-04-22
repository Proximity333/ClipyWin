using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClipyWin.Utilities;

namespace ClipyWin.Storage;

public class Settings
{
    private readonly object _lock = new();
    private Dictionary<string, JsonElement> _values = new();

    public Settings()
    {
        Load();
        RegisterDefaults();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile)) return;
            var json = File.ReadAllText(AppPaths.SettingsFile);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (parsed != null) _values = parsed;
        }
        catch
        {
            _values = new Dictionary<string, JsonElement>();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(_values, opts));
        }
    }

    private void RegisterDefaults()
    {
        SetDefault(Constants.Settings.MaxHistorySize, 30);
        SetDefault(Constants.Settings.ShowStatusItem, 1);
        SetDefault(Constants.Settings.InputPasteCommand, true);
        SetDefault(Constants.Settings.ReorderClipsAfterPasting, true);
        SetDefault(Constants.Settings.MenuIconSize, 16);
        SetDefault(Constants.Settings.MaxMenuItemTitleLength, 20);
        SetDefault(Constants.Settings.NumberOfItemsPlaceInline, 0);
        SetDefault(Constants.Settings.NumberOfItemsPlaceInsideFolder, 10);
        SetDefault(Constants.Settings.MenuItemsTitleStartWithZero, false);
        SetDefault(Constants.Settings.AddClearHistoryMenuItem, true);
        SetDefault(Constants.Settings.ShowAlertBeforeClearHistory, true);
        SetDefault(Constants.Settings.ShowIconInTheMenu, true);
        SetDefault(Constants.Settings.MenuItemsAreMarkedWithNumbers, true);
        SetDefault(Constants.Settings.AddNumericKeyEquivalents, false);
        SetDefault(Constants.Settings.ShowToolTipOnMenuItem, true);
        SetDefault(Constants.Settings.MaxLengthOfToolTip, 200);
        SetDefault(Constants.Settings.OverwriteSameHistory, true);
        SetDefault(Constants.Settings.CopySameHistory, true);
        SetDefault(Constants.Settings.LoginItem, false);
    }

    private void SetDefault<T>(string key, T value)
    {
        if (_values.ContainsKey(key)) return;
        Set(key, value);
    }

    public T? Get<T>(string key, T? fallback = default)
    {
        if (!_values.TryGetValue(key, out var el)) return fallback;
        try
        {
            return el.Deserialize<T>();
        }
        catch
        {
            return fallback;
        }
    }

    public int GetInt(string key, int fallback = 0) => Get<int>(key, fallback);
    public bool GetBool(string key, bool fallback = false) => Get<bool>(key, fallback);
    public string GetString(string key, string fallback = "") => Get<string>(key, fallback) ?? fallback;

    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            var json = JsonSerializer.SerializeToElement(value);
            _values[key] = json;
        }
        Save();
        Changed?.Invoke(this, key);
    }

    public event EventHandler<string>? Changed;
}
