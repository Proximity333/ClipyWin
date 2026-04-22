using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClipyWin.Models;

public enum ClipDataType
{
    Text,
    Rtf,
    Html,
    FileDrop,
    Image
}

public class ClipData
{
    public List<ClipDataType> Types { get; set; } = new();
    public string StringValue { get; set; } = string.Empty;
    public string? RtfValue { get; set; }
    public string? HtmlValue { get; set; }
    public List<string> FileNames { get; set; } = new();
    public byte[]? ImagePng { get; set; }

    public ClipDataType? PrimaryType => Types.FirstOrDefault();

    public bool IsOnlyStringType => Types.Count == 1 && Types[0] == ClipDataType.Text;

    public string ComputeHash()
    {
        using var sha = SHA256.Create();
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var t in Types) bw.Write((int)t);
            bw.Write(StringValue ?? string.Empty);
            bw.Write(RtfValue ?? string.Empty);
            bw.Write(HtmlValue ?? string.Empty);
            foreach (var f in FileNames) bw.Write(f);
            if (ImagePng is { Length: > 0 }) bw.Write(ImagePng);
        }
        ms.Position = 0;
        var bytes = sha.ComputeHash(ms);
        return Convert.ToHexString(bytes);
    }

    public void WriteToFile(string path)
    {
        var dto = new ClipDataDto
        {
            Types = Types.Select(t => (int)t).ToList(),
            StringValue = StringValue,
            RtfValue = RtfValue,
            HtmlValue = HtmlValue,
            FileNames = FileNames,
            ImagePng = ImagePng
        };
        using var fs = File.Create(path);
        JsonSerializer.Serialize(fs, dto);
    }

    public static ClipData? ReadFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        using var fs = File.OpenRead(path);
        var dto = JsonSerializer.Deserialize<ClipDataDto>(fs);
        if (dto == null) return null;
        return new ClipData
        {
            Types = dto.Types.Select(i => (ClipDataType)i).ToList(),
            StringValue = dto.StringValue ?? string.Empty,
            RtfValue = dto.RtfValue,
            HtmlValue = dto.HtmlValue,
            FileNames = dto.FileNames ?? new List<string>(),
            ImagePng = dto.ImagePng
        };
    }

    private class ClipDataDto
    {
        public List<int> Types { get; set; } = new();
        public string? StringValue { get; set; }
        public string? RtfValue { get; set; }
        public string? HtmlValue { get; set; }
        public List<string>? FileNames { get; set; }
        public byte[]? ImagePng { get; set; }
    }
}
