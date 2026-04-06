using System.Text.Json.Serialization;
using MagicalDeckbuilder.Cards;
using MagicalDeckbuilder.Game;

namespace MagicalDeckbuilder.Storage;

/// <summary>
/// Represents a saved deck that can be persisted to disk
/// </summary>
public class SavedDeck
{
    /// <summary>
    /// Unique identifier for the deck
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User-defined name for the deck (max 20 characters, no spaces)
    /// </summary>
    public string Name { get; set; } = "NewDeck";
    
    /// <summary>
    /// Optional description or notes about the deck
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// When the deck was first created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the deck was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of cards in the deck
    /// </summary>
    public int CardCount { get; set; }
    
    /// <summary>
    /// List of cards in this deck (references to CardFactory templates)
    /// </summary>
    public List<SavedCard> Cards { get; set; } = new();
    
    /// <summary>
    /// Type of deck (Player-created or Opponent)
    /// </summary>
    public DeckType Type { get; set; } = DeckType.Player;
    
    /// <summary>
    /// Difficulty level for opponent decks
    /// </summary>
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Journeyman;
    
    /// <summary>
    /// Version for schema migration support
    /// </summary>
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// Represents a card reference in a saved deck
/// </summary>
public class SavedCard
{
    /// <summary>
    /// Reference ID matching a CardFactory template
    /// </summary>
    public string CardTemplateId { get; set; } = "";
    
    /// <summary>
    /// Number of copies of this card (1 for unique, 2+ for duplicates)
    /// </summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Type of deck
/// </summary>
public enum DeckType
{
    Player,
    Opponent,
    Preset
}
