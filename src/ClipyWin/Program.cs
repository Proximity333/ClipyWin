using System;
using ClipyWin.Storage;
using ClipyWin.Utilities;
using Velopack;

namespace ClipyWin;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            VelopackApp.Build().Run();
        }
        catch (Exception ex)
        {
            Log.Error("VelopackApp.Run", ex);
        }

        try
        {
            var earlySettings = new Settings();
            Loc.Set(earlySettings.GetString(Constants.Settings.Culture, "en"));
        }
        catch (Exception ex)
        {
            Log.Error("Loc.Set early", ex);
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
