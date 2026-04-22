using System;
using LiteDB;

namespace ClipyWin.Models;

public class Snippet
{
    [BsonId]
    public string Identifier { get; set; } = Guid.NewGuid().ToString();
    public int Index { get; set; }
    public bool Enable { get; set; } = true;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
