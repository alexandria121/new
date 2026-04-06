using System.Text.Json;
using System.Text.Json.Serialization;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Game;

namespace MagicalDeckbuilder.Storage;

/// <summary>
/// Handles serialization and deserialization of decks to/from JSON
/// </summary>
public static class DeckSerialization
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    
    /// <summary>
    /// Serialize a saved deck to JSON string
    /// </summary>
    public static string ToJson(SavedDeck deck)
    {
        return JsonSerializer.Serialize(deck, Options);
    }
    
    /// <summary>
    /// Deserialize a saved deck from JSON string
    /// </summary>
    public static SavedDeck? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<SavedDeck>(json, Options);
        }
        catch (JsonException ex)
        {
            MagicalDeckbuilder.Logging.ErrorLogger.Instance.Error(
                "DeckSerialization", 
                "[Operation: FromJson] Failed to deserialize deck", 
                ex);
            return null;
        }
    }
    
    /// <summary>
    /// Serialize a list of saved decks to JSON string
    /// </summary>
    public static string DecksToJson(IEnumerable<SavedDeck> decks)
    {
        return JsonSerializer.Serialize(decks.ToList(), Options);
    }
    
    /// <summary>
    /// Deserialize a list of saved decks from JSON string
    /// </summary>
    public static List<SavedDeck>? DecksFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<List<SavedDeck>>(json, Options);
        }
        catch (JsonException ex)
        {
            MagicalDeckbuilder.Logging.ErrorLogger.Instance.Error(
                "DeckSerialization", 
                "[Operation: DecksFromJson] Failed to deserialize decks", 
                ex);
            return null;
        }
    }
    
    /// <summary>
    /// Serialize the deck index to JSON string
    /// </summary>
    public static string IndexToJson(DeckIndex index)
    {
        return JsonSerializer.Serialize(index, Options);
    }
    
    /// <summary>
    /// Deserialize the deck index from JSON string
    /// </summary>
    public static DeckIndex? IndexFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
            
        try
        {
            return JsonSerializer.Deserialize<DeckIndex>(json, Options);
        }
        catch (JsonException ex)
        {
            MagicalDeckbuilder.Logging.ErrorLogger.Instance.Error(
                "DeckSerialization", 
                "[Operation: IndexFromJson] Failed to deserialize deck index", 
                ex);
            return null;
        }
    }
}

/// <summary>
/// Index file for quick deck lookup without loading all deck files
/// </summary>
public class DeckIndex
{
    public string Version { get; set; } = "1.0";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public List<DeckIndexEntry> Decks { get; set; } = new();
}

/// <summary>
/// Entry in the deck index
/// </summary>
public class DeckIndexEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DeckType Type { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public int CardCount { get; set; }
    public DateTime ModifiedAt { get; set; }
}
