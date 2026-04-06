using System.IO;
using MagicalDeckbuilder.Cards;

namespace MagicalDeckbuilder.Assets;

/// <summary>
/// Utility class to manage placeholder card images for testing
/// Note: Actual image generation requires WPF - use the UI project for rendering
/// This class provides placeholder metadata and file path management
/// </summary>
public static class PlaceholderGenerator
{
    // Standard card dimensions (playing card ratio)
    public const int CardWidth = 250;
    public const int CardHeight = 350;

    // Element color hex values for UI binding
    private static readonly Dictionary<ElementType, string> ElementColorHex = new()
    {
        { ElementType.Radioactivity, "#00FF64" },
        { ElementType.Flesh, "#C86464" },
        { ElementType.Toxin, "#96C800" },
        { ElementType.Fungus, "#649664" },
        { ElementType.Thermodynamics, "#FF6432" },
        { ElementType.Time, "#9664C8" },
        { ElementType.Food, "#C89664" },
        { ElementType.Eldritch, "#6400C8" }
    };

    // Card type color hex values for UI binding
    private static readonly Dictionary<CardType, string> TypeColorHex = new()
    {
        { CardType.Spell, "#5050B4" },
        { CardType.Creature, "#508C50" },
        { CardType.Artifact, "#B48C3C" },
        { CardType.Enchantment, "#A050A0" },
        { CardType.Weapon, "#A03C28" },
        { CardType.Armor, "#646464" },
        { CardType.Event, "#C8A050" },
        { CardType.Blank, "#C8C8C8" }
    };

    /// <summary>
    /// Gets the hex color for an element type (for XAML binding)
    /// </summary>
    public static string GetElementColor(ElementType element)
    {
        return ElementColorHex.GetValueOrDefault(element, "#808080");
    }

    /// <summary>
    /// Gets the hex color for a card type (for XAML binding)
    /// </summary>
    public static string GetTypeColor(CardType type)
    {
        return TypeColorHex.GetValueOrDefault(type, "#808080");
    }

    /// <summary>
    /// Generate placeholder metadata - actual rendering should be done in XAML/WPF
    /// Creates a simple text file with card info for debugging purposes
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

        foreach (var type in cardTypes)
        {
            foreach (var element in elements)
            {
                var name = $"{type}_{element}";
                var infoFile = Path.Combine(outputDirectory, $"{name}.info");
                File.WriteAllText(infoFile, $"Placeholder: {name}\nElement: {element}\nType: {type}");
                generated++;
            }
        }

        foreach (var element in elements)
        {
            var infoFile = Path.Combine(outputDirectory, $"creature_{element}.info");
            File.WriteAllText(infoFile, $"Placeholder: creature_{element}\nElement: {element}\nType: Creature");
            generated++;
        }

        foreach (var type in cardTypes)
        {
            var infoFile = Path.Combine(outputDirectory, $"{type}_radioactivity.info");
            File.WriteAllText(infoFile, $"Placeholder: {type}_radioactivity\nElement: Radioactivity\nType: {type}");
            generated++;
        }

        Console.WriteLine($"Generated {generated} placeholder info files in {outputDirectory}");
        Console.WriteLine("Note: For actual card images, use XAML-based rendering in the UI project.");
    }

    /// <summary>
    /// Generate simple square placeholder info
    /// </summary>
    public static void GenerateSimpleSquares(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        foreach (var element in Enum.GetValues<ElementType>())
        {
            var color = GetElementColor(element);
            var infoFile = Path.Combine(outputDirectory, $"square_{element}.info");
            File.WriteAllText(infoFile, $"Square: {element}\nColor: {color}");
        }

        foreach (var type in Enum.GetValues<CardType>())
        {
            var color = GetTypeColor(type);
            var infoFile = Path.Combine(outputDirectory, $"square_{type}.info");
            File.WriteAllText(infoFile, $"Square: {type}\nColor: {color}");
        }

        var rarityColors = new Dictionary<int, string>
        {
            { 1, "#B4B4B4" },
            { 2, "#3CA03C" },
            { 3, "#3264C8" },
            { 4, "#C89632" }
        };

        foreach (var rarity in rarityColors)
        {
            var infoFile = Path.Combine(outputDirectory, $"rarity_{rarity.Key}.info");
            File.WriteAllText(infoFile, $"Rarity: {rarity.Key}\nColor: {rarity.Value}");
        }

        Console.WriteLine($"Generated simple square placeholders in {outputDirectory}");
    }
}
