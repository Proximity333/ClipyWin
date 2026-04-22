using System;
using System.Collections.Generic;
using System.Windows.Input;
using ClipyWin.Interop;
using ClipyWin.Storage;
using ClipyWin.Utilities;

namespace ClipyWin.Services;

public sealed class HotKeyService : IDisposable
{
    private GlobalHotKey? _hotKey;
    private readonly Dictionary<string, int> _registered = new();

    public void Initialize()
    {
        _hotKey = new GlobalHotKey();
    }

    public void Register(string name, ModifierKeys modifiers, Key key, Action callback)
    {
        if (_hotKey == null) throw new InvalidOperationException("Call Initialize first");
        Unregister(name);
        if (key == Key.None) return;
        try
        {
            var id = _hotKey.Register(modifiers, key, callback);
            _registered[name] = id;
            Log.Info($"Hotkey registered: {name} = {modifiers}+{key} (id={id})");
        }
        catch (InvalidOperationException ex)
        {
            Log.Error($"Hotkey registration FAILED for {name} = {modifiers}+{key}", ex);
        }
    }

    public void Register(string name, HotKeyCombo combo, Action callback)
        => Register(name, combo.Modifiers, combo.Key, callback);

    public void ApplyBindings(Settings settings, Action mainCallback, Action snippetsCallback)
    {
        var mainCombo = HotKeyCombo.Parse(settings.GetString(Constants.HotKey.MainKeyCombo, "Ctrl+Shift+V"));
        if (mainCombo.IsEmpty) mainCombo = new HotKeyCombo(ModifierKeys.Control | ModifierKeys.Shift, Key.V);
        Register("main", mainCombo, mainCallback);

        var snippetCombo = HotKeyCombo.Parse(settings.GetString(Constants.HotKey.SnippetKeyCombo, ""));
        if (!snippetCombo.IsEmpty)
            Register("snippets", snippetCombo, snippetsCallback);
        else
            Unregister("snippets");
    }

    public void Unregister(string name)
    {
        if (_hotKey == null) return;
        if (_registered.Remove(name, out var id))
            _hotKey.Unregister(id);
    }

    public void Dispose()
    {
        _hotKey?.Dispose();
        _hotKey = null;
        _registered.Clear();
    }
}
