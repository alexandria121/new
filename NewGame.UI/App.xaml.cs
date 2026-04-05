using System.IO;
using System.Windows;
using MagicalDeckbuilder.Assets;
using MagicalDeckbuilder.Game;

namespace NewGame.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Generate placeholder images if they don't exist
        var baseDir = AppContext.BaseDirectory;
        var placeholdersDir = Path.Combine(baseDir, "Assets", "Placeholders");

        if (!Directory.Exists(placeholdersDir) || !Directory.GetFiles(placeholdersDir, "*.png").Any())
        {
            Console.WriteLine("=== GENERATING PLACEHOLDER GRAPHICS ===\n");
            Directory.CreateDirectory(placeholdersDir);
            PlaceholderGenerator.GenerateAllPlaceholders(placeholdersDir);
            var squaresDir = Path.Combine(placeholdersDir, "Squares");
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
}
