using System;
using System.Threading;
using System.Windows;
using ClipyWin.Interop;
using ClipyWin.Models;
using ClipyWin.Storage;

namespace ClipyWin.Services;

public sealed class PasteService
{
    private readonly ClipyDb _db;
    private readonly Settings _settings;
    private readonly ClipService _clipService;
    private readonly object _lock = new();

    public PasteService(ClipyDb db, Settings settings, ClipService clipService)
    {
        _db = db;
        _settings = settings;
        _clipService = clipService;
    }

    public void PasteClip(ClipEntry entry) => PasteInternal(entry, plainText: false);

    public void PasteClipPlain(ClipEntry entry) => PasteInternal(entry, plainText: true);

    private void PasteInternal(ClipEntry entry, bool plainText)
    {
        var data = ClipData.ReadFromFile(entry.DataPath);
        if (data == null) return;

        if (plainText)
        {
            data = new ClipData
            {
                StringValue = data.StringValue,
                Types = { Models.ClipDataType.Text }
            };
        }

        CopyToClipboard(data);

        if (_settings.GetBool(Constants.Settings.InputPasteCommand, true))
        {
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(InputSimulator.SendCtrlV);
            });
        }
    }

    public void CopyToClipboard(ClipData data)
    {
        lock (_lock)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var obj = new DataObject();
                if (data.ImagePng is { Length: > 0 })
                {
                    using var ms = new System.IO.MemoryStream(data.ImagePng);
                    var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(ms,
                        System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count > 0)
                        obj.SetImage(decoder.Frames[0]);
                }
                if (data.FileNames.Count > 0)
                {
                    var collection = new System.Collections.Specialized.StringCollection();
                    foreach (var f in data.FileNames) collection.Add(f);
                    obj.SetFileDropList(collection);
                }
                if (!string.IsNullOrEmpty(data.RtfValue))
                    obj.SetText(data.RtfValue, TextDataFormat.Rtf);
                if (!string.IsNullOrEmpty(data.HtmlValue))
                    obj.SetText(data.HtmlValue, TextDataFormat.Html);
                if (!string.IsNullOrEmpty(data.StringValue))
                    obj.SetText(data.StringValue, TextDataFormat.UnicodeText);

                Clipboard.SetDataObject(obj, copy: true);
            });
        }
    }

    public void CopyStringToClipboard(string s)
    {
        Application.Current?.Dispatcher.Invoke(() => Clipboard.SetText(s));
    }

    public void PasteString(string content)
    {
        if (content == null) return;

        Application.Current?.Dispatcher.Invoke(() => Clipboard.SetDataObject(content, copy: true));

        if (_settings.GetBool(Constants.Settings.InputPasteCommand, true))
        {
            System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
            {
                Application.Current?.Dispatcher.Invoke(InputSimulator.SendCtrlV);
            });
        }
    }
}
