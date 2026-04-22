using System;
using System.Text;
using System.Windows.Input;

namespace ClipyWin.Utilities;

public readonly struct HotKeyCombo : IEquatable<HotKeyCombo>
{
    public ModifierKeys Modifiers { get; }
    public Key Key { get; }
    public bool IsEmpty => Key == Key.None;

    public HotKeyCombo(ModifierKeys modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public static readonly HotKeyCombo Empty = new(ModifierKeys.None, Key.None);

    public override string ToString()
    {
        if (IsEmpty) return string.Empty;
        var sb = new StringBuilder();
        if ((Modifiers & ModifierKeys.Control) != 0) sb.Append("Ctrl+");
        if ((Modifiers & ModifierKeys.Alt) != 0)     sb.Append("Alt+");
        if ((Modifiers & ModifierKeys.Shift) != 0)   sb.Append("Shift+");
        if ((Modifiers & ModifierKeys.Windows) != 0) sb.Append("Win+");
        sb.Append(KeyToString(Key));
        return sb.ToString();
    }

    public static HotKeyCombo Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Empty;
        var parts = s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var mods = ModifierKeys.None;
        var key = Key.None;
        foreach (var p in parts)
        {
            switch (p.ToLowerInvariant())
            {
                case "ctrl":    case "control": mods |= ModifierKeys.Control; break;
                case "alt":                     mods |= ModifierKeys.Alt; break;
                case "shift":                   mods |= ModifierKeys.Shift; break;
                case "win":     case "windows": mods |= ModifierKeys.Windows; break;
                default:
                    if (Enum.TryParse<Key>(NormalizeKey(p), ignoreCase: true, out var k))
                        key = k;
                    break;
            }
        }
        return new HotKeyCombo(mods, key);
    }

    private static string NormalizeKey(string p)
    {
        if (p.Length == 1 && char.IsLetter(p[0])) return p.ToUpperInvariant();
        if (p.Length == 1 && char.IsDigit(p[0])) return "D" + p;
        return p;
    }

    private static string KeyToString(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9) return ((int)(key - Key.D0)).ToString();
        if (key >= Key.NumPad0 && key <= Key.NumPad9) return "Num" + ((int)(key - Key.NumPad0));
        if (key >= Key.A && key <= Key.Z) return key.ToString();
        return key.ToString();
    }

    public static bool IsModifier(Key k) =>
        k == Key.LeftCtrl || k == Key.RightCtrl ||
        k == Key.LeftAlt || k == Key.RightAlt ||
        k == Key.LeftShift || k == Key.RightShift ||
        k == Key.LWin || k == Key.RWin ||
        k == Key.System;

    public bool Equals(HotKeyCombo other) => Modifiers == other.Modifiers && Key == other.Key;
    public override bool Equals(object? obj) => obj is HotKeyCombo h && Equals(h);
    public override int GetHashCode() => HashCode.Combine((int)Modifiers, (int)Key);
    public static bool operator ==(HotKeyCombo a, HotKeyCombo b) => a.Equals(b);
    public static bool operator !=(HotKeyCombo a, HotKeyCombo b) => !a.Equals(b);
}
