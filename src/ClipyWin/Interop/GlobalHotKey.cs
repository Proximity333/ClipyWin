using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;

namespace ClipyWin.Interop;

public sealed class GlobalHotKey : IDisposable
{
    private readonly HwndSource _source;
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _callbacks = new();
    private int _nextId = 1;

    public GlobalHotKey()
    {
        var parameters = new HwndSourceParameters("ClipyWinHotKey")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3)
        };
        _source = new HwndSource(parameters);
        _hwnd = _source.Handle;
    }

    public int Register(ModifierKeys modifiers, Key key, Action callback)
    {
        var fs = MapModifiers(modifiers) | NativeMethods.MOD_NOREPEAT;
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        var id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, fs, vk))
            throw new InvalidOperationException($"RegisterHotKey failed for {modifiers}+{key}");
        _callbacks[id] = callback;
        return id;
    }

    public void Unregister(int id)
    {
        if (_callbacks.Remove(id))
            NativeMethods.UnregisterHotKey(_hwnd, id);
    }

    private static uint MapModifiers(ModifierKeys m)
    {
        uint fs = 0;
        if ((m & ModifierKeys.Alt) != 0) fs |= NativeMethods.MOD_ALT;
        if ((m & ModifierKeys.Control) != 0) fs |= NativeMethods.MOD_CONTROL;
        if ((m & ModifierKeys.Shift) != 0) fs |= NativeMethods.MOD_SHIFT;
        if ((m & ModifierKeys.Windows) != 0) fs |= NativeMethods.MOD_WIN;
        return fs;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            Utilities.Log.Info($"WM_HOTKEY received: id={id}");
            if (_callbacks.TryGetValue(id, out var cb))
            {
                try { cb(); }
                catch (Exception ex) { Utilities.Log.Error($"Hotkey callback threw (id={id})", ex); }
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var id in _callbacks.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);
        _callbacks.Clear();
        _source.Dispose();
    }
}
