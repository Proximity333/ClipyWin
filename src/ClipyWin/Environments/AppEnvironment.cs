using System;
using ClipyWin.Managers;
using ClipyWin.Services;
using ClipyWin.Storage;

namespace ClipyWin.Environments;

public sealed class AppEnvironment : IDisposable
{
    public static AppEnvironment Current { get; private set; } = null!;

    public Settings Settings { get; }
    public ClipyDb Db { get; }
    public ClipService ClipService { get; }
    public PasteService PasteService { get; }
    public HotKeyService HotKeyService { get; }
    public DataCleanService DataCleanService { get; }
    public ExcludeAppService ExcludeAppService { get; }
    public MenuManager MenuManager { get; }
    public UpdateService UpdateService { get; }

    private AppEnvironment()
    {
        Settings = new Settings();
        Db = new ClipyDb();
        ExcludeAppService = new ExcludeAppService(Settings);
        ClipService = new ClipService(Db, Settings, ExcludeAppService);
        PasteService = new PasteService(Db, Settings, ClipService);
        DataCleanService = new DataCleanService(Db, Settings);
        HotKeyService = new HotKeyService();
        MenuManager = new MenuManager(Db, Settings, PasteService);
        UpdateService = new UpdateService(Settings.GetString(Constants.Settings.UpdateFeedUrl, ""));
    }

    public static AppEnvironment Initialize()
    {
        if (Current != null) return Current;
        Current = new AppEnvironment();
        return Current;
    }

    public void Dispose()
    {
        MenuManager.Dispose();
        HotKeyService.Dispose();
        DataCleanService.Dispose();
        ClipService.Dispose();
        Db.Dispose();
    }
}
