using System.Windows.Forms;
using MagicalDeckbuilder.Assets;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Game;
using MagicalDeckbuilder.UI;
using Card = MagicalDeckbuilder.Cards.Card;

namespace MagicalDeckbuilder;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // First, generate placeholder images if they don't exist
        var baseDir = AppContext.BaseDirectory;

        // Save to bin directory for easier access from WinForms
        var placeholdersDir = Path.Combine(baseDir, "Assets", "Placeholders");

        if (!Directory.Exists(placeholdersDir) || !Directory.GetFiles(placeholdersDir, "*.png").Any())
        {
            Console.WriteLine("=== GENERATING PLACEHOLDER GRAPHICS ===\n");
            
            // Ensure directory exists
            Directory.CreateDirectory(placeholdersDir);
            
            PlaceholderGenerator.GenerateAllPlaceholders(placeholdersDir);
            
            var squaresDir = Path.Combine(placeholdersDir, "Squares");
            PlaceholderGenerator.GenerateSimpleSquares(squaresDir);
            Console.WriteLine();
            Console.WriteLine("=== PLACEHOLDER GENERATION COMPLETE ===\n");
        }

        // Generate some sample cards for display
        var newCards = CardFactory.GenerateNewCards();
        Console.WriteLine($"Generated {newCards.Count} sample cards\n");

        // Run the WinForms application - start with the menu
        ApplicationConfiguration.Initialize();
        Application.Run(new MenuForm());
    }
}
