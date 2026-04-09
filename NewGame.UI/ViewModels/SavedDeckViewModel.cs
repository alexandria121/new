using MagicalDeckbuilder.Storage;
using MagicalDeckbuilder.Game;
using Card = MagicalDeckbuilder.Cards.Card;

namespace NewGame.UI.ViewModels;

/// <summary>
/// ViewModel wrapper for a saved deck, providing UI-friendly properties
/// </summary>
public class SavedDeckViewModel : ViewModelBase
{
    private readonly SavedDeck? _fullDeck;
    private readonly DeckIndexEntry? _indexEntry;
    
    public SavedDeckViewModel(SavedDeck deck)
    {
        _fullDeck = deck ?? throw new ArgumentNullException(nameof(deck));
        _indexEntry = null;
    }
    
    public SavedDeckViewModel(DeckIndexEntry entry)
    {
        _indexEntry = entry ?? throw new ArgumentNullException(nameof(entry));
        _fullDeck = null;
    }
    
    public SavedDeck? Deck => _fullDeck;
    
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id => _fullDeck?.Id ?? _indexEntry?.Id ?? "";
    
    /// <summary>
    /// Deck name
    /// </summary>
    public string Name => _fullDeck?.Name ?? _indexEntry?.Name ?? "Unknown";
    
    /// <summary>
    /// Optional description
    /// </summary>
    public string Description => _fullDeck?.Description ?? "";
    
    /// <summary>
    /// Number of cards in deck
    /// </summary>
    public int CardCount => _fullDeck?.CardCount ?? _indexEntry?.CardCount ?? 0;
    
    /// <summary>
    /// Card count text for display
    /// </summary>
    public string CardCountText => $"{CardCount} cards";
    
    /// <summary>
    /// Deck type (Player or Opponent)
    /// </summary>
    public DeckType Type => _fullDeck?.Type ?? _indexEntry?.Type ?? DeckType.Player;
    
    /// <summary>
    /// Difficulty level
    /// </summary>
    public DifficultyLevel Difficulty => _fullDeck?.Difficulty ?? _indexEntry?.Difficulty ?? DifficultyLevel.Journeyman;
    
    /// <summary>
    /// When the deck was created
    /// </summary>
    public DateTime CreatedAt => _fullDeck?.CreatedAt ?? DateTime.UtcNow;
    
    /// <summary>
    /// When the deck was last modified
    /// </summary>
    public DateTime ModifiedAt => _fullDeck?.ModifiedAt ?? _indexEntry?.ModifiedAt ?? DateTime.UtcNow;
    
    /// <summary>
    /// Formatted modified date for display
    /// </summary>
    public string ModifiedDateText => ModifiedAt.ToLocalTime().ToString("MMM d, yyyy");
    
    /// <summary>
    /// Is this a player deck
    /// </summary>
    public bool IsPlayerDeck => Type == DeckType.Player;
    
    /// <summary>
    /// Is this an opponent deck
    /// </summary>
    public bool IsOpponentDeck => Type == DeckType.Opponent;
    
    /// <summary>
    /// Get the difficulty color for display
    /// </summary>
    public string DifficultyColor => Difficulty switch
    {
        DifficultyLevel.Apprentice => "#4CAF50",
        DifficultyLevel.Journeyman => "#2196F3",
        DifficultyLevel.Expert => "#FF9800",
        DifficultyLevel.Grandmaster => "#9C27B0",
        _ => "#808080"
    };
    
    /// <summary>
    /// Get the difficulty name for display
    /// </summary>
    public string DifficultyText => Difficulty.ToString();
    
    /// <summary>
    /// Get the deck type display text
    /// </summary>
    public string TypeDisplayText => Type switch
    {
        DeckType.Player => "Your Deck",
        DeckType.Opponent => $"Opponent ({DifficultyText})",
        DeckType.Preset => "Preset Deck",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Get element distribution summary
    /// </summary>
    public string ElementSummary
    {
        get
        {
            // Element summary requires full deck data, not available from index entry
            if (_fullDeck == null) return "Load deck for details";
            
            var elementCounts = new Dictionary<string, int>();
            foreach (var card in _fullDeck.Cards)
            {
                // Extract element from template ID (format: Name_Type_Element)
                var parts = card.CardTemplateId.Split('_');
                if (parts.Length >= 3)
                {
                    var element = parts[^1]; // Last part is element
                    if (!elementCounts.ContainsKey(element))
                        elementCounts[element] = 0;
                    elementCounts[element]++;
                }
            }
            
            if (elementCounts.Count == 0) return "No elements";
            
            return string.Join(", ", elementCounts
                .OrderByDescending(e => e.Value)
                .Take(3)
                .Select(e => $"{GetElementIcon(e.Key)}{e.Value}"));
        }
    }
    
    private static string GetElementIcon(string element) => element switch
    {
        "Radioactivity" => "☢",
        "Flesh" => "💀",
        "Toxin" => "☠",
        "Fungus" => "🍄",
        "Thermodynamics" => "🔥",
        "Time" => "⏳",
        "Food" => "🍖",
        "Eldritch" => "👁",
        _ => "❓"
    };

    /// <summary>
    /// Get card type distribution summary
    /// </summary>
    public string CardTypeSummary
    {
        get
        {
            if (_fullDeck == null) return "Load deck for details";

            var typeCounts = new Dictionary<string, int>();
            foreach (var card in _fullDeck.Cards)
            {
                var cardObj = CardFactory.GetCardByTemplateId(card.CardTemplateId);
                var typeName = cardObj?.Type.ToString() ?? "Unknown";
                if (!typeCounts.ContainsKey(typeName))
                    typeCounts[typeName] = 0;
                typeCounts[typeName]++;
            }

            if (typeCounts.Count == 0) return "No cards";

            return string.Join(", ", typeCounts
                .OrderByDescending(t => t.Value)
                .Take(3)
                .Select(t => $"{GetCardTypeIcon(t.Key)}{t.Value}"));
        }
    }

    private static string GetCardTypeIcon(string type) => type switch
    {
        "Creature" => "👤",
        "Spell" => "✨",
        "Artifact" => "🏺",
        "Weapon" => "⚔",
        "Armor" => "🛡",
        "Enchantment" => "🔮",
        "Event" => "⚡",
        _ => "🃏"
    };
    
    /// <summary>
    /// Can this deck be edited/deleted (player decks only)
    /// </summary>
    public bool CanEdit => Type == DeckType.Player;
    
    /// <summary>
    /// Create a deep copy of this view model
    /// </summary>
    public SavedDeckViewModel Clone()
    {
        return _fullDeck != null 
            ? new SavedDeckViewModel(_fullDeck) 
            : new SavedDeckViewModel(_indexEntry!);
    }
}
