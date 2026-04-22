using System;
using System.IO;
using System.Linq;
using System.Threading;
using ClipyWin.Storage;
using ClipyWin.Utilities;

namespace ClipyWin.Services;

public sealed class DataCleanService : IDisposable
{
    private readonly ClipyDb _db;
    private readonly Settings _settings;
    private Timer? _timer;

    public DataCleanService(ClipyDb db, Settings settings)
    {
        _db = db;
        _settings = settings;
    }

    public void StartMonitoring()
    {
        _timer = new Timer(_ => CleanOnce(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
    }

    public void CleanOnce()
    {
        try
        {
            var max = _settings.GetInt(Constants.Settings.MaxHistorySize, 30);
            var all = _db.GetClips(newestFirst: true).ToList();
            if (all.Count > max)
            {
                foreach (var overflow in all.Skip(max))
                {
                    if (!string.IsNullOrEmpty(overflow.DataPath) && File.Exists(overflow.DataPath))
                    {
                        try { File.Delete(overflow.DataPath); } catch { }
                    }
                    _db.DeleteClip(overflow.DataHash);
                }
            }

            // Orphan files in ClipsFolder
            var referenced = _db.GetClips()
                .Select(c => c.DataPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(Path.GetFileName)
                .Where(n => n != null)
                .Cast<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.EnumerateFiles(AppPaths.ClipsFolder))
            {
                if (!referenced.Contains(Path.GetFileName(file)))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch
        {
            // Best-effort cleanup — swallow transient IO/DB errors.
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
