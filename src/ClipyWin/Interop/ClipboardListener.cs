using System;
using System.Windows;
using System.Windows.Interop;

namespace ClipyWin.Interop;

public sealed class ClipboardListener : IDisposable
{
    private readonly HwndSource _source;
    private readonly IntPtr _hwnd;
    private bool _registered;

    public event EventHandler? ClipboardChanged;

    public ClipboardListener()
    {
        var parameters = new HwndSourceParameters("ClipyWinClipboardListener")
        {
            HwndSourceHook = WndProc,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };
        _source = new HwndSource(parameters);
        _hwnd = _source.Handle;
        _registered = NativeMethods.AddClipboardFormatListener(_hwnd);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.RemoveClipboardFormatListener(_hwnd);
            _registered = false;
        }
        _source.Dispose();
    }
}
