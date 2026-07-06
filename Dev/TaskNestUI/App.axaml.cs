using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TaskNestUI;

public partial class App : Application
{
    public override void Initialize()
    {
        try
        {
            AvaloniaXamlLoader.Load(this);
        }
        catch (System.Exception ex)
        {
            try
            {
                var log = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TaskNestStartup.log");
                System.IO.File.AppendAllText(log, "AvaloniaXamlLoader.Load exception: " + ex + System.Environment.NewLine);
            }
            catch {}

            throw;
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow = new MainWindow();
            }
            catch (System.Exception ex)
            {
                System.Console.Error.WriteLine("Failed to create MainWindow: " + ex);
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}