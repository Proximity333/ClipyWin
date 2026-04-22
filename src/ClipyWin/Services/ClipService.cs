using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using ClipyWin.Interop;
using ClipyWin.Models;
using ClipyWin.Storage;
using ClipyWin.Utilities;

namespace ClipyWin.Services;

public sealed class ClipService : IDisposable
{
    private readonly ClipyDb _db;
    private readonly Settings _settings;
    private readonly ExcludeAppService _excludeAppService;
    private readonly object _lock = new();
    private ClipboardListener? _listener;

    public ClipService(ClipyDb db, Settings settings, ExcludeAppService excludeAppService)
    {
        _db = db;
        _settings = settings;
        _excludeAppService = excludeAppService;
    }

    public void StartMonitoring()
    {
        if (_listener != null) return;
        _listener = new ClipboardListener();
        _listener.ClipboardChanged += OnClipboardChanged;
    }

    private void OnClipboardChanged(object? sender, EventArgs e)
    {
        // Marshal on UI thread - WPF Clipboard requires STA
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                CaptureCurrentClipboard();
            }
            catch
            {
                // Clipboard access can fail briefly when another process holds it. Ignore.
            }
        });
    }

    private void CaptureCurrentClipboard()
    {
        lock (_lock)
        {
            if (_excludeAppService.IsForegroundExcluded()) return;

            var data = ReadClipboard();
            if (data == null || data.Types.Count == 0) return;
            if (data.IsOnlyStringType && string.IsNullOrEmpty(data.StringValue)) return;

            Save(data);
        }
    }

    private static ClipData? ReadClipboard()
    {
        try
        {
            var result = new ClipData();

            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                foreach (string? f in files)
                    if (!string.IsNullOrEmpty(f)) result.FileNames.Add(f);
                if (result.FileNames.Count > 0) result.Types.Add(ClipDataType.FileDrop);
            }

            if (Clipboard.ContainsImage())
            {
                var src = Clipboard.GetImage();
                if (src != null)
                {
                    using var ms = new MemoryStream();
                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(src));
                    encoder.Save(ms);
                    result.ImagePng = ms.ToArray();
                    result.Types.Add(ClipDataType.Image);
                }
            }

            if (Clipboard.ContainsText(TextDataFormat.Rtf))
            {
                result.RtfValue = Clipboard.GetText(TextDataFormat.Rtf);
                if (!string.IsNullOrEmpty(result.RtfValue)) result.Types.Add(ClipDataType.Rtf);
            }

            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                result.HtmlValue = Clipboard.GetText(TextDataFormat.Html);
                if (!string.IsNullOrEmpty(result.HtmlValue)) result.Types.Add(ClipDataType.Html);
            }

            if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                result.StringValue = Clipboard.GetText(TextDataFormat.UnicodeText) ?? string.Empty;
                if (!string.IsNullOrEmpty(result.StringValue)) result.Types.Add(ClipDataType.Text);
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    private void Save(ClipData data)
    {
        var hash = data.ComputeHash();
        var copySame = _settings.GetBool(Constants.Settings.CopySameHistory, true);
        var overwriteSame = _settings.GetBool(Constants.Settings.OverwriteSameHistory, true);

        var existing = _db.FindByHash(hash);
        if (existing != null && !copySame) return;

        var id = overwriteSame ? hash : Guid.NewGuid().ToString("N");

        var savedPath = Path.Combine(AppPaths.ClipsFolder, $"{Guid.NewGuid():N}.json");
        data.WriteToFile(savedPath);

        // Delete old data file if overwriting
        if (existing != null && !string.IsNullOrEmpty(existing.DataPath) && File.Exists(existing.DataPath))
        {
            try { File.Delete(existing.DataPath); } catch { }
        }

        var entry = new ClipEntry
        {
            DataHash = id,
            DataPath = savedPath,
            Title = TruncateTitle(data.StringValue),
            PrimaryType = data.PrimaryType?.ToString() ?? "",
            IsColorCode = IsHexColor(data.StringValue),
            UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _db.UpsertClip(entry);
        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string TruncateTitle(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        const int max = 10000;
        return s.Length > max ? s[..max] : s;
    }

    private static readonly System.Text.RegularExpressions.Regex _hexColorRegex =
        new(@"^#?(?:[0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static bool IsHexColor(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return _hexColorRegex.IsMatch(s.Trim());
    }

    public void DeleteClip(ClipEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.DataPath) && File.Exists(entry.DataPath))
        {
            try { File.Delete(entry.DataPath); } catch { }
        }
        _db.DeleteClip(entry.DataHash);
        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearAll()
    {
        foreach (var c in _db.GetClips())
        {
            if (!string.IsNullOrEmpty(c.DataPath) && File.Exists(c.DataPath))
            {
                try { File.Delete(c.DataPath); } catch { }
            }
        }
        _db.ClearClips();
        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ClipsChanged;

    public void Dispose()
    {
        if (_listener != null)
        {
            _listener.ClipboardChanged -= OnClipboardChanged;
            _listener.Dispose();
            _listener = null;
        }
    }
}
