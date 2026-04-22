using System;
using LiteDB;

namespace ClipyWin.Models;

public class ClipEntry
{
    [BsonId]
    public string DataHash { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PrimaryType { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public bool IsColorCode { get; set; }
    public long UpdateTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
