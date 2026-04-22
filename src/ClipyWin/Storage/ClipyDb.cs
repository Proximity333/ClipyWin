using System;
using System.Collections.Generic;
using System.Linq;
using ClipyWin.Models;
using ClipyWin.Utilities;
using LiteDB;

namespace ClipyWin.Storage;

public sealed class ClipyDb : IDisposable
{
    private readonly LiteDatabase _db;

    public ClipyDb()
    {
        _db = new LiteDatabase($"Filename={AppPaths.DatabaseFile};Connection=shared");
        Clips.EnsureIndex(x => x.UpdateTime);
        Folders.EnsureIndex(x => x.Index);
    }

    public ILiteCollection<ClipEntry> Clips => _db.GetCollection<ClipEntry>("clips");
    public ILiteCollection<Folder> Folders => _db.GetCollection<Folder>("folders");

    public IEnumerable<ClipEntry> GetClips(bool newestFirst = true, int? take = null)
    {
        var q = Clips.Query();
        q = newestFirst ? q.OrderByDescending(c => c.UpdateTime) : q.OrderBy(c => c.UpdateTime);
        return take.HasValue ? q.Limit(take.Value).ToEnumerable() : q.ToEnumerable();
    }

    public ClipEntry? FindByHash(string hash) => Clips.FindById(hash);

    public void UpsertClip(ClipEntry entry) => Clips.Upsert(entry);

    public bool DeleteClip(string hash) => Clips.Delete(hash);

    public void ClearClips() => Clips.DeleteAll();

    public void Dispose() => _db.Dispose();
}
