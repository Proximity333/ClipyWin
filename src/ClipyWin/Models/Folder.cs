using System;
using System.Collections.Generic;
using LiteDB;

namespace ClipyWin.Models;

public class Folder
{
    [BsonId]
    public string Identifier { get; set; } = Guid.NewGuid().ToString();
    public int Index { get; set; }
    public bool Enable { get; set; } = true;
    public string Title { get; set; } = string.Empty;
    public List<Snippet> Snippets { get; set; } = new();
}
