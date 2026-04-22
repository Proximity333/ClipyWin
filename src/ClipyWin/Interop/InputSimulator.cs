using System;

namespace ClipyWin.Interop;

public static class InputSimulator
{
    public static void SendCtrlV()
    {
        var fg = NativeMethods.GetForegroundWindow();
        Utilities.Log.Info($"SendCtrlV: foreground=0x{fg.ToInt64():X}");

        var inputs = new[]
        {
            MakeKey(NativeMethods.VK_CONTROL, false),
            MakeKey(NativeMethods.VK_V, false),
            MakeKey(NativeMethods.VK_V, true),
            MakeKey(NativeMethods.VK_CONTROL, true)
        };
        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        Utilities.Log.Info($"SendInput sent={sent}");
    }

    private static NativeMethods.INPUT MakeKey(ushort vk, bool up)
    {
        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.INPUTUNION
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = up ? NativeMethods.KEYEVENTF_KEYUP : 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }
}
