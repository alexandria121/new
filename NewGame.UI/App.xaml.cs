using System.IO;
using System.Windows;
using System.Windows.Threading;
using MagicalDeckbuilder.Assets;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.Logging;

namespace NewGame.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Set up global exception handlers FIRST
        SetupExceptionHandlers();

        base.OnStartup(e);

        // Log application start
        ErrorLogger.Instance.Info("App", "Application starting...");
        
        // Generate placeholder metadata (actual rendering done in XAML)
        var baseDir = AppContext.BaseDirectory;
        var placeholdersDir = Path.Combine(baseDir, "Assets", "Placeholders");

        // Generate placeholder files if directory doesn't exist or is empty
        if (!Directory.Exists(placeholdersDir) || !Directory.GetFiles(placeholdersDir, "*.info").Any())
        {
            Console.WriteLine("=== GENERATING PLACEHOLDER METADATA ===\n");
            Directory.CreateDirectory(placeholdersDir);
            PlaceholderGenerator.GenerateAllPlaceholders(placeholdersDir);
            var squaresDir = Path.Combine(placeholdersDir, "Squares");
            Directory.CreateDirectory(squaresDir);
            PlaceholderGenerator.GenerateSimpleSquares(squaresDir);
            Console.WriteLine();
            Console.WriteLine("=== PLACEHOLDER GENERATION COMPLETE ===\n");
        }

        // Generate some sample cards for display
        var newCards = CardFactory.CreateStarterDeck();
        Console.WriteLine($"Generated {newCards.Count} sample cards\n");
        
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void SetupExceptionHandlers()
    {
        // Handle UI thread exceptions
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        
        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        
        // Handle task exceptions
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogger.Instance.Fatal("UIThread", "Unhandled UI exception", e.Exception);
        
        // Show error to user and get details
        var errorWindow = new Views.ErrorWindow(e.Exception);
        errorWindow.ShowDialog();
        
        e.Handled = true; // Prevent crash
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        ErrorLogger.Instance.Fatal("AppDomain", "Unhandled domain exception", exception);
        
        if (e.IsTerminating)
        {
            ErrorLogger.Instance.Fatal("AppDomain", "Application is terminating due to unhandled exception");
            if (exception != null)
            {
                try
                {
                    var errorWindow = new Views.ErrorWindow(exception);
                    errorWindow.ShowDialog();
                }
                catch
                {
                    Console.Error.WriteLine($"FATAL: {exception.GetType().Name}: {exception.Message}");
                    Console.Error.WriteLine(exception.StackTrace);
                }
                Environment.Exit(1);
            }
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ErrorLogger.Instance.Error("TaskScheduler", "Unobserved task exception", e.Exception);
        e.SetObserved(); // Prevent crash
    }
}
