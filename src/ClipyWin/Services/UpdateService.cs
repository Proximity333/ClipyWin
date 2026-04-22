using System;
using System.Threading.Tasks;
using ClipyWin.Utilities;
using Velopack;
using Velopack.Sources;

namespace ClipyWin.Services;

public sealed class UpdateService
{
    private readonly string? _updateUrl;
    private UpdateManager? _manager;

    public UpdateService(string? updateUrl)
    {
        _updateUrl = string.IsNullOrWhiteSpace(updateUrl) ? null : updateUrl;
    }

    public bool IsConfigured => _updateUrl != null;

    public string? CurrentVersion => _manager?.CurrentVersion?.ToString();

    private UpdateManager? GetManager()
    {
        if (_updateUrl == null) return null;
        if (_manager != null) return _manager;
        try
        {
            _manager = new UpdateManager(new SimpleWebSource(_updateUrl));
            return _manager;
        }
        catch (Exception ex)
        {
            Log.Error("UpdateManager.Create", ex);
            return null;
        }
    }

    public async Task<UpdateCheckResult> CheckAsync()
    {
        var mgr = GetManager();
        if (mgr == null || !mgr.IsInstalled)
            return new UpdateCheckResult(false, null, "Updates not available in this build.");

        try
        {
            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info == null)
                return new UpdateCheckResult(true, null, "You are up to date.");

            var version = info.TargetFullRelease?.Version?.ToString();
            return new UpdateCheckResult(true, version, $"Update available: {version}");
        }
        catch (Exception ex)
        {
            Log.Error("UpdateManager.CheckForUpdates", ex);
            return new UpdateCheckResult(true, null, $"Update check failed: {ex.Message}");
        }
    }

    public async Task<bool> DownloadAndApplyAsync()
    {
        var mgr = GetManager();
        if (mgr == null || !mgr.IsInstalled) return false;

        try
        {
            var info = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info == null) return false;

            await mgr.DownloadUpdatesAsync(info).ConfigureAwait(false);
            mgr.ApplyUpdatesAndRestart(info);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("UpdateManager.DownloadAndApply", ex);
            return false;
        }
    }
}

public readonly record struct UpdateCheckResult(bool Supported, string? NewVersion, string Message);
