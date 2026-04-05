using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.Assets;

/// <summary>
/// Utility class to generate placeholder card images for testing
/// </summary>
public static class PlaceholderGenerator
{
    // Standard card dimensions (playing card ratio)
    public const int CardWidth = 250;
    public const int CardHeight = 350;

    // Element colors
    private static readonly Dictionary<ElementType, Color> ElementColors = new()
    {
        { ElementType.Radioactivity, Color.FromArgb(0, 255, 100) },    // Radioactive green glow
        { ElementType.Flesh, Color.FromArgb(200, 100, 100) },          // Fleshy red
        { ElementType.Toxin, Color.FromArgb(150, 200, 0) },            // Toxic yellow-green
        { ElementType.Fungus, Color.FromArgb(100, 150, 100) },         // Mushroom green
        { ElementType.Thermodynamics, Color.FromArgb(255, 100, 50) }, // Heat orange-red
        { ElementType.Time, Color.FromArgb(150, 100, 200) },           // Time purple
        { ElementType.Food, Color.FromArgb(200, 150, 100) },          // Food brown
        { ElementType.Eldritch, Color.FromArgb(100, 0, 150) }        // Eldritch purple-black
    };

    // Card type colors
    private static readonly Dictionary<CardType, Color> TypeColors = new()
    {
        { CardType.Spell, Color.FromArgb(80, 80, 180) },
        { CardType.Creature, Color.FromArgb(80, 140, 80) },
        { CardType.Artifact, Color.FromArgb(180, 140, 60) },
        { CardType.Enchantment, Color.FromArgb(160, 80, 160) },
        { CardType.Weapon, Color.FromArgb(160, 60, 60) },
        { CardType.Armor, Color.FromArgb(100, 100, 100) },
        { CardType.Event, Color.FromArgb(200, 160, 80) },
        { CardType.Blank, Color.FromArgb(200, 200, 200) }
    };

    /// <summary>
    /// Generate a placeholder card image for a specific element and type
    /// </summary>
    public static Bitmap GenerateCardPlaceholder(ElementType element, CardType type, string name)
    {
        var bitmap = new Bitmap(CardWidth, CardHeight);
        using var g = Graphics.FromImage(bitmap);

        // Background - card type color
        var bgColor = TypeColors.GetValueOrDefault(type, Color.Gray);
        g.Clear(bgColor);

        // Draw border with element color
        var elementColor = ElementColors.GetValueOrDefault(element, Color.Black);
        using var borderPen = new Pen(elementColor, 8);
        g.DrawRectangle(borderPen, 4, 4, CardWidth - 8, CardHeight - 8);

        // Draw card name at top
        using var fontTitle = new Font("Arial", 14, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var titleRect = new RectangleF(10, 15, CardWidth - 20, 25);
        g.DrawString(name, fontTitle, brush, titleRect);

        // Draw type indicator
        using var fontType = new Font("Arial", 12);
        var typeRect = new RectangleF(10, 45, CardWidth - 20, 20);
        g.DrawString(type.ToString(), fontType, brush, typeRect);

        // Draw element indicator
        using var fontElement = new Font("Arial", 11);
        var elementRect = new RectangleF(10, CardHeight - 30, CardWidth - 20, 20);
        g.DrawString($"Element: {element}", fontElement, brush, elementRect);

        // Draw inner rectangle to simulate card art area
        var innerRect = new Rectangle(30, 80, CardWidth - 60, CardHeight - 140);
        using var innerBrush = new SolidBrush(Color.FromArgb(180, elementColor));
        g.FillRectangle(innerBrush, innerRect);

        // Add "Placeholder" text in center
        using var fontPlaceholder = new Font("Arial", 16, FontStyle.Italic);
        var placeholderRect = new RectangleF(30, CardHeight / 2 - 20, CardWidth - 60, 40);
        g.DrawString("PLACEHOLDER", fontPlaceholder, brush, placeholderRect);

        return bitmap;
    }

    /// <summary>
    /// Generate all placeholder images for testing
    /// </summary>
    public static void GenerateAllPlaceholders(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var cardTypes = Enum.GetValues<CardType>();
        var elements = Enum.GetValues<ElementType>();

        int generated = 0;

        // Generate one placeholder per card type
        foreach (var type in cardTypes)
        {
            foreach (var element in elements)
            {
                var name = $"{type}_{element}";
                using var bitmap = GenerateCardPlaceholder(element, type, name);
                var filename = Path.Combine(outputDirectory, $"{name}.png");
                bitmap.Save(filename, ImageFormat.Png);
                generated++;
            }
        }

        // Generate element-only placeholders (generic type)
        foreach (var element in elements)
        {
            using var bitmap = GenerateCardPlaceholder(element, CardType.Creature, $"{element} Creature");
            var filename = Path.Combine(outputDirectory, $"creature_{element}.png");
            bitmap.Save(filename, ImageFormat.Png);
            generated++;
        }

        // Generate type-only placeholders (generic element - Radioactivity)
        foreach (var type in cardTypes)
        {
            using var bitmap = GenerateCardPlaceholder(ElementType.Radioactivity, type, type.ToString());
            var filename = Path.Combine(outputDirectory, $"{type}_radioactivity.png");
            bitmap.Save(filename, ImageFormat.Png);
            generated++;
        }

        Console.WriteLine($"Generated {generated} placeholder images in {outputDirectory}");
    }

    /// <summary>
    /// Generate a simple colored square placeholder
    /// </summary>
    public static Bitmap GenerateSimpleSquare(Color color, int size = 100)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(color);
        
        // Add border
        using var pen = new Pen(Color.Black, 2);
        g.DrawRectangle(pen, 0, 0, size - 1, size - 1);
        
        return bitmap;
    }

    /// <summary>
    /// Generate multiple simple square placeholders for quick testing
    /// </summary>
    public static void GenerateSimpleSquares(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Generate colored squares for each element
        foreach (var element in Enum.GetValues<ElementType>())
        {
            var color = ElementColors.GetValueOrDefault(element, Color.Gray);
            using var bitmap = GenerateSimpleSquare(color);
            var filename = Path.Combine(outputDirectory, $"square_{element}.png");
            bitmap.Save(filename, ImageFormat.Png);
        }

        // Generate colored squares for each card type
        foreach (var type in Enum.GetValues<CardType>())
        {
            var color = TypeColors.GetValueOrDefault(type, Color.Gray);
            using var bitmap = GenerateSimpleSquare(color);
            var filename = Path.Combine(outputDirectory, $"square_{type}.png");
            bitmap.Save(filename, ImageFormat.Png);
        }

        // Generate rarity squares
        var rarityColors = new Dictionary<int, Color>
        {
            { 1, Color.FromArgb(180, 180, 180) },  // Common - Gray
            { 2, Color.FromArgb(60, 160, 60) },     // Uncommon - Green
            { 3, Color.FromArgb(50, 100, 200) },   // Rare - Blue
            { 4, Color.FromArgb(200, 150, 50) }    // Legendary - Gold
        };

        foreach (var rarity in rarityColors)
        {
            using var bitmap = GenerateSimpleSquare(rarity.Value);
            var filename = Path.Combine(outputDirectory, $"rarity_{rarity.Key}.png");
            bitmap.Save(filename, ImageFormat.Png);
        }

        Console.WriteLine($"Generated simple square placeholders in {outputDirectory}");
    }
}