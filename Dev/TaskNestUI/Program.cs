using Avalonia;
using System;

namespace TaskNestUI;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestStartup.log");
        try
        {
            System.IO.File.AppendAllText(log, "Main start: " + System.DateTime.Now + System.Environment.NewLine);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            System.IO.File.AppendAllText(log, "Main end: " + System.DateTime.Now + System.Environment.NewLine);
        }
        catch (System.Exception ex)
        {
            try { System.IO.File.AppendAllText(log, "Unhandled startup exception: " + ex + System.Environment.NewLine); } catch {}
            System.Console.Error.WriteLine("Unhandled startup exception: " + ex);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
